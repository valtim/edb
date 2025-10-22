# Documento de Arquitetura - Sistema de Diário de Bordo Digital

## 1. Visão Geral

### 1.1 Propósito
Este documento descreve a arquitetura de um sistema de Diário de Bordo Digital para aeronaves civis brasileiras, em conformidade com as Resoluções ANAC nº 457/2017 e nº 458/2017, definindo seus componentes principais, padrões de design, tecnologias e decisões arquiteturais.

### 1.2 Escopo
O sistema de Diário de Bordo Digital é projetado para atender todos os requisitos regulatórios da ANAC, sendo escalável, seguro, auditável e de alta disponibilidade, seguindo as melhores práticas da indústria aeronáutica e de desenvolvimento de software.

### 1.3 Base Regulatória
- **Resolução ANAC nº 457/2017**: Regulamenta o Diário de Bordo das aeronaves civis brasileiras
- **Resolução ANAC nº 458/2017**: Regulamenta o uso de sistemas informatizados para registro e guarda de informações

### 1.4 Definições e Acrônimos
- **ANAC**: Agência Nacional de Aviação Civil
- **RAB**: Registro Aeronáutico Brasileiro
- **UTC**: Tempo Universal Coordenado (Universal Time Coordinated)
- **RBAC**: Regulamento Brasileiro da Aviação Civil
- **IFR**: Instrument Flight Rules
- **IATA**: International Air Transport Association
- **OACI**: Organização da Aviação Civil Internacional (ICAO)
- **API**: Application Programming Interface
- **REST**: Representational State Transfer
- **SPA**: Single Page Application
- **JWT**: JSON Web Token
- **HTTPS**: HyperText Transfer Protocol Secure
- **CI/CD**: Continuous Integration/Continuous Deployment
- **Blockchain**: Metodologia de registro de dados com consenso distribuído
- **Hashing**: Formação de sequência reduzida de bits por algoritmo de dispersão

---

## 2. Arquitetura de Alto Nível

### 2.1 Diagrama de Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│                    Usuários do Sistema                       │
│  (Pilotos, Operadores, Manutenção, Fiscalização ANAC)      │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    Load Balancer / SSL                       │
│                         (HTTPS)                              │
└─────────────────────────────────────────────────────────────┘
                            │
            ┌───────────────┴───────────────┐
            ▼                               ▼
