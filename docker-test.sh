#!/bin/bash

# Script para testar o ambiente Docker do Diário de Bordo

set -e

echo "🔍 Testando ambiente Docker do Diário de Bordo..."

# Verificar se Docker está rodando
echo -n "✅ Docker daemon: "
if docker info >/dev/null 2>&1; then
    echo "OK"
else
    echo "❌ ERRO - Docker não está rodando"
    exit 1
fi

# Verificar Docker Compose
echo -n "✅ Docker Compose: "
if command -v docker-compose >/dev/null 2>&1; then
    echo "OK ($(docker-compose version --short))"
    DOCKER_COMPOSE="docker-compose"
elif docker compose version >/dev/null 2>&1; then
    echo "OK ($(docker compose version --short))"
    DOCKER_COMPOSE="docker compose"
else
    echo "❌ ERRO - Docker Compose não encontrado"
    exit 1
fi

# Verificar sintaxe do docker-compose.yml
echo -n "✅ Sintaxe docker-compose.yml: "
if $DOCKER_COMPOSE config --quiet 2>/dev/null; then
    echo "OK"
else
    echo "❌ ERRO - Sintaxe inválida"
    $DOCKER_COMPOSE config
    exit 1
fi

# Verificar se Dockerfiles existem
echo -n "✅ Dockerfile API: "
if [ -f "src/DiarioBordo.API/Dockerfile" ]; then
    echo "OK"
else
    echo "❌ ERRO - Dockerfile da API não encontrado"
    exit 1
fi

echo -n "✅ Dockerfile Frontend: "
if [ -f "frontend/Dockerfile" ]; then
    echo "OK"
else
    echo "❌ ERRO - Dockerfile do Frontend não encontrado"
    exit 1
fi

# Verificar configurações nginx
echo -n "✅ Configuração Nginx: "
if [ -f "docker/nginx/nginx.conf" ]; then
    echo "OK"
else
    echo "❌ ERRO - Configuração nginx não encontrada"
    exit 1
fi

# Verificar recursos do sistema
echo "📊 Recursos do sistema:"
echo "   💾 RAM disponível: $(free -h 2>/dev/null | awk '/^Mem:/ {print $7}' || echo 'N/A (macOS)')"
echo "   💽 Espaço em disco: $(df -h . | awk 'NR==2 {print $4}')"

# Verificar portas
echo "🔌 Verificando portas:"
for port in 80 443 3306 5000 6379 9000; do
    echo -n "   Porta $port: "
    if lsof -i :$port >/dev/null 2>&1; then
        echo "❌ EM USO"
    else
        echo "✅ LIVRE"
    fi
done

echo ""
echo "🎉 Ambiente Docker validado com sucesso!"
echo ""
echo "📝 Próximos passos:"
echo "   1. Copiar .env.example para .env: cp .env.example .env"
echo "   2. Ajustar configurações em .env se necessário"
echo "   3. Executar: ./docker-up.sh"
echo ""