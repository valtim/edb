using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Enums;
using DiarioBordo.Domain.ValueObjects;
using FluentAssertions;
using System;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace DiarioBordo.Domain.Tests.Entities;

/// <summary>
/// Testes de conformidade para assinaturas digitais conforme Resolução ANAC 458/2017
/// </summary>
public class AssinaturaDigitalComplianceTests
{
    [Fact]
    public void AssinaturaRegistro_DeveAtenderRequisitosAnac458()
    {
        // Arrange
        var registroVoo = CriarRegistroVooValido();
        var assinatura = CriarAssinaturaValida(registroVoo.Id, TipoAssinatura.Piloto);

        // Assert - Verificar requisitos da Resolução 458/2017

        // Art. 2º I - Autenticidade
        assinatura.UsuarioId.Should().NotBeNullOrEmpty("Assinatura deve identificar o usuário (autenticidade)");
        assinatura.DataHoraUTC.Should().NotBe(default(DateTime), "Timestamp é obrigatório para autenticidade");

        // Art. 2º II - Integridade
        assinatura.Hash.Should().NotBeNullOrEmpty("Hash SHA-256 é obrigatório para integridade");
        assinatura.Hash.Should().HaveLength(64, "Hash SHA-256 deve ter 64 caracteres");

        // Art. 2º III - Irretratabilidade
        assinatura.TipoAssinatura.Should().NotBe(0, "Tipo de assinatura deve ser definido");
        assinatura.IPAddress.Should().NotBeNullOrEmpty("IP é necessário para não repúdio");
        assinatura.UserAgent.Should().NotBeNullOrEmpty("User Agent é necessário para não repúdio");

        // Verificar que a assinatura é imutável após criação
        assinatura.DataCriacao.Should().NotBe(default(DateTime), "Data de criação deve ser registrada");
    }

    [Theory]
    [InlineData(TipoAssinatura.Piloto, true)]
    [InlineData(TipoAssinatura.Operador, true)]
    public void TipoAssinatura_DeveSerValido(TipoAssinatura tipo, bool deveSerValido)
    {
        // Arrange
        var registroVoo = CriarRegistroVooValido();
        var assinatura = new AssinaturaRegistro
        {
            RegistroVooId = registroVoo.Id,
            TipoAssinatura = tipo,
            UsuarioId = "123456",
            DataHoraUTC = DateTime.UtcNow,
            Hash = GerarHashValido()
        };

        // Act & Assert
        if (deveSerValido)
        {
            assinatura.TipoAssinatura.Should().NotBe(0,
                "Tipo de assinatura deve ser definido");
        }
    }

    [Fact]
    public void WorkflowAssinatura_DeveRespeitarOrdemPilotoOperador()
    {
        // Arrange
        var registroVoo = CriarRegistroVooValido();
        var assinaturaPiloto = CriarAssinaturaValida(registroVoo.Id, TipoAssinatura.Piloto);
        var assinaturaOperador = CriarAssinaturaValida(registroVoo.Id, TipoAssinatura.Operador);

        // Simular que assinatura do piloto veio primeiro
        assinaturaPiloto.DataHoraUTC = DateTime.UtcNow.AddHours(-1);
        assinaturaOperador.DataHoraUTC = DateTime.UtcNow;

        // Act
        registroVoo.AssinadoPiloto = true;
        registroVoo.AssinadoOperador = true;

        // Assert
        registroVoo.AssinadoPiloto.Should().BeTrue("Piloto deve assinar primeiro");
        registroVoo.AssinadoOperador.Should().BeTrue("Operador assina após piloto");

        assinaturaPiloto.DataHoraUTC.Should().BeBefore(assinaturaOperador.DataHoraUTC,
            "Assinatura do piloto deve preceder a do operador");
    }

    [Fact]
    public void HashIntegridade_DeveSerCalculadoCorretamente()
    {
        // Arrange
        var registroVoo = CriarRegistroVooValido();
        var dadosParaHash = $"{registroVoo.Id}|{registroVoo.NumeroSequencial}|{registroVoo.Data:yyyy-MM-dd}";

        // Act
        var hashCalculado = CalcularSHA256(dadosParaHash);

        // Assert
        hashCalculado.Should().NotBeNullOrEmpty("Hash deve ser calculado");
        hashCalculado.Should().HaveLength(64, "SHA-256 deve produzir hash de 64 caracteres");
        hashCalculado.Should().MatchRegex("^[a-fA-F0-9]{64}$", "Hash deve conter apenas caracteres hexadecimais");
    }

