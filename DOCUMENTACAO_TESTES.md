# Documentação de Testes - Sistema de Diário de Bordo Digital

**Versão**: 1.0  
**Data**: 22 de outubro de 2025  
**Autor**: Equipe de Qualidade  
**Status**: Em Desenvolvimento

---

## 1. Visão Geral dos Testes

### 1.1 Objetivo
Esta documentação define a estratégia de testes para o Sistema de Diário de Bordo Digital, garantindo conformidade com as Resoluções ANAC 457/2017 e 458/2017, qualidade de software e experiência do usuário.

### 1.2 Estratégia de Testes
Utilizamos a **Pirâmide de Testes** com foco em conformidade regulatória:

```
        ┌─────────────────┐
       ╱   Testes E2E     ╲      (10% - Fluxos críticos ANAC)
      ┌───────────────────┐
     ╱ Testes Integração  ╲      (20% - APIs e workflows)
    ┌─────────────────────┐
   ╱   Testes Unitários   ╲      (70% - Validações e regras)
  └─────────────────────────┘
```

### 1.3 Cobertura de Testes
- **Meta de Cobertura**: 80% mínimo (95% para validadores ANAC)
- **Testes Críticos**: 100% cobertura para conformidade regulatória
- **Testes de Regressão**: Automatizados para todos os fluxos principais

### 1.4 Ambientes de Teste
- **Desenvolvimento**: Testes unitários e de integração
- **Homologação**: Testes E2E e de aceitação
- **Pré-Produção**: Testes de performance e stress
- **Produção**: Testes de monitoramento e saúde

---

## 2. Testes Unitários

### 2.1 Validadores ANAC (Críticos)

#### TU001 - Validador Código ANAC
**Objetivo**: Garantir validação correta dos códigos ANAC de 6 dígitos.

```csharp
[TestClass]
public class CodigoANACValidatorTests
{
    [TestMethod]
    [DataRow("123456", true)]  // Válido
    [DataRow("000001", true)]  // Válido com zeros
    [DataRow("12345", false)]  // Muito curto
    [DataRow("1234567", false)] // Muito longo
    [DataRow("12345A", false)]  // Contém letra
    [DataRow("", false)]        // Vazio
    [DataRow(null, false)]      // Nulo
    public void ValidarCodigoANAC_DeveRetornarResultadoCorreto(string codigo, bool esperado)
    {
        // Arrange
        var validator = new CodigoANACValidator();
        
        // Act
        var resultado = validator.IsValid(codigo);
        
        // Assert
        Assert.AreEqual(esperado, resultado);
    }
}
```

#### TU002 - Validador Data de Voo
**Objetivo**: Validar formato dd/mm/aaaa e regras de data.

```csharp
[TestMethod]
public void ValidarDataVoo_DataFutura_DeveRetornarFalso()
{
    // Arrange
    var validator = new DataVooValidator();
    var dataFutura = DateTime.Today.AddDays(1);
    
    // Act
    var resultado = validator.IsValid(dataFutura);
    
    // Assert
    Assert.IsFalse(resultado);
}
```

#### TU003 - Validador Horários UTC
**Objetivo**: Garantir que horários sejam válidos e em UTC.

```csharp
[TestMethod]
public void ValidarHorarioUTC_FormatoInvalido_DeveRetornarFalso()
{
    // Arrange
    var validator = new HorarioUTCValidator();
    
    // Act & Assert
    Assert.IsFalse(validator.IsValid("25:00")); // Hora inválida
    Assert.IsFalse(validator.IsValid("12:60")); // Minuto inválido
    Assert.IsTrue(validator.IsValid("23:59"));  // Válido
}
```

#### TU004 - Validador Aeroportos
**Objetivo**: Validar códigos IATA (3), ICAO (4) e coordenadas.

```csharp
[TestMethod]
[DataRow("GRU", true)]      // IATA válido
[DataRow("SBGR", true)]     // ICAO válido
[DataRow("-23.432,-46.469", true)] // Coordenadas válidas
[DataRow("AB", false)]      // IATA muito curto
[DataRow("ABCDE", false)]   // ICAO muito longo
public void ValidarAeroporto_DeveRetornarResultadoCorreto(string codigo, bool esperado)
{
    var validator = new AeroportoValidator();
    Assert.AreEqual(esperado, validator.IsValid(codigo));
}
```

