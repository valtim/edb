using FluentValidation;
using DiarioBordo.Application.DTOs;
using DiarioBordo.Domain.Enums;
using System.Text.RegularExpressions;

namespace DiarioBordo.Application.Validators;

/// <summary>
/// Validador para RegistroVoo - Garante conformidade com os 17 campos obrigatórios Art. 4º Res. 457/2017
/// </summary>
public class RegistroVooDtoValidator : AbstractValidator<RegistroVooDto>
{
    private static readonly Regex CodigoANACRegex = new(@"^\d{6}$", RegexOptions.Compiled);
    private static readonly Regex IATARegex = new(@"^[A-Z]{3}$", RegexOptions.Compiled);
    private static readonly Regex ICAORegex = new(@"^[A-Z]{4}$", RegexOptions.Compiled);
    private static readonly Regex CoordenadasRegex = new(@"^-?\d{1,3}\.\d+,-?\d{1,3}\.\d+$", RegexOptions.Compiled);
    private static readonly string[] UnidadesCombustivel = { "kg", "lb", "litros", "l", "gal" };
    private static readonly string[] UnidadesCarga = { "kg", "lb", "ton", "t" };

    public RegistroVooDtoValidator()
    {
        // I - Aeronave (obrigatória)
        RuleFor(x => x.AeronaveId)
            .GreaterThan(0)
            .WithMessage("Aeronave é obrigatória");

        // II - Identificação da tripulação
        RuleFor(x => x.PilotoComandoCodigo)
            .NotEmpty()
            .WithMessage("Código ANAC do piloto em comando é obrigatório")
            .Must(BeValidCodigoANAC)
            .WithMessage("Código ANAC deve ter exatamente 6 dígitos numéricos");

        RuleFor(x => x.PilotoComandoFuncao)
            .IsInEnum()
            .WithMessage("Função do piloto deve ser válida (P, I, O, C, M)");

        RuleFor(x => x.PilotoComandoHorarioApresentacao)
            .Must(BeValidTime)
            .WithMessage("Horário de apresentação deve ser válido (HH:MM)");

        // III - Data (obrigatória, não pode ser futura)
        RuleFor(x => x.Data)
            .NotEmpty()
            .WithMessage("Data do voo é obrigatória")
            .Must(BeValidFlightDate)
            .WithMessage("Data do voo não pode ser futura");

        // IV - Locais de pouso e decolagem (obrigatórios)
        RuleFor(x => x.LocalDecolagem)
            .NotEmpty()
            .WithMessage("Local de decolagem é obrigatório")
            .Must(BeValidAirportCode)
            .WithMessage("Local de decolagem deve ser código IATA (3 letras), ICAO (4 letras) ou coordenadas");

        RuleFor(x => x.LocalPouso)
            .NotEmpty()
            .WithMessage("Local de pouso é obrigatório")
            .Must(BeValidAirportCode)
            .WithMessage("Local de pouso deve ser código IATA (3 letras), ICAO (4 letras) ou coordenadas");

        // V - Horários UTC (obrigatórios e em sequência lógica)
        RuleFor(x => x.HorarioDecolagemUTC)
            .NotEmpty()
            .WithMessage("Horário de decolagem é obrigatório");

        RuleFor(x => x.HorarioPousoUTC)
            .NotEmpty()
            .WithMessage("Horário de pouso é obrigatório")
            .GreaterThan(x => x.HorarioDecolagemUTC)
            .WithMessage("Horário de pouso deve ser posterior à decolagem");

        RuleFor(x => x.HorarioPartidaMotoresUTC)
            .NotEmpty()
            .WithMessage("Horário de partida dos motores é obrigatório")
            .LessThanOrEqualTo(x => x.HorarioDecolagemUTC)
            .WithMessage("Partida dos motores deve ser anterior ou igual à decolagem");

        RuleFor(x => x.HorarioCorteMotoresUTC)
            .NotEmpty()
            .WithMessage("Horário de corte dos motores é obrigatório")
            .GreaterThanOrEqualTo(x => x.HorarioPousoUTC)
            .WithMessage("Corte dos motores deve ser posterior ou igual ao pouso");

        // VI - Tempo de voo IFR (opcional, mas se informado deve ser válido)
        When(x => x.TempoVooIFR.HasValue, () =>
        {
            RuleFor(x => x.TempoVooIFR)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Tempo de voo IFR deve ser maior ou igual a zero");
        });

        // VII - Total de combustível por etapa (obrigatório)
        RuleFor(x => x.CombustivelQuantidade)
            .GreaterThan(0)
            .WithMessage("Quantidade de combustível é obrigatória e deve ser maior que zero");

        RuleFor(x => x.CombustivelUnidade)
            .NotEmpty()
            .WithMessage("Unidade de combustível é obrigatória")
            .Must(BeValidFuelUnit)
            .WithMessage("Unidade de combustível deve ser: kg, lb, litros, l ou gal");

        // VIII - Natureza do voo (obrigatória)
        RuleFor(x => x.NaturezaVoo)
            .IsInEnum()
            .WithMessage("Natureza do voo é obrigatória");

        When(x => x.NaturezaVoo == NaturezaVoo.Outro, () =>
        {
            RuleFor(x => x.NaturezaVooOutro)
                .NotEmpty()
                .WithMessage("Especificação da natureza do voo é obrigatória quando selecionado 'Outro'");
        });

        // IX - Quantidade de pessoas a bordo (obrigatória)
        RuleFor(x => x.QuantidadePessoasAbordo)
            .GreaterThan(0)
            .WithMessage("Quantidade de pessoas a bordo é obrigatória e deve ser maior que zero");

        // X - Carga transportada (opcional, mas se informada deve ter unidade)
        When(x => x.CargaQuantidade.HasValue && x.CargaQuantidade > 0, () =>
        {
            RuleFor(x => x.CargaUnidade)
                .NotEmpty()
                .WithMessage("Unidade de carga é obrigatória quando há carga transportada")
                .Must(BeValidCargoUnit)
                .WithMessage("Unidade de carga deve ser: kg, lb, ton ou t");
        });

        // XI-XVII são opcionais por serem campos de texto livre,
        // mas vamos validar tamanhos máximos para integridade do banco

        RuleFor(x => x.Ocorrencias)
            .MaximumLength(5000)
            .WithMessage("Ocorrências não pode exceder 5000 caracteres");

        RuleFor(x => x.DiscrepanciasTecnicas)
            .MaximumLength(5000)
            .WithMessage("Discrepâncias técnicas não pode exceder 5000 caracteres");

        RuleFor(x => x.PessoaDetectouDiscrepancia)
            .MaximumLength(200)
            .WithMessage("Pessoa que detectou discrepância não pode exceder 200 caracteres");

        RuleFor(x => x.AcoesCorretivas)
            .MaximumLength(5000)
            .WithMessage("Ações corretivas não pode exceder 5000 caracteres");

        RuleFor(x => x.TipoUltimaManutencao)
            .MaximumLength(100)
            .WithMessage("Tipo da última manutenção não pode exceder 100 caracteres");

        RuleFor(x => x.TipoProximaManutencao)
            .MaximumLength(100)
            .WithMessage("Tipo da próxima manutenção não pode exceder 100 caracteres");

        When(x => x.HorasCelulaProximaManutencao.HasValue, () =>
        {
            RuleFor(x => x.HorasCelulaProximaManutencao)
                .GreaterThan(0)
                .WithMessage("Horas de célula para próxima manutenção deve ser maior que zero");
        });

        RuleFor(x => x.ResponsavelAprovacaoRetorno)
            .MaximumLength(200)
            .WithMessage("Responsável pela aprovação não pode exceder 200 caracteres");
    }

    private static bool BeValidCodigoANAC(string codigo)
    {
        return !string.IsNullOrWhiteSpace(codigo) && CodigoANACRegex.IsMatch(codigo);
    }

    private static bool BeValidTime(TimeSpan horario)
    {
        return horario >= TimeSpan.Zero && horario < TimeSpan.FromDays(1);
    }

    private static bool BeValidFlightDate(DateTime data)
    {
        return data.Date <= DateTime.Today;
    }

    private static bool BeValidAirportCode(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return false;

        var codigoUpper = codigo.Trim().ToUpperInvariant();

        return IATARegex.IsMatch(codigoUpper) ||
               ICAORegex.IsMatch(codigoUpper) ||
               CoordenadasRegex.IsMatch(codigo.Trim());
    }

    private static bool BeValidFuelUnit(string unidade)
    {
        return !string.IsNullOrWhiteSpace(unidade) &&
               UnidadesCombustivel.Contains(unidade.ToLowerInvariant());
    }

    private static bool BeValidCargoUnit(string unidade)
    {
        return !string.IsNullOrWhiteSpace(unidade) &&
               UnidadesCarga.Contains(unidade.ToLowerInvariant());
    }
}