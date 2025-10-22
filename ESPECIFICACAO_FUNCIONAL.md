# Especificação Funcional - Sistema de Diário de Bordo Digital

**Versão**: 1.0  
**Data**: 22 de outubro de 2025  
**Autor**: Equipe de Produto  
**Status**: Em Desenvolvimento

---

## 1. Visão Geral

### 1.1 Objetivo
Especificar os requisitos funcionais do Sistema de Diário de Bordo Digital para aeronaves civis brasileiras, em conformidade com as Resoluções ANAC nº 457/2017 e nº 458/2017.

### 1.2 Escopo
Este documento define as funcionalidades que devem ser implementadas no sistema para atender às necessidades dos usuários e aos requisitos regulatórios da ANAC.

### 1.3 Usuários do Sistema
- **Pilotos**: Registram e assinam informações de voo
- **Operadores**: Aprovam e assinam informações de manutenção
- **Diretores de Operações**: Responsáveis técnicos pela assinatura
- **Equipe de Manutenção**: Registram informações técnicas
- **Fiscalização ANAC**: Consultam dados para auditoria
- **Administradores**: Gerenciam o sistema

---

## 2. Módulos Funcionais

### 2.1 Módulo de Gestão de Aeronaves

#### RF001 - Cadastrar Aeronave
**Descrição**: Permitir o cadastro de aeronaves no sistema.

**Critérios de Aceite**:
- Sistema deve solicitar os campos obrigatórios conforme Art. 8º Res. 457:
  - Marcas de nacionalidade e matrícula
  - Fabricante
  - Modelo
  - Número de série
  - Categoria de registro da aeronave
- Validar formato da matrícula (ex: PT-ABC, PP-XYZ)
- Verificar unicidade da matrícula no sistema
- Gerar número sequencial automático para registros de voo
- Definir status da aeronave (Ativa/Inativa)

**Fluxo Principal**:
1. Usuário acessa "Cadastro de Aeronaves"
2. Sistema apresenta formulário com campos obrigatórios
3. Usuário preenche informações da aeronave
4. Sistema valida dados conforme regras ANAC
5. Sistema salva aeronave e exibe confirmação

**Regras de Negócio**:
- RN001: Matrícula deve seguir padrão brasileiro (2 letras + hífen + 3 caracteres)
- RN002: Somente usuários com perfil "Operador" ou superior podem cadastrar aeronaves
- RN003: Aeronave inativa não pode ter novos registros de voo

#### RF002 - Consultar Aeronaves
**Descrição**: Permitir consulta e listagem de aeronaves cadastradas.

**Critérios de Aceite**:
- Listar aeronaves com filtros por matrícula, fabricante, modelo
- Exibir status atual (Ativa/Inativa)
- Mostrar data do último voo registrado
- Permitir exportação da lista em PDF/Excel

#### RF003 - Atualizar Dados da Aeronave
**Descrição**: Permitir atualização de informações da aeronave.

**Critérios de Aceite**:
- Permitir edição de campos não-chave (fabricante, modelo, etc.)
- Não permitir alteração da matrícula após criação
- Registrar todas alterações em log de auditoria
- Solicitar justificativa para mudanças significativas

### 2.2 Módulo de Gestão de Tripulantes

#### RF004 - Cadastrar Tripulante
**Descrição**: Permitir cadastro de tripulantes com códigos ANAC.

**Critérios de Aceite**:
- Solicitar Código ANAC (6 dígitos numéricos)
- Validar formato do CPF
- Registrar nome completo, email, telefone
- Definir funções autorizadas (P, I, O, C, M)
- Validar unicidade do código ANAC e CPF

**Regras de Negócio**:
- RN004: Código ANAC deve ter exatamente 6 dígitos
- RN005: CPF deve ser válido conforme algoritmo oficial
- RN006: Email deve ser único no sistema

