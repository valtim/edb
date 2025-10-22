namespace DiarioBordo.Domain.Enums;

/// <summary>
/// Tipo de assinatura digital conforme Resolução ANAC 458/2017
/// </summary>
public enum TipoAssinatura
{
    /// <summary>
    /// Assinatura do Piloto em Comando (incisos I-XII Art. 4º)
    /// </summary>
    Piloto = 1,

    /// <summary>
    /// Assinatura do Operador (incisos XIII-XVII Art. 4º)
    /// </summary>
    Operador = 2
}