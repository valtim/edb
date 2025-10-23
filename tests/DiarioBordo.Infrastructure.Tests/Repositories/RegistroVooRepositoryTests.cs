using DiarioBordo.Domain.Entities;
using DiarioBordo.Domain.Enums;
using DiarioBordo.Infrastructure.Repositories;
using FluentAssertions;
using Moq;
using NHibernate;
using NHibernate.Linq;
using System.Linq.Expressions;
using Xunit;

namespace DiarioBordo.Infrastructure.Tests.Repositories;

/// <summary>
/// Testes unitários para RegistroVooRepository
/// </summary>
public class RegistroVooRepositoryTests : IDisposable
{
    private readonly Mock<ISession> _mockSession;
    private readonly Mock<ITransaction> _mockTransaction;
    private readonly Mock<IQueryable<RegistroVoo>> _mockQueryable;
    private readonly RegistroVooRepository _repository;

    public RegistroVooRepositoryTests()
    {
        _mockSession = new Mock<ISession>();
        _mockTransaction = new Mock<ITransaction>();
        _mockQueryable = new Mock<IQueryable<RegistroVoo>>();

        _mockSession.Setup(s => s.BeginTransaction()).Returns(_mockTransaction.Object);
        _mockSession.Setup(s => s.Query<RegistroVoo>()).Returns(_mockQueryable.Object);

        _repository = new RegistroVooRepository(_mockSession.Object);
    }

    [Fact]
    public async Task ObterUltimos30DiasAsync_DeveRetornarRegistrosDoPeriodo()
    {
        // Arrange
        var aeronaveId = 1;
        var dataLimite = DateTime.UtcNow.AddDays(-30);
        var registrosEsperados = CriarRegistrosVooMock(aeronaveId);

        ConfigurarMockQuery(registrosEsperados);

        // Act
        var resultado = await _repository.ObterUltimos30DiasAsync(aeronaveId);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Should().HaveCount(registrosEsperados.Count);
        resultado.All(r => r.AeronaveId == aeronaveId).Should().BeTrue();
        resultado.All(r => r.Data >= dataLimite).Should().BeTrue();
    }

    [Fact]
    public async Task ObterPorNumeroSequencialAsync_DeveRetornarRegistroCorreto()
    {
        // Arrange
        var aeronaveId = 1;
        var numeroSequencial = 100;
        var registroEsperado = CriarRegistroVooMock(aeronaveId, numeroSequencial);

        ConfigurarMockQuerySingle(registroEsperado);

        // Act
        var resultado = await _repository.ObterPorNumeroSequencialAsync(aeronaveId, numeroSequencial);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.AeronaveId.Should().Be(aeronaveId);
        resultado.NumeroSequencial.Should().Be(numeroSequencial);
    }

    [Fact]
    public async Task ObterPendentesSincronizacaoAsync_DeveRetornarApenasPendentes()
    {
        // Arrange
        var registrosPendentes = new List<RegistroVoo>
        {
            CriarRegistroVooMock(1, 1, assinadoPiloto: true, assinadoOperador: true, sincronizadoAnac: false),
            CriarRegistroVooMock(1, 2, assinadoPiloto: true, assinadoOperador: true, sincronizadoAnac: false)
        };

        ConfigurarMockQuery(registrosPendentes);

        // Act
        var resultado = await _repository.ObterPendentesSincronizacaoAsync();

        // Assert
        resultado.Should().NotBeNull();
        resultado.Should().HaveCount(2);
        resultado.All(r => r.AssinadoPiloto).Should().BeTrue();
        resultado.All(r => r.AssinadoOperador).Should().BeTrue();
        resultado.All(r => !r.SincronizadoANAC).Should().BeTrue();
    }

    [Fact]
    public async Task GetComPrazoVencidoAsync_DeveRetornarRegistrosVencidos()
    {
        // Arrange
        var dataLimite = DateTime.UtcNow.AddDays(-2);
        var registrosVencidos = new List<RegistroVoo>
        {
            CriarRegistroVooMock(1, 1, assinadoPiloto: false, dataCriacao: DateTime.UtcNow.AddDays(-5)),
            CriarRegistroVooMock(1, 2, assinadoOperador: false, dataCriacao: DateTime.UtcNow.AddDays(-3))
        };

        ConfigurarMockQuery(registrosVencidos);

        // Act
        var resultado = await _repository.GetComPrazoVencidoAsync(dataLimite);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Should().HaveCount(2);
        resultado.All(r => r.DataCriacao < dataLimite).Should().BeTrue();
        resultado.Any(r => !r.AssinadoPiloto || !r.AssinadoOperador).Should().BeTrue();
    }

