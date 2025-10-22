using DiarioBordo.Application.DTOs;
using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Repositories;
using FluentValidation;

namespace DiarioBordo.Application.Services;

/// <summary>
/// Serviço para gerenciamento de registros de voo
/// Implementa regras de negócio e conformidade ANAC
/// </summary>
public interface IRegistroVooService
{
    Task<RegistroVooDto> CriarRegistroAsync(RegistroVooDto registroDto, int operadorId);
    Task<RegistroVooDto> AtualizarRegistroAsync(int id, RegistroVooDto registroDto, int operadorId);
    Task<RegistroVooDto?> ObterPorIdAsync(int id);
    Task<IEnumerable<RegistroVooDto>> ObterUltimos30DiasAsync(int aeronaveId);
    Task<IEnumerable<RegistroVooDto>> ObterPendentesAssinaturaAsync(int operadorId);
    Task ExcluirRegistroAsync(int id, int operadorId);
    Task<bool> ValidarNumeroSequencialAsync(int aeronaveId, int numeroSequencial, int? registroId = null);
}

public class RegistroVooService : IRegistroVooService
{
    private readonly IRegistroVooRepository _registroRepository;
    private readonly IAeronaveRepository _aeronaveRepository;
    private readonly ITripulanteRepository _tripulanteRepository;
    private readonly IValidator<RegistroVooDto> _validator;

    public RegistroVooService(
        IRegistroVooRepository registroRepository,
        IAeronaveRepository aeronaveRepository,
        ITripulanteRepository tripulanteRepository,
        IValidator<RegistroVooDto> validator)
    {
        _registroRepository = registroRepository;
        _aeronaveRepository = aeronaveRepository;
        _tripulanteRepository = tripulanteRepository;
        _validator = validator;
    }

