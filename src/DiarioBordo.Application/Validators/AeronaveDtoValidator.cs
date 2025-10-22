using FluentValidation;
using DiarioBordo.Application.DTOs;
using System.Text.RegularExpressions;

namespace DiarioBordo.Application.Validators;

/// <summary>
/// Validador para dados de aeronave
/// </summary>
public class AeronaveDtoValidator : AbstractValidator<AeronaveDto>
{
    private static readonly Regex RABRegex = new(@"^(PR-|PP-|PT-|PU-)[A-Z]{3}$", RegexOptions.Compiled);

    public AeronaveDtoValidator()
    {
        RuleFor(x => x.Matricula)
            .NotEmpty()
            .WithMessage("Matrícula é obrigatória")
            .Must(BeValidRAB)
            .WithMessage("Matrícula deve seguir padrão RAB brasileiro (PR-XXX, PP-XXX, PT-XXX ou PU-XXX)");

        RuleFor(x => x.Modelo)
            .NotEmpty()
            .WithMessage("Modelo é obrigatório")
            .MaximumLength(100)
            .WithMessage("Modelo não pode exceder 100 caracteres");

        RuleFor(x => x.Fabricante)
            .NotEmpty()
            .WithMessage("Fabricante é obrigatório")
            .MaximumLength(100)
            .WithMessage("Fabricante não pode exceder 100 caracteres");

        RuleFor(x => x.NumeroSerie)
            .NotEmpty()
            .WithMessage("Número de série é obrigatório")
            .MaximumLength(50)
            .WithMessage("Número de série não pode exceder 50 caracteres");

        RuleFor(x => x.AnoFabricacao)
            .GreaterThan(1900)
            .WithMessage("Ano de fabricação deve ser maior que 1900")
            .LessThanOrEqualTo(DateTime.Now.Year)
            .WithMessage("Ano de fabricação não pode ser futuro");

        When(x => x.HorasTotaisCelula.HasValue, () =>
        {
            RuleFor(x => x.HorasTotaisCelula)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Horas totais de célula deve ser maior ou igual a zero");
        });

        RuleFor(x => x.TipoRBAC)
            .IsInEnum()
            .WithMessage("Tipo RBAC é obrigatório");

        RuleFor(x => x.OperadorId)
            .GreaterThan(0)
            .WithMessage("Operador é obrigatório");
    }

    private static bool BeValidRAB(string matricula)
    {
        return !string.IsNullOrWhiteSpace(matricula) &&
               RABRegex.IsMatch(matricula.Trim().ToUpperInvariant());
    }
}