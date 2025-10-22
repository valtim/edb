using DiarioBordo.Domain.Enums;
using DiarioBordo.Domain.ValueObjects;

namespace DiarioBordo.Domain.Entities;

/// <summary>
/// Registro de Voo com os 17 campos obrigatórios conforme Art. 4º Resolução ANAC 457/2017
/// </summary>
public class RegistroVoo : BaseEntity
{
    #region I - Número sequencial cronológico

    /// <summary>
    /// I - Número sequencial cronológico que identifique o registro daquele voo
    /// </summary>
    public virtual long NumeroSequencial { get; set; }

    #endregion

    #region II - Identificação dos tripulantes

    /// <summary>
    /// II - Piloto em Comando
    /// </summary>
    public virtual Tripulante PilotoComando { get; set; } = null!;

    /// <summary>
    /// Código ANAC do Piloto em Comando
    /// </summary>
    public virtual CodigoANAC PilotoComandoCodigo { get; set; } = null!;

    /// <summary>
    /// Função do piloto a bordo
    /// </summary>
    public virtual FuncaoTripulante PilotoComandoFuncao { get; set; } = FuncaoTripulante.P;

    /// <summary>
    /// Horário de apresentação do piloto
    /// </summary>
    public virtual TimeSpan PilotoComandoHorarioApresentacao { get; set; }

    #endregion

    #region III - Data

    /// <summary>
    /// III - Data do voo (formato dd/mm/aaaa)
    /// </summary>
    public virtual DateTime Data { get; set; }

    #endregion

    #region IV - Locais de pouso e decolagem

    /// <summary>
    /// IV - Local de decolagem (IATA/OACI/coordenadas)
    /// </summary>
    public virtual CodigoAeroporto LocalDecolagem { get; set; } = null!;

    /// <summary>
    /// IV - Local de pouso (IATA/OACI/coordenadas)
    /// </summary>
    public virtual CodigoAeroporto LocalPouso { get; set; } = null!;

    #endregion

    #region V - Horários de pouso, decolagem, partida e corte dos motores

    /// <summary>
    /// V - Horário de decolagem em UTC
    /// </summary>
    public virtual DateTime HorarioDecolagemUTC { get; set; }

    /// <summary>
    /// V - Horário de pouso em UTC
    /// </summary>
    public virtual DateTime HorarioPousoUTC { get; set; }

    /// <summary>
    /// V - Horário de partida dos motores em UTC
    /// </summary>
    public virtual DateTime HorarioPartidaMotoresUTC { get; set; }

    /// <summary>
    /// V - Horário de corte dos motores em UTC
    /// </summary>
    public virtual DateTime HorarioCorteMotoresUTC { get; set; }

    #endregion

    #region VI - Tempo de voo IFR

    /// <summary>
    /// VI - Tempo de voo IFR em horas decimais
    /// </summary>
    public virtual decimal? TempoVooIFR { get; set; }

    #endregion

    #region VII - Total de combustível por etapa de voo

    /// <summary>
    /// VII - Quantidade de combustível
    /// </summary>
    public virtual decimal CombustivelQuantidade { get; set; }

    /// <summary>
    /// VII - Unidade de medida do combustível (kg, lb, litros)
    /// </summary>
    public virtual string CombustivelUnidade { get; set; } = string.Empty;

    #endregion

    #region VIII - Natureza do voo

    /// <summary>
    /// VIII - Natureza do voo (privado, comercial, outro)
    /// </summary>
    public virtual NaturezaVoo NaturezaVoo { get; set; }

    /// <summary>
    /// Descrição adicional quando natureza é "Outro"
    /// </summary>
    public virtual string? NaturezaVooOutro { get; set; }

    #endregion

    #region IX - Quantidade de pessoas a bordo

    /// <summary>
    /// IX - Quantidade de pessoas a bordo
    /// </summary>
    public virtual int QuantidadePessoasAbordo { get; set; }

    #endregion

    #region X - Carga transportada

    /// <summary>
    /// X - Quantidade de carga transportada
    /// </summary>
    public virtual decimal? CargaQuantidade { get; set; }

    /// <summary>
    /// X - Unidade de medida da carga (kg, lb)
    /// </summary>
    public virtual string? CargaUnidade { get; set; }

    #endregion

    #region XI - Ocorrências

    /// <summary>
    /// XI - Ocorrências durante o voo
    /// </summary>
    public virtual string? Ocorrencias { get; set; }

    #endregion

    #region XII - Discrepâncias técnicas e pessoa que as detectou

    /// <summary>
    /// XII - Discrepâncias técnicas identificadas
    /// </summary>
    public virtual string? DiscrepanciasTecnicas { get; set; }