#### RF005 - Consultar Tripulantes
**Descrição**: Permitir busca de tripulantes por código ANAC ou nome.

**Critérios de Aceite**:
- Busca por código ANAC, nome ou CPF
- Exibir histórico de voos do tripulante
- Mostrar licenças e validades
- Permitir filtro por função (Piloto, Copiloto, etc.)

### 2.3 Módulo de Registro de Voo (Core)

#### RF006 - Criar Registro de Voo
**Descrição**: Permitir criação de novo registro de voo com todos os campos obrigatórios (Art. 4º Res. 457).

**Critérios de Aceite**:
- Formulário deve conter os 17 campos obrigatórios:
  1. Número sequencial cronológico (gerado automaticamente)
  2. Identificação da tripulação (código ANAC + função + horário apresentação)
  3. Data (formato dd/mm/aaaa)
  4. Locais de decolagem e pouso (IATA/ICAO/coordenadas)
  5. Horários UTC (decolagem, pouso, partida motores, corte motores)
  6. Tempo de voo IFR (horas decimais)
  7. Total de combustível por etapa (quantidade + unidade)
  8. Natureza do voo (privado/comercial/outro)
  9. Quantidade de pessoas a bordo
  10. Carga transportada (quantidade + unidade)
  11. Ocorrências
  12. Discrepâncias técnicas e pessoa que detectou
  13. Ações corretivas
  14. Tipo da última intervenção de manutenção
  15. Tipo da próxima intervenção de manutenção
  16. Horas de célula previstas para próxima manutenção
  17. Responsável pela aprovação para retorno ao serviço

**Validações Específicas**:
- Data não pode ser futura
- Horários devem ser em UTC
- Códigos de aeroporto devem existir (IATA 3 chars, ICAO 4 chars)
- Combustível deve ter quantidade e unidade (kg, lb, litros)
- Tripulantes devem ter códigos ANAC válidos

**Fluxo Principal**:
1. Piloto seleciona aeronave
2. Sistema gera número sequencial automático
3. Piloto preenche formulário com dados do voo
4. Sistema valida todos os campos obrigatórios
5. Piloto assina digitalmente o registro (incisos I-XII)
6. Sistema salva registro e envia para fila de assinatura do operador

**Regras de Negócio**:
- RN007: Apenas piloto em comando pode criar registros
- RN008: Registro deve ser assinado pelo piloto até fim da jornada
- RN009: Após assinatura do piloto, campos I-XII tornam-se somente leitura

#### RF007 - Assinar Registro como Piloto
**Descrição**: Permitir assinatura digital do registro pelo piloto em comando.

**Critérios de Aceite**:
- Solicitar usuário e senha individuais (Art. 4º § 2º)
- Capturar timestamp UTC da assinatura
- Gerar hash SHA-256 do registro completo
- Registrar IP e user agent para auditoria
- Bloquear edição dos campos I-XII após assinatura
- Notificar operador sobre pendência de assinatura

**Fluxo Principal**:
1. Piloto clica em "Assinar Registro"
2. Sistema apresenta modal de assinatura
3. Piloto informa credenciais
4. Sistema valida autenticação
5. Sistema gera hash e registra assinatura
6. Sistema atualiza status e notifica operador

#### RF008 - Assinar Informações de Manutenção
**Descrição**: Permitir assinatura das informações de manutenção pelo operador.

**Critérios de Aceite**:
- Operador pode assinar informações dos incisos XIII-XVII
- Respeitar prazos conforme tipo RBAC:
  - RBAC 121: 2 dias após assinatura do piloto
  - RBAC 135: 15 dias após assinatura do piloto
  - Outros: 30 dias após assinatura do piloto
- Gerar alertas de prazo próximo ao vencimento
- Permitir assinatura apenas por operador ou pessoa designada

#### RF009 - Consultar Registros de Voo
**Descrição**: Permitir consulta aos registros de voo com diferentes filtros.

