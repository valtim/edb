namespace DiarioBordo.Domain.Entities;

/// <summary>
/// Log de Auditoria conforme Resolução ANAC 458/2017 Art. 2º II
/// Sistema de Registro de Logs para todas as operações críticas
/// </summary>
public class LogAuditoria : BaseEntity
{
    /// <summary>
    /// Data e hora da operação em UTC
    /// </summary>
    public virtual DateTime DataHoraUTC { get; set; }

    /// <summary>
    /// Tipo de operação realizada (INSERT, UPDATE, DELETE, SIGN, SYNC)
    /// </summary>
    public virtual string Operacao { get; set; } = string.Empty;

    /// <summary>
    /// Tabela/Entidade afetada pela operação
    /// </summary>
    public virtual string Tabela { get; set; } = string.Empty;

    /// <summary>
    /// ID do registro afetado
    /// </summary>
    public virtual int RegistroId { get; set; }

    /// <summary>
    /// ID do usuário que executou a operação
    /// </summary>
    public virtual string UsuarioId { get; set; } = string.Empty;

    /// <summary>
    /// Dados anteriores à operação (JSON)
    /// </summary>
    public virtual string? DadosAnteriores { get; set; }

    /// <summary>
    /// Dados posteriores à operação (JSON)
    /// </summary>
    public virtual string? DadosNovos { get; set; }

    /// <summary>
    /// Endereço IP do usuário
    /// </summary>
    public virtual string? IPAddress { get; set; }

    /// <summary>
    /// User Agent do navegador
    /// </summary>
    public virtual string? UserAgent { get; set; }

    /// <summary>
    /// Informações adicionais sobre a operação
    /// </summary>
    public virtual string? InformacoesAdicionais { get; set; }

    /// <summary>
    /// Indica se a operação foi bem-sucedida
    /// </summary>
    public virtual bool Sucesso { get; set; } = true;

    /// <summary>
    /// Mensagem de erro (se aplicável)
    /// </summary>
    public virtual string? MensagemErro { get; set; }

    // Propriedades de compatibilidade para Jobs antigos
    public virtual string TabelaAfetada => Tabela;
    public virtual string? ValoresAnteriores => DadosAnteriores;
    public virtual string? ValoresNovos => DadosNovos;
    public virtual DateTime DataOperacao => DataHoraUTC;
    public virtual string? EnderecoIP => IPAddress;
}