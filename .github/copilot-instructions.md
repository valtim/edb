# Diário de Bordo Digital - Instruções para Assistente de IA

## Visão Geral do Projeto

Este é um **Sistema de Diário de Bordo Digital** para aviação civil brasileira, desenvolvido para atender às Resoluções ANAC **457/2017** e **458/2017**. O sistema permite que pilotos e operadores registrem digitalmente informações de voo, substituindo diários em papel tradicionais mantendo conformidade regulatória.

## Arquitetura e Stack Tecnológico

### Backend: C# .NET 8 + NHibernate + MySQL
- **Clean Architecture**: Camadas Domain, Application, Infrastructure, API
- **ORM**: NHibernate 5.5+ com mapeamentos FluentNHibernate
- **Banco de Dados**: MySQL 8.0+ com engine InnoDB, timezone UTC
- **Autenticação**: JWT + ASP.NET Identity com autenticação multifator
- **API**: REST com documentação Swagger/OpenAPI
- **Tempo Real**: SignalR para notificações
- **Jobs em Background**: Hangfire para sincronização ANAC

### Frontend: Angular 17+ (Standalone Components)
- **Gerenciamento de Estado**: NgRx (padrão Redux)
- **Framework UI**: Angular Material 17+
- **Formulários**: Reactive Forms com validadores ANAC customizados
- **Suporte PWA**: Capacidade offline com service worker
- **Tempo Real**: RxJS Observables + cliente SignalR

### Infraestrutura
- **Cache**: Redis para sessões e requisitos de dados de 30 dias
- **Segurança**: TLS 1.3, criptografia AES-256, assinaturas digitais
- **Backup**: Estratégia tripla (local + nuvem + offline)
- **Integração**: Blockchain ANAC ou sincronização de banco de dados

## Requisitos Regulatórios Críticos

### Resolução ANAC 457/2017 - Diário de Bordo
Ao trabalhar com registros de voo (`RegistroVoo`), sempre incluir estes **17 campos obrigatórios** (Art. 4º):
1. Número sequencial, 2. Identificação da tripulação (códigos ANAC 6 dígitos), 3. Data (dd/mm/aaaa), 4. Aeroportos (IATA/ICAO/coordenadas), 5. Horários UTC, 6. Tempo de voo IFR, 7. Combustível (quantidade + unidade), 8. Natureza do voo, 9. Pessoas a bordo, 10. Carga, 11. Ocorrências, 12. Discrepâncias técnicas, 13. Ações corretivas, 14. Última manutenção, 15. Próxima manutenção, 16. Horas de célula, 17. Responsável por aprovação para retorno

### Resolução ANAC 458/2017 - Sistemas Eletrônicos
- **Assinaturas Digitais**: Devem ter autenticidade, integridade e irretratabilidade
- **Logs de Auditoria**: Todas operações registradas na tabela `LogsAuditoria`
- **Disponibilidade 30 Dias**: Últimos 30 dias sempre acessíveis (Art. 8º II Res. 457)
- **Retenção de Dados**: 5 anos após cancelamento RAB da aeronave
- **Prazos de Assinatura**: RBAC 121 (2 dias), RBAC 135 (15 dias), outros (30 dias)

## Padrões de Desenvolvimento Essenciais

### Uso do NHibernate
```csharp
// Sempre usar sessions corretamente com using statements
using (var session = _sessionFactory.OpenSession())
using (var transaction = session.BeginTransaction())
{
    // Eager loading para evitar queries N+1
    var registros = await session.Query<RegistroVoo>()
        .Fetch(r => r.Aeronave)
        .Fetch(r => r.PilotoComando)
        .ToListAsync();
    
    await transaction.CommitAsync();
}
```

### Padrões de Validação ANAC
- **Códigos ANAC**: Sempre 6 dígitos (`^\d{6}$`)
- **Datas**: Formato dd/mm/aaaa para exibição, UTC para armazenamento
- **Aeroportos**: IATA (3 chars), ICAO (4 chars), ou coordenadas
- **Horários**: Sempre UTC no banco, converter para exibição

