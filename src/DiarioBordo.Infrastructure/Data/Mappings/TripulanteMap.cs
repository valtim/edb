using FluentNHibernate.Mapping;
using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.ValueObjects;

namespace DiarioBordo.Infrastructure.Data.Mappings;

/// <summary>
/// Mapeamento FluentNHibernate para Tripulante
/// </summary>
public class TripulanteMap : ClassMap<Tripulante>
{
    public TripulanteMap()
    {
        Table("Tripulantes");

        // Chave primária
        Id(x => x.Id).GeneratedBy.Identity();

        // Código ANAC como Value Object
        Map(x => x.CodigoANAC)
            .CustomType<string>()
            .Access.CamelCaseField(Prefix.Underscore)
            .Column("CodigoANAC")
            .Length(6)
            .Not.Nullable()
            .Unique();

        // Campos principais
        Map(x => x.Nome).Length(200).Not.Nullable();

        // CPF como Value Object
        Map(x => x.CPF)
            .CustomType<string>()
            .Access.CamelCaseField(Prefix.Underscore)
            .Column("CPF")
            .Length(11)
            .Not.Nullable()
            .Unique();

        Map(x => x.Email).Length(200).Nullable();
        Map(x => x.Telefone).Length(20).Nullable();
        Map(x => x.Ativo).Not.Nullable();

        // Campos de auditoria
        Map(x => x.DataCriacao).Not.Nullable();
        Map(x => x.DataModificacao).Nullable();

        // Funções autorizadas como conjunto
        HasManyToMany(x => x.FuncoesAutorizadas)
            .Table("TripulanteFuncoes")
            .ParentKeyColumn("TripulanteId")
            .ChildKeyColumn("FuncaoId")
            .Element("Funcao", e => e.Type<int>())
            .Cascade.All();

        // Relacionamentos
        HasMany(x => x.RegistrosComoPiloto)
            .KeyColumn("PilotoComandoId")
            .Cascade.None()
            .Inverse()
            .LazyLoad();

        // Cache de segunda-level
        Cache.ReadWrite().Region("Tripulantes");
    }
}