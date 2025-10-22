using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Repositories;
using NHibernate;
using NHibernate.Linq;

namespace DiarioBordo.Infrastructure.Repositories;

/// <summary>
/// Repositório para Aeronave
/// </summary>
public class AeronaveRepository : Repository<Aeronave>, IAeronaveRepository
{
    public AeronaveRepository(ISession session) : base(session) { }

    public async Task<Aeronave?> ObterPorMatriculaAsync(string matricula)
    {
        return await _session.Query<Aeronave>()
            .Where(a => a.Matricula == matricula.ToUpperInvariant())
            .FirstOrDefaultAsync();
    }

    public async Task<IList<Aeronave>> ObterPorOperadorAsync(int operadorId)
    {
        return await _session.Query<Aeronave>()
            .Where(a => a.Ativo)
            .OrderBy(a => a.Matricula)
            .ToListAsync();
    }

    public async Task<IList<Aeronave>> ObterAtivasAsync()
    {
        return await _session.Query<Aeronave>()
            .Where(a => a.Ativa)
            .OrderBy(a => a.Matricula)
            .ToListAsync();
    }
}