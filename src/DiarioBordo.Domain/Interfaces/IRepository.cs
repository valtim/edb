using DiarioBordo.Domain.Entities;

namespace DiarioBordo.Domain.Interfaces;

/// <summary>
/// Interface base para repositórios
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IList<T>> GetAllAsync();
    Task<T> SaveAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<bool> ExistsAsync(int id);
}