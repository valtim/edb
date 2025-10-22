using FluentNHibernate.Mapping;
using DiarioBordo.Domain.Entities;

namespace DiarioBordo.Infrastructure.Data.Mappings;

/// <summary>
/// Mapeamento FluentNHibernate para RegistroVoo - Entidade crítica com todos os 17 campos obrigatórios
/// </summary>
public class RegistroVooMap : ClassMap<RegistroVoo>
{
    public RegistroVooMap()
    {
        Table("RegistrosVoo");

        // Chave primária
        Id(x => x.Id).GeneratedBy.Identity();

        // I - Número sequencial cronológico
        Map(x => x.NumeroSequencial).Not.Nullable();

        // III - Data
        Map(x => x.Data).Not.Nullable();

        // II - Identificação da tripulação
        Map(x => x.PilotoComandoFuncao).CustomType<int>().Length(1).Not.Nullable();
        Map(x => x.PilotoComandoHorarioApresentacao).Not.Nullable();

        // IV - Locais de pouso e decolagem
        Map(x => x.LocalDecolagem).Length(50).Not.Nullable();
        Map(x => x.LocalPouso).Length(50).Not.Nullable();

        // V - Horários UTC (pouso, decolagem, partida, corte motores)
        Map(x => x.HorarioDecolagemUTC).Not.Nullable();
        Map(x => x.HorarioPousoUTC).Not.Nullable();
        Map(x => x.HorarioPartidaMotoresUTC).Not.Nullable();
        Map(x => x.HorarioCorteMotoresUTC).Not.Nullable();

        // VI - Tempo de voo IFR
        Map(x => x.TempoVooIFR).Precision(5).Scale(2).Nullable();

        // VII - Total de combustível por etapa
        Map(x => x.CombustivelQuantidade).Precision(10).Scale(2).Not.Nullable();
        Map(x => x.CombustivelUnidade).Length(10).Not.Nullable();

        // VIII - Natureza do voo
        Map(x => x.NaturezaVoo).CustomType<int>().Not.Nullable();
        Map(x => x.NaturezaVooOutro).Length(100).Nullable(); // Para quando NaturezaVoo = Outro

        // IX - Quantidade de pessoas a bordo
        Map(x => x.QuantidadePessoasAbordo).Not.Nullable();

        // X - Carga transportada
        Map(x => x.CargaQuantidade).Precision(10).Scale(2).Nullable();
        Map(x => x.CargaUnidade).Length(10).Nullable();

        // XI - Ocorrências
        Map(x => x.Ocorrencias).CustomSqlType("TEXT").Nullable();

        // XII - Discrepâncias técnicas e pessoa que detectou
        Map(x => x.DiscrepanciasTecnicas).CustomSqlType("TEXT").Nullable();
        Map(x => x.PessoaDetectouDiscrepancia).Length(200).Nullable();

        // XIII - Ações corretivas
        Map(x => x.AcoesCorretivas).CustomSqlType("TEXT").Nullable();

        // XIV - Tipo da última intervenção de manutenção
        Map(x => x.TipoUltimaManutencao).Length(100).Nullable();

        // XV - Tipo da próxima intervenção de manutenção
        Map(x => x.TipoProximaManutencao).Length(100).Nullable();

        // XVI - Horas de célula previstas para próxima manutenção
        Map(x => x.HorasCelulaProximaManutencao).Precision(10).Scale(2).Nullable();

        // XVII - Responsável pela aprovação para retorno ao serviço
        Map(x => x.ResponsavelAprovacaoRetorno).Length(200).Nullable();

        // Campos de controle de assinatura
        Map(x => x.AssinadoPiloto).Not.Nullable();
        Map(x => x.DataAssinaturaPilotoUTC).Nullable();
        Map(x => x.AssinadoOperador).Not.Nullable();
        Map(x => x.DataAssinaturaOperadorUTC).Nullable();
        Map(x => x.HashRegistro).Length(128).Nullable();

        // Controle de sincronização ANAC
        Map(x => x.SincronizadoANAC).Not.Nullable();
        Map(x => x.DataSincronizacaoANAC).Nullable();

        // Campos de auditoria
        Map(x => x.CriadoPor).Length(100).Not.Nullable();
        Map(x => x.DataCriacao).Not.Nullable();
        Map(x => x.ModificadoPor).Length(100).Nullable();
        Map(x => x.DataModificacao).Nullable();

        // Relacionamentos
        References(x => x.Aeronave)
            .Column("AeronaveId")
            .Not.Nullable()
            .LazyLoad();

        References(x => x.PilotoComando)
            .Column("PilotoComandoId")
            .Not.Nullable()
            .LazyLoad();

        HasMany(x => x.Assinaturas)
            .KeyColumn("RegistroVooId")
            .Cascade.All()
            .Inverse()
            .LazyLoad();

        // Índices críticos para performance ANAC
        // Consulta últimos 30 dias - requisito crítico Art. 8º II
        Map(x => x.AeronaveId).Index("idx_aeronave_data_30dias");
        Map(x => x.Data).Index("idx_aeronave_data_30dias");

        // Workflow de assinaturas
        Map(x => x.AssinadoPiloto).Index("idx_pendente_assinatura_piloto");
        Map(x => x.AssinadoOperador).Index("idx_pendente_assinatura_operador");

        // Sincronização ANAC
        Map(x => x.SincronizadoANAC).Index("idx_sincronizacao_anac");

        // Cache de segunda-level para registros recentes
        Cache.ReadWrite().Region("RegistrosVoo");
    }
}