    /// <summary>
    /// XII - Pessoa que detectou as discrepâncias
    /// </summary>
    public virtual string? PessoaDetectouDiscrepancia { get; set; }

    #endregion

    #region XIII - Ações corretivas

    /// <summary>
    /// XIII - Ações corretivas realizadas
    /// </summary>
    public virtual string? AcoesCorretivas { get; set; }

    #endregion

    #region XIV - Tipo da última intervenção de manutenção

    /// <summary>
    /// XIV - Tipo da última intervenção de manutenção (exceto trânsito e diária)
    /// </summary>
    public virtual string? TipoUltimaManutencao { get; set; }

    #endregion

    #region XV - Tipo da próxima intervenção de manutenção

    /// <summary>
    /// XV - Tipo da próxima intervenção de manutenção (exceto trânsito e diária)
    /// </summary>
    public virtual string? TipoProximaManutencao { get; set; }

    #endregion

    #region XVI - Horas de célula previstas para a próxima intervenção

    /// <summary>
    /// XVI - Horas de célula previstas para a próxima intervenção de manutenção
    /// </summary>
    public virtual decimal? HorasCelulaProximaManutencao { get; set; }

    #endregion

    #region XVII - Responsável pela aprovação para retorno ao serviço

    /// <summary>
    /// XVII - Responsável pela aprovação para retorno ao serviço
    /// </summary>
    public virtual string? ResponsavelAprovacaoRetorno { get; set; }

    #endregion

    #region Relacionamentos e Controle

    /// <summary>
    /// Aeronave do registro
    /// </summary>
    public virtual Aeronave Aeronave { get; set; } = null!;

    /// <summary>
    /// ID da aeronave (FK)
    /// </summary>
    public virtual int AeronaveId { get; set; }

    /// <summary>
    /// Assinaturas digitais deste registro
    /// </summary>
    public virtual IList<AssinaturaRegistro> Assinaturas { get; set; } = new List<AssinaturaRegistro>();

    #endregion

    #region Status de Assinatura

    /// <summary>
    /// Indica se foi assinado pelo piloto (Art. 6º)
    /// </summary>
    public virtual bool AssinadoPiloto { get; set; } = false;

    /// <summary>
    /// Data/hora da assinatura do piloto em UTC
    /// </summary>
    public virtual DateTime? DataAssinaturaPilotoUTC { get; set; }

    /// <summary>
    /// Indica se foi assinado pelo operador (Art. 9º)
    /// </summary>
    public virtual bool AssinadoOperador { get; set; } = false;

    /// <summary>
    /// Data/hora da assinatura do operador em UTC
    /// </summary>
    public virtual DateTime? DataAssinaturaOperadorUTC { get; set; }

    #endregion

    #region Integração ANAC

    /// <summary>
    /// Hash SHA-256 do registro para integridade
    /// </summary>
    public virtual string? HashRegistro { get; set; }

    /// <summary>
    /// Indica se foi sincronizado com ANAC
    /// </summary>
    public virtual bool SincronizadoANAC { get; set; } = false;

    /// <summary>
    /// Data/hora da sincronização com ANAC
    /// </summary>
    public virtual DateTime? DataSincronizacaoANAC { get; set; }

    #endregion

    #region Auditoria

    /// <summary>
    /// Usuário que criou o registro
    /// </summary>
    public virtual string CriadoPor { get; set; } = string.Empty;

    /// <summary>
    /// Usuário que modificou o registro pela última vez
    /// </summary>
    public virtual string? ModificadoPor { get; set; }

    #endregion

    #region Propriedades Calculadas para Compatibilidade

    /// <summary>
    /// Status de assinatura do piloto (compatibilidade)
    /// </summary>
    public virtual bool StatusAssinaturaPiloto
    {
        get => AssinadoPiloto;
        set => AssinadoPiloto = value;
    }

    /// <summary>
    /// Status de assinatura do operador (compatibilidade)
    /// </summary>
    public virtual bool StatusAssinaturaOperador
    {
        get => AssinadoOperador;
        set => AssinadoOperador = value;
    }

    /// <summary>
    /// Data de assinatura do piloto (compatibilidade)
    /// </summary>
    public virtual DateTime? DataAssinaturaPiloto
    {
        get => DataAssinaturaPilotoUTC;
        set => DataAssinaturaPilotoUTC = value;
    }

    /// <summary>
    /// Data de assinatura do operador (compatibilidade)
    /// </summary>
    public virtual DateTime? DataAssinaturaOperador
    {
        get => DataAssinaturaOperadorUTC;
        set => DataAssinaturaOperadorUTC = value;
    }

