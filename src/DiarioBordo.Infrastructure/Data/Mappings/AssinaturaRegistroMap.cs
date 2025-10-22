using FluentNHibernate.Mapping;
using DiarioBordo.Domain.Entities;

namespace DiarioBordo.Infrastructure.Data.Mappings;

/// <summary>
/// Mapeamento FluentNHibernate para AssinaturaRegistro - Auditoria de assinaturas digitais
/// </summary>
public class AssinaturaRegistroMap : ClassMap<AssinaturaRegistro>
{
    public AssinaturaRegistroMap()
    {
        Table("AssinaturasRegistro");

        // Chave primária
        Id(x => x.Id).GeneratedBy.Identity();

        // Campos principais da assinatura digital
        Map(x => x.UsuarioId).Length(100).Not.Nullable();
        Map(x => x.TipoAssinatura).CustomType<int>().Not.Nullable();
        Map(x => x.DataHoraUTC).Not.Nullable();
        Map(x => x.Hash).Length(128).Not.Nullable(); // SHA-256 = 64 chars hex, mas deixamos 128 para futuro

        // Campos de auditoria
        Map(x => x.IPAddress).Length(45).Nullable(); // IPv6 máximo
        Map(x => x.UserAgent).Length(500).Nullable();
        Map(x => x.DadosAdicionais).CustomSqlType("TEXT").Nullable();

        // Campos de BaseEntity
        Map(x => x.DataCriacao).Not.Nullable();
        Map(x => x.DataModificacao).Nullable();

        // Relacionamento com RegistroVoo
        References(x => x.RegistroVoo)
            .Column("RegistroVooId")
            .Not.Nullable()
            .LazyLoad();

        // Índices para auditoria e consultas
        Map(x => x.RegistroVooId).Index("idx_registro_assinatura");
        Map(x => x.UsuarioId).Index("idx_usuario_assinatura");
        Map(x => x.DataHoraUTC).Index("idx_data_assinatura");

        // Não usar cache para dados de auditoria (sempre fresh)
    }
}