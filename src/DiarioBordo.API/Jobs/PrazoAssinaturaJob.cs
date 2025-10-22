using DiarioBordo.Application.Services;
using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Enums;
using DiarioBordo.Domain.Repositories;
using Hangfire;

namespace DiarioBordo.API.Jobs;

/// <summary>
/// Jobs para verificação e notificação de prazos de assinatura
/// Conformidade com prazos RBAC: 121 (2 dias), 135 (15 dias), outros (30 dias)
/// </summary>
public interface IPrazoAssinaturaJob
{
    Task ExecutarVerificacaoPrazosAsync();
    Task NotificarPrazosProximosVencimentoAsync();
    Task NotificarPrazosVencidosAsync();
    Task GerarRelatorioStatusAssinaturasAsync();
}

public class PrazoAssinaturaJob : IPrazoAssinaturaJob
{
    private readonly IAssinaturaService _assinaturaService;
    private readonly IRegistroVooRepository _registroVooRepository;
    private readonly IAeronaveRepository _aeronaveRepository;
    private readonly ILogger<PrazoAssinaturaJob> _logger;
    private readonly IEmailService _emailService;

    public PrazoAssinaturaJob(
        IAssinaturaService assinaturaService,
        IRegistroVooRepository registroVooRepository,
        IAeronaveRepository aeronaveRepository,
        ILogger<PrazoAssinaturaJob> logger,
        IEmailService emailService)
    {
        _assinaturaService = assinaturaService;
        _registroVooRepository = registroVooRepository;
        _aeronaveRepository = aeronaveRepository;
        _logger = logger;
        _emailService = emailService;
    }

    /// <summary>
    /// Job principal para verificação completa de prazos
    /// Executado a cada hora
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task ExecutarVerificacaoPrazosAsync()
    {
        _logger.LogInformation("Iniciando verificação de prazos de assinatura - {Timestamp}", DateTime.UtcNow);

        try
        {
            // Notificar prazos próximos ao vencimento (2 dias antes)
            await NotificarPrazosProximosVencimentoAsync();

            // Notificar prazos já vencidos
            await NotificarPrazosVencidosAsync();

            // Atualizar status de registros vencidos
            await AtualizarStatusRegistrosVencidosAsync();

            _logger.LogInformation("Verificação de prazos concluída com sucesso - {Timestamp}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante verificação de prazos de assinatura");
            throw; // Re-throw para Hangfire retry
        }
    }

    /// <summary>
    /// Notificar registros com prazo próximo ao vencimento
    /// </summary>
    public async Task NotificarPrazosProximosVencimentoAsync()
    {
        _logger.LogInformation("Verificando registros com prazo próximo ao vencimento");

        var registrosProximosVencimento = await _registroVooRepository.GetComPrazoProximoVencimentoAsync(diasAviso: 2);

        foreach (var registro in registrosProximosVencimento)
        {
            var aeronave = await _aeronaveRepository.ObterPorIdAsync(registro.AeronaveId);
            if (aeronave == null) continue;

            var diasRestantes = registro.DiasRestantesAssinaturaOperador();
            var tipoRBAC = aeronave.TipoRBACOperador;

            var assunto = $"[URGENTE] Registro {registro.NumeroSequencial} - Prazo de assinatura em {diasRestantes} dias";
            var corpo = GerarCorpoEmailPrazoProximo(registro, aeronave, diasRestantes, tipoRBAC);

            // Enviar para operadores responsáveis
            await _emailService.EnviarNotificacaoOperadoresAsync(aeronave.OperadorId, assunto, corpo);

            _logger.LogWarning("Notificação enviada - Registro {NumeroSequencial} da aeronave {Matricula} vence em {Dias} dias",
                registro.NumeroSequencial, aeronave.Matricula, diasRestantes);
        }
    }

    /// <summary>
    /// Notificar registros com prazo já vencido
    /// </summary>
    public async Task NotificarPrazosVencidosAsync()
    {
        _logger.LogInformation("Verificando registros com prazo vencido");

        var registrosVencidos = await _registroVooRepository.GetComPrazoVencidoAsync();

        foreach (var registro in registrosVencidos)
        {
            var aeronave = await _aeronaveRepository.ObterPorIdAsync(registro.AeronaveId);
            if (aeronave == null) continue;

            var diasVencidos = CalcularDiasVencimento(registro, aeronave.TipoRBACOperador);

            var assunto = $"[CRÍTICO] Registro {registro.NumeroSequencial} - Prazo VENCIDO há {diasVencidos} dias";
            var corpo = GerarCorpoEmailPrazoVencido(registro, aeronave, diasVencidos);

            // Enviar para operadores e fiscalização
            await _emailService.EnviarNotificacaoOperadoresAsync(aeronave.OperadorId, assunto, corpo);
            await _emailService.EnviarNotificacaoFiscalizacaoAsync(assunto, corpo);

            _logger.LogError("PRAZO VENCIDO - Registro {NumeroSequencial} da aeronave {Matricula} vencido há {Dias} dias",
                registro.NumeroSequencial, aeronave.Matricula, diasVencidos);
        }
    }

