using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Enums;

namespace DiarioBordo.Domain.Repositories;

/// <summary>
/// Interface do repositório para RegistroVoo
/// </summary>
public interface IRegistroVooRepository : IRepository<RegistroVoo>
{
    Task<IList<RegistroVoo>> ObterPorAeronaveAsync(int aeronaveId);
    Task<IList<RegistroVoo>> ObterUltimos30DiasAsync(int aeronaveId);
    Task<IList<RegistroVoo>> ObterPorPeriodoAsync(int aeronaveId, DateTime dataInicio, DateTime dataFim);
    Task<RegistroVoo?> ObterPorNumeroSequencialAsync(int aeronaveId, int numeroSequencial);
    Task<IList<RegistroVoo>> ObterPendentesSincronizacaoAsync();
    Task<IList<RegistroVoo>> ObterComPrazoProximoVencimentoAsync(int diasAviso);
    Task<IList<RegistroVoo>> ObterComPrazoVencidoAsync();

    // Métodos de compatibilidade com Jobs (nomes antigos + sobrecargas DateTime)
    Task<IList<RegistroVoo>> GetComPrazoProximoVencimentoAsync(int diasAviso = 2);
    Task<IList<RegistroVoo>> GetComPrazoProximoVencimentoAsync(DateTime dataLimite);
    Task<IList<RegistroVoo>> GetComPrazoVencidoAsync();
    Task<IList<RegistroVoo>> GetComPrazoVencidoAsync(DateTime dataLimite);

    Task<IList<RegistroVoo>> ObterComProblemaSincronizacaoAsync();
    Task<IList<RegistroVoo>> ObterComFalhaSincronizacaoAsync();
    Task<int> ObterProximoNumeroSequencialAsync(int aeronaveId);
    Task<IList<RegistroVoo>> ObterPendentesAssinaturaPorAeronaveAsync(int aeronaveId);
    Task<IList<RegistroVoo>> ObterPendentesAssinaturaOperadorAsync(int operadorId);
    Task<IList<RegistroVoo>> ObterPendentesAssinaturaPorPilotoAsync(int pilotoId);
    Task<IList<RegistroVoo>> ObterPendentesAssinaturaOperadorComPrazoAsync(int operadorId, int diasPrazo);
    Task<IList<RegistroVoo>> ObterRegistrosPendentesAssinaturaAsync(int operadorId);
    Task<IList<RegistroVoo>> ObterPorPilotoComandoAsync(int pilotoId, DateTime dataInicio, DateTime dataFim);
    Task<long> ObterUltimoNumeroSequencialAsync(int aeronaveId);
    Task<bool> ValidarNumeroSequencialUnicoAsync(int aeronaveId, long numeroSequencial, int? excluirRegistroId = null);
}