┌─────────────────────┐         ┌─────────────────────┐
│   Frontend Layer    │         │   Backend Layer     │
│    (Angular 17+)    │         │    (C# .NET 8)      │
│                     │         │                     │
│  - TypeScript       │◀───────▶│  - ASP.NET Core     │
│  - RxJS             │   API   │  - REST API         │
│  - Angular Material │  REST   │  - NHibernate       │
│  - PWA Support      │         │  - SignalR          │
└─────────────────────┘         └─────────────────────┘
                                          │
                    ┌─────────────────────┼─────────────────────┐
                    ▼                     ▼                     ▼
        ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐
        │  Database MySQL │   │  Cache Redis    │   │  File Storage   │
        │                 │   │                 │   │                 │
        │  - InnoDB       │   │  - Sessions     │   │  - Backups      │
        │  - Replication  │   │  - JWT Tokens   │   │  - Logs         │
        │  - Audit Logs   │   │  - Temp Data    │   │  - Exports      │
        └─────────────────┘   └─────────────────┘   └─────────────────┘
                    │
                    ▼
        ┌─────────────────────────┐
        │  Integração ANAC        │
        │  - Blockchain (Opt)     │
        │  - Banco de Dados ANAC  │
        └─────────────────────────┘
```

### 2.2 Camadas da Aplicação

#### 2.2.1 Frontend (Presentation Layer)
- Interface do usuário responsiva
- Validação de dados em tempo real
- Operação offline (PWA)
- Sincronização automática
- Assinatura eletrônica de registros

#### 2.2.2 Backend (Application Layer)
- API REST securitizada
- Validação de dados segundo normas ANAC
- Controle de acesso baseado em roles (RBAC)
- Assinatura digital de registros
- Sistema de auditoria completo
- Integração com sistemas ANAC

#### 2.2.3 Persistence Layer
- Banco de dados MySQL com replicação
- Auditoria de todas as operações
- Backup automático
- Retenção de dados por 5 anos (mínimo)
- Cache distribuído para performance

---

## 3. Componentes Principais

### 3.1 Frontend (Angular)

#### 3.1.1 Tecnologias
- **Framework**: Angular 17+ (Standalone Components)
- **Linguagem**: TypeScript 5+
- **State Management**: NgRx (Redux pattern)
- **UI Framework**: Angular Material 17+
- **Forms**: Reactive Forms com validação customizada
- **HTTP Client**: HttpClient com interceptors
- **Real-time**: RxJS Observables
- **Autenticação**: Angular Guards + JWT
- **Build Tool**: Angular CLI / Webpack
- **Package Manager**: npm
- **PWA**: @angular/pwa para operação offline

#### 3.1.2 Estrutura de Pastas
```
src/
├── app/
│   ├── core/                    # Serviços singleton, guards, interceptors
│   │   ├── guards/             # Route guards (auth, role)
│   │   ├── interceptors/       # HTTP interceptors (auth, error)
│   │   ├── services/           # Serviços core (auth, api, storage)
│   │   └── models/             # Interfaces e tipos
│   ├── shared/                 # Componentes, diretivas, pipes compartilhados
│   │   ├── components/         # Componentes reutilizáveis
│   │   ├── directives/         # Diretivas customizadas
│   │   ├── pipes/              # Pipes customizados
│   │   └── validators/         # Validadores ANAC
│   ├── features/               # Módulos de funcionalidades
│   │   ├── diario-bordo/      # Gestão do diário de bordo
│   │   │   ├── components/
│   │   │   ├── services/
│   │   │   └── store/         # NgRx state
│   │   ├── aeronaves/         # Cadastro de aeronaves
│   │   ├── tripulacao/        # Gestão de tripulantes
│   │   ├── relatorios/        # Relatórios e exportações
│   │   └── admin/             # Administração do sistema
│   ├── assets/                # Recursos estáticos
│   └── environments/          # Configurações de ambiente
```

#### 3.1.3 Funcionalidades Frontend

##### Registro de Voo (Art. 4º Resolução 457)
- Formulário reativo com validação em tempo real
- Campos obrigatórios conforme regulamentação:
  - Número sequencial cronológico
  - Identificação de tripulantes (Código ANAC 6 dígitos)
  - Data (dd/mm/aaaa)
  - Locais de pouso/decolagem (IATA/OACI/coordenadas)
  - Horários UTC (pouso, decolagem, partida, corte motores)
  - Tempo de voo IFR
  - Combustível por etapa (com unidade)
  - Natureza do voo (privado/comercial/outro)
  - Quantidade de pessoas a bordo
  - Carga transportada (com unidade)
  - Ocorrências
  - Discrepâncias técnicas
  - Ações corretivas
  - Tipo de manutenção (última e próxima)
  - Horas de célula previstas
  - Responsável pela aprovação

##### Assinatura Eletrônica
- Modal de assinatura com usuário e senha individual
- Captura de timestamp UTC
- Hash do registro para integridade
- Indicador visual de status de assinatura
- Bloqueio de edição após assinatura do piloto

##### Visualização de Registros
- Lista com filtros avançados
- Últimos 30 dias sempre disponíveis
- Indicadores de quem assinou cada informação
- Exportação em PDF/Excel
- Modo offline com sincronização

##### Dashboard
- Indicadores de status da aeronave
- Alertas de manutenção próxima
- Registros pendentes de assinatura
- Gráficos de horas de voo

#### 3.1.4 Padrões de Design Frontend
- **Standalone Components**: Componentes independentes
- **Smart/Dumb Components**: Separação de lógica e apresentação
- **NgRx Store**: Gerenciamento de estado centralizado
- **Reactive Forms**: Validação robusta e tipada
- **Lazy Loading**: Carregamento sob demanda de módulos
- **Service Worker**: Cache offline e PWA
- **Interceptors Pattern**: Tratamento centralizado de requisições

### 3.2 Backend (C# .NET)

#### 3.2.1 Tecnologias
- **Framework**: ASP.NET Core 8.0
- **Linguagem**: C# 12
- **ORM**: NHibernate 5.5+
- **API**: REST com Swagger/OpenAPI
- **Autenticação**: JWT + ASP.NET Identity
- **Validação**: FluentValidation
- **Logging**: Serilog
- **Real-time**: SignalR (notificações)
- **Background Jobs**: Hangfire
- **Testing**: xUnit, Moq, FluentAssertions
- **Segurança**: ASP.NET Core Data Protection

#### 3.2.2 Estrutura de Pastas (Clean Architecture)
```
DiarioBordo.Solution/
├── src/
│   ├── DiarioBordo.Domain/          # Camada de domínio
│   │   ├── Entities/                # Entidades do domínio
│   │   │   ├── Aeronave.cs
│   │   │   ├── RegistroVoo.cs
│   │   │   ├── Tripulante.cs
│   │   │   └── AssinaturaRegistro.cs
│   │   ├── ValueObjects/            # Objetos de valor
│   │   │   ├── CodigoANAC.cs
│   │   │   ├── Coordenadas.cs
│   │   │   └── HorarioUTC.cs
│   │   ├── Enums/                   # Enumerações
│   │   │   ├── FuncaoTripulante.cs
│   │   │   ├── NaturezaVoo.cs
│   │   │   └── TipoManutencao.cs
│   │   ├── Interfaces/              # Contratos do domínio
│   │   └── Specifications/          # Regras de negócio
│   │
│   ├── DiarioBordo.Application/     # Camada de aplicação
│   │   ├── DTOs/                    # Data Transfer Objects
│   │   ├── Services/                # Serviços de aplicação
│   │   │   ├── RegistroVooService.cs
│   │   │   ├── AssinaturaService.cs
│   │   │   └── ExportacaoService.cs
│   │   ├── Validators/              # Validadores ANAC
│   │   │   ├── RegistroVooValidator.cs
│   │   │   └── CodigoANACValidator.cs
│   │   ├── Interfaces/              # Contratos de serviços
│   │   └── Mappings/                # AutoMapper profiles
│   │
│   ├── DiarioBordo.Infrastructure/  # Camada de infraestrutura
│   │   ├── Data/                    # Configuração NHibernate
│   │   │   ├── NHibernateHelper.cs
│   │   │   ├── SessionFactory.cs
│   │   │   └── Mappings/           # FluentNHibernate mappings
│   │   │       ├── AeronaveMap.cs
│   │   │       ├── RegistroVooMap.cs
│   │   │       └── TripulanteMap.cs
│   │   ├── Repositories/            # Implementação de repositórios
│   │   ├── ExternalServices/        # Integração ANAC
│   │   │   ├── ANACBlockchainService.cs
│   │   │   └── ANACDatabaseService.cs
│   │   ├── Identity/                # ASP.NET Identity
│   │   ├── Logging/                 # Implementação de logs
│   │   └── BackgroundJobs/          # Jobs de sincronização
│   │
│   ├── DiarioBordo.API/             # Camada de apresentação
│   │   ├── Controllers/             # API Controllers
│   │   │   ├── RegistroVooController.cs
│   │   │   ├── AeronaveController.cs
│   │   │   ├── TripulanteController.cs
│   │   │   └── RelatorioController.cs
│   │   ├── Middlewares/             # Middlewares customizados
│   │   │   ├── ErrorHandlingMiddleware.cs
│   │   │   ├── AuditMiddleware.cs
│   │   │   └── RequestLoggingMiddleware.cs
│   │   ├── Filters/                 # Action filters
│   │   ├── Extensions/              # Extension methods
│   │   ├── Configuration/           # Configurações
│   │   └── Program.cs               # Entry point
│   │
│   └── DiarioBordo.Shared/          # Utilitários compartilhados
│       ├── Constants/               # Constantes ANAC
│       ├── Helpers/                 # Helpers
│       └── Extensions/              # Extensions
│
└── tests/
    ├── DiarioBordo.UnitTests/
    ├── DiarioBordo.IntegrationTests/
    └── DiarioBordo.E2ETests/
```

#### 3.2.3 Funcionalidades Backend

##### API Endpoints (REST)

**Registros de Voo**
```
POST   /api/v1/registros              # Criar registro
GET    /api/v1/registros              # Listar registros (últimos 30 dias)
GET    /api/v1/registros/{id}         # Obter registro específico
PUT    /api/v1/registros/{id}         # Atualizar registro (antes de assinar)
DELETE /api/v1/registros/{id}         # Cancelar registro (com justificativa)
POST   /api/v1/registros/{id}/assinar # Assinar registro (piloto/operador)
GET    /api/v1/registros/aeronave/{id}# Registros por aeronave
```

**Aeronaves**
```
POST   /api/v1/aeronaves              # Cadastrar aeronave
GET    /api/v1/aeronaves              # Listar aeronaves
GET    /api/v1/aeronaves/{id}         # Obter aeronave
PUT    /api/v1/aeronaves/{id}         # Atualizar aeronave
GET    /api/v1/aeronaves/{id}/diario  # Diário completo da aeronave
```

**Tripulantes**
```
POST   /api/v1/tripulantes            # Cadastrar tripulante
GET    /api/v1/tripulantes            # Listar tripulantes
GET    /api/v1/tripulantes/{codigo}   # Obter por código ANAC
PUT    /api/v1/tripulantes/{id}       # Atualizar tripulante
```

**Relatórios e Exportação**
```
GET    /api/v1/relatorios/exportar    # Exportar dados (PDF/Excel/JSON)
GET    /api/v1/relatorios/anac        # Dados para transmissão ANAC
POST   /api/v1/relatorios/sincronizar # Sincronizar com ANAC
```

**Administração**
```
GET    /api/v1/admin/auditoria        # Logs de auditoria
GET    /api/v1/admin/usuarios         # Gestão de usuários
GET    /api/v1/admin/backups          # Status de backups
```

##### Validações Específicas ANAC

```csharp
public class RegistroVooValidator : AbstractValidator<RegistroVooDto>
{
    public RegistroVooValidator()
    {
        // Código ANAC: 6 dígitos numéricos
        RuleFor(x => x.CodigoPiloto)
            .Matches(@"^\d{6}$")
            .WithMessage("Código ANAC deve ter 6 dígitos");
        
        // Data no formato dd/mm/aaaa
        RuleFor(x => x.Data)
            .Must(BeValidDate)
            .WithMessage("Data deve estar no formato dd/mm/aaaa");
        
        // Horários em UTC
        RuleFor(x => x.HorarioDecolagem)
            .Must(BeValidUTC)
            .WithMessage("Horário deve estar em UTC");
        
        // Local: IATA (3 chars) ou OACI (4 chars) ou coordenadas
        RuleFor(x => x.LocalDecolagem)
            .Must(BeValidLocation)
            .WithMessage("Local inválido (use código IATA, OACI ou coordenadas)");
        
        // Combustível com unidade
        RuleFor(x => x.Combustivel)
            .NotEmpty()
            .WithMessage("Informe combustível e unidade (kg, lb ou litros)");
    }
}
```

##### Sistema de Assinatura Digital

```csharp
public class AssinaturaService : IAssinaturaService
{
    public async Task<AssinaturaResult> AssinarRegistroAsync(
        int registroId, 
        string usuarioId, 
        string senha)
    {
        // 1. Validar credenciais
        var usuario = await _authService.ValidateCredentialsAsync(usuarioId, senha);
        
        // 2. Gerar hash do registro
        var registro = await _repository.GetByIdAsync(registroId);
        var hash = GerarHash(registro);
        
        // 3. Criar assinatura com timestamp UTC
        var assinatura = new AssinaturaRegistro
        {
            RegistroId = registroId,
            UsuarioId = usuarioId,
            DataHoraUTC = DateTime.UtcNow,
            Hash = hash,
            TipoAssinatura = usuario.TipoUsuario // Piloto ou Operador
        };
        
        // 4. Persistir assinatura
        await _repository.AddAssinaturaAsync(assinatura);
        
        // 5. Marcar registro como assinado
        registro.MarcarComoAssinado(assinatura);
        
        // 6. Auditoria
        await _auditService.LogAsync("ASSINATURA_REGISTRO", registroId, usuarioId);
        
        return new AssinaturaResult { Success = true, Hash = hash };
    }
}
```

##### Integração com ANAC (Resolução 458)

```csharp
public class ANACIntegrationService : IANACIntegrationService
{
    // Opção B: Blockchain ANAC
    public async Task SincronizarBlockchainAsync(RegistroVoo registro)
    {
        var dados = _mapper.Map<ANACBlockchainData>(registro);
        await _blockchainClient.EnviarDadosAsync(dados);
    }
    
    // Opção C: Banco de Dados ANAC
    public async Task SincronizarBancoDadosAsync(RegistroVoo registro)
    {
        var dados = _mapper.Map<ANACDatabaseData>(registro);
        await _databaseClient.EnviarDadosAsync(dados);
    }
    
    // Verificar prazos (Art. 9º § 1º Resolução 457)
    public async Task VerificarPrazosAsync()
    {
        var registrosPendentes = await _repository
            .GetRegistrosPendentesAssinaturaOperadorAsync();
        
        foreach (var registro in registrosPendentes)
        {
            var prazo = CalcularPrazo(registro.Operador);
            if (DateTime.UtcNow > registro.DataAssinaturaPiloto.AddDays(prazo))
            {
                await _notificationService.AlertarAtrasoAsync(registro);
            }
        }
    }
}
```

#### 3.2.4 Padrões de Design Backend
- **Clean Architecture**: Separação em camadas (Domain, Application, Infrastructure, API)
- **CQRS Pattern**: Separação de comandos e consultas (opcional, com MediatR)
- **Repository Pattern**: Abstração de acesso a dados
- **Unit of Work**: Transações consistentes com ISession do NHibernate
- **Dependency Injection**: IoC nativo do .NET
- **Middleware Pipeline**: Processamento de requisições
- **Result Pattern**: Tratamento de erros sem exceptions
- **Specification Pattern**: Regras de negócio reutilizáveis

#### 3.2.5 Configuração do NHibernate

**NuGet Packages**
```xml
<PackageReference Include="NHibernate" Version="5.5.1" />
<PackageReference Include="FluentNHibernate" Version="3.3.0" />
<PackageReference Include="NHibernate.NetCore" Version="3.1.0" />
<PackageReference Include="MySql.Data" Version="8.3.0" />
```

**SessionFactory Configuration**
```csharp
public class NHibernateHelper
{
    private static ISessionFactory _sessionFactory;
    private static readonly object _lock = new object();

    public static ISessionFactory SessionFactory
    {
        get
        {
            if (_sessionFactory == null)
            {
                lock (_lock)
                {
                    if (_sessionFactory == null)
                        _sessionFactory = BuildSessionFactory();
                }
            }
            return _sessionFactory;
        }
    }

    private static ISessionFactory BuildSessionFactory()
    {
        return Fluently.Configure()
            .Database(MySQLConfiguration.Standard
                .ConnectionString(c => c.FromConnectionStringWithKey("DefaultConnection"))
                .ShowSql()
                .FormatSql()
                .AdoNetBatchSize(30))
            .Mappings(m => m.FluentMappings
                .AddFromAssemblyOf<AeronaveMap>()
                .Conventions.Add(
                    Table.Is(x => x.EntityType.Name + "s"),
                    PrimaryKey.Name.Is(x => "Id"),
                    ForeignKey.EndsWith("Id")))
            .ExposeConfiguration(cfg =>
            {
                // Segunda-level cache
                cfg.SetProperty(Environment.UseSecondLevelCache, "true");
                cfg.SetProperty(Environment.UseQueryCache, "true");
                cfg.SetProperty(Environment.CacheProvider, 
                    typeof(NHibernate.Caches.Redis.RedisCacheProvider).AssemblyQualifiedName);
                
                // Logging
                cfg.SetProperty(Environment.ShowSql, "true");
                cfg.SetProperty(Environment.FormatSql, "true");
                
                // Schema export para desenvolvimento
                #if DEBUG
                var schemaExport = new SchemaExport(cfg);
                schemaExport.Create(true, false);
                #endif
            })
            .BuildSessionFactory();
    }
}

// Dependency Injection
public static class NHibernateExtensions
{
    public static IServiceCollection AddNHibernate(
        this IServiceCollection services, 
        string connectionString)
    {
        var sessionFactory = Fluently.Configure()
            .Database(MySQLConfiguration.Standard
                .ConnectionString(connectionString)
                .ShowSql()
                .AdoNetBatchSize(30))
            .Mappings(m => m.FluentMappings.AddFromAssemblyOf<AeronaveMap>())
            .ExposeConfiguration(cfg =>
            {
                cfg.SetProperty(Environment.UseSecondLevelCache, "true");
                cfg.SetProperty(Environment.UseQueryCache, "true");
            })
            .BuildSessionFactory();

        services.AddSingleton(sessionFactory);
        services.AddScoped(factory => sessionFactory.OpenSession());
        
        return services;
    }
}
```

**FluentNHibernate Mappings**
```csharp
// AeronaveMap.cs
public class AeronaveMap : ClassMap<Aeronave>
{
    public AeronaveMap()
    {
        Table("Aeronaves");
        
        Id(x => x.Id).GeneratedBy.Identity();
        
        Map(x => x.Matricula).Length(10).Not.Nullable().Unique();
        Map(x => x.MarcaNacionalidade).Length(5).Not.Nullable();
        Map(x => x.Fabricante).Length(100).Not.Nullable();
        Map(x => x.Modelo).Length(100).Not.Nullable();
        Map(x => x.NumeroSerie).Length(50).Not.Nullable();
        Map(x => x.CategoriaRegistro).Length(50).Not.Nullable();
        Map(x => x.Ativa).Not.Nullable();
        Map(x => x.DataCadastro).Not.Nullable();
        Map(x => x.DataCancelamentoRAB).Nullable();
        
        HasMany(x => x.RegistrosVoo)
            .KeyColumn("AeronaveId")
            .Cascade.All()
            .Inverse()
            .LazyLoad();
    }
}

// RegistroVooMap.cs
public class RegistroVooMap : ClassMap<RegistroVoo>
{
    public RegistroVooMap()
    {
        Table("RegistrosVoo");
        
        Id(x => x.Id).GeneratedBy.Identity();
        
        Map(x => x.NumeroSequencial).Not.Nullable();
        Map(x => x.Data).Not.Nullable();
        
        // Tripulação
        Map(x => x.PilotoComandoFuncao).Length(1).Not.Nullable();
        Map(x => x.PilotoComandoHorarioApresentacao).Not.Nullable();
        
        // Locais e Horários
        Map(x => x.LocalDecolagem).Length(50).Not.Nullable();
        Map(x => x.LocalPouso).Length(50).Not.Nullable();
        Map(x => x.HorarioDecolagemUTC).Not.Nullable();
        Map(x => x.HorarioPousoUTC).Not.Nullable();
        Map(x => x.HorarioPartidaMotoresUTC).Not.Nullable();
        Map(x => x.HorarioCorteMotoresUTC).Not.Nullable();
        
        // Tempos e Combustível
        Map(x => x.TempoVooIFR).Precision(5).Scale(2).Nullable();
        Map(x => x.CombustivelQuantidade).Precision(10).Scale(2).Not.Nullable();
        Map(x => x.CombustivelUnidade).Length(10).Not.Nullable();
        
        // Voo e Carga
        Map(x => x.NaturezaVoo).Length(50).Not.Nullable();
        Map(x => x.QuantidadePessoasAbordo).Not.Nullable();
        Map(x => x.CargaQuantidade).Precision(10).Scale(2).Nullable();
        Map(x => x.CargaUnidade).Length(10).Nullable();
        
        // Ocorrências e Manutenção
        Map(x => x.Ocorrencias).CustomSqlType("TEXT").Nullable();
        Map(x => x.DiscrepanciasTecnicas).CustomSqlType("TEXT").Nullable();
        Map(x => x.PessoaDetectouDiscrepancia).Length(200).Nullable();
        Map(x => x.AcoesCorretivas).CustomSqlType("TEXT").Nullable();
        Map(x => x.TipoUltimaManutencao).Length(100).Nullable();
        Map(x => x.TipoProximaManutencao).Length(100).Nullable();
        Map(x => x.HorasCelulaProximaManutencao).Precision(10).Scale(2).Nullable();
        Map(x => x.ResponsavelAprovacaoRetorno).Length(200).Nullable();
        
        // Controle
        Map(x => x.AssinadoPiloto).Not.Nullable();
        Map(x => x.DataAssinaturaPilotoUTC).Nullable();
        Map(x => x.AssinadoOperador).Not.Nullable();
        Map(x => x.DataAssinaturaOperadorUTC).Nullable();
        Map(x => x.HashRegistro).Length(128).Nullable();
        Map(x => x.SincronizadoANAC).Not.Nullable();
        Map(x => x.DataSincronizacaoANAC).Nullable();
        
        // Auditoria
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
        
        // Cache de segunda-level
        Cache.ReadWrite().Region("RegistrosVoo");
    }
}

// TripulanteMap.cs
public class TripulanteMap : ClassMap<Tripulante>
{
    public TripulanteMap()
    {
        Table("Tripulantes");
        
        Id(x => x.Id).GeneratedBy.Identity();
        
        Map(x => x.CodigoANAC).Length(6).Not.Nullable().Unique();
        Map(x => x.Nome).Length(200).Not.Nullable();
        Map(x => x.CPF).Length(11).Not.Nullable().Unique();
        Map(x => x.Email).Length(200).Nullable();
        Map(x => x.Telefone).Length(20).Nullable();
        Map(x => x.Ativo).Not.Nullable();
        Map(x => x.DataCadastro).Not.Nullable();
        
        // Cache de segunda-level
        Cache.ReadWrite().Region("Tripulantes");
    }
}

// AssinaturaRegistroMap.cs
public class AssinaturaRegistroMap : ClassMap<AssinaturaRegistro>
{
    public AssinaturaRegistroMap()
    {
        Table("AssinaturasRegistro");
        
        Id(x => x.Id).GeneratedBy.Identity();
        
        Map(x => x.UsuarioId).Length(100).Not.Nullable();
        Map(x => x.TipoAssinatura).Length(20).Not.Nullable();
        Map(x => x.DataHoraUTC).Not.Nullable();
        Map(x => x.Hash).Length(128).Not.Nullable();
        Map(x => x.IPAddress).Length(45).Nullable();
        Map(x => x.UserAgent).Length(500).Nullable();
        
        References(x => x.RegistroVoo)
            .Column("RegistroVooId")
            .Not.Nullable()
            .LazyLoad();
    }
}
```

**Repository Pattern com NHibernate**
```csharp
public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id);
    Task<IList<T>> GetAllAsync();
    Task<T> SaveAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}

