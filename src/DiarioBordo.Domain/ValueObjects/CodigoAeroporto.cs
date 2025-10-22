using System.Text.RegularExpressions;

namespace DiarioBordo.Domain.ValueObjects;

/// <summary>
/// Value Object para códigos de aeroporto (IATA/ICAO) e coordenadas (Art. 5º III Res. 457/2017)
/// </summary>
public sealed class CodigoAeroporto
{
    private static readonly Regex IATAPattern = new(@"^[A-Z]{3}$", RegexOptions.Compiled);
    private static readonly Regex ICAOPattern = new(@"^[A-Z]{4}$", RegexOptions.Compiled);
    private static readonly Regex CoordenadasPattern = new(@"^-?\d{1,3}\.\d+,-?\d{1,3}\.\d+$", RegexOptions.Compiled);

    public string Valor { get; private set; }
    public TipoCodigoAeroporto Tipo { get; private set; }

    private CodigoAeroporto(string valor, TipoCodigoAeroporto tipo)
    {
        Valor = valor;
        Tipo = tipo;
    }

    public static CodigoAeroporto Criar(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("Código de aeroporto não pode ser vazio ou nulo");

        valor = valor.Trim().ToUpperInvariant();

        if (IATAPattern.IsMatch(valor))
            return new CodigoAeroporto(valor, TipoCodigoAeroporto.IATA);

        if (ICAOPattern.IsMatch(valor))
            return new CodigoAeroporto(valor, TipoCodigoAeroporto.ICAO);

        if (CoordenadasPattern.IsMatch(valor))
            return new CodigoAeroporto(valor, TipoCodigoAeroporto.Coordenadas);

        throw new ArgumentException("Código de aeroporto deve ser IATA (3 letras), ICAO (4 letras) ou coordenadas (lat,lng)");
    }

    public static implicit operator string(CodigoAeroporto codigo) => codigo.Valor;
    public static implicit operator CodigoAeroporto(string valor) => Criar(valor);

    public override string ToString() => Valor;

    public override bool Equals(object? obj)
    {
        return obj is CodigoAeroporto other && Valor == other.Valor;
    }

    public override int GetHashCode() => Valor.GetHashCode();
}

public enum TipoCodigoAeroporto
{
    IATA,
    ICAO,
    Coordenadas
}