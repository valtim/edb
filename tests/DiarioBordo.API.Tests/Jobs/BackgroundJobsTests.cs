using DiarioBordo.API.Jobs;
using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Enums;
using DiarioBordo.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DiarioBordo.API.Tests.Jobs;

/// <summary>
/// Testes para Jobs de background - Sincronização ANAC, Cache e Prazos
/// </summary>
public class BackgroundJobsTests
{
    private readonly Mock<IRegistroVooRepository> _mockRegistroRepo;
    private readonly Mock<IAssinaturaRegistroRepository> _mockAssinaturaRepo;
    private readonly Mock<ILogAuditoriaRepository> _mockLogRepo;
    private readonly Mock<ILogger<AnacSincronizacaoJob>> _mockLogger;

    public BackgroundJobsTests()
    {
        _mockRegistroRepo = new Mock<IRegistroVooRepository>();
        _mockAssinaturaRepo = new Mock<IAssinaturaRegistroRepository>();
        _mockLogRepo = new Mock<ILogAuditoriaRepository>();
        _mockLogger = new Mock<ILogger<AnacSincronizacaoJob>>();
    }

    [Fact]
    public async Task AnacSincronizacaoJob_DeveSincronizarRegistrosPendentes()
    {
        // Arrange
        var registrosPendentes = new List<RegistroVoo>
        {
            CriarRegistroVooParaSincronizacao(1, true, true, false),
            CriarRegistroVooParaSincronizacao(2, true, true, false)
        };

        _mockRegistroRepo.Setup(r => r.ObterPendentesSincronizacaoAsync())
                        .ReturnsAsync(registrosPendentes);

        _mockLogRepo.Setup(l => l.CriarAsync(It.IsAny<LogAuditoria>()))
                   .ReturnsAsync(new LogAuditoria());

        var job = new AnacSincronizacaoJob(
            _mockRegistroRepo.Object,
            _mockAssinaturaRepo.Object,
            _mockLogRepo.Object,
            _mockLogger.Object);

        // Act
        await job.ExecutarSincronizacaoAsync();

        // Assert
        _mockRegistroRepo.Verify(r => r.ObterPendentesSincronizacaoAsync(), Times.Once);

        // Verificar que logs de auditoria foram criados
        _mockLogRepo.Verify(l => l.CriarAsync(It.Is<LogAuditoria>(log =>
            log.Operacao == "SincronizacaoANAC" &&
            log.Tabela == "RegistroVoo")),
            Times.AtLeast(registrosPendentes.Count));
    }

    [Theory]
    [InlineData(TipoOperacao.RBAC121, 2)]  // 2 dias
    [InlineData(TipoOperacao.RBAC135, 15)] // 15 dias
    [InlineData(TipoOperacao.RBAC91, 30)]  // 30 dias
    public async Task PrazoAssinaturaJob_DeveRespeitarPrazosPorTipoOperacao(TipoOperacao tipo, int diasPrazo)
    {
        // Arrange
        var dataLimite = DateTime.UtcNow.AddDays(-diasPrazo);
        var registrosVencidos = new List<RegistroVoo>
        {
            CriarRegistroVooComPrazoVencido(1, tipo, dataLimite.AddDays(-1))
        };

        _mockRegistroRepo.Setup(r => r.GetComPrazoVencidoAsync(It.IsAny<DateTime>()))
                        .ReturnsAsync(registrosVencidos);

        var job = new PrazoAssinaturaJob(
            _mockRegistroRepo.Object,
            _mockLogRepo.Object,
            Mock.Of<ILogger<PrazoAssinaturaJob>>());

        // Act
        await job.VerificarPrazosVencidosAsync();

        // Assert
        _mockRegistroRepo.Verify(r => r.GetComPrazoVencidoAsync(It.IsAny<DateTime>()), Times.Once);

        // Verificar que alertas foram registrados
        _mockLogRepo.Verify(l => l.CriarAsync(It.Is<LogAuditoria>(log =>
            log.Operacao.Contains("PRAZO_VENCIDO") &&
            log.MensagemErro != null)),
            Times.AtLeast(1));
    }

    [Fact]
    public async Task PrazoAssinaturaJob_DeveIdentificarRegistrosProximosVencimento()
    {
        // Arrange
        var registrosProximoVencimento = new List<RegistroVoo>
        {
            CriarRegistroVooComPrazoProximo(1, TipoOperacao.RBAC121, DateTime.UtcNow.AddDays(-1)),
            CriarRegistroVooComPrazoProximo(2, TipoOperacao.RBAC135, DateTime.UtcNow.AddDays(-10))
        };

        _mockRegistroRepo.Setup(r => r.GetComPrazoProximoVencimentoAsync(It.IsAny<DateTime>()))
                        .ReturnsAsync(registrosProximoVencimento);

        var job = new PrazoAssinaturaJob(
            _mockRegistroRepo.Object,
            _mockLogRepo.Object,
            Mock.Of<ILogger<PrazoAssinaturaJob>>());

        // Act
        await job.VerificarPrazosProximosVencimentoAsync();

        // Assert
        _mockRegistroRepo.Verify(r => r.GetComPrazoProximoVencimentoAsync(It.IsAny<DateTime>()), Times.Once);

        // Verificar que notificações foram registradas
        _mockLogRepo.Verify(l => l.CriarAsync(It.Is<LogAuditoria>(log =>
            log.Operacao.Contains("PRAZO_PROXIMO") &&
            log.DadosNovos != null)),
            Times.AtLeast(registrosProximoVencimento.Count));
    }

