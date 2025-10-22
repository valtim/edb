using DiarioBordo.Domain.Enums;

namespace DiarioBordo.Domain.Entities;

/// <summary>
/// Assinatura Digital de Registro conforme Resolução ANAC 458/2017 Art. 2º V
/// Garante autenticidade, integridade e irretratabilidade
/// </summary>
public class AssinaturaRegistro : BaseEntity
{
    /// <summary>
    /// Registro de voo assinado
    /// </summary>
    public virtual RegistroVoo RegistroVoo { get; set; } = null!;

    /// <summary>
    /// ID do registro de voo (FK)
    /// </summary>
    public virtual int RegistroVooId { get; set; }

    /// <summary>
    /// ID do usuário que assinou (autenticidade)
    /// </summary>
    public virtual string UsuarioId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo da assinatura (Piloto ou Operador)
    /// </summary>
    public virtual TipoAssinatura TipoAssinatura { get; set; }

    /// <summary>
    /// Data e hora da assinatura em UTC (irretratabilidade)
    /// </summary>
    public virtual DateTime DataHoraUTC { get; set; }

    /// <summary>
    /// Hash SHA-256 do registro no momento da assinatura (integridade)
    /// </summary>
    public virtual string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Endereço IP do usuário no momento da assinatura (auditoria)
    /// </summary>
    public virtual string? IPAddress { get; set; }

    /// <summary>
    /// User Agent do navegador (auditoria)
    /// </summary>
    public virtual string? UserAgent { get; set; }

    /// <summary>
    /// Código ANAC do assinante (compatibilidade)
    /// </summary>
    public virtual string CodigoANAC { get; set; } = string.Empty;

    /// <summary>
    /// Nome do assinante (compatibilidade)
    /// </summary>
    public virtual string NomeAssinante { get; set; } = string.Empty;

    /// <summary>
    /// Hash do registro (alias para compatibilidade)
    /// </summary>
    public virtual string HashRegistro
    {
        get => Hash;
        set => Hash = value;
    }

    /// <summary>
    /// Data da assinatura (alias para compatibilidade)
    /// </summary>
    public virtual DateTime DataAssinatura
    {
        get => DataHoraUTC;
        set => DataHoraUTC = value;
    }

    /// <summary>
    /// Endereço IP (alias para compatibilidade)
    /// </summary>
    public virtual string? EnderecoIP
    {
        get => IPAddress;
        set => IPAddress = value;
    }

    /// <summary>
    /// Indica se a assinatura foi validada (compatibilidade)
    /// </summary>
    public virtual bool AssinaturaValidada { get; set; } = true;

    /// <summary>
    /// Data de validação da assinatura (compatibilidade)
    /// </summary>
    public virtual DateTime? DataValidacao { get; set; }

    /// <summary>
    /// Dados adicionais da assinatura em JSON (certificado, etc.)
    /// </summary>
    public virtual string? DadosAdicionais { get; set; }

    /// <summary>
    /// Verifica se a assinatura é válida comparando hashes
    /// </summary>
    public virtual bool ValidarIntegridade(string hashAtual)
    {
        return Hash.Equals(hashAtual, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Descrição legível da assinatura
    /// </summary>
    public virtual string DescricaoAssinatura()
    {
        var tipo = TipoAssinatura == TipoAssinatura.Piloto ? "Piloto" : "Operador";
        return $"{tipo} {UsuarioId} em {DataHoraUTC:dd/MM/yyyy HH:mm} UTC";
    }

    /// <summary>
    /// Verifica se a assinatura ainda está dentro do prazo de validade
    /// </summary>
    public virtual bool AssinaturaValida()
    {
        // Assinaturas são permanentes, mas podemos verificar se não são muito antigas
        var tempoMaximo = TimeSpan.FromDays(365 * 5); // 5 anos conforme retenção ANAC
        return DateTime.UtcNow - DataHoraUTC <= tempoMaximo;
    }
}