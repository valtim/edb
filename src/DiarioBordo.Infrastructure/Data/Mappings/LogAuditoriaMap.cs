using FluentNHibernate.Mapping;
using DiarioBordo.Domain.Entities;

namespace DiarioBordo.Infrastructure.Data.Mappings;

/// <summary>
/// Mapeamento FluentNHibernate para LogAuditoria - Sistema de logs conforme Res. 458/2017
/// </summary>
public class LogAuditoriaMap : ClassMap<LogAuditoria>
{
    public LogAuditoriaMap()
    {
        Table("LogsAuditoria");

        // Chave primária (BIGINT para suportar muitos logs)
        Id(x => x.Id).GeneratedBy.Identity().Column("Id").CustomSqlType("BIGINT");

        // Campos principais do log
        Map(x => x.DataHoraUTC).Not.Nullable();
        Map(x => x.Operacao).Length(50).Not.Nullable();
        Map(x => x.Tabela).Length(50).Not.Nullable();
        Map(x => x.RegistroId).Not.Nullable();
        Map(x => x.UsuarioId).Length(100).Not.Nullable();

        // Dados da operação em JSON
        Map(x => x.DadosAnteriores).CustomSqlType("JSON").Nullable();
        Map(x => x.DadosNovos).CustomSqlType("JSON").Nullable();

        // Informações de auditoria
        Map(x => x.IPAddress).Length(45).Nullable();
        Map(x => x.UserAgent).Length(500).Nullable();
        Map(x => x.InformacoesAdicionais).CustomSqlType("TEXT").Nullable();

        // Status da operação
        Map(x => x.Sucesso).Not.Nullable();
        Map(x => x.MensagemErro).CustomSqlType("TEXT").Nullable();

        // Campos de BaseEntity
        Map(x => x.DataCriacao).Not.Nullable();
        Map(x => x.DataModificacao).Nullable();

        // Índices críticos para consultas de auditoria
        Map(x => x.DataHoraUTC).Index("idx_auditoria_data");
        Map(x => x.UsuarioId).Index("idx_auditoria_usuario");
        Map(x => x.Operacao).Index("idx_auditoria_operacao");

        // Índice composto para consultas por tabela+registro
        Map(x => x.Tabela).Index("idx_auditoria_tabela_registro");
        Map(x => x.RegistroId).Index("idx_auditoria_tabela_registro");

        // Não usar cache - logs de auditoria devem sempre ser fresh
    }
}