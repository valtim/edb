using FluentValidation;
using DiarioBordo.Application.DTOs;
using DiarioBordo.Domain.Enums;
using System.Text.RegularExpressions;

namespace DiarioBordo.Application.Validators;

/// <summary>
/// Validador para dados de tripulante
/// </summary>
public class TripulanteDtoValidator : AbstractValidator<TripulanteDto>
{
    private static readonly Regex CodigoANACRegex = new(@"^\d{6}$", RegexOptions.Compiled);
    private static readonly Regex CPFRegex = new(@"^\d{11}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public TripulanteDtoValidator()
    {
        RuleFor(x => x.CodigoANAC)
            .NotEmpty()
            .WithMessage("Código ANAC é obrigatório")
            .Must(BeValidCodigoANAC)
            .WithMessage("Código ANAC deve ter exatamente 6 dígitos numéricos");

        RuleFor(x => x.CPF)
            .NotEmpty()
            .WithMessage("CPF é obrigatório")
            .Must(BeValidCPF)
            .WithMessage("CPF inválido");

        RuleFor(x => x.Nome)
            .NotEmpty()
            .WithMessage("Nome é obrigatório")
            .MaximumLength(200)
            .WithMessage("Nome não pode exceder 200 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email é obrigatório")
            .Must(BeValidEmail)
            .WithMessage("Email deve ter formato válido")
            .MaximumLength(100)
            .WithMessage("Email não pode exceder 100 caracteres");

        RuleFor(x => x.Funcoes)
            .NotEmpty()
            .WithMessage("Pelo menos uma função é obrigatória")
            .Must(HaveValidFunctions)
            .WithMessage("Funções devem ser válidas (P, I, O, C, M)");

        When(x => x.ValidadeLicenca.HasValue, () =>
        {
            RuleFor(x => x.ValidadeLicenca)
                .GreaterThan(DateTime.Today)
                .WithMessage("Validade da licença deve ser futura");
        });

        RuleFor(x => x.OperadorId)
            .GreaterThan(0)
            .WithMessage("Operador é obrigatório");
    }

    private static bool BeValidCodigoANAC(string codigo)
    {
        return !string.IsNullOrWhiteSpace(codigo) && CodigoANACRegex.IsMatch(codigo);
    }

    private static bool BeValidCPF(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        cpf = cpf.Replace(".", "").Replace("-", "").Trim();

        if (!CPFRegex.IsMatch(cpf))
            return false;

        // Verifica se todos os dígitos são iguais
        if (cpf.All(c => c == cpf[0]))
            return false;

        // Valida primeiro dígito verificador
        var soma = 0;
        for (int i = 0; i < 9; i++)
            soma += int.Parse(cpf[i].ToString()) * (10 - i);

        var resto = soma % 11;
        var primeiroDigito = resto < 2 ? 0 : 11 - resto;

        if (int.Parse(cpf[9].ToString()) != primeiroDigito)
            return false;

        // Valida segundo dígito verificador
        soma = 0;
        for (int i = 0; i < 10; i++)
            soma += int.Parse(cpf[i].ToString()) * (11 - i);

        resto = soma % 11;
        var segundoDigito = resto < 2 ? 0 : 11 - resto;

        return int.Parse(cpf[10].ToString()) == segundoDigito;
    }

    private static bool BeValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email.Trim());
    }

    private static bool HaveValidFunctions(List<FuncaoTripulante> funcoes)
    {
        return funcoes != null && funcoes.Count > 0 && funcoes.All(f => Enum.IsDefined(typeof(FuncaoTripulante), f));
    }
}