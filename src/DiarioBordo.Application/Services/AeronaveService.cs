using DiarioBordo.Application.DTOs;
using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Repositories;
using FluentValidation;

namespace DiarioBordo.Application.Services;

/// <summary>
/// Serviço para gerenciamento de aeronaves
/// </summary>
public interface IAeronaveService
{
    Task<AeronaveDto> CriarAsync(AeronaveDto aeronaveDto);
    Task<AeronaveDto> AtualizarAsync(int id, AeronaveDto aeronaveDto, int operadorId);
    Task<AeronaveDto?> ObterPorIdAsync(int id);
    Task<AeronaveDto?> ObterPorMatriculaAsync(string matricula);
    Task<IEnumerable<AeronaveDto>> ObterPorOperadorAsync(int operadorId);
    Task InativarAsync(int id, int operadorId);
    Task<object?> ObterEstatisticasAsync(int id);
}

public class AeronaveService : IAeronaveService
{
    private readonly IAeronaveRepository _aeronaveRepository;
    private readonly IRegistroVooRepository _registroVooRepository;
    private readonly IValidator<AeronaveDto> _validator;

    public AeronaveService(
        IAeronaveRepository aeronaveRepository,
        IRegistroVooRepository registroVooRepository,
        IValidator<AeronaveDto> validator)
    {
        _aeronaveRepository = aeronaveRepository;
        _registroVooRepository = registroVooRepository;
        _validator = validator;
    }

