using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Repositories;
using NHibernate;
using NHibernate.Linq;

namespace DiarioBordo.Infrastructure.Repositories;

/// <summary>
/// Repositório para LogAuditoria
/// </summary>
public class LogAuditoriaRepository : Repository<LogAuditoria>, ILogAuditoriaRepository
{
    public LogAuditoriaRepository(ISession session) : base(session) { }

    public async Task<IList<LogAuditoria>> ObterPorTabelaAsync(string tabela)
    {
        return await _session.Query<LogAuditoria>()
            .Where(l => l.Tabela == tabela)
            .OrderByDescending(l => l.DataCriacao)
            .Take(100)
            .ToListAsync();
    }

    public async Task<IList<LogAuditoria>> ObterPorRegistroAsync(string tabela, int registroId)
    {
        return await _session.Query<LogAuditoria>()
            .Where(l => l.Tabela == tabela && l.RegistroId == registroId)
            .OrderByDescending(l => l.DataCriacao)
            .ToListAsync();
    }

    public async Task<IList<LogAuditoria>> ObterPorUsuarioAsync(int usuarioId)
    {
        var usuarioIdStr = usuarioId.ToString();
        return await _session.Query<LogAuditoria>()
            .Where(l => l.UsuarioId == usuarioIdStr)
            .OrderByDescending(l => l.DataCriacao)
            .Take(100)
            .ToListAsync();
    }

    public async Task<IList<LogAuditoria>> ObterPorOperacaoAsync(string operacao)
    {
        return await _session.Query<LogAuditoria>()
            .Where(l => l.Operacao == operacao)
            .OrderByDescending(l => l.DataCriacao)
            .Take(100)
            .ToListAsync();
    }

    public async Task<IList<LogAuditoria>> ObterPorPeriodoAsync(DateTime dataInicio, DateTime dataFim)
    {
        return await _session.Query<LogAuditoria>()
            .Where(l => l.DataCriacao >= dataInicio && l.DataCriacao <= dataFim)
            .OrderByDescending(l => l.DataCriacao)
            .ToListAsync();
    }
}