### 2.2 Entidades de Domínio

#### TU005 - Registro de Voo - Campos Obrigatórios
**Objetivo**: Verificar que todos os 17 campos obrigatórios são validados.

```csharp
[TestMethod]
public void CriarRegistroVoo_CamposObrigatoriosAusentes_DeveRetornarErros()
{
    // Arrange
    var registroDto = new RegistroVooDto(); // Vazio
    var validator = new RegistroVooValidator();
    
    // Act
    var resultado = validator.Validate(registroDto);
    
    // Assert
    Assert.IsFalse(resultado.IsValid);
    Assert.AreEqual(17, resultado.Errors.Count); // Todos campos obrigatórios
}
```

#### TU006 - Assinatura Digital
**Objetivo**: Testar geração de hash e propriedades da assinatura.

```csharp
[TestMethod]
public void GerarAssinatura_DadosValidos_DeveGerarHashCorreto()
{
    // Arrange
    var registro = CriarRegistroVooValido();
    var service = new AssinaturaService();
    
    // Act
    var hash1 = service.GerarHash(registro);
    var hash2 = service.GerarHash(registro);
    
    // Assert
    Assert.AreEqual(hash1, hash2); // Mesmo registro = mesmo hash
    Assert.AreEqual(64, hash1.Length); // SHA-256 = 64 chars hex
}
```

### 2.3 Serviços de Aplicação

#### TU007 - Serviço de Registro de Voo
**Objetivo**: Testar lógica de criação e validação.

```csharp
[TestMethod]
public async Task CriarRegistro_DadosValidos_DeveRetornarSucesso()
{
    // Arrange
    var mockRepo = new Mock<IRegistroVooRepository>();
    var service = new RegistroVooService(mockRepo.Object);
    var dto = CriarRegistroVooValidoDto();
    
    // Act
    var resultado = await service.CriarAsync(dto, "piloto123");
    
    // Assert
    Assert.IsTrue(resultado.Sucesso);
    Assert.IsNotNull(resultado.Dados);
    mockRepo.Verify(r => r.SaveAsync(It.IsAny<RegistroVoo>()), Times.Once);
}
```

### 2.4 Testes de Repositório (NHibernate)

#### TU008 - Consulta Últimos 30 Dias
**Objetivo**: Testar query crítica para requisito regulatório.

```csharp
[TestMethod]
public async Task GetUltimos30Dias_DeveRetornarApenas30Dias()
{
    // Arrange
    using var session = SessionFactory.OpenSession();
    var repository = new RegistroVooRepository(session);
    
    // Criar dados de teste
    await CriarRegistrosParaTeste(session, aeronaveId: 1);
    
    // Act
    var resultado = await repository.GetUltimos30DiasAsync(1);
    
    // Assert
    Assert.IsTrue(resultado.All(r => r.Data >= DateTime.Today.AddDays(-30)));
    Assert.IsTrue(resultado.Count <= 30); // Máximo 30 dias
}
```

---

## 3. Testes de Integração

### 3.1 Testes de API (Controllers)

#### TI001 - Endpoint Criar Registro de Voo
**Objetivo**: Testar fluxo completo da API REST.

```csharp
[TestMethod]
public async Task POST_RegistrosVoo_DadosValidos_DeveRetornar201()
{
    // Arrange
    var client = _factory.CreateClient();
    var token = await ObterTokenPiloto();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    var registro = new RegistroVooDto
    {
        AeronaveId = 1,
        Data = DateTime.Today,
        PilotoComandoCodigo = "123456",
        // ... outros campos obrigatórios
    };
    
    // Act
    var response = await client.PostAsJsonAsync("/api/v1/registros", registro);
    
    // Assert
    Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
    
    var resultado = await response.Content.ReadFromJsonAsync<ApiResponse<RegistroVoo>>();
    Assert.IsNotNull(resultado.Data.Id);
}
```

