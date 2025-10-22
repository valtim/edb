using DiarioBordo.Application.DTOs;
using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Enums;
using DiarioBordo.Domain.Repositories;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DiarioBordo.Application.Services;

/// <summary>
/// Serviço para gerenciamento de assinaturas digitais
/// Conformidade com Res. ANAC 458/2017 Art. 6º - Assinaturas Digitais
/// </summary>
public interface IAssinaturaService
{
    Task<AssinaturaRegistroDto> AssinarRegistroPilotoAsync(int registroId, string codigoANAC, string enderecoIP, string userAgent);
    Task<AssinaturaRegistroDto> AssinarRegistroOperadorAsync(int registroId, string codigoANAC, string enderecoIP, string userAgent);
    Task<bool> ValidarAssinaturaAsync(int assinaturaId);
    Task<IEnumerable<AssinaturaRegistroDto>> ObterAssinaturasRegistroAsync(int registroId);
    Task<IEnumerable<RegistroVooDto>> ObterRegistrosPendentesAssinaturaAsync(int operadorId, TipoRBAC tipoRBAC);
    Task NotificarPrazosVencimentoAsync();
}

public class AssinaturaService : IAssinaturaService
{
    private readonly IAssinaturaRegistroRepository _assinaturaRepository;
    private readonly IRegistroVooRepository _registroRepository;
    private readonly ITripulanteRepository _tripulanteRepository;
    private readonly IAeronaveRepository _aeronaveRepository;

    public AssinaturaService(
        IAssinaturaRegistroRepository assinaturaRepository,
        IRegistroVooRepository registroRepository,
        ITripulanteRepository tripulanteRepository,
        IAeronaveRepository aeronaveRepository)
    {
        _assinaturaRepository = assinaturaRepository;
        _registroRepository = registroRepository;
        _tripulanteRepository = tripulanteRepository;
        _aeronaveRepository = aeronaveRepository;
    }

    public async Task<AssinaturaRegistroDto> AssinarRegistroPilotoAsync(int registroId, string codigoANAC, string enderecoIP, string userAgent)
    {
        var registro = await ValidarRegistroParaAssinaturaAsync(registroId);
        var tripulante = await ValidarTripulanteAsync(codigoANAC);

        // Validar se é o piloto em comando do registro
        if (registro.PilotoComandoCodigo != codigoANAC)
        {
            throw new InvalidOperationException("Apenas o piloto em comando pode assinar este registro");
        }

        // Verificar se já foi assinado pelo piloto
        if (registro.StatusAssinaturaPiloto)
        {
            throw new InvalidOperationException("Registro já foi assinado pelo piloto");
        }

        // Gerar hash do registro completo
        var hashRegistro = GerarHashRegistro(registro);

        // Criar assinatura
        var assinatura = new AssinaturaRegistro
        {
            RegistroVooId = registroId,
            TipoAssinatura = TipoAssinatura.Piloto,
            CodigoANAC = codigoANAC,
            NomeAssinante = tripulante.Nome,
            HashRegistro = hashRegistro,
            DataAssinatura = DateTime.UtcNow,
            EnderecoIP = enderecoIP,
            UserAgent = userAgent,
            AssinaturaValidada = true, // Auto-validada para pilotos
            DataValidacao = DateTime.UtcNow,
            DataCriacao = DateTime.UtcNow
        };

        await _assinaturaRepository.AdicionarAsync(assinatura);

        // Atualizar status do registro
        registro.StatusAssinaturaPiloto = true;
        registro.DataAssinaturaPiloto = DateTime.UtcNow;
        await _registroRepository.AtualizarAsync(registro);

        return MapearEntidadeParaDto(assinatura);
    }

    public async Task<AssinaturaRegistroDto> AssinarRegistroOperadorAsync(int registroId, string codigoANAC, string enderecoIP, string userAgent)
    {
        var registro = await ValidarRegistroParaAssinaturaAsync(registroId);
        var tripulante = await ValidarTripulanteAsync(codigoANAC);

        // Verificar se o piloto já assinou
        if (!registro.StatusAssinaturaPiloto)
        {
            throw new InvalidOperationException("Registro deve ser assinado primeiro pelo piloto");
        }

        // Verificar se já foi assinado pelo operador
        if (registro.StatusAssinaturaOperador)
        {
            throw new InvalidOperationException("Registro já foi assinado pelo operador");
        }

        // Verificar se não excedeu o prazo baseado no tipo RBAC
        var aeronave = await _aeronaveRepository.ObterPorIdAsync(registro.AeronaveId);
        ValidarPrazoAssinatura(registro, aeronave!.TipoRBAC);

        // Gerar hash do registro completo
        var hashRegistro = GerarHashRegistro(registro);

        // Criar assinatura
        var assinatura = new AssinaturaRegistro
        {
            RegistroVooId = registroId,
            TipoAssinatura = TipoAssinatura.Operador,
            CodigoANAC = codigoANAC,
            NomeAssinante = tripulante.Nome,
            HashRegistro = hashRegistro,
            DataAssinatura = DateTime.UtcNow,
            EnderecoIP = enderecoIP,
            UserAgent = userAgent,
            AssinaturaValidada = true,
            DataValidacao = DateTime.UtcNow,
            DataCriacao = DateTime.UtcNow
        };

        await _assinaturaRepository.AdicionarAsync(assinatura);

        // Atualizar status do registro
        registro.StatusAssinaturaOperador = true;
        registro.DataAssinaturaOperador = DateTime.UtcNow;
        await _registroRepository.AtualizarAsync(registro);

        return MapearEntidadeParaDto(assinatura);
    }

