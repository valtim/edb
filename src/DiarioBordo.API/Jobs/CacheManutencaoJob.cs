using DiarioBordo.Domain.Repositories;
using Hangfire;
using StackExchange.Redis;
using System.Text.Json;

namespace DiarioBordo.API.Jobs;

/// <summary>
/// Jobs para manutenção do cache Redis
/// Garantia de disponibilidade dos últimos 30 dias (Resolução ANAC 457/2017 Art. 8º II)
/// </summary>
public interface ICacheManutencaoJob
{
    Task LimparCacheExpiradoAsync();
    Task PreaquecerCache30DiasAsync();
    Task VerificarIntegridadeCacheAsync();
    Task GerarRelatorioPerformanceCacheAsync();
}

public class CacheManutencaoJob : ICacheManutencaoJob
{
    private readonly IDatabase _redisDb;
    private readonly IRegistroVooRepository _registroVooRepository;
    private readonly IAeronaveRepository _aeronaveRepository;
    private readonly ILogger<CacheManutencaoJob> _logger;
    private readonly IConnectionMultiplexer _redis;

    // Chaves de cache utilizadas no sistema
    private const string CACHE_KEY_REGISTROS_30_DIAS = "registros:aeronave:{0}:30dias";
    private const string CACHE_KEY_PERFORMANCE = "performance:query:{0}";
    private const string CACHE_KEY_ESTATISTICAS = "estatisticas:aeronave:{0}";
    private const string CACHE_KEY_DASHBOARD = "dashboard:operador:{0}";

    public CacheManutencaoJob(
        IConnectionMultiplexer redis,
        IRegistroVooRepository registroVooRepository,
        IAeronaveRepository aeronaveRepository,
        ILogger<CacheManutencaoJob> logger)
    {
        _redis = redis;
        _redisDb = redis.GetDatabase();
        _registroVooRepository = registroVooRepository;
        _aeronaveRepository = aeronaveRepository;
        _logger = logger;
    }