    public async Task<AeronaveDto> CriarAsync(AeronaveDto aeronaveDto)
    {
        // Validação do DTO
        var validationResult = await _validator.ValidateAsync(aeronaveDto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Verificar se matrícula já existe
        var aeronaveExistente = await _aeronaveRepository.ObterPorMatriculaAsync(aeronaveDto.Matricula);
        if (aeronaveExistente != null)
        {
            throw new InvalidOperationException($"Matrícula {aeronaveDto.Matricula} já está em uso");
        }

        // Mapear DTO para entidade
        var aeronave = new Aeronave
        {
            Matricula = aeronaveDto.Matricula.ToUpperInvariant(),
            Modelo = aeronaveDto.Modelo,
            Fabricante = aeronaveDto.Fabricante,
            NumeroSerie = aeronaveDto.NumeroSerie,
            AnoFabricacao = aeronaveDto.AnoFabricacao,
            HorasTotaisCelula = aeronaveDto.HorasTotaisCelula ?? 0,
            TipoRBAC = aeronaveDto.TipoRBAC,
            OperadorId = aeronaveDto.OperadorId,
            Ativo = true,
            DataCriacao = DateTime.UtcNow
        };

        await _aeronaveRepository.AdicionarAsync(aeronave);
        return MapearEntidadeParaDto(aeronave);
    }

    public async Task<AeronaveDto> AtualizarAsync(int id, AeronaveDto aeronaveDto, int operadorId)
    {
        var aeronaveExistente = await _aeronaveRepository.ObterPorIdAsync(id);
        if (aeronaveExistente == null)
        {
            throw new InvalidOperationException("Aeronave não encontrada");
        }

        if (aeronaveExistente.OperadorId != operadorId)
        {
            throw new UnauthorizedAccessException("Aeronave não pertence ao operador");
        }

        // Validação do DTO
        var validationResult = await _validator.ValidateAsync(aeronaveDto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Verificar se nova matrícula já existe (se alterada)
        if (aeronaveDto.Matricula.ToUpperInvariant() != aeronaveExistente.Matricula)
        {
            var matriculaExistente = await _aeronaveRepository.ObterPorMatriculaAsync(aeronaveDto.Matricula);
            if (matriculaExistente != null)
            {
                throw new InvalidOperationException($"Matrícula {aeronaveDto.Matricula} já está em uso");
            }
        }

        // Atualizar campos
        aeronaveExistente.Matricula = aeronaveDto.Matricula.ToUpperInvariant();
        aeronaveExistente.Modelo = aeronaveDto.Modelo;
        aeronaveExistente.Fabricante = aeronaveDto.Fabricante;
        aeronaveExistente.NumeroSerie = aeronaveDto.NumeroSerie;
        aeronaveExistente.AnoFabricacao = aeronaveDto.AnoFabricacao;
        aeronaveExistente.HorasTotaisCelula = aeronaveDto.HorasTotaisCelula ?? 0;
        aeronaveExistente.TipoRBAC = aeronaveDto.TipoRBAC;
        aeronaveExistente.DataUltimaAtualizacao = DateTime.UtcNow;

        await _aeronaveRepository.AtualizarAsync(aeronaveExistente);
        return MapearEntidadeParaDto(aeronaveExistente);
    }

    public async Task<AeronaveDto?> ObterPorIdAsync(int id)
    {
        var aeronave = await _aeronaveRepository.ObterPorIdAsync(id);
        return aeronave != null ? MapearEntidadeParaDto(aeronave) : null;
    }

    public async Task<AeronaveDto?> ObterPorMatriculaAsync(string matricula)
    {
        var aeronave = await _aeronaveRepository.ObterPorMatriculaAsync(matricula);
        return aeronave != null ? MapearEntidadeParaDto(aeronave) : null;
    }

    public async Task<IEnumerable<AeronaveDto>> ObterPorOperadorAsync(int operadorId)
    {
        var aeronaves = await _aeronaveRepository.ObterPorOperadorAsync(operadorId);
        return aeronaves.Select(MapearEntidadeParaDto);
    }

    public async Task InativarAsync(int id, int operadorId)
    {
        var aeronave = await _aeronaveRepository.ObterPorIdAsync(id);
        if (aeronave == null)
        {
            throw new InvalidOperationException("Aeronave não encontrada");
        }

        if (aeronave.OperadorId != operadorId)
        {
            throw new UnauthorizedAccessException("Aeronave não pertence ao operador");
        }

        // Verificar se há registros pendentes de assinatura
        var registrosPendentes = await _registroVooRepository.ObterPendentesAssinaturaPorAeronaveAsync(id);
        if (registrosPendentes.Any())
        {
            throw new InvalidOperationException("Não é possível inativar aeronave com registros pendentes de assinatura");
        }

        aeronave.Ativo = false;
        aeronave.DataUltimaAtualizacao = DateTime.UtcNow;

        await _aeronaveRepository.AtualizarAsync(aeronave);
    }

    public async Task<object?> ObterEstatisticasAsync(int id)
    {
        var aeronave = await _aeronaveRepository.ObterPorIdAsync(id);
        if (aeronave == null)
        {
            return null;
        }

        // Obter registros dos últimos 30 dias
        var registros30Dias = await _registroVooRepository.ObterUltimos30DiasAsync(id);
        var registrosList = registros30Dias.ToList();

        // Calcular estatísticas
        var totalVoos = registrosList.Count;
        var horasVoadas = registrosList.Sum(r => r.TempoVooTotal);
        var combustivelTotal = registrosList.Sum(r => r.CombustivelQuantidade);

        // Estatísticas de assinatura
        var registrosAssinados = registrosList.Count(r => r.StatusAssinaturaPiloto && r.StatusAssinaturaOperador);
        var registrosPendentes = registrosList.Count(r => !r.StatusAssinaturaPiloto || !r.StatusAssinaturaOperador);

        // Aeroportos mais utilizados
        var aeroportosDecolagem = registrosList
            .GroupBy(r => r.LocalDecolagem)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new { Aeroporto = g.Key, Quantidade = g.Count() });

        var aeroportosPouso = registrosList
            .GroupBy(r => r.LocalPouso)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new { Aeroporto = g.Key, Quantidade = g.Count() });

        return new
        {
            AeronaveId = id,
            Matricula = aeronave.Matricula,
            Modelo = aeronave.Modelo,
            Periodo = "Últimos 30 dias",
            Estatisticas = new
            {
                TotalVoos = totalVoos,
                HorasVoadas = Math.Round(horasVoadas, 2),
                CombustivelTotal = Math.Round(combustivelTotal, 2),
                MediaHorasPorVoo = totalVoos > 0 ? Math.Round(horasVoadas / totalVoos, 2) : 0,
                MediaCombustivelPorVoo = totalVoos > 0 ? Math.Round(combustivelTotal / totalVoos, 2) : 0
            },
            StatusAssinaturas = new
            {
                RegistrosAssinados = registrosAssinados,
                RegistrosPendentes = registrosPendentes,
                PercentualCompleto = totalVoos > 0 ? Math.Round((double)registrosAssinados / totalVoos * 100, 1) : 0
            },
            AeroportosMaisUtilizados = new
            {
                Decolagem = aeroportosDecolagem,
                Pouso = aeroportosPouso
            },
            UltimoVoo = registrosList.Any() ? registrosList.OrderByDescending(r => r.Data).First().Data : (DateTime?)null
        };
    }

    private static AeronaveDto MapearEntidadeParaDto(Aeronave aeronave)
    {
        return new AeronaveDto
        {
            Id = aeronave.Id,
            Matricula = aeronave.Matricula,
            Modelo = aeronave.Modelo,
            Fabricante = aeronave.Fabricante,
            NumeroSerie = aeronave.NumeroSerie,
            AnoFabricacao = aeronave.AnoFabricacao ?? 0,
            HorasTotaisCelula = aeronave.HorasTotaisCelula,
            TipoRBAC = aeronave.TipoRBAC,
            OperadorId = aeronave.OperadorId,
            OperadorNome = "", // TODO: Implementar quando tiver serviço de operador
            Ativo = aeronave.Ativo,
            DataCriacao = aeronave.DataCriacao,
            DataUltimaAtualizacao = aeronave.DataUltimaAtualizacao
        };
    }
}