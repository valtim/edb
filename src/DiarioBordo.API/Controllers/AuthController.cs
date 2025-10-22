using DiarioBordo.API.Identity;
using DiarioBordo.API.Identity.DTOs;
using DiarioBordo.API.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DiarioBordo.API.Controllers;

/// <summary>
/// Controller de autenticação e autorização
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Realizar login no sistema
    /// </summary>
    /// <param name="loginDto">Credenciais de login</param>
    /// <returns>Token JWT e informações do usuário</returns>
    /// <response code="200">Login realizado com sucesso</response>
    /// <response code="401">Credenciais inválidas</response>
    /// <response code="423">Conta bloqueada temporariamente</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status423Locked)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            var clientIP = GetClientIP();
            _logger.LogInformation("Tentativa de login para {Email} de {IP}", loginDto.EmailOrCodigoANAC, clientIP);

            var result = await _authService.LoginAsync(loginDto);

            if (result.RequiresTwoFactor)
            {
                return Ok(result);
            }

            _logger.LogInformation("Login bem-sucedido para usuário {UserId}", result.Usuario.Id);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Falha no login para {Email}: {Message}", loginDto.EmailOrCodigoANAC, ex.Message);

            if (ex.Message.Contains("bloqueada"))
            {
                return StatusCode(StatusCodes.Status423Locked, new ProblemDetails
                {
                    Title = "Conta bloqueada",
                    Detail = ex.Message,
                    Status = StatusCodes.Status423Locked
                });
            }

            return Unauthorized(new ProblemDetails
            {
                Title = "Credenciais inválidas",
                Detail = "Email/Código ANAC ou senha incorretos",
                Status = StatusCodes.Status401Unauthorized
            });
        }
    }

    /// <summary>
    /// Registrar novo usuário
    /// </summary>
    /// <param name="registerDto">Dados do novo usuário</param>
    /// <returns>Informações do usuário criado</returns>
    /// <response code="201">Usuário criado com sucesso</response>
    /// <response code="400">Dados inválidos ou usuário já existe</response>
    [HttpPost("register")]
    [Authorize(Roles = ApplicationRole.Roles.Administrador)]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserInfoDto>> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            var user = await _authService.RegisterAsync(registerDto);

            _logger.LogInformation("Usuário {UserId} registrado com sucesso", user.Id);

            return CreatedAtAction(nameof(GetUserInfo), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Erro ao registrar usuário: {Message}", ex.Message);

            return BadRequest(new ProblemDetails
            {
                Title = "Erro no registro",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Renovar token de acesso usando refresh token
    /// </summary>
    /// <param name="refreshTokenDto">Refresh token</param>
    /// <returns>Novo token de acesso</returns>
    /// <response code="200">Token renovado com sucesso</response>
    /// <response code="401">Refresh token inválido ou expirado</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Token inválido",
                Detail = ex.Message,
                Status = StatusCodes.Status401Unauthorized
            });
        }
    }

    /// <summary>
    /// Realizar logout e invalidar tokens
    /// </summary>
    /// <returns>Confirmação do logout</returns>
    /// <response code="200">Logout realizado com sucesso</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var userId = GetUserId();
        await _authService.LogoutAsync(userId);

        _logger.LogInformation("Logout realizado para usuário {UserId}", userId);

        return Ok(new { message = "Logout realizado com sucesso" });
    }

    /// <summary>
    /// Alterar senha do usuário
    /// </summary>
    /// <param name="changePasswordDto">Dados para alteração de senha</param>
    /// <returns>Confirmação da alteração</returns>
    /// <response code="200">Senha alterada com sucesso</response>
    /// <response code="400">Senha atual incorreta ou nova senha inválida</response>
    [HttpPut("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        var userId = GetUserId();
        var success = await _authService.ChangePasswordAsync(userId, changePasswordDto);

        if (success)
        {
            _logger.LogInformation("Senha alterada para usuário {UserId}", userId);
            return Ok(new { message = "Senha alterada com sucesso" });
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Erro ao alterar senha",
            Detail = "Senha atual incorreta ou nova senha não atende aos critérios",
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Configurar autenticação de dois fatores (2FA)
    /// </summary>
    /// <returns>QR Code e códigos de recuperação</returns>
    /// <response code="200">Configuração 2FA gerada</response>
    [HttpPost("2fa/setup")]
    [Authorize]
    [ProducesResponseType(typeof(TwoFactorSetupDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TwoFactorSetupDto>> SetupTwoFactor()
    {
        var userId = GetUserId();
        var setup = await _authService.SetupTwoFactorAsync(userId);

        _logger.LogInformation("Configuração 2FA iniciada para usuário {UserId}", userId);

        return Ok(setup);
    }

    /// <summary>
    /// Habilitar autenticação de dois fatores
    /// </summary>
    /// <param name="verifyDto">Código de verificação</param>
    /// <returns>Confirmação da habilitação</returns>
    /// <response code="200">2FA habilitado com sucesso</response>
    /// <response code="400">Código de verificação inválido</response>
    [HttpPost("2fa/enable")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnableTwoFactor([FromBody] VerifyTwoFactorDto verifyDto)
    {
        var userId = GetUserId();
        var success = await _authService.EnableTwoFactorAsync(userId, verifyDto.Codigo);

        if (success)
        {
            _logger.LogInformation("2FA habilitado para usuário {UserId}", userId);
            return Ok(new { message = "Autenticação de dois fatores habilitada com sucesso" });
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Código inválido",
            Detail = "Código de verificação incorreto",
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Desabilitar autenticação de dois fatores
    /// </summary>
    /// <param name="passwordDto">Senha para confirmação</param>
    /// <returns>Confirmação da desabilitação</returns>
    /// <response code="200">2FA desabilitado com sucesso</response>
    /// <response code="400">Senha incorreta</response>
    [HttpPost("2fa/disable")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DisableTwoFactor([FromBody] PasswordConfirmationDto passwordDto)
    {
        var userId = GetUserId();
        var success = await _authService.DisableTwoFactorAsync(userId, passwordDto.Password);

        if (success)
        {
            _logger.LogInformation("2FA desabilitado para usuário {UserId}", userId);
            return Ok(new { message = "Autenticação de dois fatores desabilitada" });
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Senha incorreta",
            Detail = "Senha fornecida está incorreta",
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Verificar código 2FA durante login
    /// </summary>
    /// <param name="verifyDto">Código de verificação ou código de recuperação</param>
    /// <returns>Confirmação da verificação</returns>
    /// <response code="200">Código verificado com sucesso</response>
    /// <response code="400">Código inválido</response>
    [HttpPost("2fa/verify")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorDto verifyDto)
    {
        var userId = GetUserId();
        var success = await _authService.VerifyTwoFactorAsync(userId, verifyDto);

        if (success)
        {
            return Ok(new { message = "Código verificado com sucesso" });
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Código inválido",
            Detail = "Código de verificação ou recuperação incorreto",
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Obter informações do usuário atual
    /// </summary>
    /// <returns>Dados do usuário</returns>
    /// <response code="200">Informações do usuário</response>
    /// <response code="404">Usuário não encontrado</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserInfoDto>> GetCurrentUser()
    {
        var userId = GetUserId();
        var user = await _authService.GetUserInfoAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>
    /// Obter informações de um usuário específico
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <returns>Dados do usuário</returns>
    /// <response code="200">Informações do usuário</response>
    /// <response code="404">Usuário não encontrado</response>
    [HttpGet("users/{id:int}")]
    [Authorize(Roles = ApplicationRole.Roles.Administrador)]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserInfoDto>> GetUserInfo(int id)
    {
        var user = await _authService.GetUserInfoAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>
    /// Verificar se o usuário tem determinado papel
    /// </summary>
    /// <param name="role">Nome do papel</param>
    /// <returns>Resultado da verificação</returns>
    /// <response code="200">Resultado da verificação</response>
    [HttpGet("check-role/{role}")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult CheckRole(string role)
    {
        var hasRole = User.IsInRole(role);
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        return Ok(new
        {
            HasRole = hasRole,
            RequestedRole = role,
            UserRoles = userRoles
        });
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Usuário não identificado no token");
        }
        return userId;
    }

    private string GetClientIP()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// DTO para refresh token
/// </summary>
public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// DTO para confirmação de senha
/// </summary>
public class PasswordConfirmationDto
{
    public string Password { get; set; } = string.Empty;
}