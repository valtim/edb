# 🐳 Guia Docker - Diário de Bordo Digital

Este guia explica como executar todo o sistema Diário de Bordo Digital usando Docker containers.

## 📋 Pré-requisitos

- [Docker](https://www.docker.com/get-started) 20.10+
- [Docker Compose](https://docs.docker.com/compose/install/) 2.0+
- 8GB RAM disponível
- 20GB espaço em disco

## 🚀 Início Rápido

### 1. Clonar e Preparar
```bash
git clone <repositório>
cd diariodebordo
chmod +x docker-up.sh docker-down.sh docker-test.sh
```

### 2. Testar Ambiente (Recomendado)
```bash
./docker-test.sh
```

### 3. Configurar Variáveis
```bash
cp .env.example .env
# Editar .env conforme necessário
```

### 4. Iniciar Tudo
```bash
./docker-up.sh
```

Ou manualmente:
```bash
docker-compose up -d
```

### 3. Acessar Aplicação
- **Frontend**: http://localhost
- **API**: http://localhost/api
- **Swagger**: http://localhost/api/swagger
- **Portainer**: http://localhost:9000

## 🏗️ Arquitetura dos Containers

### 🔄 Fluxo de Dados
```
Internet → Nginx → Frontend (Angular) ↘
                ↓                      ↘
              API (.NET) → Redis (Cache)
                ↓
              MySQL (Database)
```

### 📦 Containers

| Container | Porta | Descrição |
|-----------|-------|------------|
| `nginx` | 80, 443 | Proxy reverso e load balancer |
| `frontend` | 4200 | Angular 18 aplicação |
| `api` | 8191 | .NET 9 API backend |
| `mysql` | 3306 | Base de dados principal |
| `redis` | 6379 | Cache e sessões |
| `portainer` | 9000 | Monitoramento Docker |

## ⚙️ Configuração

### Variáveis de Ambiente
Edite o arquivo `.env` para personalizar:

```bash
# Portas
FRONTEND_PORT=4200
API_PORT=8191
NGINX_HTTP_PORT=80

# Banco de dados
MYSQL_ROOT_PASSWORD=DiarioBordo2024!
MYSQL_DATABASE=diario_bordo

# JWT
JWT_SECRET_KEY=suachavesecreta
```

### Recursos de Hardware
```yaml
# docker-compose.override.yml
services:
  mysql:
    deploy:
      resources:
        limits:
          memory: 2G
        reservations:
          memory: 1G
  
  api:
    deploy:
      resources:
        limits:
          memory: 1G
```

## 🗄️ Persistência de Dados

### Volumes Docker
- `mysql-data`: Dados do MySQL
- `redis-data`: Dados do Redis
- `nginx-cache`: Cache do Nginx

### Backup Local
```bash
# Backup MySQL
docker exec diario-bordo-mysql mysqldump -u root -p diario_bordo > backup.sql

# Restore MySQL
docker exec -i diario-bordo-mysql mysql -u root -p diario_bordo < backup.sql
```

### Comandos Úteis

### Validação e Teste
```bash
# Testar ambiente antes de iniciar
./docker-test.sh

# Verificar configuração
docker-compose config --quiet
```

### Monitoramento
```bash
# Ver logs em tempo real
docker-compose logs -f

# Logs de um serviço específico
docker-compose logs -f api

# Status dos containers
docker-compose ps

# Recursos utilizados
docker stats
```

### Manutenção
```bash
# Rebuild um serviço
docker-compose build api
docker-compose up -d api

# Restart um serviço
docker-compose restart api

# Shell no container
docker exec -it diario-bordo-api bash

# Executar migrations
docker exec diario-bordo-api dotnet ef database update
```

### Limpeza
```bash
# Parar tudo
./docker-down.sh

# Remover volumes também
docker-compose down -v

# Limpeza completa
docker system prune -a --volumes
```

## 🛡️ Segurança

### Produção
Para ambiente de produção:

1. **Alterar senhas padrão**
2. **Configurar HTTPS**
3. **Limitar acesso às portas**
4. **Configurar firewall**

```bash
# Gerar certificados SSL
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout docker/nginx/ssl/key.pem \
  -out docker/nginx/ssl/cert.pem
```

### Rede
```bash
# Criar rede isolada
docker network create diario-bordo-network

# Verificar conectividade
docker exec diario-bordo-api ping mysql
```

## 📊 Monitoramento

### Health Checks
Todos os containers têm health checks:

```bash
# Verificar saúde
docker inspect diario-bordo-api --format='{{.State.Health.Status}}'

# Ver histórico de health checks
docker inspect diario-bordo-api --format='{{json .State.Health}}' | jq
```

### Portainer
Acesse http://localhost:9000 para:
- Monitorar containers
- Ver logs
- Gerenciar volumes
- Estatísticas de uso

## 🐛 Troubleshooting

### Problemas Comuns

#### Volumes duplicados
Se encontrar erro "duplicate volume names", isso foi corrigido na versão atual. Execute:
```bash
docker-compose down -v
docker system prune -f
./docker-test.sh  # Validar ambiente
./docker-up.sh    # Reiniciar
```

#### Container não inicia
```bash
# Ver logs detalhados
docker-compose logs <nome-do-serviço>

# Verificar recursos
docker system df
free -h
```

#### Banco de dados não conecta
```bash
# Testar conexão
docker exec diario-bordo-mysql mysql -u root -p -e "SHOW DATABASES;"

# Verificar network
docker network ls
docker network inspect diariodebordo_diario-bordo-network
```

#### Frontend não carrega
```bash
# Verificar se API está respondendo
curl http://localhost/api/health

# Ver logs do nginx
docker-compose logs nginx
```

#### Performance lenta
```bash
# Ver uso de recursos
docker stats

# Ajustar limites de memória
# Editar docker-compose.yml
```

### Reset Completo
```bash
#!/bin/bash
# reset-docker.sh
docker-compose down -v
docker system prune -a --volumes
docker network prune
./docker-up.sh
```

## 🔄 Atualizações

### Atualizar Aplicação
```bash
# Pull nova versão
git pull origin main

# Rebuild e restart
docker-compose build --no-cache
docker-compose up -d
```

### Backup antes de atualizar
```bash
#!/bin/bash
# backup-before-update.sh
echo "Fazendo backup..."
docker exec diario-bordo-mysql mysqldump -u root -p diario_bordo > "backup-$(date +%Y%m%d-%H%M%S).sql"
echo "Backup concluído!"
```

## 📈 Escalabilidade

### Load Balancing
Para múltiplas instâncias da API:

```yaml
# docker-compose.override.yml
services:
  api:
    deploy:
      replicas: 3
  
  nginx:
    volumes:
      - ./docker/nginx/nginx-lb.conf:/etc/nginx/nginx.conf
```

### Horizontal Scaling
```bash
# Escalar API
docker-compose up -d --scale api=3

# Verificar instâncias
docker-compose ps api
```

## 📞 Suporte

Para problemas:
1. Verificar logs: `docker-compose logs`
2. Verificar recursos: `docker system df && free -h`
3. Testar conectividade: `curl http://localhost/api/health`
4. Reset se necessário: `./reset-docker.sh`

---

**🎉 Sistema totalmente containerizado e pronto para uso!**
