using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Enums;
using DiarioBordo.Domain.Repositories;
using NHibernate;
using NHibernate.Linq;

namespace DiarioBordo.Infrastructure.Repositories;

/// <summary>
/// Repositório para Tripulante
/// </summary>
public class TripulanteRepository : Repository<Tripulante>, ITripulanteRepository
{
    public TripulanteRepository(ISession session) : base(session) { }

    public async Task<Tripulante?> ObterPorCodigoANACAsync(string codigoANAC)
    {
        return await _session.Query<Tripulante>()
            .Where(t => t.CodigoANAC.Valor == codigoANAC)
            .FirstOrDefaultAsync();
    }

    public async Task<Tripulante?> ObterPorCPFAsync(string cpf)
    {
        var cpfLimpo = cpf.Replace(".", "").Replace("-", "");
        return await _session.Query<Tripulante>()
            .Where(t => t.CPF == cpfLimpo)
            .FirstOrDefaultAsync();
    }

    public async Task<IList<Tripulante>> ObterPorOperadorAsync(int operadorId)
    {
        return await _session.Query<Tripulante>()
            .Where(t => t.Ativo)
            .OrderBy(t => t.Nome)
            .ToListAsync();
    }

    public async Task<IList<Tripulante>> ObterPorFuncaoAsync(FuncaoTripulante funcao, int? operadorId = null)
    {
        var query = _session.Query<Tripulante>()
            .Where(t => t.Ativo && t.FuncoesAutorizadas.Contains(funcao));

        if (operadorId.HasValue)
        {
            query = query.Where(t => t.OperadorId == operadorId.Value);
        }

        return await query.OrderBy(t => t.Nome).ToListAsync();
    }

    public async Task<IList<Tripulante>> ObterAtivosAsync()
    {
        return await _session.Query<Tripulante>()
            .Where(t => t.Ativo)
            .OrderBy(t => t.Nome)
            .ToListAsync();
    }

    public async Task<Tripulante?> ObterPorCodigoAnacAsync(string codigoAnac)
    {
        return await ObterPorCodigoANACAsync(codigoAnac);
    }

    public async Task<IList<Tripulante>> ObterPorNomeAsync(string nome)
    {
        return await _session.Query<Tripulante>()
            .Where(t => t.Nome.Contains(nome))
            .OrderBy(t => t.Nome)
            .ToListAsync();
    }

    public async Task<bool> CodigoAnacExisteAsync(string codigoAnac)
    {
        var count = await _session.Query<Tripulante>()
            .CountAsync(t => t.CodigoANAC.Valor == codigoAnac);
        return count > 0;
    }
}