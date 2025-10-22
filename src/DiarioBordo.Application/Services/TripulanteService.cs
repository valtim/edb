using DiarioBordo.Application.DTOs;
using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Enums;
using DiarioBordo.Domain.Repositories;
using FluentValidation;

namespace DiarioBordo.Application.Services;

/// <summary>
/// Serviço para gerenciamento de tripulantes
/// </summary>
public interface ITripulanteService
{
    Task<TripulanteDto> CriarAsync(TripulanteDto tripulanteDto);
    Task<TripulanteDto> AtualizarAsync(int id, TripulanteDto tripulanteDto, int operadorId);
    Task<TripulanteDto?> ObterPorIdAsync(int id);
    Task<TripulanteDto?> ObterPorCodigoANACAsync(string codigoANAC);
    Task<IEnumerable<TripulanteDto>> ObterPorOperadorAsync(int operadorId);
    Task<IEnumerable<TripulanteDto>> ObterPorFuncaoAsync(int operadorId, string funcao);
    Task InativarAsync(int id, int operadorId);
    Task<object?> ObterHistoricoVoosAsync(int id, DateTime? dataInicio = null, DateTime? dataFim = null);
}

public class TripulanteService : ITripulanteService
{
    private readonly ITripulanteRepository _tripulanteRepository;
    private readonly IRegistroVooRepository _registroVooRepository;
    private readonly IValidator<TripulanteDto> _validator;

    public TripulanteService(
        ITripulanteRepository tripulanteRepository,
        IRegistroVooRepository registroVooRepository,
        IValidator<TripulanteDto> validator)
    {
        _tripulanteRepository = tripulanteRepository;
        _registroVooRepository = registroVooRepository;
        _validator = validator;
    }

