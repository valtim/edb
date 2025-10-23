# 🛩️ Sistema Diário de Bordo Digital

Sistema de Diário de Bordo Digital para aviação civil brasileira, em conformidade com as **Resoluções ANAC 457/2017** e **458/2017**.

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-17+-red.svg)](https://angular.io/)
[![MySQL](https://img.shields.io/badge/MySQL-8.0-orange.svg)](https://www.mysql.com/)
[![Redis](https://img.shields.io/badge/Redis-7.0-red.svg)](https://redis.io/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## 📋 Índice

- [Sobre o Projeto](#sobre-o-projeto)
- [Conformidade Regulatória](#conformidade-regulatória)
- [Tecnologias](#tecnologias)
- [Configuração Rápida](#configuração-rápida)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Desenvolvimento](#desenvolvimento)
- [Testes](#testes)
- [Deployment](#deployment)
- [Contribuição](#contribuição)

## 🎯 Sobre o Projeto

O Sistema Diário de Bordo Digital substitui os diários em papel tradicionais, oferecendo uma solução digital completa que atende aos requisitos regulatórios da ANAC, mantendo a integridade, autenticidade e disponibilidade dos dados de voo.

### ✨ Funcionalidades Principais

- ✅ **Registro Digital de Voos** - 17 campos obrigatórios conforme ANAC 457/2017
- 🔐 **Assinaturas Digitais** - Conformidade com ANAC 458/2017
- 📊 **Consulta dos Últimos 30 Dias** - Disponibilidade garantida (< 500ms)
- 🔄 **Sincronização ANAC** - Integração via blockchain ou banco de dados
- 📱 **Interface Responsiva** - PWA com suporte offline
- 🔔 **Notificações em Tempo Real** - Via SignalR
- 📈 **Relatórios e Estatísticas** - Dashboard executivo
- 🔒 **Segurança Avançada** - JWT, MFA, criptografia AES-256

## ⚖️ Conformidade Regulatória

### Resolução ANAC 457/2017 - Diário de Bordo
- ✅ **17 Campos Obrigatórios** (Art. 4º) - Todos implementados e validados
- ✅ **Disponibilidade 30 Dias** (Art. 8º II) - Cache Redis + performance < 500ms
- ✅ **Retenção 5 Anos** - Após cancelamento RAB da aeronave
- ✅ **Numeração Sequencial** - Por aeronave, sem lacunas

### Resolução ANAC 458/2017 - Sistemas Eletrônicos
- ✅ **Assinaturas Digitais** - Autenticidade, integridade, irretratabilidade
- ✅ **Logs de Auditoria** - Todas operações registradas
- ✅ **Prazos de Assinatura** - RBAC 121 (2 dias), RBAC 135 (15 dias), outros (30 dias)
- ✅ **Segurança de Dados** - Criptografia end-to-end

## 🛠️ Tecnologias

### Backend
- **Framework**: .NET 9.0 + C#
- **Arquitetura**: Clean Architecture (Domain, Application, Infrastructure, API)
- **ORM**: NHibernate 5.5+ com FluentNHibernate
- **Banco de Dados**: MySQL 8.0+ (InnoDB, timezone UTC)
- **Cache**: Redis 7.0+
- **Autenticação**: JWT + ASP.NET Identity + MFA
- **Jobs**: Hangfire para tarefas em background
- **Tempo Real**: SignalR

### Frontend
- **Framework**: Angular 17+ (Standalone Components)
- **Estado**: NgRx (Redux pattern)
- **UI**: Angular Material 17+
- **PWA**: Service Worker para offline
- **Tempo Real**: RxJS + SignalR client

### Infraestrutura
- **Containerização**: Docker + Docker Compose
- **CI/CD**: GitHub Actions
- **Segurança**: TLS 1.3, AES-256, assinaturas digitais
- **Backup**: Estratégia tripla (local + nuvem + offline)
- **Monitoramento**: Health checks + métricas

## 🚀 Configuração Rápida

### Pré-requisitos
- .NET 9.0 SDK
- Docker Desktop
- Node.js 18+ (para frontend)
- Git

### Instalação Automática

```bash
# 1. Clone o repositório
git clone https://github.com/seu-usuario/diariodebordo.git
cd diariodebordo

# 2. Execute o script de configuração
./setup-dev.sh

# 3. Inicie o ambiente de desenvolvimento
./start-dev.sh
```

### Instalação Manual

```bash
# 1. Configurar banco de dados
docker run -d --name diario-mysql -p 3306:3306 \
  -e MYSQL_ROOT_PASSWORD=DiarioBordo123! \
  -e MYSQL_DATABASE=diario_bordo \
  -e MYSQL_USER=diario_user \
  -e MYSQL_PASSWORD=DiarioPass123! \
  mysql:8.0

# 2. Configurar Redis
docker run -d --name diario-redis -p 6379:6379 redis:7-alpine

# 3. Restaurar dependências
dotnet restore

# 4. Executar migrations
dotnet ef database update --project src/DiarioBordo.Infrastructure

# 5. Iniciar API
cd src/DiarioBordo.API
dotnet run
```

## 📁 Estrutura do Projeto

```
diariodebordo/
├── src/
│   ├── DiarioBordo.Domain/          # Entidades e regras de negócio
│   ├── DiarioBordo.Application/     # Casos de uso e DTOs
│   ├── DiarioBordo.Infrastructure/  # Repositórios e serviços externos
│   └── DiarioBordo.API/            # Controllers e configuração
├── tests/
│   ├── DiarioBordo.Domain.Tests/    # Testes unitários + conformidade ANAC
│   ├── DiarioBordo.Infrastructure.Tests/ # Testes de repositório
│   └── DiarioBordo.API.Tests/      # Testes de integração
├── frontend/                       # Aplicação Angular (PWA)
├── docs/                          # Documentação adicional
├── scripts/                       # Scripts de deployment
├── .github/                       # CI/CD workflows
├── setup-dev.sh                   # Configuração do ambiente
├── run-tests.sh                   # Execução de testes
└── start-dev.sh                   # Iniciar desenvolvimento
```

## 👩‍💻 Desenvolvimento

### Comandos Úteis

```bash
# Desenvolvimento
./start-dev.sh              # Iniciar todos os serviços
./stop-dev.sh               # Parar serviços
./reset-dev.sh              # Resetar ambiente

# Testes
./run-tests.sh              # Executar todos os testes
dotnet test                 # Testes básicos
dotnet test --collect:"XPlat Code Coverage"  # Com cobertura

# Banco de dados
dotnet ef migrations add NomeMigracao --project src/DiarioBordo.Infrastructure
dotnet ef database update --project src/DiarioBordo.Infrastructure

# Qualidade de código
dotnet format               # Formatação
dotnet outdated            # Pacotes desatualizados
```

### URLs de Desenvolvimento

- **API**: https://localhost:5001
- **Swagger**: https://localhost:5001/swagger
- **Health Check**: https://localhost:5001/health
- **Frontend**: http://localhost:4200 (quando disponível)

### Configuração do VS Code

Extensões recomendadas:
- C# Dev Kit
- NuGet Package Manager
- Thunder Client (para testes de API)
- GitLens
- SonarLint

## 🧪 Testes

### Estratégia de Testes

1. **Testes de Conformidade ANAC** - Validação regulatória obrigatória
2. **Testes Unitários** - Cobertura de regras de negócio
3. **Testes de Integração** - Endpoints da API
4. **Testes de Performance** - Requisito < 500ms para consultas
5. **Testes de Segurança** - Vulnerabilidades e configurações

### Execução

```bash
# Todos os testes
./run-tests.sh

# Por categoria
dotnet test --filter "FullyQualifiedName~AnacCompliance"     # Conformidade ANAC
dotnet test --filter "FullyQualifiedName~Performance"       # Performance
dotnet test --filter "FullyQualifiedName~Security"          # Segurança

# Cobertura de código
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage"
```

### Relatórios

Os relatórios de teste são gerados em `test-results/`:
- **Conformidade ANAC**: Validação dos 17 campos obrigatórios
- **Performance**: Verificação do limite de 500ms
- **Cobertura**: Relatório HTML em `test-results/coverage/index.html`
- **Segurança**: Análise de vulnerabilidades

## 🚀 Deployment

### Ambientes

- **Development**: Ambiente local com Docker
- **Staging**: Deploy automático via GitHub Actions
- **Production**: Deploy manual com aprovação

### CI/CD Pipeline

O pipeline automatizado inclui:

1. **Build & Test** - Compilação e execução de testes
2. **ANAC Compliance** - Verificação regulatória obrigatória
3. **Security Scan** - Análise de vulnerabilidades
4. **Performance Test** - Validação do SLA < 500ms
5. **Docker Build** - Criação de imagens
6. **Deploy** - Deployment automático/manual

### Configuração de Produção

```bash
# Variáveis de ambiente necessárias
CONNECTIONSTRINGS__DEFAULTCONNECTION="Server=prod-mysql;Database=diario_bordo;..."
CONNECTIONSTRINGS__REDIS="prod-redis:6379"
JWTSETTINGS__SECRETKEY="ChaveSecretaProducao..."
ANACSETTINGS__SYNCENABLED="true"
ANACSETTINGS__APIENDPOINT="https://api.anac.gov.br/diario-bordo"
ANACSETTINGS__CERTIFICADOPATH="/app/certs/anac.p12"
```

## 📊 Monitoramento

### Health Checks

- `/health` - Status geral da aplicação
- `/health/ready` - Pronto para receber tráfego
- `/health/live` - Aplicação está funcionando

### Métricas

- Tempo de resposta das consultas de 30 dias
- Taxa de sucesso das sincronizações ANAC
- Número de registros pendentes de assinatura
- Disponibilidade do sistema

## 🔒 Segurança

### Características de Segurança

- **Criptografia**: AES-256 em repouso, TLS 1.3 em trânsito
- **Autenticação**: JWT + refresh tokens + MFA
- **Autorização**: Baseada em papéis (RBAC)
- **Auditoria**: Logs completos de todas as operações
- **Assinaturas**: SHA-256 + timestamp + dados do usuário

### Papéis de Usuário

- **Piloto**: Criação e assinatura de registros
- **Operador**: Aprovação e assinatura de registros
- **DiretorOperacoes**: Relatórios e configurações
- **Fiscalizacao**: Acesso somente leitura para ANAC

## 🤝 Contribuição

### Como Contribuir

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/nova-funcionalidade`)
3. Execute os testes (`./run-tests.sh`)
4. Commit suas mudanças (`git commit -m 'Adiciona nova funcionalidade'`)
5. Push para a branch (`git push origin feature/nova-funcionalidade`)
6. Abra um Pull Request

### Padrões de Código

- **C#**: Seguir guidelines da Microsoft + EditorConfig
- **Commits**: Conventional Commits (feat, fix, docs, etc.)
- **Testes**: Cobertura mínima de 80%
- **Documentação**: Atualizar README e docs/ conforme necessário

### Requisitos para PR

- ✅ Todos os testes passando
- ✅ Conformidade ANAC validada
- ✅ Sem vulnerabilidades críticas
- ✅ Documentação atualizada

## 📄 Licença

Este projeto está licenciado sob a [Licença MIT](LICENSE).

## 📞 Suporte

- **Documentação**: [docs/](docs/)
- **Issues**: [GitHub Issues](https://github.com/seu-usuario/diariodebordo/issues)
- **Email**: suporte@diariobordo.com.br

---

<div align="center">

**🛩️ Sistema Diário de Bordo Digital - Conformidade ANAC 457/2017 e 458/2017**

*Desenvolvido com ❤️ para a aviação civil brasileira*

</div>