#### TI002 - Endpoint Assinar Registro
**Objetivo**: Testar assinatura digital via API.

```csharp
[TestMethod]
public async Task POST_AssinarRegistro_CredenciaisValidas_DeveRetornar200()
{
    // Arrange
    var registroId = await CriarRegistroParaTeste();
    var client = _factory.CreateClient();
    var token = await ObterTokenPiloto();
    
    var assinaturaDto = new AssinaturaDto
    {
        Usuario = "piloto123",
        Senha = "senhaSegura123"
    };
    
    // Act
    var response = await client.PostAsJsonAsync(
        $"/api/v1/registros/{registroId}/assinar", 
        assinaturaDto);
    
    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    
    // Verificar se registro foi marcado como assinado
    var registro = await ObterRegistro(registroId);
    Assert.IsTrue(registro.AssinadoPiloto);
    Assert.IsNotNull(registro.DataAssinaturaPilotoUTC);
}
```

### 3.2 Testes de Workflow de Assinaturas

#### TI003 - Fluxo Completo de Assinaturas
**Objetivo**: Testar workflow piloto → operador → ANAC.

```csharp
[TestMethod]
public async Task FluxoCompleto_CriarAssinarSincronizar_DeveCompletarComSucesso()
{
    // Arrange
    var registroId = await CriarRegistroParaTeste();
    
    // Act 1: Piloto assina
    await AssinarComoPiloto(registroId);
    
    // Act 2: Operador assina (simulando dentro do prazo)
    await AssinarComoOperador(registroId);
    
    // Act 3: Sincronização ANAC (job background)
    await ExecutarJobSincronizacao();
    
    // Assert
    var registro = await ObterRegistro(registroId);
    Assert.IsTrue(registro.AssinadoPiloto);
    Assert.IsTrue(registro.AssinadoOperador);
    Assert.IsTrue(registro.SincronizadoANAC);
}
```

### 3.3 Testes de Integração ANAC

#### TI004 - Integração Blockchain ANAC
**Objetivo**: Testar sincronização com blockchain ANAC.

```csharp
[TestMethod]
public async Task SincronizarBlockchain_RegistroCompleto_DeveTerSucesso()
{
    // Arrange
    var mockBlockchainService = new Mock<IANACBlockchainService>();
    mockBlockchainService
        .Setup(s => s.EnviarDadosAsync(It.IsAny<ANACBlockchainData>()))
        .ReturnsAsync(new ResultadoSincronizacao { Sucesso = true });
    
    var service = new ANACIntegrationService(mockBlockchainService.Object);
    var registro = CriarRegistroCompleto();
    
    // Act
    var resultado = await service.SincronizarAsync(registro);
    
    // Assert
    Assert.IsTrue(resultado.Sucesso);
    mockBlockchainService.Verify(s => 
        s.EnviarDadosAsync(It.IsAny<ANACBlockchainData>()), Times.Once);
}
```

### 3.4 Testes de Cache Redis

#### TI005 - Cache Últimos 30 Dias
**Objetivo**: Testar estratégia de cache para requisito regulatório.

```csharp
[TestMethod]
public async Task Cache30Dias_PrimeiraConsulta_DeveCarregarDoBanco()
{
    // Arrange
    var cacheKey = "registros:aeronave:1:30dias";
    await _redis.KeyDeleteAsync(cacheKey); // Limpar cache
    
    // Act
    var resultado = await _service.GetUltimos30DiasAsync(1);
    
    // Assert
    Assert.IsNotNull(resultado);
    
    // Verificar se foi armazenado no cache
    var cached = await _redis.StringGetAsync(cacheKey);
    Assert.IsTrue(cached.HasValue);
}
```

---

## 4. Testes End-to-End (E2E)

### 4.1 Testes com Cypress

#### TE001 - Fluxo Completo Piloto
**Objetivo**: Testar jornada completa do piloto no sistema.

