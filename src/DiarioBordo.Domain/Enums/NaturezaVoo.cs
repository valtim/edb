namespace DiarioBordo.Domain.Enums;

/// <summary>
/// Natureza do voo conforme Art. 5º VI Resolução ANAC 457/2017
/// </summary>
public enum NaturezaVoo
{
    /// <summary>
    /// Voo Privado
    /// </summary>
    Privado = 1,

    /// <summary>
    /// Voo Comercial
    /// </summary>
    Comercial = 2,

    /// <summary>
    /// Outro tipo de voo (deve ser especificado)
    /// </summary>
    Outro = 3
}