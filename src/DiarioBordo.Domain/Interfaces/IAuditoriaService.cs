using DiarioBordo.Domain.Entities;

namespace DiarioBordo.Domain.Interfaces;

/// <summary>
/// Serviço de auditoria para conformidade com Res. 458/2017 Art. 2º II
/// </summary>
public interface IAuditoriaService
{
    /// <summary>
    /// Registra operação no log de auditoria
    /// </summary>
    Task LogAsync(
        string operacao,
        string tabela,
        int registroId,
        string usuarioId,
        object? dadosAnteriores = null,
        object? dadosNovos = null,
        string? ipAddress = null,
        string? userAgent = null);

    /// <summary>
    /// Registra assinatura digital
    /// </summary>
    Task LogAssinaturaAsync(
        int registroVooId,
        string usuarioId,
        string tipoAssinatura,
        string hash,
        string? ipAddress = null,
        string? userAgent = null);

    /// <summary>
    /// Registra sincronização com ANAC
    /// </summary>
    Task LogSincronizacaoANACAsync(
        int registroVooId,
        bool sucesso,
        string? mensagem = null);

    /// <summary>
    /// Obtém logs de auditoria por período
    /// </summary>
    Task<IList<LogAuditoria>> GetLogsPorPeriodoAsync(
        DateTime dataInicio,
        DateTime dataFim,
        string? usuarioId = null,
        string? operacao = null);
}