    /// <summary>
    /// Tempo total de voo calculado em decimal (horas)
    /// </summary>
    public virtual decimal TempoVooTotal
    {
        get => CalcularTempoVooTotal();
        set { } // Setter vazio para compatibilidade
    }

    /// <summary>
    /// Data da última atualização (compatibilidade)
    /// </summary>
    public virtual DateTime DataUltimaAtualizacao
    {
        get => DataModificacao ?? DataCriacao;
        set { } // Setter vazio para compatibilidade
    }

    /// <summary>
    /// Último erro de sincronização (compatibilidade)
    /// </summary>
    public virtual string? UltimoErroSincronizacao { get; set; }

    /// <summary>
    /// Hash da assinatura (compatibilidade)
    /// </summary>
    public virtual string? HashAssinatura { get; set; }

    #endregion

    #region Métodos de Negócio

    /// <summary>
    /// Calcula o tempo total de voo (pouso - decolagem)
    /// </summary>
    public virtual decimal CalcularTempoVooTotal()
    {
        var diferenca = HorarioPousoUTC - HorarioDecolagemUTC;
        return (decimal)diferenca.TotalHours;
    }

    /// <summary>
    /// Calcula o tempo de motores ligados (corte - partida)
    /// </summary>
    public virtual decimal CalcularTempoMotoresLigados()
    {
        var diferenca = HorarioCorteMotoresUTC - HorarioPartidaMotoresUTC;
        return (decimal)diferenca.TotalHours;
    }

    /// <summary>
    /// Verifica se o registro pode ser editado
    /// </summary>
    public virtual bool PodeSerEditado()
    {
        // Após assinatura do piloto, apenas campos XIII-XVII podem ser editados pelo operador
        return !AssinadoPiloto || !AssinadoOperador;
    }

    /// <summary>
    /// Verifica se os campos de manutenção (XIII-XVII) podem ser editados
    /// </summary>
    public virtual bool PodeEditarCamposManutencao()
    {
        return AssinadoPiloto && !AssinadoOperador;
    }

    /// <summary>
    /// Marca o registro como assinado pelo piloto
    /// </summary>
    public virtual void MarcarAssinadoPiloto(string usuarioId)
    {
        if (AssinadoPiloto)
            throw new InvalidOperationException("Registro já foi assinado pelo piloto");

        AssinadoPiloto = true;
        DataAssinaturaPilotoUTC = DateTime.UtcNow;
        ModificadoPor = usuarioId;
        DataModificacao = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca o registro como assinado pelo operador
    /// </summary>
    public virtual void MarcarAssinadoOperador(string usuarioId)
    {
        if (!AssinadoPiloto)
            throw new InvalidOperationException("Registro deve ser assinado primeiro pelo piloto");

        if (AssinadoOperador)
            throw new InvalidOperationException("Registro já foi assinado pelo operador");

        AssinadoOperador = true;
        DataAssinaturaOperadorUTC = DateTime.UtcNow;
        ModificadoPor = usuarioId;
        DataModificacao = DateTime.UtcNow;
    }

    /// <summary>
    /// Verifica se o prazo para assinatura do operador foi ultrapassado
    /// </summary>
    public virtual bool PrazoOperadorVencido()
    {
        if (!AssinadoPiloto || AssinadoOperador || DataAssinaturaPilotoUTC == null)
            return false;

        var prazoEmDias = Aeronave.PrazoAssinaturaOperadorDias();
        var dataLimite = DataAssinaturaPilotoUTC.Value.AddDays(prazoEmDias);

        return DateTime.UtcNow > dataLimite;
    }

    /// <summary>
    /// Calcula os dias restantes para assinatura do operador
    /// </summary>
    public virtual int DiasRestantesAssinaturaOperador()
    {
        if (!AssinadoPiloto || AssinadoOperador || DataAssinaturaPilotoUTC == null)
            return 0;

        var prazoEmDias = Aeronave.PrazoAssinaturaOperadorDias();
        var dataLimite = DataAssinaturaPilotoUTC.Value.AddDays(prazoEmDias);

        var diasRestantes = (dataLimite - DateTime.UtcNow).Days;
        return Math.Max(0, diasRestantes);
    }

    /// <summary>
    /// Verifica se o registro está completo (ambas assinaturas)
    /// </summary>
    public virtual bool EstaCompleto()
    {
        return AssinadoPiloto && AssinadoOperador;
    }

    /// <summary>
    /// Gera descrição resumida do voo
    /// </summary>
    public virtual string DescricaoResumo()
    {
        return $"{LocalDecolagem} → {LocalPouso} | {Data:dd/MM/yyyy} | {PilotoComandoCodigo}";
    }

    #endregion
}