    [Fact]
    public async Task ValidarNumeroSequencialUnicoAsync_DeveRetornarTrueSeUnico()
    {
        // Arrange
        var aeronaveId = 1;
        var numeroSequencial = 100L;

        // Simular que não existe nenhum registro com esse número
        ConfigurarMockQueryCount(0);

        // Act
        var resultado = await _repository.ValidarNumeroSequencialUnicoAsync(aeronaveId, numeroSequencial);

        // Assert
        resultado.Should().BeTrue("Número sequencial único deve ser válido");
    }

    [Fact]
    public async Task ValidarNumeroSequencialUnicoAsync_DeveRetornarFalseSeDuplicado()
    {
        // Arrange
        var aeronaveId = 1;
        var numeroSequencial = 100L;

        // Simular que já existe um registro com esse número
        ConfigurarMockQueryCount(1);

        // Act
        var resultado = await _repository.ValidarNumeroSequencialUnicoAsync(aeronaveId, numeroSequencial);

        // Assert
        resultado.Should().BeFalse("Número sequencial duplicado deve ser inválido");
    }

    [Theory]
    [InlineData(2)] // RBAC 121 - 2 dias
    [InlineData(15)] // RBAC 135 - 15 dias
    [InlineData(30)] // RBAC 91 - 30 dias
    public async Task GetComPrazoProximoVencimentoAsync_DeveRespeitarPrazos(int diasAviso)
    {
        // Arrange
        var dataLimite = DateTime.UtcNow.AddDays(-diasAviso + 1);
        var registrosProximoVencimento = new List<RegistroVoo>
        {
            CriarRegistroVooMock(1, 1, assinadoPiloto: false, dataCriacao: dataLimite)
        };

        ConfigurarMockQuery(registrosProximoVencimento);

        // Act
        var resultado = await _repository.GetComPrazoProximoVencimentoAsync(diasAviso);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Should().HaveCount(1);
    }

