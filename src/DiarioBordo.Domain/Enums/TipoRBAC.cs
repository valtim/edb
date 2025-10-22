namespace DiarioBordo.Domain.Enums;

/// <summary>
/// Tipo RBAC do operador para definição de prazos (Art. 9º § 1º Res. 457/2017)
/// </summary>
public enum TipoRBAC
{
    /// <summary>
    /// RBAC 121 - Prazo: 2 dias
    /// </summary>
    RBAC121 = 1,

    /// <summary>
    /// RBAC 135 - Prazo: 15 dias
    /// </summary>
    RBAC135 = 2,

    /// <summary>
    /// Outros operadores - Prazo: 30 dias
    /// </summary>
    Outros = 3
}