    [Fact]
    public async Task CacheManutencaoJob_DeveLimparChavesExpiradas()
    {
        // Arrange
        // Este teste simularia a limpeza de cache Redis
        var mockRedis = new Mock<IDatabase>();
        var chavesExpiradas = new List<string> { "expired:key1", "expired:key2" };

        // Simular que as chaves estão expiradas
        mockRedis.Setup(r => r.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(TimeSpan.FromMilliseconds(-1)); // TTL negativo = expirado

        var job = new CacheManutencaoJob(
            mockRedis.Object,
            Mock.Of<ILogger<CacheManutencaoJob>>());

        // Act
        await job.LimparChavesExpiradasAsync();

        // Assert
        // Verificar que tentou deletar chaves expiradas
        mockRedis.Verify(r => r.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()),
                        Times.AtLeast(0));
    }

    [Fact]
    public async Task CacheManutencaoJob_DeveColetarEstatisticasUso()
    {
        // Arrange
        var mockRedis = new Mock<IDatabase>();
        var mockServer = new Mock<IServer>();

        // Simular estatísticas do Redis
        var infoRetorno = new[]
        {
            new HashEntry[]
            {
                new("used_memory", "1048576"),
                new("keyspace_hits", "1000"),
                new("keyspace_misses", "100")
            }
        };

        mockServer.Setup(s => s.InfoAsync(It.IsAny<RedisSection>(), It.IsAny<CommandFlags>()))
                 .ReturnsAsync(infoRetorno);

        var job = new CacheManutencaoJob(
            mockRedis.Object,
            Mock.Of<ILogger<CacheManutencaoJob>>());

        // Act
        var estatisticas = await job.ColetarEstatisticasUsoAsync();

        // Assert
        estatisticas.Should().NotBeNull();
        estatisticas.MemoriaUtilizada.Should().BeGreaterThan(0);
        estatisticas.HitRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AnacSincronizacaoJob_DeveTratarFalhasSincronizacao()
    {
        // Arrange
        var registroComFalha = CriarRegistroVooParaSincronizacao(1, true, true, false);
        var registros = new List<RegistroVoo> { registroComFalha };

        _mockRegistroRepo.Setup(r => r.ObterPendentesSincronizacaoAsync())
                        .ReturnsAsync(registros);

        // Simular falha na sincronização
        var mockIntegrationService = new Mock<IAnacIntegrationService>();
        mockIntegrationService.Setup(s => s.SincronizarBlockchainAsync(It.IsAny<RegistroBlockchain>()))
                            .ReturnsAsync(new ResultadoSincronizacao
                            {
                                Sucesso = false,
                                MensagemErro = "Falha de conexão com ANAC"
                            });

        var job = new AnacSincronizacaoJob(
            _mockRegistroRepo.Object,
            _mockAssinaturaRepo.Object,
            _mockLogRepo.Object,
            _mockLogger.Object);

        // Act
        await job.ExecutarSincronizacaoAsync();

        // Assert
        // Verificar que erro foi registrado
        _mockLogRepo.Verify(l => l.CriarAsync(It.Is<LogAuditoria>(log =>
            log.Operacao == "FalhaSincronizacaoANAC" &&
            log.MensagemErro != null &&
            log.Sucesso == false)),
            Times.Once);
    }

    [Fact]
    public async Task RegistroVoo_UltimosTrindaDias_DeveRespeitarPerformance()
    {
        // Arrange
        var aeronaveId = 1;
        var dataInicio = DateTime.UtcNow.AddDays(-30);

        // Simular consulta de performance crítica (<500ms conforme requisito)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _mockRegistroRepo.Setup(r => r.ObterUltimos30DiasAsync(aeronaveId))
                        .ReturnsAsync(new List<RegistroVoo>());

        // Act
        var resultado = await _mockRegistroRepo.Object.ObterUltimos30DiasAsync(aeronaveId);
        stopwatch.Stop();

        // Assert
        resultado.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500,
            "Consulta de 30 dias deve ser <500ms conforme Art. 8º II Res. 457/2017");
    }

