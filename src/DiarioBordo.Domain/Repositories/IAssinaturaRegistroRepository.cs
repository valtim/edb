using DiarioBordo.Domain.Entities;

namespace DiarioBordo.Domain.Repositories;

/// <summary>
/// Interface do repositório para AssinaturaRegistro
/// </summary>
public interface IAssinaturaRegistroRepository : IRepository<AssinaturaRegistro>
{
    Task<IList<AssinaturaRegistro>> ObterPorRegistroVooAsync(int registroVooId);
    Task<IList<AssinaturaRegistro>> ObterPorRegistroAsync(int registroVooId);
    Task<AssinaturaRegistro?> ObterAssinaturaPilotoAsync(int registroVooId);
    Task<AssinaturaRegistro?> ObterAssinaturaOperadorAsync(int registroVooId);
    Task<IList<AssinaturaRegistro>> ObterPorUsuarioAsync(int usuarioId);
    Task<bool> RegistroAssinadoPorPilotoAsync(int registroVooId);
    Task<bool> RegistroAssinadoPorOperadorAsync(int registroVooId);
}