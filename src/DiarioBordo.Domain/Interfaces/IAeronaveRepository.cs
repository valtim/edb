using DiarioBordo.Domain.Entities;

namespace DiarioBordo.Domain.Interfaces;

/// <summary>
/// Repository para Aeronaves
/// </summary>
public interface IAeronaveRepository : IRepository<Aeronave>
{
    /// <summary>
    /// Busca aeronave por matrícula
    /// </summary>
    Task<Aeronave?> GetByMatriculaAsync(string matricula);

    /// <summary>
    /// Obtém aeronaves ativas
    /// </summary>
    Task<IList<Aeronave>> GetAtivasAsync();

    /// <summary>
    /// Busca aeronaves por fabricante/modelo
    /// </summary>
    Task<IList<Aeronave>> BuscarPorFabricanteModeloAsync(string? fabricante, string? modelo);

    /// <summary>
    /// Obtém aeronaves com registros pendentes de assinatura
    /// </summary>
    Task<IList<Aeronave>> GetComRegistrosPendentesAsync();
}