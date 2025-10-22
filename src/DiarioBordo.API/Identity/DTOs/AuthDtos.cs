using System.ComponentModel.DataAnnotations;

namespace DiarioBordo.API.Identity.DTOs;

/// <summary>
/// DTO para login de usuário
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Email ou código ANAC do usuário
    /// </summary>
    [Required(ErrorMessage = "Email ou código ANAC é obrigatório")]
    public string EmailOrCodigoANAC { get; set; } = string.Empty;

    /// <summary>
    /// Senha do usuário
    /// </summary>
    [Required(ErrorMessage = "Senha é obrigatória")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres")]
    public string Senha { get; set; } = string.Empty;

    /// <summary>
    /// Lembrar do login por mais tempo
    /// </summary>
    public bool LembrarMe { get; set; } = false;

    /// <summary>
    /// Código 2FA (se habilitado)
    /// </summary>
    [StringLength(6, MinimumLength = 6)]
    public string? CodigoTwoFactor { get; set; }
}

/// <summary>
/// DTO para registro de novo usuário
/// </summary>
public class RegisterDto
{
    /// <summary>
    /// Nome completo do usuário
    /// </summary>
    [Required(ErrorMessage = "Nome completo é obrigatório")]
    [StringLength(200, ErrorMessage = "Nome não pode exceder 200 caracteres")]
    public string NomeCompleto { get; set; } = string.Empty;

    /// <summary>
    /// Email do usuário
    /// </summary>
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email deve ter formato válido")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// CPF do usuário
    /// </summary>
    [Required(ErrorMessage = "CPF é obrigatório")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "CPF deve ter 11 dígitos")]
    public string CPF { get; set; } = string.Empty;

    /// <summary>
    /// Código ANAC (6 dígitos)
    /// </summary>
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Código ANAC deve ter 6 dígitos")]
    public string? CodigoANAC { get; set; }

    /// <summary>
    /// Senha do usuário
    /// </summary>
    [Required(ErrorMessage = "Senha é obrigatória")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 100 caracteres")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        ErrorMessage = "A senha deve conter ao menos: 1 letra minúscula, 1 maiúscula, 1 número e 1 caractere especial")]
    public string Senha { get; set; } = string.Empty;

    /// <summary>
    /// Confirmação da senha
    /// </summary>
    [Required(ErrorMessage = "Confirmação de senha é obrigatória")]
    [Compare(nameof(Senha), ErrorMessage = "Senhas não conferem")]
    public string ConfirmarSenha { get; set; } = string.Empty;

    /// <summary>
    /// ID do operador (se aplicável)
    /// </summary>
    public int? OperadorId { get; set; }

    /// <summary>
    /// Papéis do usuário
    /// </summary>
    public List<string> Papeis { get; set; } = new();
}

/// <summary>
/// DTO de resposta para login
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// Token JWT de acesso
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Token de refresh
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Data de expiração do token
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Dados do usuário
    /// </summary>
    public UserInfoDto Usuario { get; set; } = new();

    /// <summary>
    /// Indica se 2FA é necessário
    /// </summary>
    public bool RequiresTwoFactor { get; set; } = false;
}

/// <summary>
/// DTO com informações do usuário
/// </summary>
public class UserInfoDto
{
    public int Id { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CodigoANAC { get; set; }
    public int? OperadorId { get; set; }
    public List<string> Papeis { get; set; } = new();
    public DateTime? UltimoAcesso { get; set; }
    public bool TwoFactorEnabled { get; set; }
}

/// <summary>
/// DTO para alteração de senha
/// </summary>
public class ChangePasswordDto
{
    /// <summary>
    /// Senha atual
    /// </summary>
    [Required(ErrorMessage = "Senha atual é obrigatória")]
    public string SenhaAtual { get; set; } = string.Empty;

    /// <summary>
    /// Nova senha
    /// </summary>
    [Required(ErrorMessage = "Nova senha é obrigatória")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 100 caracteres")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        ErrorMessage = "A senha deve conter ao menos: 1 letra minúscula, 1 maiúscula, 1 número e 1 caractere especial")]
    public string NovaSenha { get; set; } = string.Empty;

    /// <summary>
    /// Confirmação da nova senha
    /// </summary>
    [Required(ErrorMessage = "Confirmação de nova senha é obrigatória")]
    [Compare(nameof(NovaSenha), ErrorMessage = "Senhas não conferem")]
    public string ConfirmarNovaSenha { get; set; } = string.Empty;
}

/// <summary>
/// DTO para configuração de 2FA
/// </summary>
public class TwoFactorSetupDto
{
    /// <summary>
    /// Secret gerado para TOTP
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// QR Code em Base64 para configuração
    /// </summary>
    public string QrCodeBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Códigos de recuperação
    /// </summary>
    public List<string> RecoveryCodes { get; set; } = new();
}

/// <summary>
/// DTO para verificação de 2FA
/// </summary>
public class VerifyTwoFactorDto
{
    /// <summary>
    /// Código de verificação
    /// </summary>
    [Required(ErrorMessage = "Código de verificação é obrigatório")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Código deve ter 6 dígitos")]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Indica se é um código de recuperação
    /// </summary>
    public bool IsRecoveryCode { get; set; } = false;
}