**Critérios de Aceite**:
- Últimos 30 dias sempre disponíveis (Art. 8º II)
- Filtros por aeronave, período, piloto, status de assinatura
- Exibir indicador visual de quem assinou cada informação
- Performance: consulta de 30 dias em <500ms
- Permitir busca por número sequencial
- Mostrar histórico completo da aeronave

**Interface**:
- Lista com paginação (50 registros por página)
- Colunas: Data, Aeronave, Piloto, Origem-Destino, Status Assinatura
- Filtros laterais expansíveis
- Botão de exportação (PDF/Excel)

#### RF010 - Editar Registro de Voo
**Descrição**: Permitir edição limitada de registros conforme status.

**Critérios de Aceite**:
- Antes da assinatura do piloto: edição completa permitida
- Após assinatura do piloto: apenas campos XIII-XVII editáveis pelo operador
- Após assinatura completa: nenhuma edição permitida
- Registrar todas alterações em log de auditoria
- Exibir histórico de modificações

**Regras de Negócio**:
- RN010: Somente criador pode editar antes da assinatura
- RN011: Operador pode editar apenas informações de manutenção
- RN012: Após sincronização ANAC, registro torna-se somente leitura

### 2.4 Módulo de Assinaturas Digitais

#### RF011 - Gerenciar Assinaturas Digitais
**Descrição**: Implementar sistema de assinaturas conforme Res. 458 Art. 2º.

**Propriedades Obrigatórias**:
- **Autenticidade**: Identificação do signatário
- **Integridade**: Hash do documento
- **Irretratabilidade**: Impossibilidade de negar

**Critérios de Aceite**:
- Capturar dados completos da assinatura:
  - Usuário e senha individuais
  - Data/hora UTC
  - Hash SHA-256 do registro
  - IP address e user agent
  - Tipo de assinatura (PILOTO/OPERADOR)
- Validar credenciais em tempo real
- Armazenar assinatura de forma imutável
- Permitir verificação posterior da integridade

#### RF012 - Workflow de Assinaturas
**Descrição**: Gerenciar fluxo de assinaturas em duas etapas.

**Fluxo de Estados**:
1. **Criado**: Registro criado, pendente assinatura piloto
2. **Assinado Piloto**: Piloto assinou, pendente operador
3. **Assinado Completo**: Ambos assinaram, pronto para ANAC
4. **Sincronizado**: Enviado para ANAC com sucesso

**Critérios de Aceite**:
- Dashboard mostrando registros por status
- Notificações automáticas de pendências
- Alertas de prazo próximo ao vencimento
- Relatórios de compliance por período

### 2.5 Módulo de Integração ANAC

#### RF013 - Sincronizar com ANAC
**Descrição**: Integrar com sistemas ANAC conforme Res. 458 Art. 3º II.

**Opções de Integração**:
- **Opção B**: Blockchain ANAC (preferencial)
- **Opção C**: Banco de dados ANAC

**Critérios de Aceite**:
- Enviar registros completamente assinados para ANAC
- Receber confirmação de recebimento
- Registrar status de sincronização
- Implementar retry automático para falhas
- Manter log detalhado de todas as transmissões

#### RF014 - Monitorar Sincronização
**Descrição**: Acompanhar status das sincronizações com ANAC.

**Critérios de Aceite**:
- Dashboard com estatísticas de sincronização
- Lista de registros pendentes/falhados
- Alertas para falhas de comunicação
- Relatório de conformidade regulatória
- Job automático de verificação a cada hora

### 2.6 Módulo de Relatórios

#### RF015 - Gerar Relatórios Regulatórios
**Descrição**: Produzir relatórios necessários para conformidade ANAC.

**Tipos de Relatórios**:
- Diário de bordo completo por aeronave
- Registros por período e piloto
- Status de assinaturas pendentes
- Histórico de manutenção
- Relatório de auditoria

