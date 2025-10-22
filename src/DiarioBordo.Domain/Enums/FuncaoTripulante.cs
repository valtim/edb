namespace DiarioBordo.Domain.Enums;

/// <summary>
/// Função do tripulante conforme Art. 5º I Resolução ANAC 457/2017
/// </summary>
public enum FuncaoTripulante
{
    /// <summary>
    /// Piloto em Comando
    /// </summary>
    P = 1,

    /// <summary>
    /// Piloto em Instrução
    /// </summary>
    I = 2,

    /// <summary>
    /// Copiloto
    /// </summary>
    O = 3,

    /// <summary>
    /// Comissário
    /// </summary>
    C = 4,

    /// <summary>
    /// Mecânico de Voo
    /// </summary>
    M = 5
}