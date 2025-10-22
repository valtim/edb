using DiarioBordo.API.Identity;
using DiarioBordo.API.Identity.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using QRCoder;

namespace DiarioBordo.API.Identity.Services;

/// <summary>
/// Serviço de autenticação com JWT e 2FA
/// </summary>
public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
    Task<UserInfoDto> RegisterAsync(RegisterDto registerDto);
    Task<LoginResponseDto> RefreshTokenAsync(string refreshToken);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
    Task<TwoFactorSetupDto> SetupTwoFactorAsync(int userId);
    Task<bool> EnableTwoFactorAsync(int userId, string verificationCode);
    Task<bool> DisableTwoFactorAsync(int userId, string password);
    Task<bool> VerifyTwoFactorAsync(int userId, VerifyTwoFactorDto verifyDto);
    Task LogoutAsync(int userId);
    Task<UserInfoDto?> GetUserInfoAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    // Cache de refresh tokens (em produção, usar Redis)
    private static readonly Dictionary<string, RefreshTokenInfo> _refreshTokens = new();

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
    {
        // Buscar usuário por email ou código ANAC
        var user = await _userManager.FindByEmailAsync(loginDto.EmailOrCodigoANAC) ??
                  await _userManager.Users.FirstOrDefaultAsync(u => u.CodigoANAC == loginDto.EmailOrCodigoANAC);

        if (user == null || !user.Ativo)
        {
            _logger.LogWarning("Tentativa de login com usuário inválido: {Email}", loginDto.EmailOrCodigoANAC);
            throw new UnauthorizedAccessException("Credenciais inválidas");
        }

        // Verificar senha
        var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Senha, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Usuário bloqueado: {UserId}", user.Id);
            throw new UnauthorizedAccessException("Conta temporariamente bloqueada devido a tentativas excessivas de login");
        }

        if (!result.Succeeded)
        {
            _logger.LogWarning("Tentativa de login falhada para usuário: {UserId}", user.Id);
            throw new UnauthorizedAccessException("Credenciais inválidas");
        }

        // Verificar se 2FA é necessário
        if (user.TwoFactorEnabled && string.IsNullOrEmpty(loginDto.CodigoTwoFactor))
        {
            return new LoginResponseDto
            {
                RequiresTwoFactor = true,
                Usuario = await MapToUserInfoDto(user)
            };
        }

        // Verificar código 2FA se fornecido
        if (user.TwoFactorEnabled && !string.IsNullOrEmpty(loginDto.CodigoTwoFactor))
        {
            var twoFactorResult = await VerifyTwoFactorCodeAsync(user, loginDto.CodigoTwoFactor);
            if (!twoFactorResult)
            {
                throw new UnauthorizedAccessException("Código de verificação inválido");
            }
        }

        // Gerar tokens
        var tokenResult = await GenerateAccessTokenAsync(user);
        var accessToken = tokenResult.Item1;
        var expiresAt = tokenResult.Item2;
        var refreshToken = GenerateRefreshToken();

        // Armazenar refresh token
        _refreshTokens[refreshToken] = new RefreshTokenInfo
        {
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Atualizar último acesso
        user.UltimoAcesso = DateTime.UtcNow;
        user.TentativasLoginFalhadas = 0;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Login bem-sucedido para usuário: {UserId}", user.Id);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            Usuario = await MapToUserInfoDto(user),
            RequiresTwoFactor = false
        };
    }

    public async Task<UserInfoDto> RegisterAsync(RegisterDto registerDto)
    {
        // Verificar se email já existe
        var existingEmailUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingEmailUser != null)
        {
            throw new InvalidOperationException("Email já está em uso");
        }

        // Verificar se CPF já existe
        var existingCpfUser = await _userManager.Users.FirstOrDefaultAsync(u => u.CPF == registerDto.CPF);
        if (existingCpfUser != null)
        {
            throw new InvalidOperationException("CPF já está cadastrado");
        }

        // Verificar se código ANAC já existe (se fornecido)
        if (!string.IsNullOrEmpty(registerDto.CodigoANAC))
        {
            var existingAnacUser = await _userManager.Users.FirstOrDefaultAsync(u => u.CodigoANAC == registerDto.CodigoANAC);
            if (existingAnacUser != null)
            {
                throw new InvalidOperationException("Código ANAC já está em uso");
            }
        }

        // Criar usuário
        var user = new ApplicationUser
        {
            UserName = registerDto.Email,
            Email = registerDto.Email,
            NomeCompleto = registerDto.NomeCompleto,
            CPF = registerDto.CPF,
            CodigoANAC = registerDto.CodigoANAC,
            OperadorId = registerDto.OperadorId,
            EmailConfirmed = true // Em produção, implementar confirmação por email
        };

        var result = await _userManager.CreateAsync(user, registerDto.Senha);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Erro ao criar usuário: {errors}");
        }

        // Adicionar papéis
        if (registerDto.Papeis.Any())
        {
            await _userManager.AddToRolesAsync(user, registerDto.Papeis);
        }

        _logger.LogInformation("Usuário registrado com sucesso: {UserId}", user.Id);

        return await MapToUserInfoDto(user);
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken)
    {
        if (!_refreshTokens.TryGetValue(refreshToken, out var tokenInfo) ||
            tokenInfo.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token inválido ou expirado");
        }

        var user = await _userManager.FindByIdAsync(tokenInfo.UserId.ToString());
        if (user == null || !user.Ativo)
        {
            throw new UnauthorizedAccessException("Usuário inválido");
        }

        // Gerar novo access token
        var (accessToken, expiresAt) = await GenerateAccessTokenAsync(user);
        var newRefreshToken = GenerateRefreshToken();

        // Remover token antigo e adicionar novo
        _refreshTokens.Remove(refreshToken);
        _refreshTokens[newRefreshToken] = new RefreshTokenInfo
        {
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = expiresAt,
            Usuario = await MapToUserInfoDto(user)
        };
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.SenhaAtual, changePasswordDto.NovaSenha);
        if (result.Succeeded)
        {
            _logger.LogInformation("Senha alterada para usuário: {UserId}", userId);
        }

        return result.Succeeded;
    }

    public async Task<TwoFactorSetupDto> SetupTwoFactorAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException("Usuário não encontrado");
        }

        // Gerar secret para TOTP
        var secret = GenerateBase32Secret();
        user.TwoFactorSecret = secret;

        // Gerar códigos de recuperação
        var recoveryCodes = GenerateRecoveryCodes();
        user.RecoveryCodes = string.Join(",", recoveryCodes);

        await _userManager.UpdateAsync(user);

        // Gerar QR Code
        var qrCodeBase64 = GenerateQrCode(user.Email ?? string.Empty, secret);

        return new TwoFactorSetupDto
        {
            Secret = secret,
            QrCodeBase64 = qrCodeBase64,
            RecoveryCodes = recoveryCodes
        };
    }

    public async Task<bool> EnableTwoFactorAsync(int userId, string verificationCode)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            return false;
        }

        var isValid = VerifyTotpCode(user.TwoFactorSecret, verificationCode);
        if (isValid)
        {
            user.TwoFactorEnabled = true;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("2FA habilitado para usuário: {UserId}", userId);
        }

        return isValid;
    }

    public async Task<bool> DisableTwoFactorAsync(int userId, string password)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (passwordValid)
        {
            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            user.RecoveryCodes = null;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("2FA desabilitado para usuário: {UserId}", userId);
        }

        return passwordValid;
    }

    public async Task<bool> VerifyTwoFactorAsync(int userId, VerifyTwoFactorDto verifyDto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        return await VerifyTwoFactorCodeAsync(user, verifyDto.Codigo, verifyDto.IsRecoveryCode);
    }

    public async Task LogoutAsync(int userId)
    {
        // Remover todos os refresh tokens do usuário
        var tokensToRemove = _refreshTokens.Where(kvp => kvp.Value.UserId == userId).Select(kvp => kvp.Key).ToList();
        foreach (var token in tokensToRemove)
        {
            _refreshTokens.Remove(token);
        }

        _logger.LogInformation("Logout realizado para usuário: {UserId}", userId);
    }

    public async Task<UserInfoDto?> GetUserInfoAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user != null ? await MapToUserInfoDto(user) : null;
    }

    private async Task<(string token, DateTime expiresAt)> GenerateAccessTokenAsync(ApplicationUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);

        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.NomeCompleto),
            new("OperadorId", user.OperadorId?.ToString() ?? ""),
            new("CodigoANAC", user.CodigoANAC ?? "")
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpiryInMinutes"]!));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), expiresAt);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private async Task<UserInfoDto> MapToUserInfoDto(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new UserInfoDto
        {
            Id = user.Id,
            NomeCompleto = user.NomeCompleto,
            Email = user.Email!,
            CodigoANAC = user.CodigoANAC,
            OperadorId = user.OperadorId,
            Papeis = roles.ToList(),
            UltimoAcesso = user.UltimoAcesso,
            TwoFactorEnabled = user.TwoFactorEnabled
        };
    }

    private async Task<bool> VerifyTwoFactorCodeAsync(ApplicationUser user, string code, bool isRecoveryCode = false)
    {
        if (isRecoveryCode)
        {
            if (string.IsNullOrEmpty(user.RecoveryCodes))
                return false;

            var recoveryCodes = user.RecoveryCodes.Split(',').ToList();
            if (recoveryCodes.Contains(code))
            {
                recoveryCodes.Remove(code);
                user.RecoveryCodes = string.Join(",", recoveryCodes);
                await _userManager.UpdateAsync(user);
                return true;
            }
        }
        else
        {
            if (string.IsNullOrEmpty(user.TwoFactorSecret))
                return false;

            return VerifyTotpCode(user.TwoFactorSecret, code);
        }

        return false;
    }

    private static string GenerateBase32Secret()
    {
        var bytes = new byte[20];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base32Encode(bytes);
    }

    private static List<string> GenerateRecoveryCodes()
    {
        var codes = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            codes.Add(GenerateRecoveryCode());
        }
        return codes;
    }

    private static string GenerateRecoveryCode()
    {
        var bytes = new byte[4];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return BitConverter.ToUInt32(bytes).ToString("D8");
    }

    private string GenerateQrCode(string email, string secret)
    {
        var appName = _configuration["App:Name"] ?? "Diário de Bordo ANAC";
        var totpUrl = $"otpauth://totp/{appName}:{email}?secret={secret}&issuer={appName}";

        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(totpUrl, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new Base64QRCode(qrCodeData);
        return qrCode.GetGraphic(20);
    }

    private static bool VerifyTotpCode(string secret, string code)
    {
        var timeStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;

        // Verificar código atual e códigos adjacentes (±1 time step para tolerância de clock skew)
        for (long i = -1; i <= 1; i++)
        {
            var computedCode = GenerateTotpCode(secret, timeStep + i);
            if (computedCode == code)
                return true;
        }

        return false;
    }

    private static string GenerateTotpCode(string secret, long timeStep)
    {
        var secretBytes = Base32Decode(secret);
        var timeBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timeBytes);

        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(timeBytes);

        var offset = hash[hash.Length - 1] & 0xf;
        var code = ((hash[offset] & 0x7f) << 24) |
                   ((hash[offset + 1] & 0xff) << 16) |
                   ((hash[offset + 2] & 0xff) << 8) |
                   (hash[offset + 3] & 0xff);

        return (code % 1000000).ToString("D6");
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new StringBuilder();

        for (int i = 0; i < data.Length; i += 5)
        {
            var bytes = new byte[5];
            Array.Copy(data, i, bytes, 0, Math.Min(5, data.Length - i));

            var value = ((long)bytes[0] << 32) | ((long)bytes[1] << 24) | ((long)bytes[2] << 16) | ((long)bytes[3] << 8) | bytes[4];

            for (int j = 0; j < 8; j++)
            {
                result.Append(alphabet[(int)(value >> (35 - j * 5)) & 0x1F]);
            }
        }

        return result.ToString();
    }

    private static byte[] Base32Decode(string encoded)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new List<byte>();

        for (int i = 0; i < encoded.Length; i += 8)
        {
            long value = 0;
            for (int j = 0; j < 8 && i + j < encoded.Length; j++)
            {
                var index = alphabet.IndexOf(encoded[i + j]);
                if (index >= 0)
                    value = (value << 5) | (uint)index;
            }

            for (int j = 0; j < 5; j++)
            {
                result.Add((byte)(value >> (32 - j * 8)));
            }
        }

        return result.ToArray();
    }

    private class RefreshTokenInfo
    {
        public int UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}