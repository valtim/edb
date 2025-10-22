using DiarioBordo.Domain.Enums;

namespace DiarioBordo.Application.DTOs;

/// <summary>
/// DTO para transferência de dados de tripulante
/// </summary>
public class TripulanteDto
{
    public int Id { get; set; }
    public string CodigoANAC { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<FuncaoTripulante> Funcoes { get; set; } = new();
    public DateTime? ValidadeLicenca { get; set; }
    public int OperadorId { get; set; }
    public string OperadorNome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataUltimaAtualizacao { get; set; }
}