    public async Task<bool> ValidarAssinaturaAsync(int assinaturaId)
    {
        var assinatura = await _assinaturaRepository.ObterPorIdAsync(assinaturaId);
        if (assinatura == null)
        {
            return false;
        }

        var registro = await _registroRepository.ObterPorIdAsync(assinatura.RegistroVooId);
        if (registro == null)
        {
            return false;
        }

        // Recalcular hash e comparar
        var hashAtual = GerarHashRegistro(registro);
        var hashValido = assinatura.HashRegistro == hashAtual;

        // Atualizar status de validação se necessário
        if (assinatura.AssinaturaValidada != hashValido)
        {
            assinatura.AssinaturaValidada = hashValido;
            assinatura.DataValidacao = DateTime.UtcNow;
            await _assinaturaRepository.AtualizarAsync(assinatura);
        }

        return hashValido;
    }

    public async Task<IEnumerable<AssinaturaRegistroDto>> ObterAssinaturasRegistroAsync(int registroId)
    {
        var assinaturas = await _assinaturaRepository.ObterPorRegistroAsync(registroId);
        return assinaturas.Select(MapearEntidadeParaDto);
    }

    public async Task<IEnumerable<RegistroVooDto>> ObterRegistrosPendentesAssinaturaAsync(int operadorId, TipoRBAC tipoRBAC)
    {
        var diasPrazo = CalcularDiasPrazoAssinatura(tipoRBAC);
        var registros = await _registroRepository.ObterPendentesAssinaturaOperadorComPrazoAsync(operadorId, diasPrazo);

        return registros.Select(r => new RegistroVooDto
        {
            Id = r.Id,
            NumeroSequencial = r.NumeroSequencial,
            AeronaveId = r.AeronaveId,
            Data = r.Data,
            LocalDecolagem = r.LocalDecolagem,
            LocalPouso = r.LocalPouso,
            StatusAssinaturaPiloto = r.StatusAssinaturaPiloto,
            StatusAssinaturaOperador = r.StatusAssinaturaOperador,
            DataAssinaturaPiloto = r.DataAssinaturaPiloto,
            DataCriacao = r.DataCriacao,
            // Calcular dias restantes para assinatura
            DiasRestantesAssinatura = CalcularDiasRestantes(r.Data, tipoRBAC)
        });
    }

    public async Task NotificarPrazosVencimentoAsync()
    {
        var operadorId = 1; // TODO: Obter operador do contexto
        var registrosPendentes = await _registroRepository.ObterRegistrosPendentesAssinaturaAsync(operadorId);

        foreach (var registro in registrosPendentes)
        {
            var aeronave = await _aeronaveRepository.ObterPorIdAsync(registro.AeronaveId);
            var diasVencidos = CalcularDiasVencimento(registro.Data, aeronave!.TipoRBAC);

            if (diasVencidos > 0)
            {
                // TODO: Implementar sistema de notificações (email, push, etc.)
                // Por agora, registrar log de auditoria
                Console.WriteLine($"ALERTA: Registro {registro.NumeroSequencial} da aeronave {aeronave.Matricula} " +
                                $"vencido há {diasVencidos} dias (RBAC {aeronave.TipoRBAC})");
            }
        }
    }

    private async Task<RegistroVoo> ValidarRegistroParaAssinaturaAsync(int registroId)
    {
        var registro = await _registroRepository.ObterPorIdAsync(registroId);
        if (registro == null)
        {
            throw new InvalidOperationException("Registro de voo não encontrado");
        }
        return registro;
    }

