using DiarioBordo.Domain.Enums;

namespace DiarioBordo.Domain.Entities;

/// <summary>
/// Entidade Aeronave conforme Art. 8º I Resolução ANAC 457/2017
/// </summary>
public class Aeronave : BaseEntity
{
    /// <summary>
    /// Matrícula da aeronave (ex: PT-ABC, PP-XYZ)
    /// </summary>
    public virtual string Matricula { get; set; } = string.Empty;

    /// <summary>
    /// Marcas de nacionalidade (ex: PT, PP)
    /// </summary>
    public virtual string MarcaNacionalidade { get; set; } = string.Empty;

    /// <summary>
    /// Fabricante da aeronave
    /// </summary>
    public virtual string Fabricante { get; set; } = string.Empty;

    /// <summary>
    /// Modelo da aeronave
    /// </summary>
    public virtual string Modelo { get; set; } = string.Empty;

    /// <summary>
    /// Número de série da aeronave
    /// </summary>
    public virtual string NumeroSerie { get; set; } = string.Empty;

    /// <summary>
    /// Categoria de registro da aeronave no RAB
    /// </summary>
    public virtual string CategoriaRegistro { get; set; } = string.Empty;

    /// <summary>
    /// Indica se a aeronave está ativa no RAB
    /// </summary>
    public virtual bool Ativa { get; set; } = true;

    /// <summary>
    /// Data de cancelamento da matrícula no RAB (se aplicável)
    /// </summary>
    public virtual DateTime? DataCancelamentoRAB { get; set; }

    /// <summary>
    /// Tipo RBAC do operador para cálculo de prazos
    /// </summary>
    public virtual TipoRBAC TipoRBACOperador { get; set; } = TipoRBAC.Outros;

    /// <summary>
    /// Horas totais de célula da aeronave
    /// </summary>
    public virtual decimal HorasCelulaTotal { get; set; }

    /// <summary>
    /// Ano de fabricação da aeronave (compatibilidade)
    /// </summary>
    public virtual int? AnoFabricacao { get; set; }

    /// <summary>
    /// Horas totais de célula (alias para compatibilidade)
    /// </summary>
    public virtual decimal HorasTotaisCelula
    {
        get => HorasCelulaTotal;
        set => HorasCelulaTotal = value;
    }

    /// <summary>
    /// Tipo RBAC (alias para compatibilidade)
    /// </summary>
    public virtual TipoRBAC TipoRBAC
    {
        get => TipoRBACOperador;
        set => TipoRBACOperador = value;
    }

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
    /// Alias para Ativa (compatibilidade com services)
    /// </summary>
    public virtual bool Ativo
    {
        get => Ativa;
        set => Ativa = value;
    }

    /// <summary>
    /// Registros de voo desta aeronave
    /// </summary>
    public virtual IList<RegistroVoo> RegistrosVoo { get; set; } = new List<RegistroVoo>();

    /// <summary>
    /// Gera o próximo número sequencial para registro de voo
    /// </summary>
    public virtual long ProximoNumeroSequencial()
    {
        if (!RegistrosVoo.Any())
            return 1;

        return RegistrosVoo.Max(r => r.NumeroSequencial) + 1;
    }

    /// <summary>
    /// Obtém os registros dos últimos 30 dias (Art. 8º II Res. 457/2017)
    /// </summary>
    public virtual IEnumerable<RegistroVoo> RegistrosUltimos30Dias()
    {
        var dataLimite = DateTime.Today.AddDays(-30);
        return RegistrosVoo
            .Where(r => r.Data >= dataLimite)
            .OrderByDescending(r => r.Data);
    }

    /// <summary>
    /// Verifica se a aeronave pode ter novos registros
    /// </summary>
    public virtual bool PodeReceberNovoRegistro()
    {
        return Ativa && DataCancelamentoRAB == null;
    }

    /// <summary>
    /// Obtém o prazo em dias para assinatura do operador baseado no tipo RBAC
    /// </summary>
    public virtual int PrazoAssinaturaOperadorDias()
    {
        return TipoRBACOperador switch
        {
            TipoRBAC.RBAC121 => 2,   // Art. 9º § 1º I
            TipoRBAC.RBAC135 => 15,  // Art. 9º § 1º II  
            TipoRBAC.Outros => 30,   // Art. 9º § 1º III
            _ => 30
        };
    }
}