**Critérios de Aceite**:
- Exportação em PDF, Excel e JSON
- Assinatura digital dos relatórios
- Timestamp UTC em todos os relatórios
- Filtros flexíveis por data, aeronave, piloto
- Performance: geração em <30 segundos

#### RF016 - Exportar Dados
**Descrição**: Permitir exportação de dados para terceiros (venda/transferência).

**Critérios de Aceite**:
- Exportar histórico completo da aeronave (Art. 15 Res. 457)
- Incluir todas as assinaturas e auditorias
- Formato aceito pela ANAC
- Assinatura digital do operador
- Opção de entrega física ou digital

### 2.7 Módulo de Auditoria

#### RF017 - Sistema de Logs de Auditoria
**Descrição**: Registrar todos os eventos conforme Res. 458 Art. 2º II.

**Eventos Auditados**:
- Criação, edição, exclusão de registros
- Todas as assinaturas digitais
- Tentativas de acesso não autorizado
- Sincronizações com ANAC
- Alterações de configuração
- Login/logout de usuários

**Critérios de Aceite**:
- Capturar: usuário, data/hora UTC, operação, dados antes/depois
- Armazenamento imutável (append-only)
- Busca e filtros avançados
- Retenção por 5 anos mínimo
- Exportação para auditoria externa

#### RF018 - Monitoramento de Compliance
**Descrição**: Acompanhar aderência aos requisitos regulatórios.

**Indicadores**:
- % de registros assinados no prazo
- Taxa de sincronização com ANAC
- Registros pendentes por aeronave
- Tempo médio de assinatura
- Alertas de não conformidade

### 2.8 Módulo de Administração

#### RF019 - Gestão de Usuários
**Descrição**: Gerenciar usuários e permissões do sistema.

**Perfis de Usuário**:
- **Piloto**: Criar e assinar registros próprios
- **Operador**: Assinar informações de manutenção
- **Diretor Operações**: Pessoa competente para assinatura
- **Manutenção**: Registrar informações técnicas
- **Fiscalização**: Acesso somente leitura
- **Admin**: Gestão completa do sistema

**Critérios de Aceite**:
- CRUD completo de usuários
- Associação com códigos ANAC
- Configuração de permissões por módulo
- Histórico de acessos e ações
- Bloqueio/desbloqueio de contas

#### RF020 - Configurações do Sistema
**Descrição**: Permitir configuração de parâmetros operacionais.

**Configurações**:
- Prazos de assinatura por tipo RBAC
- Parâmetros de integração ANAC
- Políticas de backup e retenção
- Configurações de cache e performance
- Alertas e notificações

---

## 3. Requisitos de Interface

### 3.1 Interface Web (Angular)

#### RU001 - Responsividade
- Interface adaptável para desktop, tablet e mobile
- Suporte a PWA para uso offline
- Sincronização automática quando online

#### RU002 - Usabilidade
- Formulários com validação em tempo real
- Auto-complete para códigos ANAC e aeroportos
- Wizard para criação de registros complexos
- Atalhos de teclado para operações frequentes

#### RU003 - Acessibilidade
- Conformidade com WCAG 2.1 AA
- Suporte a leitores de tela
- Navegação por teclado
- Alto contraste opcional

### 3.2 Dashboard Principal

#### RU004 - Dashboard do Piloto
- Registros pendentes de assinatura
- Últimos voos realizados
- Alertas de prazo
- Acesso rápido a nova criação

#### RU005 - Dashboard do Operador
- Registros pendentes de assinatura operador
- Status de sincronização ANAC
- Alertas de compliance
- Estatísticas de frota

---

## 4. Requisitos de Performance

### RP001 - Tempos de Resposta
- Consulta últimos 30 dias: <500ms
- Criação de registro: <2 segundos
- Login/autenticação: <1 segundo
- Geração de relatórios: <30 segundos

### RP002 - Disponibilidade
- Uptime: 99.9% (8.76 horas/ano máximo downtime)
- Manutenções programadas apenas em janelas autorizadas
- Failover automático em caso de falha

