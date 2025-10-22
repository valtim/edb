using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DiarioBordo.API.Identity;

/// <summary>
/// Usuário do sistema com integração ASP.NET Identity
/// Conformidade com papéis ANAC
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    /// <summary>
    /// Nome completo do usuário
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string NomeCompleto { get; set; } = string.Empty;

    /// <summary>
    /// CPF do usuário (sem formatação)
    /// </summary>
    [Required]
    [StringLength(11, MinimumLength = 11)]
    public string CPF { get; set; } = string.Empty;

    /// <summary>
    /// Código ANAC do usuário (6 dígitos)
    /// </summary>
    [StringLength(6, MinimumLength = 6)]
    public string? CodigoANAC { get; set; }

    /// <summary>
    /// ID do operador ao qual o usuário pertence
    /// </summary>
    public int? OperadorId { get; set; }

    /// <summary>
    /// Indica se o usuário está ativo
    /// </summary>
    public bool Ativo { get; set; } = true;

    /// <summary>
    /// Data de criação do usuário
    /// </summary>
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data da última atualização
    /// </summary>
    public DateTime? DataUltimaAtualizacao { get; set; }

    /// <summary>
    /// Último acesso ao sistema
    /// </summary>
    public DateTime? UltimoAcesso { get; set; }

    /// <summary>
    /// Endereço IP do último acesso
    /// </summary>
    public string? UltimoIP { get; set; }

    /// <summary>
    /// Tentativas de login falhadas consecutivas
    /// </summary>
    public int TentativasLoginFalhadas { get; set; } = 0;

    /// <summary>
    /// Data do bloqueio temporário (se aplicável)
    /// </summary>
    public DateTime? DataBloqueioTemporario { get; set; }

    /// <summary>
    /// Indica se 2FA está habilitado
    /// </summary>
    public bool TwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// Secret para TOTP (Google Authenticator, Authy, etc.)
    /// </summary>
    public string? TwoFactorSecret { get; set; }

    /// <summary>
    /// Códigos de recuperação 2FA
    /// </summary>
    public string? RecoveryCodes { get; set; }
}