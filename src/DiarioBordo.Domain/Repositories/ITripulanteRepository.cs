using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Enums;

namespace DiarioBordo.Domain.Repositories;

/// <summary>
/// Interface do repositório para Tripulante
/// </summary>
public interface ITripulanteRepository : IRepository<Tripulante>
{
    Task<Tripulante?> ObterPorCodigoAnacAsync(string codigoAnac);
    Task<Tripulante?> ObterPorCodigoANACAsync(string codigoAnac);
    Task<Tripulante?> ObterPorCPFAsync(string cpf);
    Task<IList<Tripulante>> ObterPorNomeAsync(string nome);
    Task<IList<Tripulante>> ObterAtivosAsync();
    Task<IList<Tripulante>> ObterPorOperadorAsync(int operadorId);
    Task<IList<Tripulante>> ObterPorFuncaoAsync(FuncaoTripulante funcao, int? operadorId = null);
    Task<bool> CodigoAnacExisteAsync(string codigoAnac);
}