using DiarioBordo.Domain.Enums;

namespace DiarioBordo.Application.DTOs;

/// <summary>
/// DTO para criação/edição de registro de voo com todos os 17 campos obrigatórios
/// </summary>
public partial class RegistroVooDto
{
    // I - Número sequencial (gerado automaticamente)
    public long? NumeroSequencial { get; set; }

    // Referências
    public int AeronaveId { get; set; }

    // II - Identificação da tripulação
    public string PilotoComandoCodigo { get; set; } = string.Empty;
    public FuncaoTripulante PilotoComandoFuncao { get; set; }
    public TimeSpan PilotoComandoHorarioApresentacao { get; set; }

    // III - Data
    public DateTime Data { get; set; }

    // IV - Locais de pouso e decolagem
    public string LocalDecolagem { get; set; } = string.Empty;
    public string LocalPouso { get; set; } = string.Empty;

    // V - Horários UTC
    public DateTime HorarioDecolagemUTC { get; set; }
    public DateTime HorarioPousoUTC { get; set; }
    public DateTime HorarioPartidaMotoresUTC { get; set; }
    public DateTime HorarioCorteMotoresUTC { get; set; }

    // VI - Tempo de voo IFR
    public decimal? TempoVooIFR { get; set; }

    // VII - Total de combustível por etapa
    public decimal CombustivelQuantidade { get; set; }
    public string CombustivelUnidade { get; set; } = string.Empty;

    // VIII - Natureza do voo
    public NaturezaVoo NaturezaVoo { get; set; }
    public string? NaturezaVooOutro { get; set; }

    // IX - Quantidade de pessoas a bordo
    public int QuantidadePessoasAbordo { get; set; }

    // X - Carga transportada
    public decimal? CargaQuantidade { get; set; }
    public string? CargaUnidade { get; set; }

    // XI - Ocorrências
    public string? Ocorrencias { get; set; }

    // XII - Discrepâncias técnicas
    public string? DiscrepanciasTecnicas { get; set; }
    public string? PessoaDetectouDiscrepancia { get; set; }

    // XIII - Ações corretivas
    public string? AcoesCorretivas { get; set; }

    // XIV - Tipo da última intervenção de manutenção
    public string? TipoUltimaManutencao { get; set; }

    // XV - Tipo da próxima intervenção de manutenção
    public string? TipoProximaManutencao { get; set; }

    // XVI - Horas de célula previstas
    public decimal? HorasCelulaProximaManutencao { get; set; }

    // XVII - Responsável pela aprovação
    public string? ResponsavelAprovacaoRetorno { get; set; }

    // Campos de controle (readonly para clientes)
    public bool AssinadoPiloto { get; set; }
    public DateTime? DataAssinaturaPilotoUTC { get; set; }
    public bool AssinadoOperador { get; set; }
    public DateTime? DataAssinaturaOperadorUTC { get; set; }
    public bool SincronizadoANAC { get; set; }

    // Propriedades de compatibilidade
    public int? Id { get; set; }
    public bool StatusAssinaturaPiloto { get; set; }
    public bool StatusAssinaturaOperador { get; set; }
    public DateTime? DataAssinaturaPiloto { get; set; }
    public DateTime? DataAssinaturaOperador { get; set; }
    public DateTime? DataCriacao { get; set; }
    public DateTime? DataUltimaAtualizacao { get; set; }

    // Informações calculadas
    public decimal TempoVooTotal
    {
        get => CalcularTempoVooTotal();
        set { } // Setter vazio para compatibilidade
    }

    private decimal CalcularTempoVooTotal()
    {
        if (HorarioCorteMotoresUTC <= HorarioPartidaMotoresUTC)
            return 0;

        var tempoTotal = HorarioCorteMotoresUTC - HorarioPartidaMotoresUTC;
        return (decimal)tempoTotal.TotalHours;
    }
}