### RP003 - Capacidade
- Suporte a 1000 usuários simultâneos
- 10.000 registros de voo/dia
- Crescimento de 50% ano a ano
- Armazenamento de 5 anos de dados

---

## 5. Requisitos de Segurança

### RS001 - Autenticação
- Login obrigatório para todas as funcionalidades
- Autenticação multifator para operadores
- Sessão com timeout configurável
- Bloqueio por tentativas de login inválidas

### RS002 - Autorização
- Controle de acesso baseado em roles (RBAC)
- Segregação por aeronave/operador
- Auditoria de todas as ações
- Princípio do menor privilégio

### RS003 - Criptografia
- HTTPS obrigatório (TLS 1.3)
- Dados sensíveis criptografados em repouso (AES-256)
- Assinaturas digitais com certificados ICP-Brasil
- Senhas com hash bcrypt

---

## 6. Requisitos de Integração

### RI001 - APIs Externas
- Integração com blockchain/banco ANAC
- Consulta códigos de aeroportos (IATA/ICAO)
- Validação códigos ANAC de tripulantes
- Serviços de notificação (email/SMS)

### RI002 - Importação/Exportação
- Importar dados de sistemas legados
- Exportar para formatos padrão (JSON, XML, CSV)
- Integração com sistemas ERP de operadores
- API REST para terceiros autorizados

---

## 7. Casos de Uso Críticos

### UC001 - Registro Completo de Voo
**Ator**: Piloto em Comando  
**Pré-condições**: Piloto autenticado, aeronave cadastrada  
**Fluxo**:
1. Piloto seleciona aeronave
2. Sistema carrega formulário com dados pré-preenchidos
3. Piloto completa informações do voo
4. Sistema valida todos os 17 campos obrigatórios
5. Piloto assina digitalmente
6. Sistema registra assinatura e notifica operador
7. Operador assina informações de manutenção dentro do prazo
8. Sistema sincroniza com ANAC automaticamente

**Pós-condições**: Registro completo, assinado e sincronizado

### UC002 - Consulta de Auditoria ANAC
**Ator**: Fiscal ANAC  
**Pré-condições**: Acesso autorizado pela ANAC  
**Fluxo**:
1. Fiscal acessa sistema com credenciais especiais
2. Sistema apresenta interface de consulta
3. Fiscal filtra por aeronave, período ou operador
4. Sistema exibe registros com todas as assinaturas
5. Fiscal exporta dados necessários
6. Sistema registra acesso em log de auditoria

---

## 8. Critérios de Aceitação Gerais

### CG001 - Conformidade Regulatória
- 100% dos campos obrigatórios implementados
- Validações conforme formatos ANAC
- Prazos de assinatura respeitados
- Auditoria completa de todas as operações

### CG002 - Qualidade de Software
- Cobertura de testes unitários >80%
- Testes de integração para fluxos críticos
- Testes de performance automatizados
- Zero vulnerabilidades críticas de segurança

### CG003 - Experiência do Usuário
- Tempo de aprendizado <2 horas para pilotos
- Formulário de registro completável em <10 minutos
- Zero perda de dados em operações críticas
- Feedback visual para todas as ações

---

## 9. Glossário

- **ANAC**: Agência Nacional de Aviação Civil
- **RAB**: Registro Aeronáutico Brasileiro
- **RBAC**: Regulamento Brasileiro da Aviação Civil
- **UTC**: Tempo Universal Coordenado
- **IFR**: Regras de Voo por Instrumentos
- **IATA**: Associação Internacional de Transporte Aéreo
- **ICAO**: Organização da Aviação Civil Internacional

---

**Aprovações**:
- [ ] Product Owner: _________________ Data: _______
- [ ] Líder Técnico: _________________ Data: _______
- [ ] Especialista ANAC: _____________ Data: _______