    public async Task<RegistroVooDto> CriarRegistroAsync(RegistroVooDto registroDto, int operadorId)
    {
        // Validação do DTO
        var validationResult = await _validator.ValidateAsync(registroDto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Validações de negócio
        await ValidarAeronaveAsync(registroDto.AeronaveId, operadorId);
        await ValidarTripulanteAsync(registroDto.PilotoComandoCodigo, operadorId);

        // Validar número sequencial único por aeronave
        var numeroSequencial = await GerarProximoNumeroSequencialAsync(registroDto.AeronaveId);

        // Mapear DTO para entidade
        var registro = MapearDtoParaEntidade(registroDto);
        registro.NumeroSequencial = numeroSequencial;
        registro.DataCriacao = DateTime.UtcNow;
        registro.StatusAssinaturaPiloto = false;
        registro.StatusAssinaturaOperador = false;

        // Calcular tempo total de voo
        var tempoVoo = registro.HorarioPousoUTC - registro.HorarioDecolagemUTC;
        registro.TempoVooTotal = (decimal)tempoVoo.TotalHours;

        // Salvar no repositório
        await _registroRepository.AdicionarAsync(registro);

        return MapearEntidadeParaDto(registro);
    }

    public async Task<RegistroVooDto> AtualizarRegistroAsync(int id, RegistroVooDto registroDto, int operadorId)
    {
        var registroExistente = await _registroRepository.ObterPorIdAsync(id);
        if (registroExistente == null)
        {
            throw new InvalidOperationException("Registro de voo não encontrado");
        }

        // Não permitir alteração de registro já assinado
        if (registroExistente.StatusAssinaturaPiloto || registroExistente.StatusAssinaturaOperador)
        {
            throw new InvalidOperationException("Não é possível alterar registro já assinado");
        }

        // Validação do DTO
        var validationResult = await _validator.ValidateAsync(registroDto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Validações de negócio
        await ValidarAeronaveAsync(registroDto.AeronaveId, operadorId);
        await ValidarTripulanteAsync(registroDto.PilotoComandoCodigo, operadorId);

        // Validar número sequencial se alterado
        if (registroDto.NumeroSequencial != registroExistente.NumeroSequencial)
        {
            var numeroValido = await ValidarNumeroSequencialAsync(
                registroDto.AeronaveId,
                (int)(registroDto.NumeroSequencial ?? 0),
                id);

            if (!numeroValido)
            {
                throw new InvalidOperationException("Número sequencial já existe para esta aeronave");
            }
        }

        // Atualizar campos
        AtualizarCamposRegistro(registroExistente, registroDto);
        registroExistente.DataUltimaAtualizacao = DateTime.UtcNow;

        await _registroRepository.AtualizarAsync(registroExistente);

        return MapearEntidadeParaDto(registroExistente);
    }

    public async Task<RegistroVooDto?> ObterPorIdAsync(int id)
    {
        var registro = await _registroRepository.ObterPorIdAsync(id);
        return registro != null ? MapearEntidadeParaDto(registro) : null;
    }

    public async Task<IEnumerable<RegistroVooDto>> ObterUltimos30DiasAsync(int aeronaveId)
    {
        var registros = await _registroRepository.ObterUltimos30DiasAsync(aeronaveId);
        return registros.Select(MapearEntidadeParaDto);
    }

    public async Task<IEnumerable<RegistroVooDto>> ObterPendentesAssinaturaAsync(int operadorId)
    {
        var registros = await _registroRepository.ObterPendentesAssinaturaOperadorAsync(operadorId);
        return registros.Select(MapearEntidadeParaDto);
    }

    public async Task ExcluirRegistroAsync(int id, int operadorId)
    {
        var registro = await _registroRepository.ObterPorIdAsync(id);
        if (registro == null)
        {
            throw new InvalidOperationException("Registro de voo não encontrado");
        }

        // Não permitir exclusão de registro já assinado
        if (registro.StatusAssinaturaPiloto || registro.StatusAssinaturaOperador)
        {
            throw new InvalidOperationException("Não é possível excluir registro já assinado");
        }

        await _registroRepository.RemoverAsync(registro);
    }

    public async Task<bool> ValidarNumeroSequencialAsync(int aeronaveId, int numeroSequencial, int? registroId = null)
    {
        return await _registroRepository.ValidarNumeroSequencialUnicoAsync(aeronaveId, numeroSequencial, registroId);
    }

    private async Task<int> GerarProximoNumeroSequencialAsync(int aeronaveId)
    {
        var ultimoNumero = await _registroRepository.ObterUltimoNumeroSequencialAsync(aeronaveId);
        return (int)(ultimoNumero + 1);
    }

    private async Task ValidarAeronaveAsync(int aeronaveId, int operadorId)
    {
        var aeronave = await _aeronaveRepository.ObterPorIdAsync(aeronaveId);
        if (aeronave == null)
        {
            throw new InvalidOperationException("Aeronave não encontrada");
        }

        if (aeronave.OperadorId != operadorId)
        {
            throw new UnauthorizedAccessException("Aeronave não pertence ao operador");
        }

        if (!aeronave.Ativo)
        {
            throw new InvalidOperationException("Aeronave não está ativa");
        }
    }

    private async Task ValidarTripulanteAsync(string codigoANAC, int operadorId)
    {
        var tripulante = await _tripulanteRepository.ObterPorCodigoANACAsync(codigoANAC);
        if (tripulante == null)
        {
            throw new InvalidOperationException("Tripulante não encontrado");
        }

        if (tripulante.OperadorId != operadorId)
        {
            throw new UnauthorizedAccessException("Tripulante não pertence ao operador");
        }

        if (!tripulante.Ativo)
        {
            throw new InvalidOperationException("Tripulante não está ativo");
        }
    }

    private static RegistroVoo MapearDtoParaEntidade(RegistroVooDto dto)
    {
        return new RegistroVoo
        {
            AeronaveId = dto.AeronaveId,
            Data = dto.Data,
            LocalDecolagem = dto.LocalDecolagem,
            LocalPouso = dto.LocalPouso,
            HorarioPartidaMotoresUTC = dto.HorarioPartidaMotoresUTC,
            HorarioDecolagemUTC = dto.HorarioDecolagemUTC,
            HorarioPousoUTC = dto.HorarioPousoUTC,
            HorarioCorteMotoresUTC = dto.HorarioCorteMotoresUTC,
            TempoVooIFR = dto.TempoVooIFR,
            CombustivelQuantidade = dto.CombustivelQuantidade,
            CombustivelUnidade = dto.CombustivelUnidade,
            NaturezaVoo = dto.NaturezaVoo,
            NaturezaVooOutro = dto.NaturezaVooOutro,
            QuantidadePessoasAbordo = dto.QuantidadePessoasAbordo,
            CargaQuantidade = dto.CargaQuantidade,
            CargaUnidade = dto.CargaUnidade,
            Ocorrencias = dto.Ocorrencias,
            DiscrepanciasTecnicas = dto.DiscrepanciasTecnicas,
            PessoaDetectouDiscrepancia = dto.PessoaDetectouDiscrepancia,
            AcoesCorretivas = dto.AcoesCorretivas,
            TipoUltimaManutencao = dto.TipoUltimaManutencao,
            TipoProximaManutencao = dto.TipoProximaManutencao,
            HorasCelulaProximaManutencao = dto.HorasCelulaProximaManutencao,
            ResponsavelAprovacaoRetorno = dto.ResponsavelAprovacaoRetorno,
            PilotoComandoCodigo = dto.PilotoComandoCodigo,
            PilotoComandoFuncao = dto.PilotoComandoFuncao,
            PilotoComandoHorarioApresentacao = dto.PilotoComandoHorarioApresentacao
        };
    }

    private static void AtualizarCamposRegistro(RegistroVoo registro, RegistroVooDto dto)
    {
        registro.AeronaveId = dto.AeronaveId;
        registro.Data = dto.Data;
        registro.LocalDecolagem = dto.LocalDecolagem;
        registro.LocalPouso = dto.LocalPouso;
        registro.HorarioPartidaMotoresUTC = dto.HorarioPartidaMotoresUTC;
        registro.HorarioDecolagemUTC = dto.HorarioDecolagemUTC;
        registro.HorarioPousoUTC = dto.HorarioPousoUTC;
        registro.HorarioCorteMotoresUTC = dto.HorarioCorteMotoresUTC;
        registro.TempoVooIFR = dto.TempoVooIFR;
        registro.CombustivelQuantidade = dto.CombustivelQuantidade;
        registro.CombustivelUnidade = dto.CombustivelUnidade;
        registro.NaturezaVoo = dto.NaturezaVoo;
        registro.NaturezaVooOutro = dto.NaturezaVooOutro;
        registro.QuantidadePessoasAbordo = dto.QuantidadePessoasAbordo;
        registro.CargaQuantidade = dto.CargaQuantidade;
        registro.CargaUnidade = dto.CargaUnidade;
        registro.Ocorrencias = dto.Ocorrencias;
        registro.DiscrepanciasTecnicas = dto.DiscrepanciasTecnicas;
        registro.PessoaDetectouDiscrepancia = dto.PessoaDetectouDiscrepancia;
        registro.AcoesCorretivas = dto.AcoesCorretivas;
        registro.TipoUltimaManutencao = dto.TipoUltimaManutencao;
        registro.TipoProximaManutencao = dto.TipoProximaManutencao;
        registro.HorasCelulaProximaManutencao = dto.HorasCelulaProximaManutencao;
        registro.ResponsavelAprovacaoRetorno = dto.ResponsavelAprovacaoRetorno;
        registro.PilotoComandoCodigo = dto.PilotoComandoCodigo;
        registro.PilotoComandoFuncao = dto.PilotoComandoFuncao;
        registro.PilotoComandoHorarioApresentacao = dto.PilotoComandoHorarioApresentacao;

        // Recalcular tempo total de voo
        var tempoVoo = registro.HorarioPousoUTC - registro.HorarioDecolagemUTC;
        registro.TempoVooTotal = (decimal)tempoVoo.TotalHours;
    }

    private static RegistroVooDto MapearEntidadeParaDto(RegistroVoo registro)
    {
        return new RegistroVooDto
        {
            Id = registro.Id,
            NumeroSequencial = registro.NumeroSequencial,
            AeronaveId = registro.AeronaveId,
            Data = registro.Data,
            LocalDecolagem = registro.LocalDecolagem,
            LocalPouso = registro.LocalPouso,
            HorarioPartidaMotoresUTC = registro.HorarioPartidaMotoresUTC,
            HorarioDecolagemUTC = registro.HorarioDecolagemUTC,
            HorarioPousoUTC = registro.HorarioPousoUTC,
            HorarioCorteMotoresUTC = registro.HorarioCorteMotoresUTC,
            TempoVooIFR = registro.TempoVooIFR,
            TempoVooTotal = registro.TempoVooTotal,
            CombustivelQuantidade = registro.CombustivelQuantidade,
            CombustivelUnidade = registro.CombustivelUnidade,
            NaturezaVoo = registro.NaturezaVoo,
            NaturezaVooOutro = registro.NaturezaVooOutro,
            QuantidadePessoasAbordo = registro.QuantidadePessoasAbordo,
            CargaQuantidade = registro.CargaQuantidade,
            CargaUnidade = registro.CargaUnidade,
            Ocorrencias = registro.Ocorrencias,
            DiscrepanciasTecnicas = registro.DiscrepanciasTecnicas,
            PessoaDetectouDiscrepancia = registro.PessoaDetectouDiscrepancia,
            AcoesCorretivas = registro.AcoesCorretivas,
            TipoUltimaManutencao = registro.TipoUltimaManutencao,
            TipoProximaManutencao = registro.TipoProximaManutencao,
            HorasCelulaProximaManutencao = registro.HorasCelulaProximaManutencao,
            ResponsavelAprovacaoRetorno = registro.ResponsavelAprovacaoRetorno,
            PilotoComandoCodigo = registro.PilotoComandoCodigo,
            PilotoComandoFuncao = registro.PilotoComandoFuncao,
            PilotoComandoHorarioApresentacao = registro.PilotoComandoHorarioApresentacao,
            StatusAssinaturaPiloto = registro.StatusAssinaturaPiloto,
            StatusAssinaturaOperador = registro.StatusAssinaturaOperador,
            DataAssinaturaPiloto = registro.DataAssinaturaPiloto,
            DataAssinaturaOperador = registro.DataAssinaturaOperador,
            DataCriacao = registro.DataCriacao,
            DataUltimaAtualizacao = registro.DataUltimaAtualizacao
        };
    }
}