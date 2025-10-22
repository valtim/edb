using DiarioBordo.Domain.Enums;

namespace DiarioBordo.Application.DTOs;

/// <summary>
/// DTO para assinatura de registro - Conformidade com Res. ANAC 458/2017
/// </summary>
public class AssinaturaRegistroDto
{
    public int Id { get; set; }
    public int RegistroVooId { get; set; }
    public TipoAssinatura TipoAssinatura { get; set; }
    public string CodigoANAC { get; set; } = string.Empty;
    public string NomeAssinante { get; set; } = string.Empty;
    public string HashRegistro { get; set; } = string.Empty; // SHA-256
    public DateTime DataAssinatura { get; set; }
    public string EnderecoIP { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool AssinaturaValidada { get; set; }
    public DateTime? DataValidacao { get; set; }

    // Campos para auditoria
    public DateTime DataCriacao { get; set; }
}