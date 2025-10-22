using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Repositories;
using NHibernate;
using NHibernate.Linq;

namespace DiarioBordo.Infrastructure.Repositories;

/// <summary>
/// Implementação base do repositório usando NHibernate
/// </summary>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ISession _session;

    public Repository(ISession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public virtual async Task<T?> ObterPorIdAsync(int id)
    {
        return await _session.GetAsync<T>(id);
    }

    public virtual async Task<IList<T>> ObterTodosAsync()
    {
        return await _session.Query<T>().ToListAsync();
    }

    public virtual async Task CriarAsync(T entity)
    {
        entity.DataCriacao = DateTime.UtcNow;
        await _session.SaveAsync(entity);
        await _session.FlushAsync();
    }

    public virtual async Task AdicionarAsync(T entity)
    {
        await CriarAsync(entity);
    }

    public virtual async Task AtualizarAsync(T entity)
    {
        entity.DataModificacao = DateTime.UtcNow;
        await _session.UpdateAsync(entity);
        await _session.FlushAsync();
    }

    public virtual async Task DeletarAsync(int id)
    {
        var entity = await _session.GetAsync<T>(id);
        if (entity != null)
        {
            await _session.DeleteAsync(entity);
            await _session.FlushAsync();
        }
    }

    public virtual async Task RemoverAsync(T entity)
    {
        await _session.DeleteAsync(entity);
        await _session.FlushAsync();
    }

    public virtual async Task<bool> ExisteAsync(int id)
    {
        var count = await _session.Query<T>()
            .CountAsync(x => x.Id == id);
        return count > 0;
    }
}