```typescript
describe('Fluxo Completo do Piloto', () => {
  it('Deve criar, preencher e assinar registro de voo', () => {
    // Login como piloto
    cy.login('piloto123', 'senha123');
    
    // Navegar para criação de registro
    cy.visit('/registros/novo');
    cy.get('[data-cy=selecionar-aeronave]').select('PT-ABC');
    
    // Preencher campos obrigatórios
    cy.get('[data-cy=data-voo]').type('22/10/2025');
    cy.get('[data-cy=piloto-comando]').type('123456');
    cy.get('[data-cy=local-decolagem]').type('SBGR');
    cy.get('[data-cy=local-pouso]').type('SBSP');
    // ... preencher todos os 17 campos
    
    // Salvar registro
    cy.get('[data-cy=salvar-registro]').click();
    cy.get('[data-cy=sucesso]').should('be.visible');
    
    // Assinar registro
    cy.get('[data-cy=assinar-registro]').click();
    cy.get('[data-cy=modal-assinatura]').should('be.visible');
    cy.get('[data-cy=usuario-assinatura]').type('piloto123');
    cy.get('[data-cy=senha-assinatura]').type('senha123');
    cy.get('[data-cy=confirmar-assinatura]').click();
    
    // Verificar assinatura realizada
    cy.get('[data-cy=status-assinado-piloto]').should('contain', 'Assinado');
  });
});
```

#### TE002 - Fluxo Operador
**Objetivo**: Testar aprovação de manutenção pelo operador.

```typescript
describe('Fluxo do Operador', () => {
  it('Deve aprovar informações de manutenção', () => {
    // Pré-condição: registro já assinado pelo piloto
    cy.criarRegistroAssinadoPiloto().then((registroId) => {
      
      // Login como operador
      cy.login('operador123', 'senha123');
      
      // Acessar registros pendentes
      cy.visit('/registros/pendentes');
      cy.get(`[data-cy=registro-${registroId}]`).click();
      
      // Preencher informações de manutenção
      cy.get('[data-cy=ultima-manutencao]').type('Inspeção 100H');
      cy.get('[data-cy=proxima-manutencao]').type('Inspeção 200H');
      cy.get('[data-cy=horas-celula]').type('1850.5');
      cy.get('[data-cy=responsavel-aprovacao]').type('João Silva - CANAC 654321');
      
      // Assinar como operador
      cy.get('[data-cy=assinar-operador]').click();
      cy.get('[data-cy=usuario-assinatura]').type('operador123');
      cy.get('[data-cy=senha-assinatura]').type('senha123');
      cy.get('[data-cy=confirmar-assinatura]').click();
      
      // Verificar conclusão
      cy.get('[data-cy=status-completo]').should('be.visible');
    });
  });
});
```

### 4.2 Testes de Conformidade ANAC

#### TE003 - Validação Campos Obrigatórios
**Objetivo**: Garantir que sistema impede criação sem campos obrigatórios.

```typescript
describe('Conformidade ANAC - Campos Obrigatórios', () => {
  it('Deve exibir erro para cada campo obrigatório ausente', () => {
    cy.login('piloto123', 'senha123');
    cy.visit('/registros/novo');
    
    // Tentar salvar sem preencher campos
    cy.get('[data-cy=salvar-registro]').click();
    
    // Verificar 17 mensagens de erro (uma para cada campo obrigatório)
    cy.get('[data-cy=erro-campo]').should('have.length', 17);
    
    // Verificar mensagens específicas
    cy.get('[data-cy=erro-data]').should('contain', 'Data é obrigatória');
    cy.get('[data-cy=erro-piloto]').should('contain', 'Código ANAC do piloto é obrigatório');
    // ... verificar todos os campos
  });
});
```

#### TE004 - Teste Prazos de Assinatura
**Objetivo**: Verificar alertas de prazo conforme tipo RBAC.

```typescript
describe('Prazos de Assinatura RBAC', () => {
  it('RBAC 121 - Deve alertar após 2 dias', () => {
    // Criar registro para operador RBAC 121
    cy.criarRegistroRBAC121().then((registroId) => {
      
      // Simular passagem de 2 dias
      cy.clock(Date.now() + (2 * 24 * 60 * 60 * 1000));
      
      // Login como operador
      cy.login('operador121', 'senha123');
      cy.visit('/dashboard');
      
      // Verificar alerta de prazo
      cy.get('[data-cy=alerta-prazo]').should('be.visible');
      cy.get('[data-cy=alerta-prazo]').should('contain', 'Prazo vencido');
    });
  });
});
```