    /// <summary>
    /// Limpar entradas de cache expiradas
    /// Executado diariamente às 02:00
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task LimparCacheExpiradoAsync()
    {
        _logger.LogInformation("Iniciando limpeza de cache expirado - {Timestamp}", DateTime.UtcNow);

        try
        {
            var servidor = _redis.GetServer(_redis.GetEndPoints().First());
            var chaves = servidor.Keys(pattern: "*", database: _redisDb.Database);

            var chavesRemovidas = 0;
            var totalChaves = 0;

            foreach (var key in chaves)
            {
                totalChaves++;
                var ttl = await _redisDb.KeyTimeToLiveAsync(key);

                // Se a chave expirou (TTL negativo) ou está sem TTL definido há mais de 7 dias
                if (ttl == TimeSpan.FromMilliseconds(-1) || (ttl == null && await ChaveAntiga(key)))
                {
                    await _redisDb.KeyDeleteAsync(key);
                    chavesRemovidas++;
                    _logger.LogDebug("Chave removida: {Chave}", key);
                }
            }

            // Limpeza específica de chaves de performance antigas (> 48h)
            await LimparCachePerformanceAntigoAsync();

            var memoriaLiberada = await CalcularMemoriaUtilizadaAsync();

            _logger.LogInformation("Limpeza de cache concluída - Chaves removidas: {Removidas}/{Total}, Memória: {Memoria}MB",
                chavesRemovidas, totalChaves, memoriaLiberada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante limpeza do cache");
            throw;
        }
    }

    /// <summary>
    /// Preaquecimento do cache para dados dos últimos 30 dias
    /// Executado diariamente às 05:00 (horário de baixo uso)
    /// </summary>
    [Queue("cache")]
    public async Task PreaquecerCache30DiasAsync()
    {
        _logger.LogInformation("Iniciando preaquecimento do cache dos últimos 30 dias");

        try
        {
            var aeronaves = await _aeronaveRepository.ObterAtivasAsync();
            var totalAeronaves = aeronaves.Count();
            var aeronavesProcesadas = 0;

            foreach (var aeronave in aeronaves)
            {
                await PreaquecerCacheAeronaveAsync(aeronave.Id);
                aeronavesProcesadas++;

                if (aeronavesProcesadas % 10 == 0)
                {
                    _logger.LogInformation("Progresso preaquecimento: {Processadas}/{Total} aeronaves",
                        aeronavesProcesadas, totalAeronaves);
                }
            }

            _logger.LogInformation("Preaquecimento concluído para {Total} aeronaves", totalAeronaves);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante preaquecimento do cache");
            throw;
        }
    }

    /// <summary>
    /// Verificar integridade dos dados em cache vs. banco
    /// Executado semanalmente aos domingos às 03:00
    /// </summary>
    [Queue("integrity")]
    public async Task VerificarIntegridadeCacheAsync()
    {
        _logger.LogInformation("Iniciando verificação de integridade do cache");

        try
        {
            var aeronaves = await _aeronaveRepository.ObterAtivasAsync();
            var inconsistencias = new List<InconsistenciaCache>();

            foreach (var aeronave in aeronaves.Take(50)) // Limitar a 50 por execução para não sobrecarregar
            {
                var inconsistencia = await VerificarIntegridadeAeronaveAsync(aeronave.Id);
                if (inconsistencia != null)
                {
                    inconsistencias.Add(inconsistencia);
                }
            }

            if (inconsistencias.Any())
            {
                _logger.LogWarning("Encontradas {Count} inconsistências no cache", inconsistencias.Count);
                await CorrigirInconsistenciasAsync(inconsistencias);
            }
            else
            {
                _logger.LogInformation("Verificação de integridade concluída - Cache consistente");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante verificação de integridade do cache");
            throw;
        }
    }

    /// <summary>
    /// Gerar relatório de performance do cache
    /// Executado diariamente às 23:30
    /// </summary>
    [Queue("reports")]
    public async Task GerarRelatorioPerformanceCacheAsync()
    {
        _logger.LogInformation("Gerando relatório de performance do cache");

        try
        {
            var info = await _redis.GetServer(_redis.GetEndPoints().First()).InfoAsync();
            var memoria = await CalcularMemoriaUtilizadaAsync();
            var estatisticas = await ColetarEstatisticasUsoAsync();

            var relatorio = new RelatorioPerformanceCache
            {
                DataRelatorio = DateTime.UtcNow,
                MemoriaUtilizadaMB = memoria,
                TotalChaves = estatisticas.TotalChaves,
                ChavesExpiradas = estatisticas.ChavesExpiradas,
                HitRate = estatisticas.HitRate,
                TempoRespostaMs = estatisticas.TempoRespostaMs,
                AeronavesMaisAcessadas = estatisticas.AeronavesMaisAcessadas
            };

            var relatorioJson = JsonSerializer.Serialize(relatorio, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Salvar relatório em cache para dashboard
            await _redisDb.StringSetAsync(
                $"relatorio:cache:{DateTime.Today:yyyy-MM-dd}",
                relatorioJson,
                TimeSpan.FromDays(7)
            );

            _logger.LogInformation("Relatório de performance gerado - Memória: {Memoria}MB, Hit Rate: {HitRate}%",
                memoria, relatorio.HitRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de performance do cache");
            throw;
        }
    }

    private async Task<bool> ChaveAntiga(RedisKey chave)
    {
        try
        {
            // Para chaves sem TTL, verificar se foram criadas há mais de 7 dias
            // Implementação simplificada - em produção usar metadata ou logs
            return false; // Placeholder - implementar lógica específica
        }
        catch
        {
            return false;
        }
    }

    private async Task LimparCachePerformanceAntigoAsync()
    {
        var servidor = _redis.GetServer(_redis.GetEndPoints().First());
        var chavesPerformance = servidor.Keys(pattern: "performance:*", database: _redisDb.Database);

        foreach (var chave in chavesPerformance)
        {
            var ttl = await _redisDb.KeyTimeToLiveAsync(chave);
            if (ttl.HasValue && ttl.Value.TotalHours > 48)
            {
                await _redisDb.KeyDeleteAsync(chave);
            }
        }
    }

    private async Task PreaquecerCacheAeronaveAsync(int aeronaveId)
    {
        try
        {
            // Cache dos registros dos últimos 30 dias (requisito ANAC)
            var chaveRegistros = string.Format(CACHE_KEY_REGISTROS_30_DIAS, aeronaveId);
            var existeCache = await _redisDb.KeyExistsAsync(chaveRegistros);

            if (!existeCache)
            {
                var registros = await _registroVooRepository.ObterUltimos30DiasAsync(aeronaveId);
                var registrosJson = JsonSerializer.Serialize(registros);

                await _redisDb.StringSetAsync(chaveRegistros, registrosJson, TimeSpan.FromHours(24));
                _logger.LogDebug("Cache preaquecido para aeronave {AeronaveId}", aeronaveId);
            }

            // Cache de estatísticas básicas
            var chaveEstatisticas = string.Format(CACHE_KEY_ESTATISTICAS, aeronaveId);
            if (!await _redisDb.KeyExistsAsync(chaveEstatisticas))
            {
                var estatisticas = await CalcularEstatisticasAeronaveAsync(aeronaveId);
                await _redisDb.StringSetAsync(chaveEstatisticas, estatisticas, TimeSpan.FromHours(6));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao preaqecer cache da aeronave {AeronaveId}", aeronaveId);
        }
    }

    private async Task<InconsistenciaCache?> VerificarIntegridadeAeronaveAsync(int aeronaveId)
    {
        try
        {
            var chaveCache = string.Format(CACHE_KEY_REGISTROS_30_DIAS, aeronaveId);
            var dadosCache = await _redisDb.StringGetAsync(chaveCache);

            if (!dadosCache.HasValue)
                return null; // Cache vazio não é inconsistência

            var registrosCache = JsonSerializer.Deserialize<List<dynamic>>(dadosCache!);
            var registrosBanco = await _registroVooRepository.ObterUltimos30DiasAsync(aeronaveId);

            if (registrosCache?.Count != registrosBanco.Count())
            {
                return new InconsistenciaCache
                {
                    AeronaveId = aeronaveId,
                    ChaveCache = chaveCache,
                    QuantidadeCache = registrosCache?.Count ?? 0,
                    QuantidadeBanco = registrosBanco.Count(),
                    TipoInconsistencia = "ContadorDiferente"
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao verificar integridade da aeronave {AeronaveId}", aeronaveId);
            return null;
        }
    }

    private async Task CorrigirInconsistenciasAsync(List<InconsistenciaCache> inconsistencias)
    {
        foreach (var inconsistencia in inconsistencias)
        {
            _logger.LogInformation("Corrigindo inconsistência: Aeronave {AeronaveId}, Cache: {Cache}, Banco: {Banco}",
                inconsistencia.AeronaveId, inconsistencia.QuantidadeCache, inconsistencia.QuantidadeBanco);

            // Remover cache inconsistente para forçar atualização
            await _redisDb.KeyDeleteAsync(inconsistencia.ChaveCache);

            // Preaqecer novamente com dados corretos
            await PreaquecerCacheAeronaveAsync(inconsistencia.AeronaveId);
        }
    }

    private async Task<double> CalcularMemoriaUtilizadaAsync()
    {
        try
        {
            var servidor = _redis.GetServer(_redis.GetEndPoints().First());
            var info = await servidor.InfoAsync();

            // Procurar por used_memory nas informações do servidor
            var memoryInfo = info.FirstOrDefault(section =>
                section.Any(kv => kv.Key == "used_memory"));

            if (memoryInfo != null)
            {
                var memoriaBytes = long.Parse(memoryInfo.First(x => x.Key == "used_memory").Value);
                return Math.Round(memoriaBytes / (1024.0 * 1024.0), 2); // MB
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<EstatisticasUsoCache> ColetarEstatisticasUsoAsync()
    {
        try
        {
            var servidor = _redis.GetServer(_redis.GetEndPoints().First());
            var info = await servidor.InfoAsync();

            // Coletar estatísticas básicas do Redis
            var totalChaves = await servidor.DatabaseSizeAsync();

            // Converter grupos de informações em dicionário
            var statsDict = new Dictionary<string, string>();
            foreach (var section in info)
            {
                foreach (var kvp in section)
                {
                    statsDict[kvp.Key] = kvp.Value;
                }
            }

            // Hit rate aproximado (baseado em estatísticas do Redis)
            var hits = long.Parse(statsDict.GetValueOrDefault("keyspace_hits", "0"));
            var misses = long.Parse(statsDict.GetValueOrDefault("keyspace_misses", "0"));
            var hitRate = hits + misses > 0 ? Math.Round((double)hits / (hits + misses) * 100, 2) : 0;

            return new EstatisticasUsoCache
            {
                TotalChaves = totalChaves,
                ChavesExpiradas = 0, // Placeholder
                HitRate = hitRate,
                TempoRespostaMs = 0.5, // Placeholder - medir em produção
                AeronavesMaisAcessadas = new List<string>() // Implementar se necessário
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao coletar estatísticas de uso do cache");
            return new EstatisticasUsoCache();
        }
    }

    private async Task<string> CalcularEstatisticasAeronaveAsync(int aeronaveId)
    {
        // Implementar cálculo de estatísticas básicas para dashboard
        var estatisticas = new
        {
            TotalVoos30Dias = (await _registroVooRepository.ObterUltimos30DiasAsync(aeronaveId)).Count(),
            UltimaAtualizacao = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(estatisticas);
    }

    public class InconsistenciaCache
    {
        public int AeronaveId { get; set; }
        public string ChaveCache { get; set; } = string.Empty;
        public int QuantidadeCache { get; set; }
        public int QuantidadeBanco { get; set; }
        public string TipoInconsistencia { get; set; } = string.Empty;
    }

    public class EstatisticasUsoCache
    {
        public long TotalChaves { get; set; }
        public long ChavesExpiradas { get; set; }
        public double HitRate { get; set; }
        public double TempoRespostaMs { get; set; }
        public List<string> AeronavesMaisAcessadas { get; set; } = new();
    }

    public class RelatorioPerformanceCache
    {
        public DateTime DataRelatorio { get; set; }
        public double MemoriaUtilizadaMB { get; set; }
        public long TotalChaves { get; set; }
        public long ChavesExpiradas { get; set; }
        public double HitRate { get; set; }
        public double TempoRespostaMs { get; set; }
        public List<string> AeronavesMaisAcessadas { get; set; } = new();
    }
}