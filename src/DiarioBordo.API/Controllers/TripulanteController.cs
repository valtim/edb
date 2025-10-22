using DiarioBordo.Application.DTOs;
using DiarioBordo.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiarioBordo.API.Controllers;

/// <summary>
/// Controller para gerenciamento de tripulantes
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class TripulanteController : ControllerBase
{
    private readonly ITripulanteService _tripulanteService;
    private readonly ILogger<TripulanteController> _logger;

    public TripulanteController(ITripulanteService tripulanteService, ILogger<TripulanteController> logger)
    {
        _tripulanteService = tripulanteService;
        _logger = logger;
    }

    /// <summary>
    /// Obter todos os tripulantes do operador
    /// </summary>
    /// <returns>Lista de tripulantes</returns>
    /// <response code="200">Lista de tripulantes do operador</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TripulanteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TripulanteDto>>> ObterTodos()
    {
        var operadorId = GetOperadorId();
        var tripulantes = await _tripulanteService.ObterPorOperadorAsync(operadorId);
        return Ok(tripulantes);
    }

    /// <summary>
    /// Obter tripulante por ID
    /// </summary>
    /// <param name="id">ID do tripulante</param>
    /// <returns>Dados do tripulante</returns>
    /// <response code="200">Tripulante encontrado</response>
    /// <response code="404">Tripulante não encontrado</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TripulanteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripulanteDto>> ObterPorId(int id)
    {
        var tripulante = await _tripulanteService.ObterPorIdAsync(id);
        if (tripulante == null)
        {
            return NotFound();
        }
        return Ok(tripulante);
    }

    /// <summary>
    /// Obter tripulante por código ANAC
    /// </summary>
    /// <param name="codigoANAC">Código ANAC de 6 dígitos</param>
    /// <returns>Dados do tripulante</returns>
    /// <response code="200">Tripulante encontrado</response>
    /// <response code="404">Tripulante não encontrado</response>
    [HttpGet("codigo-anac/{codigoANAC}")]
    [ProducesResponseType(typeof(TripulanteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripulanteDto>> ObterPorCodigoANAC(string codigoANAC)
    {
        var tripulante = await _tripulanteService.ObterPorCodigoANACAsync(codigoANAC);
        if (tripulante == null)
        {
            return NotFound();
        }
        return Ok(tripulante);
    }

    /// <summary>
    /// Obter tripulantes por função
    /// </summary>
    /// <param name="funcao">Função do tripulante (P, I, O, C, M)</param>
    /// <returns>Lista de tripulantes com a função especificada</returns>
    /// <response code="200">Lista de tripulantes</response>
    [HttpGet("funcao/{funcao}")]
    [ProducesResponseType(typeof(IEnumerable<TripulanteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TripulanteDto>>> ObterPorFuncao(string funcao)
    {
        var operadorId = GetOperadorId();
        var tripulantes = await _tripulanteService.ObterPorFuncaoAsync(operadorId, funcao);
        return Ok(tripulantes);
    }

    /// <summary>
    /// Criar novo tripulante
    /// </summary>
    /// <param name="tripulanteDto">Dados do tripulante</param>
    /// <returns>Tripulante criado</returns>
    /// <response code="201">Tripulante criado com sucesso</response>
    /// <response code="400">Dados inválidos ou código ANAC já existe</response>
    [HttpPost]
    [Authorize(Roles = "Operador,DiretorOperacoes")]
    [ProducesResponseType(typeof(TripulanteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TripulanteDto>> CriarTripulante([FromBody] TripulanteDto tripulanteDto)
    {
        try
        {
            var operadorId = GetOperadorId();
            tripulanteDto.OperadorId = operadorId;

            var tripulante = await _tripulanteService.CriarAsync(tripulanteDto);

            _logger.LogInformation("Tripulante {CodigoANAC} criado por operador {OperadorId}",
                tripulante.CodigoANAC, operadorId);

            return CreatedAtAction(nameof(ObterPorId), new { id = tripulante.Id }, tripulante);
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
    /// Atualizar dados do tripulante
    /// </summary>
    /// <param name="id">ID do tripulante</param>
    /// <param name="tripulanteDto">Dados atualizados</param>
    /// <returns>Tripulante atualizado</returns>
    /// <response code="200">Tripulante atualizado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="404">Tripulante não encontrado</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Operador,DiretorOperacoes")]
    [ProducesResponseType(typeof(TripulanteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TripulanteDto>> AtualizarTripulante(int id, [FromBody] TripulanteDto tripulanteDto)
    {
        try
        {
            var operadorId = GetOperadorId();
            var tripulante = await _tripulanteService.AtualizarAsync(id, tripulanteDto, operadorId);

            _logger.LogInformation("Tripulante {Id} atualizado por operador {OperadorId}", id, operadorId);

            return Ok(tripulante);
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
    /// Inativar tripulante
    /// </summary>
    /// <param name="id">ID do tripulante</param>
    /// <returns>Confirmação da inativação</returns>
    /// <response code="204">Tripulante inativado com sucesso</response>
    /// <response code="400">Tripulante possui registros ativos</response>
    /// <response code="404">Tripulante não encontrado</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "DiretorOperacoes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InativarTripulante(int id)
    {
        try
        {
            var operadorId = GetOperadorId();
            await _tripulanteService.InativarAsync(id, operadorId);

            _logger.LogInformation("Tripulante {Id} inativado por operador {OperadorId}", id, operadorId);

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
    /// Verificar validade da licença do tripulante
    /// </summary>
    /// <param name="id">ID do tripulante</param>
    /// <returns>Status da licença</returns>
    /// <response code="200">Status da licença</response>
    /// <response code="404">Tripulante não encontrado</response>
    [HttpGet("{id:int}/licenca/status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> VerificarLicenca(int id)
    {
        var tripulante = await _tripulanteService.ObterPorIdAsync(id);
        if (tripulante == null)
        {
            return NotFound();
        }

        var hoje = DateTime.Today;
        var status = new
        {
            TripulanteId = id,
            CodigoANAC = tripulante.CodigoANAC,
            Nome = tripulante.Nome,
            ValidadeLicenca = tripulante.ValidadeLicenca,
            LicencaValida = tripulante.ValidadeLicenca.HasValue && tripulante.ValidadeLicenca > hoje,
            DiasRestantes = tripulante.ValidadeLicenca.HasValue ?
                Math.Max(0, (tripulante.ValidadeLicenca.Value - hoje).Days) : (int?)null,
            Status = tripulante.ValidadeLicenca.HasValue
                ? (tripulante.ValidadeLicenca > hoje ? "VÁLIDA" : "VENCIDA")
                : "NÃO_INFORMADA"
        };

        return Ok(status);
    }

    /// <summary>
    /// Obter histórico de voos do tripulante
    /// </summary>
    /// <param name="id">ID do tripulante</param>
    /// <param name="dataInicio">Data inicial (opcional)</param>
    /// <param name="dataFim">Data final (opcional)</param>
    /// <returns>Histórico de voos</returns>
    /// <response code="200">Histórico de voos</response>
    /// <response code="404">Tripulante não encontrado</response>
    [HttpGet("{id:int}/historico-voos")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ObterHistoricoVoos(int id, DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        var historico = await _tripulanteService.ObterHistoricoVoosAsync(id, dataInicio, dataFim);
        if (historico == null)
        {
            return NotFound();
        }
        return Ok(historico);
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