---

## 5. Testes de Performance

### 5.1 Testes de Carga

#### TP001 - Consulta Últimos 30 Dias
**Objetivo**: Verificar performance da consulta crítica.

```javascript
// k6 script
import http from 'k6/http';
import { check } from 'k6';

export let options = {
  vus: 100, // 100 usuários virtuais
  duration: '5m',
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% das requisições < 500ms
  },
};

export default function() {
  let response = http.get('https://api.diariobordo.com/api/v1/registros?ultimos30dias=true', {
    headers: { 'Authorization': `Bearer ${__ENV.JWT_TOKEN}` },
  });
  
  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
    'has data': (r) => r.json().data.length > 0,
  });
}
```

#### TP002 - Criação de Registros Simultâneos
**Objetivo**: Testar criação massiva de registros.

```javascript
export let options = {
  stages: [
    { duration: '2m', target: 50 },   // Ramp up
    { duration: '5m', target: 100 },  // Stay at 100
    { duration: '2m', target: 0 },    // Ramp down
  ],
};

export default function() {
  let payload = JSON.stringify({
    aeronaveId: 1,
    data: '22/10/2025',
    pilotoComandoCodigo: '123456',
    // ... outros campos obrigatórios
  });
  
  let response = http.post('https://api.diariobordo.com/api/v1/registros', payload, {
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${__ENV.JWT_TOKEN}`,
    },
  });
  
  check(response, {
    'created successfully': (r) => r.status === 201,
    'response time < 2s': (r) => r.timings.duration < 2000,
  });
}
```

### 5.2 Testes de Stress

#### TP003 - Limite de Capacidade
**Objetivo**: Encontrar ponto de quebra do sistema.

```javascript
export let options = {
  executor: 'ramping-arrival-rate',
  startRate: 10,
  timeUnit: '1s',
  preAllocatedVUs: 50,
  maxVUs: 500,
  stages: [
    { target: 50, duration: '5m' },
    { target: 100, duration: '10m' },
    { target: 200, duration: '10m' },
    { target: 0, duration: '5m' },
  ],
};
```

---

## 6. Testes de Segurança

### 6.1 Testes de Autenticação

#### TS001 - Tentativas de Login Inválidas
**Objetivo**: Verificar bloqueio após tentativas falhadas.

```csharp
[TestMethod]
public async Task Login_TentativasFalhadas_DeveBloquerUsuario()
{
    // Arrange
    var client = _factory.CreateClient();
    var loginInvalido = new { Usuario = "piloto123", Senha = "senhaErrada" };
    
    // Act: 5 tentativas falhadas
    for (int i = 0; i < 5; i++)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", loginInvalido);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    // Act: 6ª tentativa (deve estar bloqueado)
    var responseBloqueado = await client.PostAsJsonAsync("/api/auth/login", loginInvalido);
    
    // Assert
    Assert.AreEqual(HttpStatusCode.Locked, responseBloqueado.StatusCode);
}
```

### 6.2 Testes de Autorização

#### TS002 - Acesso Negado por Papel
**Objetivo**: Verificar controle de acesso por roles.

```csharp
[TestMethod]
public async Task AssinarComoOperador_UsuarioPiloto_DeveRetornar403()
{
    // Arrange
    var client = _factory.CreateClient();
    var tokenPiloto = await ObterTokenPiloto(); // Token sem role de operador
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", tokenPiloto);
    
    // Act
    var response = await client.PostAsync("/api/v1/registros/1/assinar-operador", null);
    
    // Assert
    Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
}
```

### 6.3 Testes de Injeção SQL

#### TS003 - Proteção Contra SQL Injection
**Objetivo**: Verificar que NHibernate protege contra injeções.

```csharp
[TestMethod]
public async Task BuscarRegistros_SqlInjection_DeveRetornarResultadoSeguro()
{
    // Arrange
    var client = _factory.CreateClient();
    var token = await ObterTokenValido();
    
    // Tentar injeção SQL no parâmetro de busca
    var sqlInjection = "1'; DROP TABLE RegistrosVoo; --";
    
    // Act
    var response = await client.GetAsync($"/api/v1/registros?aeronaveId={sqlInjection}");
    
    // Assert
    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    
    // Verificar que tabela ainda existe (não foi dropada)
    var verificacao = await client.GetAsync("/api/v1/registros");
    Assert.AreEqual(HttpStatusCode.OK, verificacao.StatusCode);
}
```

---

## 7. Testes de Auditoria

### 7.1 Testes de Log de Auditoria

#### TA001 - Registro de Operações Críticas
**Objetivo**: Verificar que todas operações são auditadas.

```csharp
[TestMethod]
public async Task CriarRegistro_DeveGerarLogAuditoria()
{
    // Arrange
    var mockAuditService = new Mock<IAuditoriaService>();
    var service = new RegistroVooService(mockRepo.Object, mockAuditService.Object);
    
    // Act
    await service.CriarAsync(registroDto, "piloto123");
    
    // Assert
    mockAuditService.Verify(a => a.LogAsync(
        "CRIAR_REGISTRO", 
        It.IsAny<int>(), 
        "piloto123",
        It.IsAny<object>()), Times.Once);
}
```

#### TA002 - Integridade dos Logs
**Objetivo**: Verificar que logs não podem ser alterados.

```csharp
[TestMethod]
public async Task TentarAlterarLog_DeveSerRejeitado()
{
    // Arrange
    var logId = await CriarLogParaTeste();
    
    // Act & Assert
    await Assert.ThrowsExceptionAsync<InvalidOperationException>(
        () => _auditRepository.UpdateAsync(logId, "dados alterados"));
}
```

---

## 8. Testes de Backup e Recuperação

### 8.1 Testes de Backup

#### TB001 - Backup Automático
**Objetivo**: Verificar execução do backup diário.

```csharp
[TestMethod]
public async Task BackupAutomatico_DeveExecutarDiariamente()
{
    // Arrange
    var mockBackupService = new Mock<IBackupService>();
    var scheduler = new BackupScheduler(mockBackupService.Object);
    
    // Act
    await scheduler.ExecutarBackupDiario();
    
    // Assert
    mockBackupService.Verify(b => b.ExecutarBackupCompleto(), Times.Once);
}
```

### 8.2 Testes de Disaster Recovery

#### TB002 - Recuperação de Dados
**Objetivo**: Testar restauração após falha simulada.

```csharp
[TestMethod]
public async Task RecuperarDados_AposSimulacaoFalha_DeveRestaurarIntegridade()
{
    // Arrange
    var dadosOriginais = await ObterTodosRegistros();
    await SimularFalhaNoBanco();
    
    // Act
    var service = new DisasterRecoveryService();
    await service.RestaurarUltimoBackup();
    
    // Assert
    var dadosRestaurados = await ObterTodosRegistros();
    Assert.AreEqual(dadosOriginais.Count, dadosRestaurados.Count);
}
```

---

## 9. Testes de Conformidade Regulatória

### 9.1 Checklist de Conformidade ANAC

#### TC001 - Resolução 457/2017 - Artigo 4º
**Objetivo**: Verificar implementação completa dos 17 campos obrigatórios.

```csharp
[TestMethod]
public void RegistroVoo_DeveConter17CamposObrigatorios()
{
    // Arrange
    var registro = new RegistroVoo();
    var propriedades = typeof(RegistroVoo).GetProperties();
    
    // Lista dos 17 campos obrigatórios conforme Art. 4º
    var camposObrigatorios = new[]
    {
        nameof(RegistroVoo.NumeroSequencial),      // I
        nameof(RegistroVoo.PilotoComandoCodigo),   // II
        nameof(RegistroVoo.Data),                  // III
        nameof(RegistroVoo.LocalDecolagem),        // IV
        nameof(RegistroVoo.LocalPouso),            // IV
        nameof(RegistroVoo.HorarioDecolagemUTC),   // V
        nameof(RegistroVoo.HorarioPousoUTC),       // V
        nameof(RegistroVoo.TempoVooIFR),           // VI
        nameof(RegistroVoo.CombustivelQuantidade), // VII
        nameof(RegistroVoo.NaturezaVoo),           // VIII
        nameof(RegistroVoo.QuantidadePessoasAbordo), // IX
        nameof(RegistroVoo.CargaQuantidade),       // X
        nameof(RegistroVoo.Ocorrencias),           // XI
        nameof(RegistroVoo.DiscrepanciasTecnicas), // XII
        nameof(RegistroVoo.AcoesCorretivas),       // XIII
        nameof(RegistroVoo.TipoUltimaManutencao),  // XIV
        nameof(RegistroVoo.TipoProximaManutencao), // XV
        nameof(RegistroVoo.HorasCelulaProximaManutencao), // XVI
        nameof(RegistroVoo.ResponsavelAprovacaoRetorno)   // XVII
    };
    
    // Assert
    foreach (var campo in camposObrigatorios)
    {
        Assert.IsTrue(propriedades.Any(p => p.Name == campo), 
            $"Campo obrigatório ausente: {campo}");
    }
}
```

#### TC002 - Resolução 458/2017 - Assinatura Digital
**Objetivo**: Verificar propriedades da assinatura digital.

```csharp
[TestMethod]
public void AssinaturaDigital_DeveAtenderResolucao458()
{
    // Arrange
    var assinatura = new AssinaturaRegistro
    {
        UsuarioId = "piloto123",
        DataHoraUTC = DateTime.UtcNow,
        Hash = "hash_sha256_do_registro",
        TipoAssinatura = TipoAssinatura.Piloto
    };
    
    // Assert - Propriedades obrigatórias (Art. 2º V)
    Assert.IsNotNull(assinatura.UsuarioId);        // Autenticidade
    Assert.IsNotNull(assinatura.Hash);             // Integridade
    Assert.IsNotNull(assinatura.DataHoraUTC);      // Irretratabilidade
    Assert.IsTrue(assinatura.Hash.Length == 64);   // SHA-256
}
```

### 9.2 Testes de Prazo Regulatório

#### TC003 - Prazos de Assinatura por RBAC
**Objetivo**: Verificar alertas de prazo conforme tipo de operador.

```csharp
[TestMethod]
[DataRow(TipoRBAC.RBAC121, 2)]   // 2 dias
[DataRow(TipoRBAC.RBAC135, 15)]  // 15 dias
[DataRow(TipoRBAC.Outros, 30)]   // 30 dias
public async Task VerificarPrazo_ConfirmeTipoRBAC_DeveAlertarCorretamente(
    TipoRBAC tipo, int diasPrazo)
{
    // Arrange
    var operador = CriarOperador(tipo);
    var registro = await CriarRegistroAssinadoPiloto(operador.Id);
    
    // Simular passagem do tempo até o prazo
    var dataLimite = registro.DataAssinaturaPilotoUTC.Value.AddDays(diasPrazo);
    
    // Act
    var service = new PrazoService();
    var alertas = await service.VerificarPrazosAsync(dataLimite);
    
    // Assert
    Assert.IsTrue(alertas.Any(a => a.RegistroId == registro.Id));
}
```

---

## 10. Matriz de Rastreabilidade

| Requisito Funcional | Teste Unitário | Teste Integração | Teste E2E | Teste Performance | Teste Segurança |
|---------------------|----------------|------------------|-----------|-------------------|------------------|
| RF001 - Cadastrar Aeronave | TU-AeronaveValidator | TI-CadastroAeronave | TE-FluxoCadastro | - | TS-AutorizacaoOperador |
| RF006 - Criar Registro | TU001-TU004 | TI001 | TE001 | TP002 | TS002 |
| RF007 - Assinar Piloto | TU006 | TI002 | TE001 | - | TS001 |
| RF008 - Assinar Operador | TU006 | TI003 | TE002 | - | TS002 |
| RF009 - Consultar Registros | TU008 | TI-ConsultaAPI | TE-Consulta | TP001 | TS-FiltroSeguro |
| RF013 - Sincronizar ANAC | - | TI004 | - | - | - |
| RF017 - Auditoria | TA001-TA002 | TI-LogAuditoria | - | - | - |

---

## 11. Estratégia de Automação

### 11.1 Pipeline CI/CD

```yaml
# .github/workflows/tests.yml
name: Testes Automatizados

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - name: Executar Testes Unitários
        run: |
          dotnet test --collect:"XPlat Code Coverage"
          dotnet tool install -g dotnet-reportgenerator-globaltool
          reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage
      - name: Verificar Cobertura Mínima
        run: |
          # Falhar se cobertura < 80%
          coverage=$(grep -o 'line-rate="[^"]*"' coverage/index.htm | head -1 | grep -o '[0-9.]*')
          if (( $(echo "$coverage < 0.8" | bc -l) )); then
            echo "Cobertura insuficiente: $coverage"
            exit 1
          fi

  integration-tests:
    runs-on: ubuntu-latest
    services:
      mysql:
        image: mysql:8.0
        env:
          MYSQL_ROOT_PASSWORD: root
          MYSQL_DATABASE: diariobordo_test
      redis:
        image: redis:alpine
    steps:
      - name: Executar Testes de Integração
        run: dotnet test --filter "Category=Integration"

  e2e-tests:
    runs-on: ubuntu-latest
    steps:
      - name: Executar Testes E2E
        run: |
          npm install
          npm run cy:run

  performance-tests:
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
      - name: Executar Testes de Performance
        run: k6 run performance-tests/load-test.js
