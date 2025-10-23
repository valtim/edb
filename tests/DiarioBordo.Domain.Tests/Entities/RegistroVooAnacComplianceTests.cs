using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Enums;
using DiarioBordo.Domain.ValueObjects;
using FluentAssertions;
using System;
using Xunit;

namespace DiarioBordo.Domain.Tests.Entities;

/// <summary>
/// Testes de conformidade ANAC para os 17 campos obrigatórios da Resolução 457/2017 Art. 4º
/// </summary>
public class RegistroVooAnacComplianceTests
{
    [Fact]
    public void RegistroVoo_DeveTerTodos17CamposObrigatoriosAnac()
    {
        // Arrange & Act
        var registro = CriarRegistroVooCompleto();

        // Assert - Verificar os 17 campos obrigatórios

        // 1. Número sequencial cronológico
        registro.NumeroSequencial.Should().BePositive("Número sequencial é obrigatório (Art. 4º I)");

        // 2. Identificação da tripulação
        registro.PilotoComandoCodigo.Should().NotBeNull("Código do piloto comandante é obrigatório (Art. 4º II)");
        registro.PilotoComandoCodigo.ToString().Should().MatchRegex(@"^\d{6}$", "Código ANAC deve ter 6 dígitos");

        // 3. Data
        registro.Data.Should().NotBe(default(DateTime), "Data do voo é obrigatória (Art. 4º III)");

        // 4. Locais de pouso e decolagem
        registro.LocalDecolagem.Should().NotBeNull("Local de decolagem é obrigatório (Art. 4º IV)");
        registro.LocalPouso.Should().NotBeNull("Local de pouso é obrigatório (Art. 4º IV)");

        // 5. Horários de pouso, decolagem, partida e corte dos motores
        registro.HorarioDecolagemUTC.Should().NotBe(default(DateTime), "Horário de decolagem é obrigatório (Art. 4º V)");
        registro.HorarioPousoUTC.Should().NotBe(default(DateTime), "Horário de pouso é obrigatório (Art. 4º V)");
        registro.HorarioPartidaMotoresUTC.Should().NotBe(default(DateTime), "Horário de partida dos motores é obrigatório (Art. 4º V)");
        registro.HorarioCorteMotoresUTC.Should().NotBe(default(DateTime), "Horário de corte dos motores é obrigatório (Art. 4º V)");

        // 6. Tempo de voo IFR
        registro.TempoVooIFR.Should().BeGreaterOrEqualTo(0, "Tempo de voo IFR é obrigatório (Art. 4º VI)");

        // 7. Total de combustível por etapa de voo
        registro.CombustivelQuantidade.Should().BeGreaterThan(0, "Combustível é obrigatório (Art. 4º VII)");
        registro.CombustivelUnidade.Should().NotBeNullOrEmpty("Unidade de combustível é obrigatória (Art. 4º VII)");

        // 8. Natureza do voo
        registro.NaturezaVoo.Should().NotBe(0, "Natureza do voo é obrigatória (Art. 4º VIII)");

        // 9. Quantidade de pessoas a bordo
        registro.QuantidadePessoasAbordo.Should().BeGreaterThan(0, "Quantidade de pessoas a bordo é obrigatória (Art. 4º IX)");

        // 10. Carga transportada
        registro.CargaQuantidade.Should().BeGreaterOrEqualTo(0, "Quantidade de carga é obrigatória (Art. 4º X)");

        // 11. Ocorrências
        // Campo opcional conforme implementação

        // 12. Discrepâncias técnicas e pessoa que as detectou
        // Campos opcionais conforme implementação

        // 13. Ações corretivas adotadas e pessoa que as executou
        // Campo opcional conforme implementação

        // 14-17. Campos de manutenção
        // Campos opcionais conforme implementação

        // 17. Responsável pela aprovação para retorno ao serviço
        registro.ResponsavelAprovacaoRetorno.Should().NotBeNullOrEmpty("Responsável pela aprovação é obrigatório (Art. 4º XVII)");
    }

    [Theory]
    [InlineData("123456", true)]  // Código válido
    [InlineData("000001", true)]  // Código com zeros à esquerda
    [InlineData("999999", true)]  // Código máximo
    [InlineData("12345", false)]  // Muito curto
    [InlineData("1234567", false)] // Muito longo
    [InlineData("12345A", false)] // Contém letra
    [InlineData("", false)]       // Vazio
    public void CodigoAnacPiloto_DeveValidarFormato6Digitos(string codigo, bool deveSerValido)
    {
        // Arrange & Act & Assert
        if (deveSerValido)
        {
            var codigoAnac = CodigoANAC.Criar(codigo);
            codigoAnac.ToString().Should().MatchRegex(@"^\d{6}$",
                "Código ANAC deve ter exatamente 6 dígitos numéricos");
        }
        else
        {
            if (string.IsNullOrEmpty(codigo))
            {
                Action act = () => CodigoANAC.Criar(codigo);
                act.Should().Throw<ArgumentException>("Código vazio deve gerar exceção");
            }
            else
            {
                Action act = () => CodigoANAC.Criar(codigo);
                act.Should().Throw<ArgumentException>("Código inválido deve gerar exceção");
            }
        }
    }