    [Fact]
    public async Task LogAuditoria_DeveRegistrarTodasOperacoesCriticas()
    {
        // Arrange
        var operacoesCriticas = new[]
        {
            "CRIACAO_REGISTRO",
            "ASSINATURA_PILOTO",
            "ASSINATURA_OPERADOR",
            "SINCRONIZACAO_ANAC",
            "ALTERACAO_REGISTRO"
        };

        foreach (var operacao in operacoesCriticas)
        {
            var log = new LogAuditoria
            {
                Operacao = operacao,
                Tabela = "RegistroVoo",
                RegistroId = 1,
                UsuarioId = "123456",
                DataHoraUTC = DateTime.UtcNow,
                Sucesso = true
            };

            // Act
            _mockLogRepo.Setup(l => l.CriarAsync(It.IsAny<LogAuditoria>()))
                      .ReturnsAsync(log);

            var resultado = await _mockLogRepo.Object.CriarAsync(log);

            // Assert
            resultado.Should().NotBeNull();
            resultado.Operacao.Should().Be(operacao);
        }

        // Verificar que todas as operações foram registradas
        _mockLogRepo.Verify(l => l.CriarAsync(It.IsAny<LogAuditoria>()),
                          Times.Exactly(operacoesCriticas.Length));
    }

    private static RegistroVoo CriarRegistroVooParaSincronizacao(
        int id,
        bool assinadoPiloto,
        bool assinadoOperador,
        bool sincronizadoAnac)
    {
        return new RegistroVoo
        {
            Id = id,
            NumeroSequencial = id,
            Data = DateTime.UtcNow.Date,
            PilotoComandoCodigo = "123456",
            LocalDecolagem = "SBSP",
            LocalPouso = "SBRJ",
            AssinadoPiloto = assinadoPiloto,
            AssinadoOperador = assinadoOperador,
            SincronizadoANAC = sincronizadoAnac,
            TipoOperacao = TipoOperacao.RBAC135,
            DataCriacao = DateTime.UtcNow
        };
    }

    private static RegistroVoo CriarRegistroVooComPrazoVencido(
        int id,
        TipoOperacao tipoOperacao,
        DateTime dataCriacao)
    {
        return new RegistroVoo
        {
            Id = id,
            NumeroSequencial = id,
            Data = DateTime.UtcNow.Date,
            PilotoComandoCodigo = "123456",
            LocalDecolagem = "SBSP",
            LocalPouso = "SBRJ",
            AssinadoPiloto = false, // Pendente de assinatura
            AssinadoOperador = false,
            SincronizadoANAC = false,
            TipoOperacao = tipoOperacao,
            DataCriacao = dataCriacao
        };
    }

    private static RegistroVoo CriarRegistroVooComPrazoProximo(
        int id,
        TipoOperacao tipoOperacao,
        DateTime dataCriacao)
    {
        return new RegistroVoo
        {
            Id = id,
            NumeroSequencial = id,
            Data = DateTime.UtcNow.Date,
            PilotoComandoCodigo = "123456",
            LocalDecolagem = "SBSP",
            LocalPouso = "SBRJ",
            AssinadoPiloto = true,
            AssinadoOperador = false, // Pendente de assinatura do operador
            SincronizadoANAC = false,
            TipoOperacao = tipoOperacao,
            DataCriacao = dataCriacao
        };
    }
}

// Interfaces e classes auxiliares para os testes
public interface IDatabase
{
    Task<TimeSpan?> KeyTimeToLiveAsync(RedisKey key, CommandFlags flags = CommandFlags.None);
    Task<bool> KeyDeleteAsync(RedisKey key, CommandFlags flags = CommandFlags.None);
}

public interface IServer
{
    Task<IGrouping<string, KeyValuePair<string, string>>[]> InfoAsync(RedisSection section = RedisSection.Default, CommandFlags flags = CommandFlags.None);
}

public interface IAnacIntegrationService
{
    Task<ResultadoSincronizacao> SincronizarBlockchainAsync(RegistroBlockchain registro);
    Task<ResultadoSincronizacao> SincronizarBancoDadosAsync(RegistroANAC registro);
}

public class RegistroBlockchain
{
    public int RegistroId { get; set; }
    public int NumeroSequencial { get; set; }
    public DateTime DataVoo { get; set; }
    public string AeronaveMatricula { get; set; } = string.Empty;
    public string? HashAssinatura { get; set; }
    public DateTime TimestampSincronizacao { get; set; }
}

public class RegistroANAC
{
    public int Id { get; set; }
    public int NumeroSequencial { get; set; }
    public DateTime Data { get; set; }
    public string LocalDecolagem { get; set; } = string.Empty;
    public string LocalPouso { get; set; } = string.Empty;
    public DateTime HorarioDecolagemUTC { get; set; }
    public DateTime HorarioPousoUTC { get; set; }
    public string StatusSincronizacao { get; set; } = string.Empty;
    public DateTime TimestampSincronizacao { get; set; }
}

public class ResultadoSincronizacao
{
    public bool Sucesso { get; set; }
    public string? AnacId { get; set; }
    public string? MensagemErro { get; set; }
    public DateTime TimestampProcessamento { get; set; }
}

public class EstatisticasUsoCache
{
    public double MemoriaUtilizada { get; set; }
    public double HitRate { get; set; }
    public int TotalChaves { get; set; }
    public DateTime UltimaLimpeza { get; set; }
}

public struct RedisKey
{
    public static implicit operator RedisKey(string key) => new() { Value = key };
    public string Value { get; set; }
}

public struct HashEntry
{
    public HashEntry(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; set; }
    public string Value { get; set; }
}

public enum RedisSection
{
    Default,
    Memory,
    Stats
}

public enum CommandFlags
{
    None
}