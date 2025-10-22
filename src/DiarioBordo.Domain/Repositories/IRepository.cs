using DiarioBordo.Domain.Entities;

namespace DiarioBordo.Domain.Repositories;

/// <summary>
/// Interface base para repositórios genéricos
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> ObterPorIdAsync(int id);
    Task<IList<T>> ObterTodosAsync();
    Task CriarAsync(T entidade);
    Task AdicionarAsync(T entidade);  // Alias para CriarAsync
    Task AtualizarAsync(T entidade);
    Task DeletarAsync(int id);
    Task RemoverAsync(T entidade);    // Método para remover por entidade
    Task<bool> ExisteAsync(int id);
}