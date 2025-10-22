using DiarioBordo.Application.DTOs;
using DiarioBordo.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DiarioBordo.API.Controllers;

/// <summary>
/// Controller para gerenciamento de registros de voo
/// Conformidade com Resoluções ANAC 457/2017 e 458/2017
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class RegistroVooController : ControllerBase
{
    private readonly IRegistroVooService _registroVooService;
    private readonly IAssinaturaService _assinaturaService;
    private readonly ILogger<RegistroVooController> _logger;

    public RegistroVooController(
        IRegistroVooService registroVooService,
        IAssinaturaService assinaturaService,
        ILogger<RegistroVooController> logger)
    {
        _registroVooService = registroVooService;
        _assinaturaService = assinaturaService;
        _logger = logger;
    }

    /// <summary>
    /// Criar novo registro de voo
    /// </summary>
    /// <param name="registroDto">Dados do registro com os 17 campos obrigatórios Art. 4º Res. 457/2017</param>
    /// <returns>Registro criado</returns>
    /// <response code="201">Registro criado com sucesso</response>
    /// <response code="400">Dados inválidos ou violação de regras ANAC</response>
    /// <response code="401">Não autorizado</response>
    /// <response code="403">Sem permissão para esta operação</response>
    [HttpPost]
    [ProducesResponseType(typeof(RegistroVooDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RegistroVooDto>> CriarRegistro([FromBody] RegistroVooDto registroDto)
    {
        try
        {
            var operadorId = GetOperadorId();
            var registro = await _registroVooService.CriarRegistroAsync(registroDto, operadorId);

            _logger.LogInformation("Registro de voo {NumeroSequencial} criado para aeronave {AeronaveId} por operador {OperadorId}",
                registro.NumeroSequencial, registro.AeronaveId, operadorId);

            return CreatedAtAction(nameof(ObterPorId), new { id = registro.Id }, registro);
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning("Validação falhou ao criar registro: {Errors}", string.Join(", ", ex.Errors));
            return BadRequest(new ProblemDetails
            {
                Title = "Dados inválidos",
                Detail = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)),
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Operação inválida ao criar registro: {Message}", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Operação inválida",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Obter registro de voo por ID
    /// </summary>
    /// <param name="id">ID do registro</param>
    /// <returns>Dados completos do registro</returns>
    /// <response code="200">Registro encontrado</response>
    /// <response code="404">Registro não encontrado</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RegistroVooDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistroVooDto>> ObterPorId(int id)
    {
        var registro = await _registroVooService.ObterPorIdAsync(id);
        if (registro == null)
        {
            return NotFound();
        }

        return Ok(registro);
    }

    /// <summary>
    /// Obter registros dos últimos 30 dias para uma aeronave
    /// Conforme Art. 8º II Res. 457/2017 - Disponibilidade obrigatória
    /// </summary>
    /// <param name="aeronaveId">ID da aeronave</param>
    /// <returns>Lista de registros dos últimos 30 dias</returns>
    /// <response code="200">Lista de registros (pode ser vazia)</response>
    [HttpGet("aeronave/{aeronaveId:int}/ultimos-30-dias")]
    [ProducesResponseType(typeof(IEnumerable<RegistroVooDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RegistroVooDto>>> ObterUltimos30Dias(int aeronaveId)
    {
        var registros = await _registroVooService.ObterUltimos30DiasAsync(aeronaveId);
        return Ok(registros);
    }

    /// <summary>
    /// Obter registros pendentes de assinatura do operador
    /// Filtrados por prazos RBAC (121: 2 dias, 135: 15 dias, outros: 30 dias)
    /// </summary>
    /// <returns>Lista de registros pendentes</returns>
    /// <response code="200">Lista de registros pendentes</response>
    [HttpGet("pendentes-assinatura")]
    [Authorize(Roles = "Operador,DiretorOperacoes")]
    [ProducesResponseType(typeof(IEnumerable<RegistroVooDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RegistroVooDto>>> ObterPendentesAssinatura()
    {
        var operadorId = GetOperadorId();
        var registros = await _registroVooService.ObterPendentesAssinaturaAsync(operadorId);
        return Ok(registros);
    }

    /// <summary>
    /// Atualizar registro de voo
    /// Só permitido se ainda não foi assinado
    /// </summary>
    /// <param name="id">ID do registro</param>
    /// <param name="registroDto">Dados atualizados</param>
    /// <returns>Registro atualizado</returns>
    /// <response code="200">Registro atualizado com sucesso</response>
    /// <response code="400">Dados inválidos ou registro já assinado</response>
    /// <response code="404">Registro não encontrado</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(RegistroVooDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistroVooDto>> AtualizarRegistro(int id, [FromBody] RegistroVooDto registroDto)
    {
        try
        {
            var operadorId = GetOperadorId();
            var registro = await _registroVooService.AtualizarRegistroAsync(id, registroDto, operadorId);

            _logger.LogInformation("Registro de voo {Id} atualizado por operador {OperadorId}", id, operadorId);

            return Ok(registro);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Dados inválidos",
                Detail = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)),
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Operação inválida",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Excluir registro de voo
    /// Só permitido se ainda não foi assinado
    /// </summary>
    /// <param name="id">ID do registro</param>
    /// <returns>Confirmação da exclusão</returns>
    /// <response code="204">Registro excluído com sucesso</response>
    /// <response code="400">Registro já assinado, não pode ser excluído</response>
    /// <response code="404">Registro não encontrado</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Operador,DiretorOperacoes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExcluirRegistro(int id)
    {
        try
        {
            var operadorId = GetOperadorId();
            await _registroVooService.ExcluirRegistroAsync(id, operadorId);

            _logger.LogInformation("Registro de voo {Id} excluído por operador {OperadorId}", id, operadorId);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Operação inválida",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Assinar registro como piloto
    /// Conforme Res. 458/2017 - Assinatura digital
    /// </summary>
    /// <param name="id">ID do registro</param>
    /// <returns>Dados da assinatura criada</returns>
    /// <response code="200">Assinatura realizada com sucesso</response>
    /// <response code="400">Erro na assinatura ou registro já assinado</response>
    /// <response code="404">Registro não encontrado</response>
    [HttpPost("{id:int}/assinar-piloto")]
    [Authorize(Roles = "Piloto")]
    [ProducesResponseType(typeof(AssinaturaRegistroDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssinaturaRegistroDto>> AssinarComoPiloto(int id)
    {
        try
        {
            var codigoANAC = GetCodigoANAC();
            var enderecoIP = GetClientIP();
            var userAgent = Request.Headers.UserAgent.ToString();

            var assinatura = await _assinaturaService.AssinarRegistroPilotoAsync(id, codigoANAC, enderecoIP, userAgent);

            _logger.LogInformation("Registro {RegistroId} assinado pelo piloto {CodigoANAC}", id, codigoANAC);

            return Ok(assinatura);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Erro na assinatura",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Assinar registro como operador
    /// Prazos por tipo RBAC: 121 (2 dias), 135 (15 dias), outros (30 dias)
    /// </summary>
    /// <param name="id">ID do registro</param>
    /// <returns>Dados da assinatura criada</returns>
    /// <response code="200">Assinatura realizada com sucesso</response>
    /// <response code="400">Erro na assinatura, prazo vencido ou piloto não assinou</response>
    /// <response code="404">Registro não encontrado</response>
    [HttpPost("{id:int}/assinar-operador")]
    [Authorize(Roles = "Operador,DiretorOperacoes")]
    [ProducesResponseType(typeof(AssinaturaRegistroDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssinaturaRegistroDto>> AssinarComoOperador(int id)
    {
        try
        {
            var codigoANAC = GetCodigoANAC();
            var enderecoIP = GetClientIP();
            var userAgent = Request.Headers.UserAgent.ToString();

            var assinatura = await _assinaturaService.AssinarRegistroOperadorAsync(id, codigoANAC, enderecoIP, userAgent);

            _logger.LogInformation("Registro {RegistroId} assinado pelo operador {CodigoANAC}", id, codigoANAC);

            return Ok(assinatura);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Erro na assinatura",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Obter histórico de assinaturas de um registro
    /// Para auditoria e rastreabilidade
    /// </summary>
    /// <param name="id">ID do registro</param>
    /// <returns>Lista de assinaturas do registro</returns>
    /// <response code="200">Lista de assinaturas</response>
    /// <response code="404">Registro não encontrado</response>
    [HttpGet("{id:int}/assinaturas")]
    [ProducesResponseType(typeof(IEnumerable<AssinaturaRegistroDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<AssinaturaRegistroDto>>> ObterAssinaturasRegistro(int id)
    {
        var assinaturas = await _assinaturaService.ObterAssinaturasRegistroAsync(id);
        return Ok(assinaturas);
    }

    /// <summary>
    /// Validar integridade das assinaturas de um registro
    /// Verificação de hash SHA-256 para conformidade ANAC
    /// </summary>
    /// <param name="id">ID do registro</param>
    /// <returns>Status de validação das assinaturas</returns>
    /// <response code="200">Status de validação</response>
    /// <response code="404">Registro não encontrado</response>
    [HttpPost("{id:int}/validar-assinaturas")]
    [Authorize(Roles = "Operador,DiretorOperacoes,Fiscalizacao")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ValidarAssinaturasRegistro(int id)
    {
        var assinaturas = await _assinaturaService.ObterAssinaturasRegistroAsync(id);
        var resultados = new List<object>();

        foreach (var assinatura in assinaturas)
        {
            var valida = await _assinaturaService.ValidarAssinaturaAsync(assinatura.Id);
            resultados.Add(new
            {
                assinatura.Id,
                assinatura.TipoAssinatura,
                assinatura.CodigoANAC,
                assinatura.DataAssinatura,
                Valida = valida,
                Status = valida ? "ÍNTEGRA" : "COMPROMETIDA"
            });
        }

        return Ok(new { RegistroId = id, Assinaturas = resultados });
    }

    private int GetOperadorId()
    {
        var operadorIdClaim = User.FindFirst("OperadorId")?.Value;
        if (string.IsNullOrEmpty(operadorIdClaim) || !int.TryParse(operadorIdClaim, out var operadorId))
        {
            throw new UnauthorizedAccessException("Operador não identificado no token");
        }
        return operadorId;
    }

    private string GetCodigoANAC()
    {
        var codigoANAC = User.FindFirst("CodigoANAC")?.Value;
        if (string.IsNullOrEmpty(codigoANAC))
        {
            throw new UnauthorizedAccessException("Código ANAC não identificado no token");
        }
        return codigoANAC;
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