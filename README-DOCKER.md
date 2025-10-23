# 🐳 Diário de Bordo Digital - Docker Setup

## 🚀 Início Rápido

### 1. Executar Tudo com Docker
```bash
# Tornar scripts executáveis (primeira vez)
chmod +x docker-up.sh docker-down.sh

# Iniciar todos os serviços
./docker-up.sh
```

### 2. Acessar Aplicação
- **Frontend**: http://localhost
- **API**: http://localhost/api
- **Swagger**: http://localhost/api/swagger
- **Portainer**: http://localhost:9000

## 📦 Containers Inclusos

| Serviço | Container | Porta | Descrição |
|---------|-----------|-------|----------|
| Frontend | `diario-bordo-frontend` | 4200 | Angular 18 PWA |
| API | `diario-bordo-api` | 5000 | .NET 9 Web API |
| Database | `diario-bordo-mysql` | 3306 | MySQL 8.0 |
| Cache | `diario-bordo-redis` | 6379 | Redis 7 |
| Proxy | `diario-bordo-nginx` | 80/443 | Nginx |
| Monitor | `diario-bordo-portainer` | 9000 | Portainer |

## 🛠️ Comandos Úteis

### Gerenciamento
```bash
# Iniciar serviços
./docker-up.sh

# Parar serviços
./docker-down.sh

# Ver logs em tempo real
docker-compose logs -f

# Ver logs de um serviço específico
docker-compose logs -f api

# Status dos containers
docker-compose ps

# Rebuild um serviço
docker-compose build api --no-cache
docker-compose up -d api
```

### Desenvolvimento
```bash
# Modo desenvolvimento (com hot reload)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up

# Shell no container
docker exec -it diario-bordo-api bash

# Executar migrations
docker exec diario-bordo-api dotnet ef database update

# Backup do banco
docker exec diario-bordo-mysql mysqldump -u root -p diario_bordo > backup.sql
```

## 🔧 Configuração

### Variáveis de Ambiente
Edite `.env` para personalizar:

```bash
# Portas principais
NGINX_HTTP_PORT=80
API_PORT=5000
FRONTEND_PORT=4200

# Banco de dados
MYSQL_ROOT_PASSWORD=SuaSenhaSegura
MYSQL_DATABASE=diario_bordo

# JWT
JWT_SECRET_KEY=SuaChaveSecretaMuitoLonga
```

### SSL/HTTPS (Produção)
```bash
# Gerar certificados auto-assinados
mkdir -p docker/nginx/ssl
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout docker/nginx/ssl/key.pem \
  -out docker/nginx/ssl/cert.pem
```

## 📊 Monitoramento

### Health Checks
Todos os containers têm health checks automáticos:

```bash
# Verificar saúde de um container
docker inspect diario-bordo-api --format='{{.State.Health.Status}}'

# Ver histórico de health checks
docker inspect diario-bordo-api --format='{{json .State.Health}}' | jq
```

### Portainer Dashboard
Acesse http://localhost:9000 para:
- Monitorar recursos
- Ver logs em tempo real
- Gerenciar containers
- Estatísticas de uso

## 🐛 Troubleshooting

### Problemas Comuns

#### Container não inicia
```bash
# Ver logs detalhados
docker-compose logs <nome-do-serviço>

# Verificar recursos do sistema
docker system df
free -h
```

#### Banco não conecta
```bash
# Testar conexão direta
docker exec diario-bordo-mysql mysql -u root -p -e "SHOW DATABASES;"

# Verificar rede
docker network inspect diariodebordo_diario-bordo-network
```

#### Frontend não carrega
```bash
# Testar API
curl http://localhost/api/health

# Ver logs do nginx
docker-compose logs nginx
```

### Reset Completo
```bash
# Parar tudo e remover volumes
docker-compose down -v

# Limpar sistema Docker
docker system prune -a --volumes

# Reiniciar
./docker-up.sh
```

## 🔄 Atualizações

### Atualizar Aplicação
```bash
# Pull código novo
git pull origin main

# Rebuild containers
docker-compose build --no-cache
docker-compose up -d
```

### Backup antes de Atualizar
```bash
# Script automático
#!/bin/bash
echo "Fazendo backup..."
docker exec diario-bordo-mysql mysqldump -u root -p diario_bordo > "backup-$(date +%Y%m%d-%H%M%S).sql"
echo "Backup concluído!"
```

## 🚀 Deploy Produção

### Preparação
1. Alterar senhas no `.env`
2. Configurar certificados SSL
3. Ajustar recursos dos containers
4. Configurar backup automático

### Docker Swarm (Opcional)
```bash
# Inicializar swarm
docker swarm init

# Deploy stack
docker stack deploy -c docker-compose.yml diario-bordo

# Escalar serviços
docker service scale diario-bordo_api=3
```

## 📈 Performance

### Otimizações
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
      replicas: 2
      resources:
        limits:
          memory: 1G
```

### Monitoramento Avançado
```bash
# Instalar Prometheus + Grafana (opcional)
docker run -d --name prometheus prom/prometheus
docker run -d --name grafana grafana/grafana
```

## 📋 Checklist de Deploy

- [ ] Containers buildando sem erro
- [ ] Health checks passando
- [ ] Banco de dados conectando
- [ ] Frontend carregando
- [ ] API respondendo
- [ ] SSL configurado (produção)
- [ ] Backup automático configurado
- [ ] Monitoring funcionando
- [ ] Logs sendo coletados
- [ ] Performance testada

---

## 📞 Suporte

Para problemas:
1. Verificar logs: `docker-compose logs`
2. Verificar recursos: `docker system df && free -h`
3. Testar conectividade: `curl http://localhost/api/health`
4. Reset se necessário: `./docker-down.sh && ./docker-up.sh`

**🎉 Sistema 100% containerizado e pronto para uso!**
