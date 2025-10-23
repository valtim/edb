#!/bin/bash

# Script para parar o ambiente Docker do Diário de Bordo

set -e

echo "🛑 Parando Diário de Bordo Digital..."

# Determinar comando do Docker Compose
if command -v docker-compose >/dev/null 2>&1; then
    DOCKER_COMPOSE="docker-compose"
else
    DOCKER_COMPOSE="docker compose"
fi

# Parar e remover containers
echo "📦 Parando containers..."
$DOCKER_COMPOSE down

echo "✅ Todos os serviços foram parados."
echo ""
echo "💡 Dicas:"
echo "   Para remover volumes também: docker-compose down -v"
echo "   Para limpar completamente: docker system prune -a"
echo "   Para reiniciar: ./docker-up.sh"
echo ""
