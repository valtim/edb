using DiarioBordo.Domain.Entities;

namespace DiarioBordo.Domain.Repositories;

/// <summary>
/// Interface do repositório para Aeronave
/// </summary>
public interface IAeronaveRepository : IRepository<Aeronave>
{
    Task<Aeronave?> ObterPorMatriculaAsync(string matricula);
    Task<IList<Aeronave>> ObterPorOperadorAsync(int operadorId);
    Task<IList<Aeronave>> ObterAtivasAsync();
}