using DiarioBordo.Domain.Enums;

namespace DiarioBordo.Application.DTOs;

/// <summary>
/// DTO para transferência de dados de aeronave
/// </summary>
public class AeronaveDto
{
    public int Id { get; set; }
    public string Matricula { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string Fabricante { get; set; } = string.Empty;
    public string NumeroSerie { get; set; } = string.Empty;
    public int AnoFabricacao { get; set; }
    public decimal? HorasTotaisCelula { get; set; }
    public TipoRBAC TipoRBAC { get; set; }
    public int OperadorId { get; set; }
    public string OperadorNome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; }
    public DateTime? DataUltimaAtualizacao { get; set; }
}