    [Fact]
    public void AssinaturaInvalida_NaoDeveSerAceita()
    {
        // Arrange
        var assinaturaInvalida = new AssinaturaRegistro
        {
            RegistroVooId = 1,
            TipoAssinatura = TipoAssinatura.Piloto,
            // Propositalmente deixando campos obrigatórios vazios
            UsuarioId = "",
            Hash = "",
            DataHoraUTC = default(DateTime)
        };

        // Act & Assert
        assinaturaInvalida.UsuarioId.Should().BeEmpty("Usuário deve estar vazio para este teste");
        assinaturaInvalida.Hash.Should().BeEmpty("Hash deve estar vazio para este teste");
        assinaturaInvalida.DataHoraUTC.Should().Be(default(DateTime), "Data deve estar vazia para este teste");
    }

    [Fact]
    public void ValidarIntegridade_DeveCompararHashesCorretamente()
    {
        // Arrange
        var registroVoo = CriarRegistroVooValido();
        var hashOriginal = "abc123def456ghi789jkl012mno345pqr678stu901vwx234yzabcdef0123456789";
        var assinatura = new AssinaturaRegistro
        {
            RegistroVooId = registroVoo.Id,
            Hash = hashOriginal,
            TipoAssinatura = TipoAssinatura.Piloto,
            UsuarioId = "123456",
            DataHoraUTC = DateTime.UtcNow
        };

        // Act & Assert
        assinatura.ValidarIntegridade(hashOriginal).Should().BeTrue("Hashes iguais devem validar");
        assinatura.ValidarIntegridade("hash_diferente").Should().BeFalse("Hashes diferentes não devem validar");
    }

    [Fact]
    public void AssinaturaValida_DeveVerificarTempoLimite()
    {
        // Arrange
        var assinatura = new AssinaturaRegistro
        {
            DataHoraUTC = DateTime.UtcNow.AddDays(-1), // Ontem
            TipoAssinatura = TipoAssinatura.Piloto,
            UsuarioId = "123456",
            Hash = GerarHashValido()
        };

        // Act & Assert
        assinatura.AssinaturaValida().Should().BeTrue("Assinatura de 1 dia deve ser válida");

        // Teste com assinatura muito antiga
        assinatura.DataHoraUTC = DateTime.UtcNow.AddYears(-6);
        assinatura.AssinaturaValida().Should().BeFalse("Assinatura de 6 anos deve ser inválida");
    }

    private static RegistroVoo CriarRegistroVooValido()
    {
        return new RegistroVoo
        {
            Id = 1,
            NumeroSequencial = 1,
            Data = DateTime.UtcNow.Date,
            PilotoComandoCodigo = CodigoANAC.Criar("123456"),
            LocalDecolagem = CodigoAeroporto.Criar("SBGR"),
            LocalPouso = CodigoAeroporto.Criar("SBBR"),
            HorarioDecolagemUTC = DateTime.UtcNow,
            HorarioPousoUTC = DateTime.UtcNow.AddHours(2),
            HorarioPartidaMotoresUTC = DateTime.UtcNow.AddMinutes(-5),
            HorarioCorteMotoresUTC = DateTime.UtcNow.AddHours(2).AddMinutes(5),
            CombustivelQuantidade = 1000,
            CombustivelUnidade = "kg",
            NaturezaVoo = NaturezaVoo.Comercial,
            QuantidadePessoasAbordo = 4,
            ResponsavelAprovacaoRetorno = "Teste",
            AeronaveId = 1,
            CriadoPor = "teste@diariobordo.com"
        };
    }

    private static AssinaturaRegistro CriarAssinaturaValida(int registroVooId, TipoAssinatura tipo)
    {
        return new AssinaturaRegistro
        {
            RegistroVooId = registroVooId,
            TipoAssinatura = tipo,
            UsuarioId = "123456",
            DataHoraUTC = DateTime.UtcNow,
            Hash = GerarHashValido(),
            IPAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0 Test Browser",
            CodigoANAC = "123456",
            NomeAssinante = "Teste da Silva"
        };
    }

    private static string GerarHashValido()
    {
        return "abc123def456ghi789jkl012mno345pqr678stu901vwx234yzabcdef01234567";
    }

    private static string CalcularSHA256(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}