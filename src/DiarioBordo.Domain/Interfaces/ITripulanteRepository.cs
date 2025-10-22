using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.ValueObjects;

namespace DiarioBordo.Domain.Interfaces;

/// <summary>
/// Repository para Tripulantes
/// </summary>
public interface ITripulanteRepository : IRepository<Tripulante>
{
    /// <summary>
    /// Busca tripulante por código ANAC
    /// </summary>
    Task<Tripulante?> GetByCodigoANACAsync(CodigoANAC codigoANAC);

    /// <summary>
    /// Busca tripulante por CPF
    /// </summary>
    Task<Tripulante?> GetByCPFAsync(string cpf);

    /// <summary>
    /// Obtém tripulantes ativos
    /// </summary>
    Task<IList<Tripulante>> GetAtivosAsync();

    /// <summary>
    /// Busca tripulantes por nome (pesquisa parcial)
    /// </summary>
    Task<IList<Tripulante>> BuscarPorNomeAsync(string nome);
}