public class Repository<T> : IRepository<T> where T : class
{
    private readonly ISession _session;

    public Repository(ISession session)
    {
        _session = session;
    }

    public async Task<T> GetByIdAsync(int id)
    {
        return await _session.GetAsync<T>(id);
    }

    public async Task<IList<T>> GetAllAsync()
    {
        return await _session.Query<T>().ToListAsync();
    }

    public async Task<T> SaveAsync(T entity)
    {
        await _session.SaveAsync(entity);
        await _session.FlushAsync();
        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        await _session.UpdateAsync(entity);
        await _session.FlushAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        await _session.DeleteAsync(entity);
        await _session.FlushAsync();
    }
}

// Repository específico para RegistroVoo
public interface IRegistroVooRepository : IRepository<RegistroVoo>
{
    Task<IList<RegistroVoo>> GetUltimos30DiasAsync(int aeronaveId);
    Task<IList<RegistroVoo>> GetPendentesAssinaturaOperadorAsync();
    Task<IList<RegistroVoo>> GetPendentesSincronizacaoAsync();
}

public class RegistroVooRepository : Repository<RegistroVoo>, IRegistroVooRepository
{
    private readonly ISession _session;

    public RegistroVooRepository(ISession session) : base(session)
    {
        _session = session;
    }

    public async Task<IList<RegistroVoo>> GetUltimos30DiasAsync(int aeronaveId)
    {
        var dataLimite = DateTime.UtcNow.AddDays(-30);
        
        return await _session.Query<RegistroVoo>()
            .Where(r => r.AeronaveId == aeronaveId && r.Data >= dataLimite)
            .OrderByDescending(r => r.Data)
            .ToListAsync();
        
        // OU usando HQL
        var hql = @"FROM RegistroVoo r 
                    WHERE r.AeronaveId = :aeronaveId 
                    AND r.Data >= :dataLimite
                    ORDER BY r.Data DESC";
        
        return await _session.CreateQuery(hql)
            .SetParameter("aeronaveId", aeronaveId)
            .SetParameter("dataLimite", dataLimite)
            .SetCacheable(true)
            .SetCacheRegion("RegistrosVoo30Dias")
            .ListAsync<RegistroVoo>();
    }

