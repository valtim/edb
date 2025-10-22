using DiarioBordo.Application.DTOs;
using DiarioBordo.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioBordo.API.Controllers;

/// <summary>
/// Controller para gerenciamento de aeronaves
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class AeronaveController : ControllerBase
{
    private readonly IAeronaveService _aeronaveService;
    private readonly ILogger<AeronaveController> _logger;

    public AeronaveController(IAeronaveService aeronaveService, ILogger<AeronaveController> logger)
    {
        _aeronaveService = aeronaveService;
        _logger = logger;
    }

    /// <summary>
    /// Obter todas as aeronaves do operador
    /// </summary>
    /// <returns>Lista de aeronaves</returns>
    /// <response code="200">Lista de aeronaves do operador</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AeronaveDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AeronaveDto>>> ObterTodas()
    {
        var operadorId = GetOperadorId();
        var aeronaves = await _aeronaveService.ObterPorOperadorAsync(operadorId);
        return Ok(aeronaves);
    }

    /// <summary>
    /// Obter aeronave por ID
    /// </summary>
    /// <param name="id">ID da aeronave</param>
    /// <returns>Dados da aeronave</returns>
    /// <response code="200">Aeronave encontrada</response>
    /// <response code="404">Aeronave não encontrada</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AeronaveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AeronaveDto>> ObterPorId(int id)
    {
        var aeronave = await _aeronaveService.ObterPorIdAsync(id);
        if (aeronave == null)
        {
            return NotFound();
        }
        return Ok(aeronave);
    }

    /// <summary>
    /// Obter aeronave por matrícula RAB
    /// </summary>
    /// <param name="matricula">Matrícula da aeronave (PR-XXX, PP-XXX, etc.)</param>
    /// <returns>Dados da aeronave</returns>
    /// <response code="200">Aeronave encontrada</response>
    /// <response code="404">Aeronave não encontrada</response>
    [HttpGet("matricula/{matricula}")]
    [ProducesResponseType(typeof(AeronaveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AeronaveDto>> ObterPorMatricula(string matricula)
    {
        var aeronave = await _aeronaveService.ObterPorMatriculaAsync(matricula);
        if (aeronave == null)
        {
            return NotFound();
        }
        return Ok(aeronave);
    }

    /// <summary>
    /// Criar nova aeronave
    /// </summary>
    /// <param name="aeronaveDto">Dados da aeronave</param>
    /// <returns>Aeronave criada</returns>
    /// <response code="201">Aeronave criada com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    [HttpPost]
    [Authorize(Roles = "Operador,DiretorOperacoes")]
    [ProducesResponseType(typeof(AeronaveDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AeronaveDto>> CriarAeronave([FromBody] AeronaveDto aeronaveDto)
    {
        try
        {
            var operadorId = GetOperadorId();
            aeronaveDto.OperadorId = operadorId;

            var aeronave = await _aeronaveService.CriarAsync(aeronaveDto);

            _logger.LogInformation("Aeronave {Matricula} criada por operador {OperadorId}",
                aeronave.Matricula, operadorId);

            return CreatedAtAction(nameof(ObterPorId), new { id = aeronave.Id }, aeronave);
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
    }

    /// <summary>
    /// Atualizar dados da aeronave
    /// </summary>
    /// <param name="id">ID da aeronave</param>
    /// <param name="aeronaveDto">Dados atualizados</param>
    /// <returns>Aeronave atualizada</returns>
    /// <response code="200">Aeronave atualizada com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="404">Aeronave não encontrada</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Operador,DiretorOperacoes")]
    [ProducesResponseType(typeof(AeronaveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AeronaveDto>> AtualizarAeronave(int id, [FromBody] AeronaveDto aeronaveDto)
    {
        try
        {
            var operadorId = GetOperadorId();
            var aeronave = await _aeronaveService.AtualizarAsync(id, aeronaveDto, operadorId);

            _logger.LogInformation("Aeronave {Id} atualizada por operador {OperadorId}", id, operadorId);

            return Ok(aeronave);
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
    /// Inativar aeronave
    /// </summary>
    /// <param name="id">ID da aeronave</param>
    /// <returns>Confirmação da inativação</returns>
    /// <response code="204">Aeronave inativada com sucesso</response>
    /// <response code="400">Aeronave possui registros ativos</response>
    /// <response code="404">Aeronave não encontrada</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "DiretorOperacoes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InativarAeronave(int id)
    {
        try
        {
            var operadorId = GetOperadorId();
            await _aeronaveService.InativarAsync(id, operadorId);

            _logger.LogInformation("Aeronave {Id} inativada por operador {OperadorId}", id, operadorId);

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
    /// Obter estatísticas da aeronave
    /// </summary>
    /// <param name="id">ID da aeronave</param>
    /// <returns>Estatísticas de voo</returns>
    /// <response code="200">Estatísticas da aeronave</response>
    /// <response code="404">Aeronave não encontrada</response>
    [HttpGet("{id:int}/estatisticas")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ObterEstatisticas(int id)
    {
        var estatisticas = await _aeronaveService.ObterEstatisticasAsync(id);
        if (estatisticas == null)
        {
            return NotFound();
        }
        return Ok(estatisticas);
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
}