using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Repositories;
using NHibernate;
using NHibernate.Linq;

namespace DiarioBordo.Infrastructure.Repositories;

/// <summary>
/// Repositório para AssinaturaRegistro
/// </summary>
public class AssinaturaRegistroRepository : Repository<AssinaturaRegistro>, IAssinaturaRegistroRepository
{
    public AssinaturaRegistroRepository(ISession session) : base(session) { }

    public async Task<IList<AssinaturaRegistro>> ObterPorRegistroVooAsync(int registroVooId)
    {
        return await _session.Query<AssinaturaRegistro>()
            .Where(a => a.RegistroVooId == registroVooId)
            .OrderBy(a => a.DataHoraUTC)
            .ToListAsync();
    }

    public async Task<IList<AssinaturaRegistro>> ObterPorRegistroAsync(int registroVooId)
    {
        return await ObterPorRegistroVooAsync(registroVooId);
    }

    public async Task<AssinaturaRegistro?> ObterAssinaturaPilotoAsync(int registroVooId)
    {
        return await _session.Query<AssinaturaRegistro>()
            .Where(a => a.RegistroVooId == registroVooId && a.TipoAssinatura == Domain.Enums.TipoAssinatura.Piloto)
            .FirstOrDefaultAsync();
    }

    public async Task<AssinaturaRegistro?> ObterAssinaturaOperadorAsync(int registroVooId)
    {
        return await _session.Query<AssinaturaRegistro>()
            .Where(a => a.RegistroVooId == registroVooId && a.TipoAssinatura == Domain.Enums.TipoAssinatura.Operador)
            .FirstOrDefaultAsync();
    }

    public async Task<IList<AssinaturaRegistro>> ObterPorUsuarioAsync(int usuarioId)
    {
        return await _session.Query<AssinaturaRegistro>()
            .Where(a => a.UsuarioId == usuarioId.ToString())
            .OrderByDescending(a => a.DataHoraUTC)
            .ToListAsync();
    }

    public async Task<bool> RegistroAssinadoPorPilotoAsync(int registroVooId)
    {
        var count = await _session.Query<AssinaturaRegistro>()
            .CountAsync(a => a.RegistroVooId == registroVooId && a.TipoAssinatura == Domain.Enums.TipoAssinatura.Piloto);
        return count > 0;
    }

    public async Task<bool> RegistroAssinadoPorOperadorAsync(int registroVooId)
    {
        var count = await _session.Query<AssinaturaRegistro>()
            .CountAsync(a => a.RegistroVooId == registroVooId && a.TipoAssinatura == Domain.Enums.TipoAssinatura.Operador);
        return count > 0;
    }
}