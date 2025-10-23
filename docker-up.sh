#!/bin/bash

# Script para inicializar o ambiente Docker do Diário de Bordo

set -e

echo "🚀 Iniciando Diário de Bordo Digital..."

# Verificar se Docker está rodando
if ! docker info >/dev/null 2>&1; then
    echo "❌ Docker não está rodando. Por favor, inicie o Docker e tente novamente."
    exit 1
fi

# Verificar se Docker Compose está disponível
if ! command -v docker-compose >/dev/null 2>&1 && ! docker compose version >/dev/null 2>&1; then
    echo "❌ Docker Compose não encontrado. Por favor, instale o Docker Compose."
    exit 1
fi

# Determinar comando do Docker Compose
if command -v docker-compose >/dev/null 2>&1; then
    DOCKER_COMPOSE="docker-compose"
else
    DOCKER_COMPOSE="docker compose"
fi

echo "📁 Criando diretórios necessários..."
mkdir -p docker/volumes/mysql
mkdir -p docker/volumes/redis
mkdir -p docker/volumes/nginx
mkdir -p docker/nginx/ssl

# Parar containers existentes
echo "🛑 Parando containers existentes..."
$DOCKER_COMPOSE down --remove-orphans

# Remover volumes órfãos (opcional)
echo "🧹 Limpando volumes órfãos..."
docker volume prune -f

# Build e start dos serviços
echo "🔨 Fazendo build dos containers..."
$DOCKER_COMPOSE build --no-cache

echo "🚀 Iniciando serviços..."
$DOCKER_COMPOSE up -d

# Aguardar serviços ficarem saudáveis
echo "⏳ Aguardando serviços ficarem prontos..."
sleep 30

# Verificar status dos serviços
echo "📊 Status dos serviços:"
$DOCKER_COMPOSE ps

# Verificar health checks
echo "🏥 Verificando saúde dos serviços..."
for service in mysql redis api frontend nginx; do
    echo -n "Verificando $service... "
    if docker inspect diario-bordo-$service --format='{{.State.Health.Status}}' 2>/dev/null | grep -q "healthy\|starting"; then
        echo "✅ OK"
    else
        echo "❌ Problema detectado"
        echo "Logs do $service:"
        docker logs diario-bordo-$service --tail 10
    fi
done

echo ""
echo "🎉 Diário de Bordo Digital iniciado com sucesso!"
echo ""
echo "📍 URLs disponíveis:"
echo "   Frontend: http://localhost"
echo "   API: http://localhost/api"
echo "   Documentação API: http://localhost/api/swagger"
echo "   Portainer: http://localhost:9000"
echo ""
echo "📊 Para monitorar logs:"
echo "   docker-compose logs -f"
echo ""
echo "🛑 Para parar tudo:"
echo "   docker-compose down"
echo ""
echo "📖 Para ver este guia novamente:"
echo "   cat docker-guide.md"
echo ""