    public async Task<IList<RegistroVoo>> GetPendentesAssinaturaOperadorAsync()
    {
        return await _session.Query<RegistroVoo>()
            .Where(r => r.AssinadoPiloto && !r.AssinadoOperador)
            .ToListAsync();
    }

    public async Task<IList<RegistroVoo>> GetPendentesSincronizacaoAsync()
    {
        return await _session.Query<RegistroVoo>()
            .Where(r => !r.SincronizadoANAC && r.AssinadoOperador)
            .ToListAsync();
    }
}
```

**Unit of Work Pattern com ISession**
```csharp
public class UnitOfWork : IDisposable
{
    private readonly ISession _session;
    private ITransaction _transaction;

    public UnitOfWork(ISessionFactory sessionFactory)
    {
        _session = sessionFactory.OpenSession();
    }

    public void BeginTransaction()
    {
        _transaction = _session.BeginTransaction();
    }

    public async Task CommitAsync()
    {
        try
        {
            await _transaction.CommitAsync();
        }
        catch
        {
            await _transaction.RollbackAsync();
            throw;
        }
        finally
        {
            _transaction.Dispose();
        }
    }

    public async Task RollbackAsync()
    {
        await _transaction.RollbackAsync();
        _transaction.Dispose();
    }

    public void Dispose()
    {
        _session?.Dispose();
        _transaction?.Dispose();
    }
}

// Uso em um Service
public class RegistroVooService
{
    private readonly ISessionFactory _sessionFactory;
    private readonly IRegistroVooRepository _repository;

