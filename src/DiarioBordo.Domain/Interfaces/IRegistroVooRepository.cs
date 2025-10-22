using DiarioBordo.Domain.Entities;

namespace DiarioBordo.Domain.Interfaces;

/// <summary>
/// Repository específico para Registros de Voo com consultas otimizadas para conformidade ANAC
/// </summary>
public interface IRegistroVooRepository : IRepository<RegistroVoo>
{
    /// <summary>
    /// Obtém registros dos últimos 30 dias de uma aeronave (Art. 8º II Res. 457/2017)
    /// Performance crítica: <500ms
    /// </summary>
    Task<IList<RegistroVoo>> GetUltimos30DiasAsync(int aeronaveId);

    /// <summary>
    /// Obtém registros pendentes de assinatura do operador
    /// </summary>
    Task<IList<RegistroVoo>> GetPendentesAssinaturaOperadorAsync();

    /// <summary>
    /// Obtém registros pendentes de sincronização com ANAC
    /// </summary>
    Task<IList<RegistroVoo>> GetPendentesSincronizacaoAsync();

    /// <summary>
    /// Obtém registros com prazo de assinatura próximo ao vencimento
    /// </summary>
    Task<IList<RegistroVoo>> GetComPrazoProximoVencimentoAsync(int diasAviso = 2);

    /// <summary>
    /// Obtém registros com prazo de assinatura próximo ao vencimento (sobrecarga com DateTime)
    /// </summary>
    Task<IList<RegistroVoo>> GetComPrazoProximoVencimentoAsync(DateTime dataLimite);

    /// <summary>
    /// Obtém registros com prazo vencido por tipo RBAC
    /// </summary>
    Task<IList<RegistroVoo>> GetComPrazoVencidoAsync();

    /// <summary>
    /// Obtém registros com prazo vencido (sobrecarga com DateTime)
    /// </summary>
    Task<IList<RegistroVoo>> GetComPrazoVencidoAsync(DateTime dataLimite);

    /// <summary>
    /// Busca registros por período e filtros
    /// </summary>
    Task<IList<RegistroVoo>> BuscarPorPeriodoAsync(
        DateTime dataInicio,
        DateTime dataFim,
        int? aeronaveId = null,
        string? codigoPiloto = null);

    /// <summary>
    /// Obtém próximo número sequencial para uma aeronave
    /// </summary>
    Task<long> GetProximoNumeroSequencialAsync(int aeronaveId);
}