using System.Text.RegularExpressions;

namespace DiarioBordo.Domain.ValueObjects;

/// <summary>
/// Value Object para Código ANAC de 6 dígitos (Art. 5º I Res. 457/2017)
/// </summary>
public sealed class CodigoANAC
{
    private static readonly Regex CodigoPattern = new(@"^\d{6}$", RegexOptions.Compiled);

    public string Valor { get; private set; }

    private CodigoANAC(string valor)
    {
        Valor = valor;
    }

    public static CodigoANAC Criar(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("Código ANAC não pode ser vazio ou nulo");

        if (!CodigoPattern.IsMatch(valor))
            throw new ArgumentException("Código ANAC deve ter exatamente 6 dígitos numéricos");

        return new CodigoANAC(valor);
    }

    public static implicit operator string(CodigoANAC codigo) => codigo.Valor;
    public static implicit operator CodigoANAC(string valor) => Criar(valor);

    public override string ToString() => Valor;

    public override bool Equals(object? obj)
    {
        return obj is CodigoANAC other && Valor == other.Valor;
    }

    public override int GetHashCode() => Valor.GetHashCode();
}