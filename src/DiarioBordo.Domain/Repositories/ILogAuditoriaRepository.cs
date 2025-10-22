using DiarioBordo.Domain.Entities;

namespace DiarioBordo.Domain.Repositories;

/// <summary>
/// Interface do repositório para LogAuditoria
/// </summary>
public interface ILogAuditoriaRepository : IRepository<LogAuditoria>
{
    Task<IList<LogAuditoria>> ObterPorTabelaAsync(string tabela);
    Task<IList<LogAuditoria>> ObterPorRegistroAsync(string tabela, int registroId);
    Task<IList<LogAuditoria>> ObterPorUsuarioAsync(int usuarioId);
    Task<IList<LogAuditoria>> ObterPorPeriodoAsync(DateTime dataInicio, DateTime dataFim);
    Task<IList<LogAuditoria>> ObterPorOperacaoAsync(string operacao);
}