    private async Task<Tripulante> ValidarTripulanteAsync(string codigoANAC)
    {
        var tripulante = await _tripulanteRepository.ObterPorCodigoANACAsync(codigoANAC);
        if (tripulante == null)
        {
            throw new InvalidOperationException("Tripulante não encontrado");
        }

        if (!tripulante.Ativo)
        {
            throw new InvalidOperationException("Tripulante não está ativo");
        }

        // Verificar validade da licença se informada
        if (tripulante.ValidadeLicenca.HasValue && tripulante.ValidadeLicenca < DateTime.Today)
        {
            throw new InvalidOperationException("Licença do tripulante vencida");
        }

        return tripulante;
    }

    private static void ValidarPrazoAssinatura(RegistroVoo registro, TipoRBAC tipoRBAC)
    {
        var diasLimite = ObterDiasLimiteAssinatura(tipoRBAC);
        var dataLimite = registro.Data.AddDays(diasLimite);

        if (DateTime.Today > dataLimite)
        {
            throw new InvalidOperationException($"Prazo para assinatura expirou. Limite: {diasLimite} dias após o voo.");
        }
    }

    private static DateTime CalcularDataLimiteAssinatura(TipoRBAC tipoRBAC)
    {
        var diasLimite = ObterDiasLimiteAssinatura(tipoRBAC);
        return DateTime.Today.AddDays(-diasLimite);
    }

    private static int CalcularDiasPrazoAssinatura(TipoRBAC tipoRBAC)
    {
        return ObterDiasLimiteAssinatura(tipoRBAC);
    }

    private static int ObterDiasLimiteAssinatura(TipoRBAC tipoRBAC)
    {
        return tipoRBAC switch
        {
            TipoRBAC.RBAC121 => 2,   // Operações regulares
            TipoRBAC.RBAC135 => 15,  // Táxi aéreo
            _ => 30                  // Outros tipos (RBAC91, etc.)
        };
    }

    private static int CalcularDiasRestantes(DateTime dataVoo, TipoRBAC tipoRBAC)
    {
        var diasLimite = ObterDiasLimiteAssinatura(tipoRBAC);
        var dataLimite = dataVoo.AddDays(diasLimite);
        var diasRestantes = (dataLimite - DateTime.Today).Days;
        return Math.Max(0, diasRestantes);
    }

    private static int CalcularDiasVencimento(DateTime dataVoo, TipoRBAC tipoRBAC)
    {
        var diasLimite = ObterDiasLimiteAssinatura(tipoRBAC);
        var dataLimite = dataVoo.AddDays(diasLimite);
        var diasVencidos = (DateTime.Today - dataLimite).Days;
        return Math.Max(0, diasVencidos);
    }

    private static string GerarHashRegistro(RegistroVoo registro)
    {
        // Serializar registro para JSON ordenado para garantir hash consistente
        var dadosRegistro = new
        {
            registro.Id,
            registro.NumeroSequencial,
            registro.AeronaveId,
            registro.Data,
            registro.LocalDecolagem,
            registro.LocalPouso,
            registro.HorarioPartidaMotoresUTC,
            registro.HorarioDecolagemUTC,
            registro.HorarioPousoUTC,
            registro.HorarioCorteMotoresUTC,
            registro.TempoVooIFR,
            registro.CombustivelQuantidade,
            registro.CombustivelUnidade,
            registro.NaturezaVoo,
            registro.NaturezaVooOutro,
            registro.QuantidadePessoasAbordo,
            registro.CargaQuantidade,
            registro.CargaUnidade,
            registro.Ocorrencias,
            registro.DiscrepanciasTecnicas,
            registro.PessoaDetectouDiscrepancia,
            registro.AcoesCorretivas,
            registro.TipoUltimaManutencao,
            registro.TipoProximaManutencao,
            registro.HorasCelulaProximaManutencao,
            registro.ResponsavelAprovacaoRetorno,
            registro.PilotoComandoCodigo,
            registro.PilotoComandoFuncao,
            registro.PilotoComandoHorarioApresentacao
        };

        var json = JsonSerializer.Serialize(dadosRegistro, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static AssinaturaRegistroDto MapearEntidadeParaDto(AssinaturaRegistro assinatura)
    {
        return new AssinaturaRegistroDto
        {
            Id = assinatura.Id,
            RegistroVooId = assinatura.RegistroVooId,
            TipoAssinatura = assinatura.TipoAssinatura,
            CodigoANAC = assinatura.CodigoANAC,
            NomeAssinante = assinatura.NomeAssinante,
            HashRegistro = assinatura.HashRegistro,
            DataAssinatura = assinatura.DataAssinatura,
            EnderecoIP = assinatura.EnderecoIP ?? string.Empty,
            UserAgent = assinatura.UserAgent ?? string.Empty,
            AssinaturaValidada = assinatura.AssinaturaValidada,
            DataValidacao = assinatura.DataValidacao,
            DataCriacao = assinatura.DataCriacao
        };
    }
}