    [Fact]
    public async Task CriarAsync_DeveSalvarNovoRegistro()
    {
        // Arrange
        var novoRegistro = CriarRegistroVooMock(1, 1);

        _mockSession.Setup(s => s.SaveOrUpdateAsync(It.IsAny<RegistroVoo>(), default))
                   .Returns(Task.CompletedTask);

        // Act
        await _repository.CriarAsync(novoRegistro);

        // Assert
        novoRegistro.Id.Should().BeGreaterThan(0);

        _mockSession.Verify(s => s.SaveOrUpdateAsync(It.IsAny<RegistroVoo>(), default), Times.Once);
        _mockTransaction.Verify(t => t.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_DeveModificarRegistroExistente()
    {
        // Arrange
        var registroExistente = CriarRegistroVooMock(1, 1);
        registroExistente.Ocorrencias = "Ocorrência atualizada";

        _mockSession.Setup(s => s.UpdateAsync(It.IsAny<RegistroVoo>(), default))
                   .Returns(Task.CompletedTask);

        // Act
        await _repository.AtualizarAsync(registroExistente);

        // Assert
        registroExistente.Should().NotBeNull();
        registroExistente.Ocorrencias.Should().Be("Ocorrência atualizada");

        _mockSession.Verify(s => s.UpdateAsync(It.IsAny<RegistroVoo>(), default), Times.Once);
        _mockTransaction.Verify(t => t.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_DeveExcluirRegistro()
    {
        // Arrange
        var registroId = 1;
        var registroParaRemover = CriarRegistroVooMock(1, 1);

        _mockSession.Setup(s => s.GetAsync<RegistroVoo>(registroId, default))
                   .ReturnsAsync(registroParaRemover);
        _mockSession.Setup(s => s.DeleteAsync(It.IsAny<RegistroVoo>(), default))
                   .Returns(Task.CompletedTask);

        // Act
        await _repository.RemoverAsync(registroParaRemover);

        // Assert
        _mockSession.Verify(s => s.GetAsync<RegistroVoo>(registroId, default), Times.Once);
        _mockSession.Verify(s => s.DeleteAsync(It.IsAny<RegistroVoo>(), default), Times.Once);
        _mockTransaction.Verify(t => t.CommitAsync(default), Times.Once);
    }

    private void ConfigurarMockQuery(List<RegistroVoo> registros)
    {
        // TODO: Implementar mock adequado sem BuildMock
        // var mockQueryable = registros.AsQueryable().BuildMock();
        // _mockSession.Setup(s => s.Query<RegistroVoo>()).Returns(mockQueryable);
    }

    private void ConfigurarMockQuerySingle(RegistroVoo registro)
    {
        var lista = new List<RegistroVoo> { registro };
        ConfigurarMockQuery(lista);
    }

    private void ConfigurarMockQueryCount(int count)
    {
        var mockQuery = new Mock<IQueryable<RegistroVoo>>();
        mockQuery.Setup(q => q.Provider).Returns(new TestAsyncQueryProvider<RegistroVoo>(new List<RegistroVoo>().AsQueryable().Provider));
        mockQuery.Setup(q => q.Expression).Returns(Expression.Constant(count));

        _mockSession.Setup(s => s.Query<RegistroVoo>()).Returns(mockQuery.Object);
    }

    private static RegistroVoo CriarRegistroVooMock(
        int aeronaveId,
        int numeroSequencial = 1,
        bool assinadoPiloto = false,
        bool assinadoOperador = false,
        bool sincronizadoAnac = false,
        DateTime? dataCriacao = null)
    {
        return new RegistroVoo
        {
            Id = numeroSequencial,
            AeronaveId = aeronaveId,
            NumeroSequencial = numeroSequencial,
            Data = DateTime.UtcNow.Date,
            PilotoComandoCodigo = "123456",
            LocalDecolagem = "SBSP",
            LocalPouso = "SBRJ",
            HorarioDecolagemUTC = DateTime.UtcNow,
            HorarioPousoUTC = DateTime.UtcNow.AddHours(2),
            HorarioPartidaMotoresUTC = DateTime.UtcNow.AddMinutes(-10),
            HorarioCorteMotoresUTC = DateTime.UtcNow.AddHours(2).AddMinutes(5),
            TempoVooIFR = 1.5m,
            CombustivelQuantidade = 1000,
            CombustivelUnidade = "kg",
            NaturezaVoo = NaturezaVoo.Comercial,
            QuantidadePessoasAbordo = 154,
            CargaQuantidade = 500,
            CargaUnidade = "kg",
            Ocorrencias = "Nenhuma",
            DiscrepanciasTecnicas = "Nenhuma",
            AcoesCorretivas = "N/A",
            TipoUltimaManutencao = "100h",
            TipoProximaManutencao = "Anual",
            HorasCelulaProximaManutencao = 45.5m,
            ResponsavelAprovacaoRetorno = "Engenheiro",
            AssinadoPiloto = assinadoPiloto,
            AssinadoOperador = assinadoOperador,
            SincronizadoANAC = sincronizadoAnac,
            DataCriacao = dataCriacao ?? DateTime.UtcNow,
            CriadoPor = "Sistema"
        };
    }

    private static List<RegistroVoo> CriarRegistrosVooMock(int aeronaveId, int quantidade = 3)
    {
        var registros = new List<RegistroVoo>();
        for (int i = 1; i <= quantidade; i++)
        {
            registros.Add(CriarRegistroVooMock(aeronaveId, i));
        }
        return registros;
    }

    public void Dispose()
    {
        _mockSession.Object?.Dispose();
        _mockTransaction.Object?.Dispose();
    }
}

// Classe auxiliar para testes assíncronos
public class TestAsyncQueryProvider<TEntity> : IQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object Execute(Expression expression)
    {
        return _inner.Execute(expression) ?? throw new InvalidOperationException("Execute returned null");
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }
}

public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_inner.MoveNext());
    }

    public T Current => _inner.Current;

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }
}