```

### 11.2 Configuração de Ambiente de Teste

```docker
# docker-compose.test.yml
version: '3.8'
services:
  mysql-test:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: root
      MYSQL_DATABASE: diariobordo_test
    ports:
      - "3307:3306"
    
  redis-test:
    image: redis:alpine
    ports:
      - "6380:6379"
    
  api-test:
    build: .
    environment:
      - ASPNETCORE_ENVIRONMENT=Testing
      - ConnectionStrings__Default=Server=mysql-test;Database=diariobordo_test;...
      - Redis__Connection=redis-test:6379
    depends_on:
      - mysql-test
      - redis-test
```

---

## 12. Métricas e Relatórios

### 12.1 Métricas de Qualidade

- **Cobertura de Testes**: >80% geral, >95% validadores ANAC
- **Taxa de Falhas**: <1% em produção
- **Tempo de Execução**: Suite completa <30 minutos
- **Flakiness**: <5% de testes instáveis

### 12.2 Relatórios Automatizados

```csharp
// Geração de relatório de testes regulatórios
public class RelatorioConformidadeANAC
{
    public async Task<RelatorioConformidade> GerarRelatorio()
    {
        return new RelatorioConformidade
        {
            DataGeracao = DateTime.UtcNow,
            CamposObrigatoriosTestados = 17,
            TestesConformidade = await ContarTestesConformidade(),
            CoberturaValidadores = await CalcularCoberturaValidadores(),
            StatusGeral = "CONFORME"
        };
    }
}
```

---

## 13. Cronograma de Execução

| Fase | Duração | Testes | Responsável |
|------|---------|---------|-------------|
| Sprint 1 | 2 semanas | Testes Unitários (Validadores ANAC) | Dev Team |
| Sprint 2 | 2 semanas | Testes Integração (APIs Core) | QA Team |
| Sprint 3 | 1 semana | Testes E2E (Fluxos Críticos) | QA Team |
| Sprint 4 | 1 semana | Testes Performance | DevOps Team |
| Sprint 5 | 1 semana | Testes Segurança | Security Team |

---

## 14. Critérios de Aceite para Produção

### Checklist de Qualidade:
- [ ] Cobertura de testes >80%
- [ ] Todos os testes de conformidade ANAC passando
- [ ] Performance dentro dos SLAs (<500ms consultas 30 dias)
- [ ] Zero vulnerabilidades críticas de segurança
- [ ] Todos os fluxos E2E funcionando
- [ ] Backup/Recovery testados com sucesso
- [ ] Documentação de testes atualizada

---

**Aprovações**:
- [ ] QA Lead: _________________ Data: _______
- [ ] Especialista ANAC: ________ Data: _______
- [ ] Líder Técnico: ____________ Data: _______