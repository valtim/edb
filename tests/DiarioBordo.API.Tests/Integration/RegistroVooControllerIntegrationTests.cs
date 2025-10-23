using DiarioBordo.API;
using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DiarioBordo.API.Tests.Integration;

/// <summary>
/// Testes de integração para endpoints da API de Registro de Voo
/// </summary>
public class RegistroVooControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public RegistroVooControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Configurar serviços de teste (banco em memória, etc.)
                // Isso seria implementado conforme a configuração do projeto
            });
        });

        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task GET_RegistrosVoo_DeveRetornarStatus200()
    {
        // Act
        var response = await _client.GetAsync("/api/registros-voo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task POST_RegistroVoo_ComDadosValidos_DeveRetornarStatus201()
    {
        // Arrange
        var novoRegistro = CriarRegistroVooDto();

        // Act
        var response = await _client.PostAsJsonAsync("/api/registros-voo", novoRegistro, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var locationHeader = response.Headers.Location;
        locationHeader.Should().NotBeNull();
        locationHeader!.ToString().Should().Contain("/api/registros-voo/");

        var registroCriado = await response.Content.ReadFromJsonAsync<RegistroVooDto>(_jsonOptions);
        registroCriado.Should().NotBeNull();
        registroCriado!.Id.Should().BePositive();
        registroCriado.NumeroSequencial.Should().Be(novoRegistro.NumeroSequencial);
    }

    [Fact]
    public async Task POST_RegistroVoo_ComDadosInvalidos_DeveRetornarStatus400()
    {
        // Arrange
        var registroInvalido = new RegistroVooDto
        {
            // Propositalmente omitindo campos obrigatórios
            PilotoComandoCodigo = "123", // Código inválido (menos de 6 dígitos)
            LocalDecolagem = "", // Local vazio
            Data = default(DateTime) // Data inválida
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/registros-voo", registroInvalido, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("validation");
    }

    [Fact]
    public async Task GET_RegistroVoo_PorId_DeveRetornarRegistroCorreto()
    {
        // Arrange
        var novoRegistro = CriarRegistroVooDto();
        var responseCreate = await _client.PostAsJsonAsync("/api/registros-voo", novoRegistro, _jsonOptions);
        var registroCriado = await responseCreate.Content.ReadFromJsonAsync<RegistroVooDto>(_jsonOptions);

        // Act
        var response = await _client.GetAsync($"/api/registros-voo/{registroCriado!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var registro = await response.Content.ReadFromJsonAsync<RegistroVooDto>(_jsonOptions);
        registro.Should().NotBeNull();
        registro!.Id.Should().Be(registroCriado.Id);
        registro.PilotoComandoCodigo.Should().Be(novoRegistro.PilotoComandoCodigo);
    }

    [Fact]
    public async Task GET_RegistroVoo_Inexistente_DeveRetornarStatus404()
    {
        // Act
        var response = await _client.GetAsync("/api/registros-voo/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_RegistroVoo_ComDadosValidos_DeveRetornarStatus200()
    {
        // Arrange
        var novoRegistro = CriarRegistroVooDto();
        var responseCreate = await _client.PostAsJsonAsync("/api/registros-voo", novoRegistro, _jsonOptions);
        var registroCriado = await responseCreate.Content.ReadFromJsonAsync<RegistroVooDto>(_jsonOptions);

        registroCriado!.Ocorrencias = "Ocorrência atualizada";

        // Act
        var response = await _client.PutAsJsonAsync($"/api/registros-voo/{registroCriado.Id}", registroCriado, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var registroAtualizado = await response.Content.ReadFromJsonAsync<RegistroVooDto>(_jsonOptions);
        registroAtualizado.Should().NotBeNull();
        registroAtualizado!.Ocorrencias.Should().Be("Ocorrência atualizada");
    }

    [Fact]
    public async Task DELETE_RegistroVoo_DeveRetornarStatus204()
    {
        // Arrange
        var novoRegistro = CriarRegistroVooDto();
        var responseCreate = await _client.PostAsJsonAsync("/api/registros-voo", novoRegistro, _jsonOptions);
        var registroCriado = await responseCreate.Content.ReadFromJsonAsync<RegistroVooDto>(_jsonOptions);

        // Act
        var response = await _client.DeleteAsync($"/api/registros-voo/{registroCriado!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verificar que o registro foi realmente removido
        var responseGet = await _client.GetAsync($"/api/registros-voo/{registroCriado.Id}");
        responseGet.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_RegistrosVoo_Ultimos30Dias_DeveRespeitarFiltro()
    {
        // Arrange
        var aeronaveId = 1;

        // Act
        var response = await _client.GetAsync($"/api/registros-voo/aeronave/{aeronaveId}/ultimos-30-dias");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var registros = await response.Content.ReadFromJsonAsync<List<RegistroVooDto>>(_jsonOptions);
        registros.Should().NotBeNull();

        // Verificar que todos os registros são dos últimos 30 dias
        var dataLimite = DateTime.UtcNow.AddDays(-30);
        registros!.All(r => r.Data >= dataLimite).Should().BeTrue();
        registros.All(r => r.AeronaveId == aeronaveId).Should().BeTrue();
    }

    [Fact]
    public async Task GET_RegistrosVoo_PendentesSincronizacao_DeveRetornarApenasNaoSincronizados()
    {
        // Act
        var response = await _client.GetAsync("/api/registros-voo/pendentes-sincronizacao");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var registros = await response.Content.ReadFromJsonAsync<List<RegistroVooDto>>(_jsonOptions);
        registros.Should().NotBeNull();

        // Todos devem estar assinados mas não sincronizados
        registros!.All(r => r.AssinadoPiloto && r.AssinadoOperador && !r.SincronizadoANAC).Should().BeTrue();
    }

    [Fact]
    public async Task POST_AssinaturaRegistro_DeveRetornarStatus201()
    {
        // Arrange
        var novoRegistro = CriarRegistroVooDto();
        var responseCreate = await _client.PostAsJsonAsync("/api/registros-voo", novoRegistro, _jsonOptions);
        var registroCriado = await responseCreate.Content.ReadFromJsonAsync<RegistroVooDto>(_jsonOptions);

        var assinatura = new AssinaturaRegistroDto
        {
            RegistroVooId = registroCriado!.Id,
            TipoAssinatura = "PILOTO",
            UsuarioId = "123456"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/assinaturas", assinatura, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var assinaturaCriada = await response.Content.ReadFromJsonAsync<AssinaturaRegistroDto>(_jsonOptions);
        assinaturaCriada.Should().NotBeNull();
        assinaturaCriada!.HashSHA256.Should().NotBeNullOrEmpty();
        assinaturaCriada.TimestampUTC.Should().NotBe(default(DateTime));
    }

    [Theory]
    [InlineData("IATA", "CGH")]
    [InlineData("ICAO", "SBSP")]
    [InlineData("Coordenadas", "S23°32'W046°38'")]
    public async Task POST_RegistroVoo_ComDiferentesFormatosAeroporto_DeveAceitar(string tipo, string codigo)
    {
        // Arrange
        var registro = CriarRegistroVooDto();
        registro.LocalDecolagem = codigo;
        registro.LocalPouso = codigo;

        // Act
        var response = await _client.PostAsJsonAsync("/api/registros-voo", registro, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Formato {tipo} ({codigo}) deve ser aceito");
    }

    [Fact]
    public async Task GET_Estatisticas_DeveRetornarMetricas()
    {
        // Act
        var response = await _client.GetAsync("/api/registros-voo/estatisticas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var estatisticas = await response.Content.ReadFromJsonAsync<EstatisticasDto>(_jsonOptions);
        estatisticas.Should().NotBeNull();
        estatisticas!.TotalRegistros.Should().BeGreaterOrEqualTo(0);
        estatisticas.RegistrosPendentesAssinatura.Should().BeGreaterOrEqualTo(0);
        estatisticas.RegistrosSincronizados.Should().BeGreaterOrEqualTo(0);
    }

    private static RegistroVooDto CriarRegistroVooDto()
    {
        return new RegistroVooDto
        {
            NumeroSequencial = 1,
            PilotoComandoCodigo = "123456",
            Data = DateTime.UtcNow.Date,
            LocalDecolagem = "SBSP",
            LocalPouso = "SBRJ",
            HorarioDecolagemUTC = DateTime.UtcNow,
            HorarioPousoUTC = DateTime.UtcNow.AddHours(2),
            HorarioPartidaMotoresUTC = DateTime.UtcNow.AddMinutes(-10),
            HorarioCorteMotoresUTC = DateTime.UtcNow.AddHours(2).AddMinutes(5),
            TempoVooIFR = 1.5m,
            CombustivelDecolagem = 1000,
            CombustivelPouso = 200,
            UnidadeCombustivel = "kg",
            NaturezaVoo = "Comercial",
            TripulantesQuantidade = 4,
            PassageirosQuantidade = 150,
            CargaQuantidade = 500,
            UnidadeCarga = "kg",
            Ocorrencias = "Nenhuma",
            DiscrepanciasTecnicas = "Nenhuma",
            ResponsavelDeteccaoDiscrepancia = "Mecânico",
            AcoesCorretivas = "N/A",
            UltimaManutencaoTipo = "100h",
            ProximaManutencaoTipo = "Anual",
            ProximaManutencaoHoras = 45.5m,
            ResponsavelAprovacaoRetorno = "Engenheiro",
            AeronaveId = 1
        };
    }
}

// DTOs para testes
public class RegistroVooDto
{
    public int Id { get; set; }
    public int NumeroSequencial { get; set; }
    public string PilotoComandoCodigo { get; set; } = string.Empty;
    public DateTime Data { get; set; }
    public string LocalDecolagem { get; set; } = string.Empty;
    public string LocalPouso { get; set; } = string.Empty;
    public DateTime HorarioDecolagemUTC { get; set; }
    public DateTime HorarioPousoUTC { get; set; }
    public DateTime HorarioPartidaMotoresUTC { get; set; }
    public DateTime HorarioCorteMotoresUTC { get; set; }
    public decimal TempoVooIFR { get; set; }
    public decimal CombustivelDecolagem { get; set; }
    public decimal CombustivelPouso { get; set; }
    public string UnidadeCombustivel { get; set; } = string.Empty;
    public string NaturezaVoo { get; set; } = string.Empty;
    public int TripulantesQuantidade { get; set; }
    public int PassageirosQuantidade { get; set; }
    public decimal CargaQuantidade { get; set; }
    public string UnidadeCarga { get; set; } = string.Empty;
    public string Ocorrencias { get; set; } = string.Empty;
    public string DiscrepanciasTecnicas { get; set; } = string.Empty;
    public string ResponsavelDeteccaoDiscrepancia { get; set; } = string.Empty;
    public string AcoesCorretivas { get; set; } = string.Empty;
    public string UltimaManutencaoTipo { get; set; } = string.Empty;
    public string ProximaManutencaoTipo { get; set; } = string.Empty;
    public decimal ProximaManutencaoHoras { get; set; }
    public string ResponsavelAprovacaoRetorno { get; set; } = string.Empty;
    public int AeronaveId { get; set; }
    public bool AssinadoPiloto { get; set; }
    public bool AssinadoOperador { get; set; }
    public bool SincronizadoANAC { get; set; }
}

public class AssinaturaRegistroDto
{
    public int Id { get; set; }
    public int RegistroVooId { get; set; }
    public string TipoAssinatura { get; set; } = string.Empty;
    public string UsuarioId { get; set; } = string.Empty;
    public DateTime TimestampUTC { get; set; }
    public string HashSHA256 { get; set; } = string.Empty;
    public string EnderecoIP { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

public class EstatisticasDto
{
    public int TotalRegistros { get; set; }
    public int RegistrosPendentesAssinatura { get; set; }
    public int RegistrosSincronizados { get; set; }
    public int RegistrosVencidos { get; set; }
    public decimal PercentualConformidade { get; set; }
}