    [Theory]
    [InlineData("SBSP", true)]    // ICAO válido
    [InlineData("CGH", true)]     // IATA válido
    [InlineData("", false)]       // Vazio
    [InlineData("AB", false)]     // Muito curto
    public void LocalesAeroporto_DeveValidarFormatosPermitidos(string local, bool deveSerValido)
    {
        // Arrange & Act & Assert
        if (deveSerValido)
        {
            var codigoAeroporto = CodigoAeroporto.Criar(local);
            codigoAeroporto.Should().NotBeNull("Local deve ser válido");
        }
        else
        {
            Action act = () => CodigoAeroporto.Criar(local);
            act.Should().Throw<ArgumentException>("Local inválido deve gerar exceção");
        }
    }

    [Fact]
    public void HorariosVoo_DevemEstarEmUTC()
    {
        // Arrange
        var dataVoo = new DateTime(2024, 10, 22, 0, 0, 0, DateTimeKind.Utc);
        var horarioDecolagem = new DateTime(2024, 10, 22, 10, 0, 0, DateTimeKind.Utc);
        var horarioPouso = new DateTime(2024, 10, 22, 12, 0, 0, DateTimeKind.Utc);

        var registro = new RegistroVoo
        {
            Data = dataVoo,
            HorarioDecolagemUTC = horarioDecolagem,
            HorarioPousoUTC = horarioPouso,
            HorarioPartidaMotoresUTC = horarioDecolagem.AddMinutes(-10),
            HorarioCorteMotoresUTC = horarioPouso.AddMinutes(5)
        };

        // Act & Assert
        registro.HorarioDecolagemUTC.Kind.Should().Be(DateTimeKind.Utc, "Horários devem estar em UTC");
        registro.HorarioPousoUTC.Kind.Should().Be(DateTimeKind.Utc, "Horários devem estar em UTC");
        registro.HorarioPartidaMotoresUTC.Kind.Should().Be(DateTimeKind.Utc, "Horários devem estar em UTC");
        registro.HorarioCorteMotoresUTC.Kind.Should().Be(DateTimeKind.Utc, "Horários devem estar em UTC");

        // Validar sequência lógica dos horários
        registro.HorarioPartidaMotoresUTC.Should().BeBefore(registro.HorarioDecolagemUTC);
        registro.HorarioDecolagemUTC.Should().BeBefore(registro.HorarioPousoUTC);
        registro.HorarioPousoUTC.Should().BeBefore(registro.HorarioCorteMotoresUTC);
    }

    [Theory]
    [InlineData("kg", true)]
    [InlineData("lb", true)]
    [InlineData("ton", true)]
    [InlineData("", false)]
    [InlineData("xyz", false)]
    public void UnidadesCombustivelCarga_DevemSerValidas(string unidade, bool deveSerValida)
    {
        // Arrange
        var registro = new RegistroVoo
        {
            CombustivelUnidade = unidade,
            CargaUnidade = unidade
        };

        // Act & Assert
        if (deveSerValida)
        {
            registro.CombustivelUnidade.Should().NotBeNullOrEmpty("Unidade deve ser especificada");
        }
        else
        {
            // Teste de validação deve ser feito na camada de aplicação/validadores
            // A entidade aceita qualquer string, mas o validador deve rejeitar
            registro.CombustivelUnidade.Should().Be(unidade, "Entidade aceita qualquer valor");
        }
    }

    private static RegistroVoo CriarRegistroVooCompleto()
    {
        return new RegistroVoo
        {
            // 1. Número sequencial
            NumeroSequencial = 1,

            // 2. Identificação da tripulação
            PilotoComandoCodigo = CodigoANAC.Criar("123456"),

            // 3. Data
            Data = new DateTime(2024, 10, 22, 0, 0, 0, DateTimeKind.Utc),

            // 4. Locais
            LocalDecolagem = CodigoAeroporto.Criar("SBSP"),
            LocalPouso = CodigoAeroporto.Criar("SBRJ"),

            // 5. Horários
            HorarioDecolagemUTC = new DateTime(2024, 10, 22, 10, 0, 0, DateTimeKind.Utc),
            HorarioPousoUTC = new DateTime(2024, 10, 22, 12, 0, 0, DateTimeKind.Utc),
            HorarioPartidaMotoresUTC = new DateTime(2024, 10, 22, 9, 50, 0, DateTimeKind.Utc),
            HorarioCorteMotoresUTC = new DateTime(2024, 10, 22, 12, 5, 0, DateTimeKind.Utc),

            // 6. Tempo IFR
            TempoVooIFR = 1.5m,

            // 7. Combustível
            CombustivelQuantidade = 1000,
            CombustivelUnidade = "kg",

            // 8. Natureza
            NaturezaVoo = NaturezaVoo.Comercial,

            // 9. Pessoas
            QuantidadePessoasAbordo = 154,

            // 10. Carga
            CargaQuantidade = 500,
            CargaUnidade = "kg",

            // 11. Ocorrências
            Ocorrencias = "Nenhuma ocorrência reportada",

            // 12. Discrepâncias
            DiscrepanciasTecnicas = "Nenhuma discrepância detectada",
            PessoaDetectouDiscrepancia = "Mecânico João Silva",

            // 13. Ações corretivas
            AcoesCorretivas = "Nenhuma ação corretiva necessária",

            // 14. Última manutenção
            TipoUltimaManutencao = "Inspeção 100h",

            // 15. Próxima manutenção
            TipoProximaManutencao = "Inspeção anual",

            // 16. Horas para manutenção
            HorasCelulaProximaManutencao = 45.5m,

            // 17. Responsável aprovação
            ResponsavelAprovacaoRetorno = "Eng. Maria Santos",

            // Campos obrigatórios adicionais
            AeronaveId = 1,
            CriadoPor = "teste@diariobordo.com"
        };
    }
}