    public async Task<TripulanteDto> CriarAsync(TripulanteDto tripulanteDto)
    {
        // Validação do DTO
        var validationResult = await _validator.ValidateAsync(tripulanteDto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Verificar se código ANAC já existe
        var tripulanteExistente = await _tripulanteRepository.ObterPorCodigoANACAsync(tripulanteDto.CodigoANAC);
        if (tripulanteExistente != null)
        {
            throw new InvalidOperationException($"Código ANAC {tripulanteDto.CodigoANAC} já está em uso");
        }

        // Verificar se CPF já existe
        var cpfExistente = await _tripulanteRepository.ObterPorCPFAsync(tripulanteDto.CPF);
        if (cpfExistente != null)
        {
            throw new InvalidOperationException("CPF já está cadastrado");
        }

        // Mapear DTO para entidade
        var tripulante = new Tripulante
        {
            CodigoANAC = tripulanteDto.CodigoANAC,
            CPF = tripulanteDto.CPF.Replace(".", "").Replace("-", ""),
            Nome = tripulanteDto.Nome,
            Email = tripulanteDto.Email.ToLowerInvariant(),
            Funcoes = tripulanteDto.Funcoes,
            ValidadeLicenca = tripulanteDto.ValidadeLicenca,
            OperadorId = tripulanteDto.OperadorId,
            Ativo = true,
            DataCriacao = DateTime.UtcNow
        };

        await _tripulanteRepository.AdicionarAsync(tripulante);
        return MapearEntidadeParaDto(tripulante);
    }

    public async Task<TripulanteDto> AtualizarAsync(int id, TripulanteDto tripulanteDto, int operadorId)
    {
        var tripulanteExistente = await _tripulanteRepository.ObterPorIdAsync(id);
        if (tripulanteExistente == null)
        {
            throw new InvalidOperationException("Tripulante não encontrado");
        }

        if (tripulanteExistente.OperadorId != operadorId)
        {
            throw new UnauthorizedAccessException("Tripulante não pertence ao operador");
        }

        // Validação do DTO
        var validationResult = await _validator.ValidateAsync(tripulanteDto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Verificar se novo código ANAC já existe (se alterado)
        if (tripulanteDto.CodigoANAC != tripulanteExistente.CodigoANAC)
        {
            var codigoExistente = await _tripulanteRepository.ObterPorCodigoANACAsync(tripulanteDto.CodigoANAC);
            if (codigoExistente != null)
            {
                throw new InvalidOperationException($"Código ANAC {tripulanteDto.CodigoANAC} já está em uso");
            }
        }

        // Verificar se novo CPF já existe (se alterado)
        var cpfLimpo = tripulanteDto.CPF.Replace(".", "").Replace("-", "");
        if (cpfLimpo != tripulanteExistente.CPF)
        {
            var cpfExistente = await _tripulanteRepository.ObterPorCPFAsync(cpfLimpo);
            if (cpfExistente != null)
            {
                throw new InvalidOperationException("CPF já está cadastrado");
            }
        }

        // Atualizar campos
        tripulanteExistente.CodigoANAC = tripulanteDto.CodigoANAC;
        tripulanteExistente.CPF = cpfLimpo;
        tripulanteExistente.Nome = tripulanteDto.Nome;
        tripulanteExistente.Email = tripulanteDto.Email.ToLowerInvariant();
        tripulanteExistente.Funcoes = tripulanteDto.Funcoes;
        tripulanteExistente.ValidadeLicenca = tripulanteDto.ValidadeLicenca;
        tripulanteExistente.DataUltimaAtualizacao = DateTime.UtcNow;

        await _tripulanteRepository.AtualizarAsync(tripulanteExistente);
        return MapearEntidadeParaDto(tripulanteExistente);
    }

    public async Task<TripulanteDto?> ObterPorIdAsync(int id)
    {
        var tripulante = await _tripulanteRepository.ObterPorIdAsync(id);
        return tripulante != null ? MapearEntidadeParaDto(tripulante) : null;
    }

    public async Task<TripulanteDto?> ObterPorCodigoANACAsync(string codigoANAC)
    {
        var tripulante = await _tripulanteRepository.ObterPorCodigoANACAsync(codigoANAC);
        return tripulante != null ? MapearEntidadeParaDto(tripulante) : null;
    }

    public async Task<IEnumerable<TripulanteDto>> ObterPorOperadorAsync(int operadorId)
    {
        var tripulantes = await _tripulanteRepository.ObterPorOperadorAsync(operadorId);
        return tripulantes.Select(MapearEntidadeParaDto);
    }

    public async Task<IEnumerable<TripulanteDto>> ObterPorFuncaoAsync(int operadorId, string funcao)
    {
        if (!Enum.TryParse<FuncaoTripulante>(funcao, true, out var funcaoEnum))
        {
            throw new ArgumentException($"Função inválida: {funcao}");
        }

        var tripulantes = await _tripulanteRepository.ObterPorFuncaoAsync(funcaoEnum, operadorId);
        return tripulantes.Select(MapearEntidadeParaDto);
    }

    public async Task InativarAsync(int id, int operadorId)
    {
        var tripulante = await _tripulanteRepository.ObterPorIdAsync(id);
        if (tripulante == null)
        {
            throw new InvalidOperationException("Tripulante não encontrado");
        }

        if (tripulante.OperadorId != operadorId)
        {
            throw new UnauthorizedAccessException("Tripulante não pertence ao operador");
        }

        // Verificar se há registros pendentes de assinatura como piloto
        var registrosPendentes = await _registroVooRepository.ObterPendentesAssinaturaPorPilotoAsync(tripulante.Id);
        if (registrosPendentes.Any())
        {
            throw new InvalidOperationException("Não é possível inativar tripulante com registros pendentes de assinatura");
        }

        tripulante.Ativo = false;
        tripulante.DataUltimaAtualizacao = DateTime.UtcNow;

        await _tripulanteRepository.AtualizarAsync(tripulante);
    }

    public async Task<object?> ObterHistoricoVoosAsync(int id, DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        var tripulante = await _tripulanteRepository.ObterPorIdAsync(id);
        if (tripulante == null)
        {
            return null;
        }

        // Definir período padrão se não especificado (últimos 6 meses)
        dataInicio ??= DateTime.Today.AddMonths(-6);
        dataFim ??= DateTime.Today;

        // Obter registros onde o tripulante foi piloto em comando
        var registros = await _registroVooRepository.ObterPorPilotoComandoAsync(
            tripulante.Id, dataInicio.Value, dataFim.Value);

        var registrosList = registros.ToList();

        // Calcular estatísticas
        var totalVoos = registrosList.Count;
        var horasVoadas = registrosList.Sum(r => r.TempoVooTotal);
        var horasIFR = registrosList.Where(r => r.TempoVooIFR.HasValue).Sum(r => r.TempoVooIFR!.Value);

        // Estatísticas por natureza de voo
        var voosPorNatureza = registrosList
            .GroupBy(r => r.NaturezaVoo)
            .Select(g => new
            {
                Natureza = g.Key.ToString(),
                Quantidade = g.Count(),
                Horas = Math.Round(g.Sum(r => r.TempoVooTotal), 2)
            })
            .OrderByDescending(x => x.Quantidade);

        // Aeroportos mais utilizados
        var aeroportosOrigens = registrosList
            .GroupBy(r => r.LocalDecolagem)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new { Aeroporto = g.Key, Quantidade = g.Count() });

        var aeroportosDestinos = registrosList
            .GroupBy(r => r.LocalPouso)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new { Aeroporto = g.Key, Quantidade = g.Count() });

        // Histórico mensal
        var historicoMensal = registrosList
            .GroupBy(r => new { r.Data.Year, r.Data.Month })
            .Select(g => new
            {
                Ano = g.Key.Year,
                Mes = g.Key.Month,
                Voos = g.Count(),
                Horas = Math.Round(g.Sum(r => r.TempoVooTotal), 2)
            })
            .OrderBy(x => x.Ano).ThenBy(x => x.Mes);

        // Status da licença
        var hoje = DateTime.Today;
        var statusLicenca = new
        {
            ValidadeLicenca = tripulante.ValidadeLicenca,
            LicencaValida = tripulante.ValidadeLicenca.HasValue && tripulante.ValidadeLicenca > hoje,
            DiasRestantes = tripulante.ValidadeLicenca.HasValue ?
                Math.Max(0, (tripulante.ValidadeLicenca.Value - hoje).Days) : (int?)null
        };

        return new
        {
            TripulanteId = id,
            CodigoANAC = tripulante.CodigoANAC,
            Nome = tripulante.Nome,
            Periodo = new { DataInicio = dataInicio.Value, DataFim = dataFim.Value },
            StatusLicenca = statusLicenca,
            Estatisticas = new
            {
                TotalVoos = totalVoos,
                HorasVoadas = Math.Round(horasVoadas, 2),
                HorasIFR = Math.Round(horasIFR, 2),
                MediaHorasPorVoo = totalVoos > 0 ? Math.Round(horasVoadas / totalVoos, 2) : 0,
                PorcentualIFR = horasVoadas > 0 ? Math.Round(horasIFR / horasVoadas * 100, 1) : 0
            },
            VoosPorNatureza = voosPorNatureza,
            AeroportosMaisUtilizados = new
            {
                Origens = aeroportosOrigens,
                Destinos = aeroportosDestinos
            },
            HistoricoMensal = historicoMensal,
            UltimoVoo = registrosList.Any() ? registrosList.OrderByDescending(r => r.Data).First().Data : (DateTime?)null
        };
    }

    private static TripulanteDto MapearEntidadeParaDto(Tripulante tripulante)
    {
        return new TripulanteDto
        {
            Id = tripulante.Id,
            CodigoANAC = tripulante.CodigoANAC,
            CPF = FormatarCPF(tripulante.CPF),
            Nome = tripulante.Nome,
            Email = tripulante.Email ?? string.Empty,
            Funcoes = tripulante.Funcoes.ToList(),
            ValidadeLicenca = tripulante.ValidadeLicenca,
            OperadorId = tripulante.OperadorId,
            OperadorNome = "", // TODO: Implementar quando tiver serviço de operador
            Ativo = tripulante.Ativo,
            DataCriacao = tripulante.DataCriacao,
            DataUltimaAtualizacao = tripulante.DataUltimaAtualizacao
        };
    }

    private static string FormatarCPF(string cpf)
    {
        if (string.IsNullOrEmpty(cpf) || cpf.Length != 11)
            return cpf;

        return $"{cpf.Substring(0, 3)}.{cpf.Substring(3, 3)}.{cpf.Substring(6, 3)}-{cpf.Substring(9, 2)}";
    }
}