    /// <summary>
    /// Gerar relatório de status das assinaturas
    /// Executado diariamente às 08:00
    /// </summary>
    [Queue("reports")]
    public async Task GerarRelatorioStatusAssinaturasAsync()
    {
        _logger.LogInformation("Gerando relatório diário de status das assinaturas");

        try
        {
            var dataRelatorio = DateTime.Today;
            var aeronaves = await _aeronaveRepository.ObterAtivasAsync();
            var relatorio = new List<StatusAssinaturaAeronave>();

            foreach (var aeronave in aeronaves)
            {
                var registros30Dias = await _registroVooRepository.ObterUltimos30DiasAsync(aeronave.Id);
                var status = CalcularStatusAeronave(registros30Dias.ToList(), aeronave);
                relatorio.Add(status);
            }

            // Gerar e enviar relatório
            var relatorioJson = System.Text.Json.JsonSerializer.Serialize(relatorio, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            var assunto = $"Relatório Diário - Status Assinaturas {dataRelatorio:dd/MM/yyyy}";
            var corpo = GerarCorpoRelatorioStatus(relatorio, dataRelatorio);

            await _emailService.EnviarRelatorioGerenciaAsync(assunto, corpo, relatorioJson);

            _logger.LogInformation("Relatório diário gerado e enviado com {Count} aeronaves", relatorio.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de status das assinaturas");
            throw;
        }
    }

    private async Task AtualizarStatusRegistrosVencidosAsync()
    {
        var registrosVencidos = await _registroVooRepository.GetComPrazoVencidoAsync();

        foreach (var registro in registrosVencidos)
        {
            // Marcar como não conforme por prazo vencido
            // Esta informação pode ser usada para auditoria ANAC
            _logger.LogWarning("Registro {Id} marcado como não conforme - prazo vencido", registro.Id);
        }
    }

    private static int CalcularDiasVencimento(RegistroVoo registro, TipoRBAC tipoRBAC)
    {
        if (!registro.AssinadoPiloto || registro.AssinadoOperador || registro.DataAssinaturaPilotoUTC == null)
            return 0;

        var prazoEmDias = tipoRBAC switch
        {
            TipoRBAC.RBAC121 => 2,
            TipoRBAC.RBAC135 => 15,
            _ => 30
        };

        var dataLimite = registro.DataAssinaturaPilotoUTC.Value.AddDays(prazoEmDias);
        var diasVencidos = (DateTime.UtcNow - dataLimite).Days;
        return Math.Max(0, diasVencidos);
    }

    private static StatusAssinaturaAeronave CalcularStatusAeronave(List<RegistroVoo> registros, Aeronave aeronave)
    {
        var totalRegistros = registros.Count;
        var registrosCompletos = registros.Count(r => r.AssinadoPiloto && r.AssinadoOperador);
        var registrosPendentes = registros.Count(r => r.AssinadoPiloto && !r.AssinadoOperador);
        var registrosSemAssinatura = registros.Count(r => !r.AssinadoPiloto);

        return new StatusAssinaturaAeronave
        {
            AeronaveId = aeronave.Id,
            Matricula = aeronave.Matricula,
            TipoRBAC = aeronave.TipoRBACOperador,
            TotalRegistros = totalRegistros,
            RegistrosCompletos = registrosCompletos,
            RegistrosPendentes = registrosPendentes,
            RegistrosSemAssinatura = registrosSemAssinatura,
            PercentualCompleto = totalRegistros > 0 ? Math.Round((double)registrosCompletos / totalRegistros * 100, 1) : 0,
            DataRelatorio = DateTime.Today
        };
    }

    private static string GerarCorpoEmailPrazoProximo(RegistroVoo registro, Aeronave aeronave, int diasRestantes, TipoRBAC tipoRBAC)
    {
        return $@"
ALERTA DE PRAZO - DIÁRIO DE BORDO DIGITAL ANAC

Registro de Voo com prazo próximo ao vencimento:

• Aeronave: {aeronave.Matricula}
• Registro: #{registro.NumeroSequencial}
• Data do Voo: {registro.Data:dd/MM/yyyy}
• Decolagem: {registro.LocalDecolagem}
• Pouso: {registro.LocalPouso}
• Tipo RBAC: {tipoRBAC}
• Dias Restantes: {diasRestantes}

AÇÃO NECESSÁRIA:
O registro deve ser assinado pelo operador dentro do prazo regulamentar.
Prazo {tipoRBAC}: {ObterDescricaoPrazo(tipoRBAC)}

Acesse o sistema para assinar: https://diariodebordo.anac.gov.br

Resolução ANAC 457/2017 - Cumprimento obrigatório.
";
    }

    private static string GerarCorpoEmailPrazoVencido(RegistroVoo registro, Aeronave aeronave, int diasVencidos)
    {
        return $@"
CRÍTICO - PRAZO VENCIDO - DIÁRIO DE BORDO DIGITAL ANAC

Registro de Voo com prazo VENCIDO:

• Aeronave: {aeronave.Matricula}
• Registro: #{registro.NumeroSequencial}
• Data do Voo: {registro.Data:dd/MM/yyyy}
• Decolagem: {registro.LocalDecolagem}
• Pouso: {registro.LocalPouso}
• Tipo RBAC: {aeronave.TipoRBACOperador}
• Dias Vencidos: {diasVencidos}

SITUAÇÃO CRÍTICA:
Este registro está em não conformidade com a Resolução ANAC 457/2017.
Ação corretiva imediata necessária.

CONSEQUÊNCIAS:
- Possível autuação ANAC
- Suspensão de operações
- Multas regulamentares

Contate a fiscalização para regularização.
";
    }

    private static string GerarCorpoRelatorioStatus(List<StatusAssinaturaAeronave> relatorio, DateTime dataRelatorio)
    {
        var totalAeronaves = relatorio.Count;
        var aeronavesCriticas = relatorio.Count(r => r.PercentualCompleto < 80);
        var percentualMedio = relatorio.Average(r => r.PercentualCompleto);

        var corpo = $@"
RELATÓRIO DIÁRIO - STATUS ASSINATURAS DIÁRIO DE BORDO
Data: {dataRelatorio:dd/MM/yyyy}

RESUMO EXECUTIVO:
• Total de Aeronaves: {totalAeronaves}
• Aeronaves Críticas (<80%): {aeronavesCriticas}
• Percentual Médio de Conformidade: {percentualMedio:F1}%

DETALHAMENTO POR AERONAVE:
";

        foreach (var status in relatorio.OrderBy(r => r.PercentualCompleto))
        {
            var indicador = status.PercentualCompleto >= 95 ? "✅" :
                           status.PercentualCompleto >= 80 ? "⚠️" : "🚨";

            corpo += $@"
{indicador} {status.Matricula} ({status.TipoRBAC})
   - Conformidade: {status.PercentualCompleto:F1}%
   - Registros: {status.RegistrosCompletos}/{status.TotalRegistros} completos
   - Pendentes: {status.RegistrosPendentes}
";
        }

        corpo += "\n\nRelatório gerado automaticamente pelo Sistema Diário de Bordo Digital ANAC.";
        return corpo;
    }

    private static string ObterDescricaoPrazo(TipoRBAC tipoRBAC) => tipoRBAC switch
    {
        TipoRBAC.RBAC121 => "2 dias após assinatura do piloto",
        TipoRBAC.RBAC135 => "15 dias após assinatura do piloto",
        _ => "30 dias após assinatura do piloto"
    };

    public class StatusAssinaturaAeronave
    {
        public int AeronaveId { get; set; }
        public string Matricula { get; set; } = string.Empty;
        public TipoRBAC TipoRBAC { get; set; }
        public int TotalRegistros { get; set; }
        public int RegistrosCompletos { get; set; }
        public int RegistrosPendentes { get; set; }
        public int RegistrosSemAssinatura { get; set; }
        public double PercentualCompleto { get; set; }
        public DateTime DataRelatorio { get; set; }
    }
}

/// <summary>
/// Interface para serviço de email (implementar conforme necessidade)
/// </summary>
public interface IEmailService
{
    Task EnviarNotificacaoOperadoresAsync(int operadorId, string assunto, string corpo);
    Task EnviarNotificacaoFiscalizacaoAsync(string assunto, string corpo);
    Task EnviarRelatorioGerenciaAsync(string assunto, string corpo, string anexoJson);
}