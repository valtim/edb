using FluentNHibernate.Mapping;
using DiarioBordo.Domain.Entities;

namespace DiarioBordo.Infrastructure.Data.Mappings;

/// <summary>
/// Mapeamento FluentNHibernate para Aeronave
/// </summary>
public class AeronaveMap : ClassMap<Aeronave>
{
    public AeronaveMap()
    {
        Table("Aeronaves");

        // Chave primária
        Id(x => x.Id).GeneratedBy.Identity();

        // Campos obrigatórios conforme Art. 8º I Res. 457/2017
        Map(x => x.Matricula).Length(10).Not.Nullable().Unique();
        Map(x => x.MarcaNacionalidade).Length(5).Not.Nullable();
        Map(x => x.Fabricante).Length(100).Not.Nullable();
        Map(x => x.Modelo).Length(100).Not.Nullable();
        Map(x => x.NumeroSerie).Length(50).Not.Nullable();
        Map(x => x.CategoriaRegistro).Length(50).Not.Nullable();

        // Campos de controle
        Map(x => x.Ativa).Not.Nullable();
        Map(x => x.DataCancelamentoRAB).Nullable();
        Map(x => x.TipoRBACOperador).CustomType<int>().Not.Nullable();
        Map(x => x.HorasCelulaTotal).Precision(10).Scale(2).Not.Nullable();

        // Campos de auditoria da BaseEntity
        Map(x => x.DataCriacao).Not.Nullable();
        Map(x => x.DataModificacao).Nullable();

        // Relacionamentos
        HasMany(x => x.RegistrosVoo)
            .KeyColumn("AeronaveId")
            .Cascade.All()
            .Inverse()
            .LazyLoad();

        // Índices críticos para performance
        // idx_matricula será criado automaticamente pelo Unique()

        // Cache de segunda-level para consultas frequentes
        Cache.ReadWrite().Region("Aeronaves");
    }
}