    public async Task<RegistroVoo> CriarRegistroComAssinaturaAsync(
        RegistroVooDto dto, 
        string usuarioId)
    {
        using (var session = _sessionFactory.OpenSession())
        using (var transaction = session.BeginTransaction())
        {
            try
            {
                // Criar registro
                var registro = new RegistroVoo { /* ... */ };
                await session.SaveAsync(registro);
                
                // Criar assinatura
                var assinatura = new AssinaturaRegistro
                {
                    RegistroVoo = registro,
                    UsuarioId = usuarioId,
                    DataHoraUTC = DateTime.UtcNow,
                    Hash = GerarHash(registro)
                };
                await session.SaveAsync(assinatura);
                
                // Commit transação
                await transaction.CommitAsync();
                
                return registro;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
```

**Segunda-Level Cache com Redis**
```csharp
// NuGet: NHibernate.Caches.Redis
// Configuration
.ExposeConfiguration(cfg =>
{
    cfg.SetProperty(Environment.UseSecondLevelCache, "true");
    cfg.SetProperty(Environment.UseQueryCache, "true");
    cfg.SetProperty(Environment.CacheProvider, 
        "NHibernate.Caches.Redis.RedisCacheProvider, NHibernate.Caches.Redis");
    cfg.SetProperty("cache.redis.connection_string", "localhost:6379");
    cfg.SetProperty("cache.redis.database", "1");
})

// Uso em queries
var registros = await session.Query<RegistroVoo>()
    .Where(r => r.AeronaveId == aeronaveId)
    .SetCacheable(true)
    .SetCacheRegion("RegistrosVoo")
    .ToListAsync();
```

### 3.3 Banco de Dados (MySQL)

#### 3.3.1 Configuração
- **Versão**: MySQL 8.0+
- **Storage Engine**: InnoDB (suporte a transações ACID)
- **Charset**: utf8mb4 (suporte a caracteres especiais)
- **Collation**: utf8mb4_unicode_ci
- **Timezone**: UTC
- **Replicação**: Master-Slave para alta disponibilidade
- **Backup**: Diário incremental + semanal completo

#### 3.3.2 Modelo de Dados

```sql
-- Tabela de Aeronaves
CREATE TABLE Aeronaves (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Matricula VARCHAR(10) NOT NULL UNIQUE,
    MarcaNacionalidade VARCHAR(5) NOT NULL,
    Fabricante VARCHAR(100) NOT NULL,
    Modelo VARCHAR(100) NOT NULL,
    NumeroSerie VARCHAR(50) NOT NULL,
    CategoriaRegistro VARCHAR(50) NOT NULL,
    Ativa BIT NOT NULL DEFAULT 1,
    DataCadastro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DataCancelamentoRAB DATETIME NULL,
    INDEX idx_matricula (Matricula),
    INDEX idx_ativa (Ativa)
) ENGINE=InnoDB;

-- Tabela de Tripulantes
CREATE TABLE Tripulantes (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    CodigoANAC CHAR(6) NOT NULL UNIQUE,
    Nome VARCHAR(200) NOT NULL,
    CPF VARCHAR(11) NOT NULL UNIQUE,
    Email VARCHAR(200) NULL,
    Telefone VARCHAR(20) NULL,
    Ativo BIT NOT NULL DEFAULT 1,
    DataCadastro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_codigo_anac (CodigoANAC),
    INDEX idx_cpf (CPF)
) ENGINE=InnoDB;

-- Tabela de Registros de Voo (Art. 4º Resolução 457)
CREATE TABLE RegistrosVoo (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    NumeroSequencial BIGINT NOT NULL,
    AeronaveId INT NOT NULL,
    Data DATE NOT NULL,
    
    -- Tripulação
    PilotoComandoId INT NOT NULL,
    PilotoComandoFuncao CHAR(1) NOT NULL, -- P, I, O
    PilotoComandoHorarioApresentacao TIME NOT NULL,
    
    -- Locais e Horários
    LocalDecolagem VARCHAR(50) NOT NULL, -- IATA/OACI/Coordenadas
    LocalPouso VARCHAR(50) NOT NULL,
    HorarioDecolagemUTC DATETIME NOT NULL,
    HorarioPousoUTC DATETIME NOT NULL,
    HorarioPartidaMotoresUTC DATETIME NOT NULL,
    HorarioCorteMotoresUTC DATETIME NOT NULL,
    
    -- Tempos e Combustível
    TempoVooIFR DECIMAL(5,2) NULL,
    CombustivelQuantidade DECIMAL(10,2) NOT NULL,
    CombustivelUnidade VARCHAR(10) NOT NULL, -- kg, lb, litros
    
    -- Voo e Carga
    NaturezaVoo VARCHAR(50) NOT NULL, -- privado, comercial, outro
    QuantidadePessoasAbordo INT NOT NULL,
    CargaQuantidade DECIMAL(10,2) NULL,
    CargaUnidade VARCHAR(10) NULL,
    
    -- Ocorrências e Manutenção
    Ocorrencias TEXT NULL,
    DiscrepanciasTecnicas TEXT NULL,
    PessoaDetectouDiscrepancia VARCHAR(200) NULL,
    AcoesCorretivas TEXT NULL,
    TipoUltimaManutencao VARCHAR(100) NULL,
    TipoProximaManutencao VARCHAR(100) NULL,
    HorasCelulaProximaManutencao DECIMAL(10,2) NULL,
    ResponsavelAprovacaoRetorno VARCHAR(200) NULL,
    
    -- Controle
    AssinadoPiloto BIT NOT NULL DEFAULT 0,
    DataAssinaturaPilotoUTC DATETIME NULL,
    AssinadoOperador BIT NOT NULL DEFAULT 0,
    DataAssinaturaOperadorUTC DATETIME NULL,
    HashRegistro VARCHAR(128) NULL,
    SincronizadoANAC BIT NOT NULL DEFAULT 0,
    DataSincronizacaoANAC DATETIME NULL,
    
    -- Auditoria
    CriadoPor VARCHAR(100) NOT NULL,
    DataCriacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ModificadoPor VARCHAR(100) NULL,
    DataModificacao DATETIME NULL,
    
    FOREIGN KEY (AeronaveId) REFERENCES Aeronaves(Id),
    FOREIGN KEY (PilotoComandoId) REFERENCES Tripulantes(Id),
    INDEX idx_aeronave_data (AeronaveId, Data),
    INDEX idx_numero_sequencial (NumeroSequencial),
    INDEX idx_assinatura_piloto (AssinadoPiloto, DataAssinaturaPilotoUTC),
    INDEX idx_sincronizacao (SincronizadoANAC)
) ENGINE=InnoDB;

-- Tabela de Assinaturas (Auditoria)
CREATE TABLE AssinaturasRegistro (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    RegistroVooId INT NOT NULL,
    UsuarioId VARCHAR(100) NOT NULL,
    TipoAssinatura VARCHAR(20) NOT NULL, -- PILOTO, OPERADOR
    DataHoraUTC DATETIME NOT NULL,
    Hash VARCHAR(128) NOT NULL,
    IPAddress VARCHAR(45) NULL,
    UserAgent VARCHAR(500) NULL,
    
    FOREIGN KEY (RegistroVooId) REFERENCES RegistrosVoo(Id),
    INDEX idx_registro (RegistroVooId),
    INDEX idx_usuario (UsuarioId)
) ENGINE=InnoDB;

-- Tabela de Auditoria (Sistema de Logs - Resolução 458)
CREATE TABLE LogsAuditoria (
    Id BIGINT PRIMARY KEY AUTO_INCREMENT,
    DataHoraUTC DATETIME NOT NULL,
    Operacao VARCHAR(50) NOT NULL, -- INSERT, UPDATE, DELETE, SIGN, SYNC
    Tabela VARCHAR(50) NOT NULL,
    RegistroId INT NOT NULL,
    UsuarioId VARCHAR(100) NOT NULL,
    DadosAnteriores JSON NULL,
    DadosNovos JSON NULL,
    IPAddress VARCHAR(45) NULL,
    UserAgent VARCHAR(500) NULL,
    
    INDEX idx_data (DataHoraUTC),
    INDEX idx_usuario (UsuarioId),
    INDEX idx_operacao (Operacao),
    INDEX idx_tabela_registro (Tabela, RegistroId)
) ENGINE=InnoDB;

-- Tabela de Backups
CREATE TABLE HistoricoBackups (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    DataHoraUTC DATETIME NOT NULL,
    TipoBackup VARCHAR(20) NOT NULL, -- FULL, INCREMENTAL
    Status VARCHAR(20) NOT NULL, -- SUCCESS, FAILED
    TamanhoBytes BIGINT NULL,
    LocalArmazenamento VARCHAR(500) NULL,
    Mensagem TEXT NULL,
    
    INDEX idx_data (DataHoraUTC),
    INDEX idx_status (Status)
) ENGINE=InnoDB;
```

#### 3.3.3 Políticas de Dados

##### Retenção (Art. 11 § 1º Resolução 457)
- Dados mantidos por **5 anos** após cancelamento da matrícula no RAB
- Após 5 anos, dados são arquivados em cold storage
- Backups mantidos conforme política de retenção

##### Disponibilidade (Art. 8º Resolução 457)
- Últimos **30 dias** sempre em cache de alta performance
- Dados anteriores acessíveis em até 2 segundos
- Replicação master-slave para failover automático

##### Auditoria (Resolução 458, Art. 4º)
- Todos os eventos registrados em `LogsAuditoria`
- Registro de INSERT, UPDATE, DELETE, assinaturas, sincronizações
- IP Address e User Agent capturados
- Dados antes/depois em formato JSON

#### 3.3.4 Segurança do Banco
- **Encryption at Rest**: Dados sensíveis criptografados (AES-256)
- **Encryption in Transit**: Conexões TLS 1.3
- **Least Privilege**: Usuários com permissões mínimas
- **Audit Logging**: Ativado para todas operações DDL/DML
- **Backup Encryption**: Backups criptografados e testados mensalmente

---

## 4. Segurança e Conformidade Regulatória

### 4.1 Autenticação e Autorização

#### 4.1.1 Autenticação (Resolução 458, Art. 2º)
- **JWT Tokens**: Para autenticação stateless da API
  - Access Token: Válido por 15 minutos
  - Refresh Token: Válido por 7 dias, armazenado em HttpOnly cookie
- **ASP.NET Identity**: Gerenciamento de usuários e perfis
- **Multi-Factor Authentication**: Obrigatório para operadores e administradores
- **Password Policy**: 
  - Mínimo 12 caracteres
  - Caracteres maiúsculos, minúsculos, números e símbolos
  - Histórico de 12 senhas
  - Expiração a cada 90 dias

#### 4.1.2 Autorização (RBAC)
```csharp
public enum TipoUsuario
{
    Piloto,              // Assina registros de voo (incisos I-XII Art. 4º)
    Operador,            // Assina informações de manutenção (Art. 9º)
    DiretorOperacoes,    // Pessoa competente (Art. 9º § 2º)
    Manutencao,          // Registra informações técnicas
    Fiscalizacao,        // ANAC - acesso somente leitura
    Administrador        // Gestão completa do sistema
}

// Políticas de autorização
[Authorize(Roles = "Piloto")]
public async Task<IActionResult> AssinarRegistroVoo(int id) { }

[Authorize(Roles = "Operador,DiretorOperacoes")]
public async Task<IActionResult> AssinarInformacoesManutencao(int id) { }

[Authorize(Roles = "Fiscalizacao")]
public async Task<IActionResult> ConsultarRegistros() { }
```

#### 4.1.3 Assinatura Digital (Resolução 458, Art. 2º)
- **Propriedades Obrigatórias**:
  - **Autenticidade**: Identificação do signatário
  - **Integridade**: Hash SHA-256 do registro
  - **Irretratabilidade**: Impossível negar a assinatura
- **Implementação**:
  ```csharp
  public class AssinaturaDigital
  {
      public string UsuarioId { get; set; }
      public DateTime DataHoraUTC { get; set; }
      public string HashSHA256 { get; set; } // Hash do registro completo
      public string ChavePublica { get; set; } // Para verificação
      public string Assinatura { get; set; }  // Registro assinado com chave privada
  }
  ```

### 4.2 Proteções de Segurança

#### 4.2.1 Segurança de Aplicação
- **HTTPS Obrigatório**: TLS 1.3 com certificado válido
- **CORS**: Configurado para domínios autorizados apenas
- **Rate Limiting**: 
  - API pública: 100 requisições/minuto por IP
  - API autenticada: 1000 requisições/minuto por usuário
- **Input Validation**: 
  - Sanitização de todos os inputs
  - Validação conforme formatos ANAC (Art. 5º Resolução 457)
  - FluentValidation no backend
  - Reactive Forms no frontend
- **SQL Injection Prevention**: 
  - NHibernate com parametrização automática
  - Named queries e HQL para operações seguras
  - Stored procedures para operações críticas
- **XSS Protection**: 
  - Content Security Policy (CSP)
  - Angular sanitization automática
  - Encoding de outputs
- **CSRF Protection**: 
  - Anti-forgery tokens
  - SameSite cookies

#### 4.2.2 Segurança de Dados (Resolução 458)

##### Encryption at Rest
- **Dados Sensíveis**: Criptografia AES-256
  - Senhas: bcrypt (work factor 12)
  - Dados pessoais: ASP.NET Data Protection
  - Backups: Criptografia antes do armazenamento

##### Encryption in Transit
- **HTTPS/TLS 1.3**: Toda comunicação cliente-servidor
- **Database Connections**: TLS para conexões MySQL
- **API Integração ANAC**: Certificados mútuo (mTLS)

##### Data Masking (Art. 4º § 4º Resolução 458)
- Logs sem dados sensíveis (CPF, senhas, tokens)
- Exemplo: CPF `123.456.789-00` → `***.***.789-**`

### 4.3 Sistema de Auditoria (Resolução 458, Art. 4º)

#### 4.3.1 Sistema de Registro de Logs
```csharp
public class AuditMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var audit = new LogAuditoria
        {
            DataHoraUTC = DateTime.UtcNow,
            UsuarioId = context.User.Identity.Name,
            Operacao = context.Request.Method,
            Endpoint = context.Request.Path,
            IPAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers["User-Agent"].ToString()
        };
        
        // Capturar request body para operações de escrita
        if (context.Request.Method != "GET")
        {
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;
            audit.RequestBody = body;
        }
        
        await _next(context);
        
        audit.StatusCode = context.Response.StatusCode;
        await _auditRepository.SaveAsync(audit);
    }
}
```

#### 4.3.2 Eventos Auditados
- Autenticação e autorização (login, logout, falhas)
- Criação, modificação, exclusão de registros
- Assinaturas eletrônicas
- Sincronização com ANAC
- Exportação de dados
- Alterações de configuração
- Acesso de fiscalização ANAC

#### 4.3.3 Retenção de Logs
- **Logs de Aplicação**: 90 dias online, 1 ano arquivado
- **Logs de Auditoria**: 5 anos (acompanha retenção de dados)
- **Logs de Segurança**: 2 anos

### 4.4 Backup e Recuperação (Resolução 458, Art. 2º III)

#### 4.4.1 Estratégia de Backup
```yaml
Backup Completo (Full):
  Frequência: Semanal (Domingo 02:00 UTC)
  Retenção: 4 semanas
  Tipo: MySQL dump completo + arquivos

Backup Incremental:
  Frequência: Diário (01:00 UTC)
  Retenção: 30 dias
  Tipo: Binary logs MySQL

Backup Transacional:
  Frequência: A cada 6 horas
  Retenção: 7 dias
  Tipo: Point-in-time recovery
```

#### 4.4.2 Armazenamento de Backups
- **Primário**: Storage local criptografado
- **Secundário**: Cloud storage (S3/Azure Blob)
- **Terciário**: Fita magnética offline (mensalmente)
- **Criptografia**: AES-256 em todos os níveis
- **Teste de Restauração**: Mensal automatizado

#### 4.4.3 Disaster Recovery (Art. 8º § 2º Resolução 457)
```
RTO (Recovery Time Objective): 4 horas
RPO (Recovery Point Objective): 1 hora
Estratégia: Failover automático para região secundária
```

**Procedimento de Recuperação**:
1. Detecção de falha (automática ou manual)
2. Ativação de servidor standby
3. Restauração do backup mais recente
4. Aplicação de logs transacionais
5. Validação de integridade dos dados
6. Redirecionamento de tráfego
7. Notificação aos usuários

### 4.5 Conformidade com Resoluções ANAC

#### 4.5.1 Resolução 457/2017 - Diário de Bordo

| Requisito | Artigo | Implementação |
|-----------|--------|---------------|
| Registro de informações obrigatórias | Art. 4º | Formulário com todos os 17 campos obrigatórios |
| Formatos de informação | Art. 5º | Validação automática de formatos (data, UTC, códigos) |
| Assinatura do piloto | Art. 6º | Assinatura eletrônica com usuário e senha individual |
| Disponibilidade de dados (30 dias) | Art. 8º II | Cache Redis + MySQL indexado |
| Identificação de quem assinou | Art. 8º § 1º | Tabela AssinaturasRegistro com auditoria |
| Perda de registros = suspensão | Art. 8º § 2º | Backup triplo + monitoramento de integridade |
| Assinatura do operador | Art. 9º | Workflow de assinatura em duas etapas |
| Prazos de assinatura operador | Art. 9º § 1º | Job automático verifica prazos (RBAC 121: 2 dias, 135: 15 dias, outros: 30 dias) |
| Guarda por 5 anos após RAB | Art. 11 § 1º | Política de retenção automatizada |
| Responsabilidade primária | Art. 11 § 2º | Operador é owner dos dados, mesmo usando terceiros |

#### 4.5.2 Resolução 458/2017 - Sistema Informatizado

| Requisito | Artigo | Implementação |
|-----------|--------|---------------|
| Autorização ANAC para uso | Art. 3º I | Processo de aceite com documentação completa |
| Segurança demonstrada | Art. 3º II | Opção B (Blockchain) ou C (Banco ANAC) |
| Autenticação eletrônica | Art. 2º I | JWT + ASP.NET Identity + MFA |
| Sistema de Registro de Logs | Art. 2º II | Tabela LogsAuditoria + Serilog |
| Backup de dados | Art. 2º III | Estratégia tripla (local + cloud + offline) |
| Verificação de dados | Art. 2º IV | Checksums SHA-256 + validação periódica |
| Assinatura digital | Art. 2º V | Autenticidade + Integridade + Irretratabilidade |
| Hashing | Art. 2º IX | SHA-256 para todos os registros |
| Blockchain (opcional) | Art. 2º X | Integração com blockchain ANAC |
| Procedimentos documentados | Art. 4º | Manual de operação + runbooks |
| Controle de acesso | Art. 4º a) 1 | RBAC com níveis de permissão |
| Auditoria de eventos | Art. 4º a) 2 | Todos eventos críticos logados |
| Criptografia | Art. 4º a) 3 | TLS 1.3 + AES-256 |
| Validação de entrada | Art. 4º a) 4 | FluentValidation + Angular Forms |
| Políticas de continuidade | Art. 4º b) | Plano de DR + testes trimestrais |
| Treinamento de usuários | Art. 4º b) 7 | Documentação + vídeos + suporte |
| Migração de sistemas | Art. 4º b) 8 | Procedimentos de exportação/importação |
| Equipe responsável | Art. 4º c) | Definição clara de responsabilidades |
| Procedimentos de auditoria | Art. 4º d) | Logs + alertas + relatórios |
| Disponibilidade para fiscalização | Art. 7º | API de consulta para ANAC |
| Responsabilidade pela guarda | Art. 8º | Operador como data controller |
| Perda = registros inexistentes | Art. 8º Parágrafo único | Penalidades conforme Resolução 457 |

---

## 5. Escalabilidade e Performance

### 5.1 Estratégias de Escalabilidade

#### 5.1.1 Horizontal Scaling (Backend)
- **Load Balancer**: NGINX ou cloud load balancer
- **Múltiplas Instâncias**: ASP.NET Core stateless
- **Auto-scaling**: Baseado em CPU (>70%) e request count
- **Session Management**: Redis para sessions compartilhadas

#### 5.1.2 Database Scaling
- **Read Replicas**: MySQL master-slave replication
  - Master: Write operations
  - Slaves: Read operations (consultas, relatórios)
- **Connection Pooling**: 
  ```csharp
  "ConnectionStrings": {
    "DefaultConnection": "Server=master;Database=DiarioBordo;Pooling=true;Min Pool Size=10;Max Pool Size=100"
  }
  ```
- **Indexação Estratégica**: Índices em colunas de busca frequente

#### 5.1.3 Caching Strategy

**Redis - Dados em Cache**
```csharp
// Últimos 30 dias de registros por aeronave (Art. 8º II Resolução 457)
Key: "registros:aeronave:{aeronaveId}:30dias"
TTL: 24 horas
Invalidação: On update/insert

// Dados de aeronaves ativas
Key: "aeronave:{id}"
TTL: 12 horas
Invalidação: On update

// Tripulantes ativos
Key: "tripulante:{codigoAnac}"
TTL: 6 horas
Invalidação: On update

// JWT Tokens blacklist (logout/revogação)
Key: "token:blacklist:{jti}"
TTL: Tempo de expiração do token
```

**Estratégia Cache-Aside**
```csharp
public async Task<RegistroVoo> GetRegistroAsync(int id)
{
    var cacheKey = $"registro:{id}";
    
    // Tenta obter do cache
    var cached = await _cache.GetStringAsync(cacheKey);
    if (cached != null)
        return JsonSerializer.Deserialize<RegistroVoo>(cached);
    
    // Se não estiver em cache, busca do banco
    var registro = await _repository.GetByIdAsync(id);
    
    // Armazena em cache
    await _cache.SetStringAsync(
        cacheKey, 
        JsonSerializer.Serialize(registro),
        new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        });
    
    return registro;
}
```

### 5.2 Otimizações de Performance

#### 5.2.1 Frontend (Angular)

**Lazy Loading**
```typescript
const routes: Routes = [
  {
    path: 'diario-bordo',
    loadChildren: () => import('./features/diario-bordo/diario-bordo.module')
      .then(m => m.DiarioBordoModule)
  },
  {
    path: 'relatorios',
    loadChildren: () => import('./features/relatorios/relatorios.module')
      .then(m => m.RelatoriosModule)
  }
];
```

**OnPush Change Detection**
```typescript
@Component({
  selector: 'app-registro-voo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `...`
})
export class RegistroVooComponent {
  // Melhora performance em ~50%
}
```

**Virtual Scrolling** (listas grandes)
```typescript
<cdk-virtual-scroll-viewport itemSize="50" class="viewport">
  <div *cdkVirtualFor="let registro of registros">
    {{ registro.numeroSequencial }}
  </div>
</cdk-virtual-scroll-viewport>
```

**Bundle Optimization**
```json
{
  "configurations": {
    "production": {
      "optimization": true,
      "buildOptimizer": true,
      "aot": true,
      "sourceMap": false,
      "namedChunks": false,
      "extractLicenses": true,
      "vendorChunk": false
    }
  }
}
```

**Service Worker (PWA)**
- Cache de assets estáticos
- Offline-first para consultas
- Background sync para envio de registros

#### 5.2.2 Backend (C# .NET)

**Query Optimization**
```csharp
// ❌ N+1 Query Problem
using (var session = _sessionFactory.OpenSession())
{
    var registros = await session.Query<RegistroVoo>().ToListAsync();
    foreach (var r in registros)
    {
        var aeronave = await session.GetAsync<Aeronave>(r.AeronaveId); // N queries!
    }
}

// ✅ Eager Loading com NHibernate
using (var session = _sessionFactory.OpenSession())
{
    var registros = await session.Query<RegistroVoo>()
        .Fetch(r => r.Aeronave)
        .Fetch(r => r.PilotoComando)
        .FetchMany(r => r.Assinaturas)
        .ToListAsync(); // 1 query apenas
    
    // OU usando HQL
    var hql = @"SELECT r FROM RegistroVoo r
                LEFT JOIN FETCH r.Aeronave
                LEFT JOIN FETCH r.PilotoComando
                LEFT JOIN FETCH r.Assinaturas";
    var registrosHql = await session.CreateQuery(hql).ListAsync<RegistroVoo>();
}
```

**Asynchronous Programming**
```csharp
// Todas operações I/O assíncronas
public async Task<IActionResult> GetRegistros()
{
    var registros = await _service.GetRegistrosAsync(); // Não bloqueia thread
    return Ok(registros);
}
```

**Response Compression**
```csharp
// Startup.cs
services.AddResponseCompression(options =>
{
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.EnableForHttps = true;
});
```

**Pagination**
```csharp
public async Task<PagedResult<RegistroVoo>> GetRegistrosAsync(
    int pageNumber = 1, 
    int pageSize = 50)
{
    using (var session = _sessionFactory.OpenSession())
    {
        // Count total
        var total = await session.Query<RegistroVoo>()
            .CountAsync();
        
        // Paginação com NHibernate
        var registros = await session.Query<RegistroVoo>()
            .OrderByDescending(r => r.Data)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        // OU usando Criteria API
        var criteria = session.CreateCriteria<RegistroVoo>()
            .AddOrder(Order.Desc("Data"))
            .SetFirstResult((pageNumber - 1) * pageSize)
            .SetMaxResults(pageSize);
        
        var registrosCriteria = await criteria.ListAsync<RegistroVoo>();
        
        return new PagedResult<RegistroVoo>
        {
            Data = registros,
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
```

**Background Jobs (Hangfire)**
```csharp
// Sincronização ANAC em background
[AutomaticRetry(Attempts = 3)]
public async Task SincronizarComANAC()
{
    var pendentes = await _repository.GetPendentesS sincronizacaoAsync();
    foreach (var registro in pendentes)
    {
        await _anac.SincronizarAsync(registro);
    }
}

// Agendar job recorrente
RecurringJob.AddOrUpdate(
    "sincronizar-anac",
    () => SincronizarComANAC(),
    Cron.Hourly); // A cada hora
```

#### 5.2.3 Database Optimization

**Índices Compostos**
```sql
-- Consulta frequente: registros por aeronave nos últimos 30 dias
CREATE INDEX idx_aeronave_data_30dias 
ON RegistrosVoo(AeronaveId, Data DESC)
WHERE Data >= DATE_SUB(CURDATE(), INTERVAL 30 DAY);

-- Registros pendentes de assinatura do operador
CREATE INDEX idx_pendente_assinatura_operador
ON RegistrosVoo(AssinadoOperador, DataAssinaturaPilotoUTC)
WHERE AssinadoOperador = 0;
```

**Particionamento (para grandes volumes)**
```sql
-- Particionar por ano (retenção de 5 anos)
ALTER TABLE RegistrosVoo
PARTITION BY RANGE (YEAR(Data)) (
    PARTITION p2020 VALUES LESS THAN (2021),
    PARTITION p2021 VALUES LESS THAN (2022),
    PARTITION p2022 VALUES LESS THAN (2023),
    PARTITION p2023 VALUES LESS THAN (2024),
    PARTITION p2024 VALUES LESS THAN (2025),
    PARTITION p2025 VALUES LESS THAN (2026),
    PARTITION pfuture VALUES LESS THAN MAXVALUE
);
```

**Query Cache**
```sql
-- MySQL query cache para consultas repetitivas
SET GLOBAL query_cache_size = 268435456; -- 256MB
SET GLOBAL query_cache_type = ON;
```

### 5.3 Monitoramento e Observabilidade

#### 5.3.1 Application Performance Monitoring (APM)

**Métricas Coletadas**
```csharp
// Program.cs - OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("DiarioBordo.API"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddSource("NHibernate") // Tracing para NHibernate
        .AddHttpClientInstrumentation());
```

**Métricas de Negócio**
- Total de registros criados/dia
- Tempo médio entre criação e assinatura do piloto
- Registros pendentes de assinatura do operador
- Taxa de sincronização com ANAC
- Tempo de resposta por endpoint

#### 5.3.2 Logging Estruturado (Serilog)

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .WriteTo.File("logs/diariobordo-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.MySQL(connectionString, "Logs")
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "diariobordo-logs-{0:yyyy.MM.dd}"
    })
    .CreateLogger();

// Uso
_logger.LogInformation(
    "Registro {RegistroId} criado por {Usuario} para aeronave {Matricula}",
    registro.Id, 
    userId, 
    aeronave.Matricula);
```

#### 5.3.3 Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddMySql(connectionString, name: "mysql")
    .AddRedis(redisConnection, name: "redis")
    .AddCheck<ANACIntegrationHealthCheck>("anac-integration")
    .AddCheck<BackupHealthCheck>("backup-status");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

**Endpoints de Health**
- `/health` - Status geral do sistema
- `/health/ready` - Ready para receber tráfego
- `/health/live` - Liveness probe (Kubernetes)

#### 5.3.4 Alertas e Notificações

**Alertas Críticos** (PagerDuty/Opsgenie)
- Sistema indisponível (uptime < 99.9%)
- Banco de dados inacessível
- Falha em backup
- Perda de dados detectada
- Integração ANAC offline > 1 hora

**Alertas de Aviso** (Email/Slack)
- CPU > 80% por 10 minutos
- Memória > 85%
- Disco > 90%
- Registros pendentes de sincronização > 1000
- Tempo de resposta > 2 segundos (p95)

**Métricas SLA**
```yaml
Uptime: 99.9% (8.76 horas downtime/ano)
Response Time (p95): < 500ms
Response Time (p99): < 1s
Error Rate: < 0.1%
Throughput Mínimo: 500 req/s
Database Query Time (p95): < 100ms
```

---

## 6. Infraestrutura e DevOps

### 6.1 Ambientes
- **Development**: Ambiente local de desenvolvimento
- **Staging**: Ambiente de homologação
- **Production**: Ambiente de produção

### 6.2 Containerização
```yaml
# Docker
- Frontend: nginx + static files
- Backend: Node.js/Python application
- Database: PostgreSQL/MongoDB
- Cache: Redis
- Reverse Proxy: nginx
```

### 6.3 Orquestração
- **Kubernetes**: Orquestração de containers
- **Docker Compose**: Desenvolvimento local
- **Helm Charts**: Gerenciamento de deployments

### 6.4 CI/CD Pipeline

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│   Git    │───▶│  Build   │───▶│   Test   │───▶│  Deploy  │
│  Commit  │    │          │    │          │    │          │
└──────────┘    └──────────┘    └──────────┘    └──────────┘
                     │               │               │
                     ▼               ▼               ▼
                 Compile        Unit Tests      Staging
                 Lint           Integration     Production
                 Bundle         E2E Tests
```

### 6.5 Ferramentas
- **CI/CD**: GitHub Actions / GitLab CI / Jenkins
- **IaC**: Terraform / CloudFormation
- **Cloud Providers**: AWS / GCP / Azure
- **Monitoring**: CloudWatch / Stackdriver

---

## 7. APIs e Integrações

### 7.1 REST API Design

#### 7.1.1 Convenções
- **Recursos**: Substantivos no plural (`/users`, `/products`)
- **Métodos HTTP**: GET, POST, PUT, PATCH, DELETE
- **Status Codes**: 200, 201, 400, 401, 403, 404, 500
- **Versionamento**: `/api/v1/...`

#### 7.1.2 Estrutura de Response
```json
{
  "status": "success",
  "data": {
    "id": 1,
    "name": "Example"
  },
  "meta": {
    "page": 1,
    "limit": 10,
    "total": 100
  }
}
```

### 7.2 GraphQL (Opcional)
- **Schema-first**: Definição clara de tipos
- **Resolvers**: Lógica de busca de dados
- **DataLoader**: Batching e caching
- **Subscriptions**: Real-time updates

### 7.3 Integrações Externas
- **Payment Gateways**: Stripe, PayPal
- **Email Service**: SendGrid, AWS SES
- **SMS Service**: Twilio, AWS SNS
- **Cloud Storage**: AWS S3, Google Cloud Storage
- **Analytics**: Google Analytics, Mixpanel

---

## 8. Qualidade e Testes

### 8.1 Estratégia de Testes

#### 8.1.1 Pirâmide de Testes
```
        ┌─────────────┐
       ╱   E2E Tests   ╲
      ┌─────────────────┐
     ╱ Integration Tests ╲
    ┌─────────────────────┐
   ╱     Unit Tests        ╲
  └───────────────────────────┘
```

#### 8.1.2 Tipos de Testes
- **Unit Tests**: Jest, Mocha, PyTest
- **Integration Tests**: Supertest, TestContainers
- **E2E Tests**: Cypress, Playwright, Selenium
- **Performance Tests**: k6, JMeter
- **Security Tests**: OWASP ZAP, Snyk

### 8.2 Code Quality
- **Linting**: ESLint, Pylint
- **Formatting**: Prettier, Black
- **Type Checking**: TypeScript, mypy
- **Code Coverage**: Minimum 80%
- **Code Review**: Pull request obrigatório

---

## 9. Backup e Disaster Recovery

### 9.1 Backup
- **Database Backups**: Diários, retenção de 30 dias
- **Incremental Backups**: A cada 6 horas
- **Object Storage**: Replicação cross-region
- **Backup Testing**: Restauração mensal

### 9.2 Disaster Recovery
- **RTO**: Recovery Time Objective < 4 horas
- **RPO**: Recovery Point Objective < 1 hora
- **Multi-Region**: Failover automático
- **Runbooks**: Procedimentos documentados

---

## 10. Documentação

### 10.1 Tipos de Documentação
- **API Documentation**: Swagger/OpenAPI, GraphQL Playground
- **Code Documentation**: JSDoc, Docstrings
- **Architecture Diagrams**: C4 Model, UML
- **User Documentation**: Guias, tutoriais
- **Runbooks**: Procedimentos operacionais

### 10.2 Ferramentas
- **API Docs**: Swagger UI, Redoc
- **Diagrams**: draw.io, PlantUML
- **Wiki**: Confluence, Notion
- **README**: Markdown files

---

## 11. Decisões Arquiteturais

### 11.1 ADRs (Architecture Decision Records)

#### ADR-001: Escolha de Framework Frontend
- **Status**: Aceito
- **Contexto**: Necessidade de framework moderno e performático
- **Decisão**: React.js
- **Consequências**: Ecossistema rico, comunidade ativa

#### ADR-002: Padrão de API
- **Status**: Aceito
- **Contexto**: Definição de padrão de comunicação
- **Decisão**: REST API
- **Consequências**: Simplicidade, cache HTTP, amplamente adotado

#### ADR-003: Banco de Dados Principal
- **Status**: Aceito
- **Contexto**: Armazenamento de dados transacionais
- **Decisão**: PostgreSQL
- **Consequências**: ACID compliance, extensões poderosas

---

## 12. Roadmap e Melhorias Futuras

### 12.1 Curto Prazo (0-3 meses)
- [ ] Implementar autenticação multi-fator
- [ ] Adicionar testes E2E
- [ ] Configurar CI/CD completo
- [ ] Implementar rate limiting

### 12.2 Médio Prazo (3-6 meses)
- [ ] Migrar para microserviços (se necessário)
- [ ] Implementar GraphQL
- [ ] Adicionar observabilidade completa
- [ ] Implementar feature flags

### 12.3 Longo Prazo (6-12 meses)
- [ ] Multi-region deployment
- [ ] Machine Learning integrations
- [ ] Progressive Web App (PWA)
- [ ] Real-time features com WebSockets

---

## 13. Referências

### 13.1 Padrões e Boas Práticas
- [12 Factor App](https://12factor.net/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [REST API Best Practices](https://restfulapi.net/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)

### 13.2 Documentação de Tecnologias
- React: https://react.dev/
- Node.js: https://nodejs.org/
- PostgreSQL: https://www.postgresql.org/
- Docker: https://docs.docker.com/

---

## 14. Apêndices

### 14.1 Glossário
- **API Gateway**: Ponto de entrada único para APIs
- **Microservices**: Arquitetura de serviços independentes
- **Monolith**: Aplicação única e integrada
- **Serverless**: Execução sem gerenciamento de servidor

### 14.2 Diagramas Adicionais

#### Fluxo de Autenticação
```
┌────────┐         ┌────────┐         ┌──────────┐
│ Client │────────▶│  API   │────────▶│   Auth   │
└────────┘  POST   └────────┘  Verify └──────────┘
    │      /login      │                    │
    │                  │                    │
    │◀─────────────────┴────────────────────┘
    │           JWT Token
```

### 14.3 Métricas e SLAs
- **Uptime**: 99.9% (aproximadamente 8.76 horas de downtime/ano)
- **Response Time**: < 200ms (p95)
- **Error Rate**: < 0.1%
- **Throughput**: 1000 req/s mínimo

---

**Versão**: 1.0  
**Data**: 2025-10-22  
**Autor**: Equipe de Arquitetura  
**Status**: Em Revisão