### Assinaturas Digitais
Toda assinatura deve capturar:
- Autenticação do usuário (usuário e senha individuais)
- Timestamp UTC
- Hash SHA-256 do registro completo
- Tipo de assinatura (PILOTO/OPERADOR)
- Endereço IP e user agent para auditoria

### Estratégia de Cache
```csharp
// Últimos 30 dias em cache por aeronave (requisito regulatório)
Key: "registros:aeronave:{aeronaveId}:30dias"
TTL: 24 hours
Invalidate: On any update/insert
```

## Esquema de Banco de Dados Essencial

### Tabelas Principais
- `RegistrosVoo`: Registros de voo com todos os 17 campos obrigatórios
- `Aeronaves`: Registro de aeronaves (conformidade RAB)
- `Tripulantes`: Membros da tripulação com códigos ANAC
- `AssinaturasRegistro`: Rastro de auditoria de assinaturas
- `LogsAuditoria`: Log completo de auditoria do sistema

### Índices Críticos
- `idx_aeronave_data_30dias` em `RegistrosVoo(AeronaveId, Data DESC)` para consultas de 30 dias
- `idx_pendente_assinatura_operador` para gerenciamento de workflow
- `idx_codigo_anac` em `Tripulantes(CodigoANAC)` para validação de tripulação

## Segurança e Conformidade

### Fluxo de Autenticação
1. JWT access token (15 min) + refresh token (7 dias)
2. Acesso baseado em papéis: Piloto, Operador, DiretorOperacoes, Fiscalizacao
3. Autenticação multifator para operadores
4. Gerenciamento de sessões via Redis

### Proteção de Dados
- **Criptografia em Repouso**: AES-256 para dados sensíveis
- **Criptografia em Trânsito**: TLS 1.3 obrigatório
- **Mascaramento de Dados**: Nenhum CPF/senha nos logs
- **Criptografia de Backup**: Todos os níveis de backup criptografados

## Operações Comuns

### Criação de Registro de Voo
1. Validar todos os 17 campos obrigatórios conforme formatos ANAC
2. Gerar número sequencial por aeronave
3. Criar com workflow de assinatura do piloto
4. Cache para disponibilidade de 30 dias
5. Enfileirar para assinatura do operador baseado no tipo RBAC

### Integração ANAC
- **Opção B**: Sincronização por blockchain (preferida)
- **Opção C**: Sincronização direta de banco de dados
- Jobs em background verificam status de sincronização a cada hora
- Falhas de sincronização geram alertas e novas tentativas

## Fluxo de Desenvolvimento

### Requisitos de Testes
- Testes unitários para validadores ANAC (conformidade crítica)
- Testes de integração para workflows de assinatura
- Testes E2E para ciclo completo de registro de voo
- Testes de performance para requisitos de consulta de 30 dias

### Considerações de Deploy
- Deploys sem downtime (dados regulatórios não podem ser perdidos)
- Estratégia de migração de banco preservando retenção de 5 anos
- Verificação de backup antes de mudanças de schema
- Notificação à ANAC para janelas de manutenção do sistema

## Padrões de Tratamento de Erros

- Nunca logar dados sensíveis (CPF, senhas, assinaturas)
- Auditar todas operações com falha em `LogsAuditoria`
- Degradação elegante para falhas de sincronização ANAC
- Mensagens de erro amigáveis preservando rastro de auditoria

## Metas de Performance

- **Tempo de Resposta**: <500ms para consultas de 30 dias
- **Disponibilidade**: 99.9% uptime (requisito regulatório)
- **Recuperação de Dados**: RTO 4 horas, RPO 1 hora
- **Usuários Concorrentes**: Suporte para 1000 sessões ativas

Lembre-se: Este sistema lida com dados críticos de segurança da aviação. Perda ou corrupção de dados pode resultar em interdição de aeronaves e sanções regulatórias. Sempre priorize integridade de dados e conformidade sobre velocidade de entrega de funcionalidades.