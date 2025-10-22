using DiarioBordo.Domain.ValueObjects;
using DiarioBordo.Domain.Enums;

namespace DiarioBordo.Domain.Entities;

/// <summary>
/// Entidade Tripulante conforme Art. 5º I Resolução ANAC 457/2017
/// </summary>
public class Tripulante : BaseEntity
{
    /// <summary>
    /// Código ANAC de 6 dígitos do tripulante
    /// </summary>
    public virtual CodigoANAC CodigoANAC { get; set; } = null!;

    /// <summary>
    /// Nome completo do tripulante
    /// </summary>
    public virtual string Nome { get; set; } = string.Empty;

    /// <summary>
    /// CPF do tripulante (apenas números)
    /// </summary>
    public virtual string CPF { get; set; } = string.Empty;

    /// <summary>
    /// Email do tripulante
    /// </summary>
    public virtual string? Email { get; set; }

    /// <summary>
    /// Telefone do tripulante
    /// </summary>
    public virtual string? Telefone { get; set; }

    /// <summary>
    /// Indica se o tripulante está ativo no sistema
    /// </summary>
    public virtual bool Ativo { get; set; } = true;

    /// <summary>
    /// Funções que o tripulante pode exercer
    /// </summary>
    public virtual ISet<FuncaoTripulante> FuncoesAutorizadas { get; set; } = new HashSet<FuncaoTripulante>();

    /// <summary>
    /// Registros de voo onde este tripulante foi piloto em comando
    /// </summary>
    public virtual IList<RegistroVoo> RegistrosComoPiloto { get; set; } = new List<RegistroVoo>();

    /// <summary>
    /// Verifica se o tripulante pode exercer uma função específica
    /// </summary>
    public virtual bool PodeExercerFuncao(FuncaoTripulante funcao)
    {
        return Ativo && FuncoesAutorizadas.Contains(funcao);
    }

    /// <summary>
    /// Adiciona uma função autorizada ao tripulante
    /// </summary>
    public virtual void AdicionarFuncao(FuncaoTripulante funcao)
    {
        FuncoesAutorizadas.Add(funcao);
    }

    /// <summary>
    /// Remove uma função autorizada do tripulante
    /// </summary>
    public virtual void RemoverFuncao(FuncaoTripulante funcao)
    {
        FuncoesAutorizadas.Remove(funcao);
    }

    /// <summary>
    /// Funções autorizadas (alias para compatibilidade)
    /// </summary>
    public virtual IList<FuncaoTripulante> Funcoes
    {
        get => FuncoesAutorizadas.ToList();
        set
        {
            FuncoesAutorizadas.Clear();
            foreach (var funcao in value)
                FuncoesAutorizadas.Add(funcao);
        }
    }

    /// <summary>
    /// Validade da licença (para compatibilidade - implementar conforme necessário)
    /// </summary>
    public virtual DateTime? ValidadeLicenca { get; set; }

    /// <summary>
    /// ID do operador (para compatibilidade - implementar conforme necessário)
    /// </summary>
    public virtual int OperadorId { get; set; }

    /// <summary>
    /// Data da última atualização (compatibilidade)
    /// </summary>
    public virtual DateTime DataUltimaAtualizacao
    {
        get => DataModificacao ?? DataCriacao;
        set { } // Setter vazio para compatibilidade
    }

    /// <summary>
    /// Obtém o histórico de voos do tripulante em um período
    /// </summary>
    public virtual IEnumerable<RegistroVoo> HistoricoVoos(DateTime dataInicio, DateTime dataFim)
    {
        return RegistrosComoPiloto
            .Where(r => r.Data >= dataInicio && r.Data <= dataFim)
            .OrderByDescending(r => r.Data);
    }

    /// <summary>
    /// Calcula total de horas voadas em um período
    /// </summary>
    public virtual decimal TotalHorasVoadas(DateTime dataInicio, DateTime dataFim)
    {
        return RegistrosComoPiloto
            .Where(r => r.Data >= dataInicio && r.Data <= dataFim)
            .Sum(r => r.CalcularTempoVooTotal());
    }
}