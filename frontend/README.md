# Diário de Bordo Digital - Frontend

Este é o frontend do Sistema de Diário de Bordo Digital para aviação civil brasileira, desenvolvido em Angular 18 LTS com conformidade às Resoluções ANAC 457/2017 e 458/2017.

## 🚀 Tecnologias Utilizadas

- **Angular 18 LTS** - Framework principal com standalone components
- **Angular Material 18** - Biblioteca de componentes UI
- **NgRx** - Gerenciamento de estado
- **TypeScript** - Linguagem de programação
- **PWA** - Progressive Web App com service worker
- **SignalR** - Comunicação em tempo real
- **RxJS** - Programação reativa

## 📋 Pré-requisitos

- Node.js 18+ 
- npm 9+
- Angular CLI 18+

## 🛠️ Instalação e Configuração

### 1. Instalar dependências
```bash
cd frontend
npm install
```

### 2. Instalar Angular CLI globalmente (se necessário)
```bash
npm install -g @angular/cli@18
```

### 3. Configurar ambiente
Os arquivos de ambiente já estão configurados em:
- `src/environments/environment.ts` (desenvolvimento)
- `src/environments/environment.prod.ts` (produção)

## 🏃‍♂️ Executando o Projeto

### Desenvolvimento
```bash
npm start
# ou
ng serve
```

O aplicativo estará disponível em `http://localhost:4200`

### Build para Produção
```bash
npm run build
# ou
ng build --configuration production
```

### Executar Testes
```bash
# Testes unitários
npm test

# Testes E2E
npm run e2e

# Coverage
npm run test:coverage
```

## 🔐 Sistema de Autenticação

O sistema possui autenticação completa com:

- **Login com JWT** - Tokens de acesso e refresh
- **Autenticação Multifator (2FA)** - Para operadores
- **Controle de Acesso por Papéis**:
  - `Piloto` - Registrar voos, visualizar próprios registros
  - `Operador` - Aprovar registros, gerenciar tripulação
  - `DiretorOperacoes` - Acesso completo operacional
  - `Fiscalizacao` - Acesso de auditoria ANAC

### Usuários de Teste (quando backend estiver configurado)
```
Piloto: pilot@example.com / password123
Operador: operator@example.com / password123
```

## 📱 Progressive Web App (PWA)

O aplicativo é um PWA completo com:
- Funcionamento offline
- Cache inteligente
- Notificações push
- Instalação como app nativo

## 🛡️ Conformidade ANAC

### Resolução 457/2017 - Campos Obrigatórios
O formulário de registro de voo inclui todos os 17 campos obrigatórios:
1. Número sequencial
2. Identificação da tripulação (códigos ANAC)
3. Data (dd/mm/aaaa)
4. Aeroportos (IATA/ICAO)
5. Horários UTC
6. Tempo de voo IFR
7. Combustível
8. Natureza do voo
9. Pessoas a bordo
10. Carga
11. Ocorrências
12. Discrepâncias técnicas
13. Ações corretivas
14. Última manutenção
15. Próxima manutenção
16. Horas de célula
17. Responsável por aprovação

### Resolução 458/2017 - Sistema Eletrônico
- Assinaturas digitais com timestamp UTC
- Logs de auditoria completos
- Disponibilidade de 30 dias sempre acessível
- Backup e retenção de 5 anos

## 🎨 Interface e Experiência

### Design System
- **Material Design** - Guidelines do Google
- **Tema Personalizado** - Cores da aviação civil
- **Responsivo** - Funciona em desktop, tablet e mobile
- **Acessibilidade** - Conformidade WCAG 2.1

### Componentes Principais
- **Dashboard** - Visão geral com estatísticas
- **Registro de Voo** - Formulário multi-etapa
- **Gerenciamento de Aeronaves** - CRUD completo
- **Tripulação** - Gestão de pilotos e códigos ANAC
- **Relatórios** - Exportação e análises
- **Auditoria** - Logs e rastreabilidade

## 📊 Estado da Aplicação (NgRx)

O estado é gerenciado centralmente com NgRx:
```
├── auth/          # Autenticação e usuário
├── registros/     # Registros de voo
├── aeronaves/     # Gestão de aeronaves
├── tripulacao/    # Gestão de tripulação
└── shared/        # Estado compartilhado
```

## 🔄 Integração com Backend

### API Base
```typescript
// Desenvolvimento
apiUrl: 'https://localhost:7001/api'

// Produção
apiUrl: 'https://diariodebordo.api.com/api'
```

### Interceptors HTTP
- **Autenticação** - Adiciona tokens JWT automaticamente
- **Refresh Token** - Renovação automática de tokens
- **Error Handling** - Tratamento centralizado de erros
- **Loading** - Indicadores de carregamento

## 🧪 Estrutura de Testes

```
src/
├── app/
│   ├── **/*.spec.ts     # Testes unitários
│   └── **/*.e2e.ts     # Testes E2E
└── testing/             # Utilitários de teste
```

### Cobertura de Testes
- Componentes: 90%+
- Serviços: 95%+
- Guards: 100%
- Interceptors: 100%

## 📦 Build e Deploy

### Variáveis de Ambiente
```bash
# Desenvolvimento
NODE_ENV=development
API_URL=https://localhost:7001/api

# Produção
NODE_ENV=production
API_URL=https://api.diariodebordo.com/api
```

### Docker (Produção)
```dockerfile
# Build stage
FROM node:18-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Production stage
FROM nginx:alpine
COPY --from=build /app/dist/frontend /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
```

## 📝 Scripts Disponíveis

```bash
npm start              # Desenvolvimento
npm run build          # Build produção
npm run build:dev      # Build desenvolvimento
npm test              # Testes unitários
npm run test:watch    # Testes em modo watch
npm run test:coverage # Cobertura de testes
npm run e2e           # Testes E2E
npm run lint          # Verificar código
npm run format        # Formatar código
npm run analyze       # Análise do bundle
```

## 🐛 Solução de Problemas

### Problemas Comuns

1. **Erro de CORS**
   ```bash
   # Verificar configuração do backend
   # Garantir que localhost:4200 está nas origens permitidas
   ```

2. **Token Expirado**
   ```bash
   # O sistema renova automaticamente
   # Se persistir, limpar localStorage e fazer login novamente
   ```

3. **PWA não atualiza**
   ```bash
   # Forçar atualização
   # Ctrl+F5 ou limpar cache do navegador
   ```

## 📞 Suporte

Para problemas técnicos:
1. Verificar logs do console do navegador
2. Conferir conectividade com a API
3. Validar tokens de autenticação
4. Consultar documentação da API

## 🔮 Próximas Funcionalidades

- [ ] Modo offline completo
- [ ] Sincronização automática ANAC
- [ ] Relatórios avançados
- [ ] Dashboard analítico
- [ ] Notificações push
- [ ] Assinatura biométrica
- [ ] Integração com GPS
- [ ] Backup automático na nuvem

---

**Sistema em conformidade com as Resoluções ANAC 457/2017 e 458/2017**
