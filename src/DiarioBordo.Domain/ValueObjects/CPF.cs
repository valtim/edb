using System.Text.RegularExpressions;

namespace DiarioBordo.Domain.ValueObjects;

/// <summary>
/// Value Object para CPF brasileiro com validação
/// </summary>
public sealed class CPF
{
    private static readonly Regex CPFPattern = new(@"^\d{11}$", RegexOptions.Compiled);

    public string Numero { get; private set; }

    private CPF(string numero)
    {
        Numero = numero;
    }

    public static CPF Criar(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
            throw new ArgumentException("CPF não pode ser vazio ou nulo");

        // Remove formatação se existir
        var cpfLimpo = Regex.Replace(numero, @"[^\d]", "");

        if (!CPFPattern.IsMatch(cpfLimpo))
            throw new ArgumentException("CPF deve ter 11 dígitos");

        if (!ValidarCPF(cpfLimpo))
            throw new ArgumentException("CPF inválido");

        return new CPF(cpfLimpo);
    }

    private static bool ValidarCPF(string cpf)
    {
        // Verifica se todos os dígitos são iguais
        if (cpf.All(c => c == cpf[0]))
            return false;

        // Validação do dígito verificador
        var multiplicador1 = new int[9] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplicador2 = new int[10] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var tempCpf = cpf.Substring(0, 9);
        var soma = 0;

        for (int i = 0; i < 9; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

        var resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;

        var digito = resto.ToString();
        tempCpf += digito;
        soma = 0;

        for (int i = 0; i < 10; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

        resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;

        digito += resto.ToString();

        return cpf.EndsWith(digito);
    }

    /// <summary>
    /// Formata o CPF como ###.###.###-##
    /// </summary>
    public string Formatado()
    {
        return $"{Numero.Substring(0, 3)}.{Numero.Substring(3, 3)}.{Numero.Substring(6, 3)}-{Numero.Substring(9, 2)}";
    }

    /// <summary>
    /// Máscara o CPF para logs (***.***.###-**)
    /// </summary>
    public string Mascarado()
    {
        return $"***.***{Numero.Substring(6, 3)}-**";
    }

    public static implicit operator string(CPF cpf) => cpf.Numero;
    public static implicit operator CPF(string numero) => Criar(numero);

    public override string ToString() => Formatado();

    public override bool Equals(object? obj)
    {
        return obj is CPF other && Numero == other.Numero;
    }

    public override int GetHashCode() => Numero.GetHashCode();
}