#!/bin/bash

# 🛠️ Script de configuração do ambiente de desenvolvimento
# Sistema Diário de Bordo Digital - ANAC Compliance

set -e

echo "🚀 Configurando ambiente de desenvolvimento..."
echo "==============================================="

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_status() {
    local status=$1
    local message=$2
    
    case $status in
        "INFO")
            echo -e "${BLUE}ℹ️  $message${NC}"
            ;;
        "SUCCESS")
            echo -e "${GREEN}✅ $message${NC}"
            ;;
        "WARNING")
            echo -e "${YELLOW}⚠️  $message${NC}"
            ;;
        "ERROR")
            echo -e "${RED}❌ $message${NC}"
            ;;
    esac
}

# Verificar sistema operacional
if [[ "$OSTYPE" == "darwin"* ]]; then
    PLATFORM="macOS"
    PACKAGE_MANAGER="brew"
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    PLATFORM="Linux"
    if command -v apt-get &> /dev/null; then
        PACKAGE_MANAGER="apt"
    elif command -v yum &> /dev/null; then
        PACKAGE_MANAGER="yum"
    else
        print_status "ERROR" "Gerenciador de pacotes não suportado"
        exit 1
    fi
else
    print_status "ERROR" "Sistema operacional não suportado"
    exit 1
fi

print_status "INFO" "Detectado: $PLATFORM com $PACKAGE_MANAGER"

# 1. Verificar/Instalar .NET 9.0 SDK
echo ""
echo "🔧 Verificando .NET SDK..."
echo "=========================="

if ! command -v dotnet &> /dev/null; then
    print_status "WARNING" ".NET SDK não encontrado. Instalando..."
    
    if [[ "$PLATFORM" == "macOS" ]]; then
        if ! command -v brew &> /dev/null; then
            print_status "ERROR" "Homebrew necessário no macOS. Instale: /bin/bash -c \"\$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\""
            exit 1
        fi
        brew install --cask dotnet
    elif [[ "$PLATFORM" == "Linux" ]]; then
        if [[ "$PACKAGE_MANAGER" == "apt" ]]; then
            wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            sudo dpkg -i packages-microsoft-prod.deb
            sudo apt-get update
            sudo apt-get install -y dotnet-sdk-9.0
        fi
    fi
else
    DOTNET_VERSION=$(dotnet --version)
    print_status "SUCCESS" ".NET SDK já instalado: $DOTNET_VERSION"
    
    # Verificar se é .NET 9.0+
    if [[ "${DOTNET_VERSION:0:1}" -lt "9" ]]; then
        print_status "WARNING" "Versão .NET inferior a 9.0. Recomendado atualizar."
    fi
fi

# 2. Verificar/Instalar Docker
echo ""
echo "🐳 Verificando Docker..."
echo "======================="

if ! command -v docker &> /dev/null; then
    print_status "WARNING" "Docker não encontrado"
    if [[ "$PLATFORM" == "macOS" ]]; then
        print_status "INFO" "Instale Docker Desktop: https://docs.docker.com/desktop/install/mac-install/"
    elif [[ "$PLATFORM" == "Linux" ]]; then
        print_status "INFO" "Instale Docker: https://docs.docker.com/engine/install/"
    fi
else
    print_status "SUCCESS" "Docker encontrado: $(docker --version)"
    
    # Verificar se Docker está rodando
    if docker info &> /dev/null; then
        print_status "SUCCESS" "Docker daemon está executando"
    else
        print_status "WARNING" "Docker daemon não está executando"
        print_status "INFO" "Inicie o Docker Desktop ou execute: sudo systemctl start docker"
    fi
fi

# 3. Verificar/Instalar MySQL e Redis
echo ""
echo "🗄️ Configurando Banco de Dados..."
echo "================================"

print_status "INFO" "Criando containers MySQL e Redis para desenvolvimento..."

# Parar containers existentes se estiverem rodando
docker stop diario-mysql diario-redis 2>/dev/null || true
docker rm diario-mysql diario-redis 2>/dev/null || true

# MySQL 8.0 para desenvolvimento
print_status "INFO" "Iniciando MySQL 8.0..."
docker run -d \
    --name diario-mysql \
    -p 3306:3306 \
    -e MYSQL_ROOT_PASSWORD=DiarioBordo123! \
    -e MYSQL_DATABASE=diario_bordo \
    -e MYSQL_USER=diario_user \
    -e MYSQL_PASSWORD=DiarioPass123! \
    -e MYSQL_ROOT_HOST=% \
    --restart unless-stopped \
    mysql:8.0 \
    --character-set-server=utf8mb4 \
    --collation-server=utf8mb4_unicode_ci \
    --default-time-zone='+00:00'

# Redis para cache
print_status "INFO" "Iniciando Redis..."
docker run -d \
    --name diario-redis \
    -p 6379:6379 \
    --restart unless-stopped \
    redis:7-alpine

# Aguardar containers iniciarem
print_status "INFO" "Aguardando containers iniciarem..."
sleep 10

# Verificar se containers estão rodando
if docker ps | grep -q "diario-mysql"; then
    print_status "SUCCESS" "MySQL iniciado com sucesso"
else
    print_status "ERROR" "Falha ao iniciar MySQL"
fi

if docker ps | grep -q "diario-redis"; then
    print_status "SUCCESS" "Redis iniciado com sucesso"
else
    print_status "ERROR" "Falha ao iniciar Redis"
fi

# 4. Configurar appsettings.Development.json
echo ""
echo "⚙️ Configurando appsettings..."
echo "============================="

APPSETTINGS_DEV="src/DiarioBordo.API/appsettings.Development.json"

if [ ! -f "$APPSETTINGS_DEV" ]; then
    print_status "INFO" "Criando appsettings.Development.json..."
    
    cat > "$APPSETTINGS_DEV" << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "NHibernate": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=diario_bordo;Uid=diario_user;Pwd=DiarioPass123!;CharSet=utf8mb4;SslMode=None;AllowPublicKeyRetrieval=true;",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "SecretKey": "ChaveSecretaDesenvolvimento123!@#$%^&*()_+=-[]{}|;:'\",.<>?/",
    "Issuer": "DiarioBordo.Dev",
    "Audience": "DiarioBordo.API.Dev",
    "ExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  },
  "AnacSettings": {
    "SyncEnabled": false,
    "SyncIntervalHours": 24,
    "ApiEndpoint": "https://api-dev.anac.gov.br/diario-bordo",
    "CertificadoPath": "",
    "ValidarAssinaturasAnac": false
  },
  "CacheSettings": {
    "DefaultExpiration": "24:00:00",
    "SlidingExpiration": "06:00:00",
    "AbsoluteExpiration": "7.00:00:00"
  },
  "BackgroundJobs": {
    "EnabledInDevelopment": true,
    "SyncJobCron": "0 0 2 * * ?",
    "CacheMaintenanceCron": "0 0 3 * * ?",
    "DeadlineMonitoringCron": "0 0 */6 * * ?"
  },
  "Security": {
    "RequireHttps": false,
    "EnableCors": true,
    "AllowedOrigins": ["http://localhost:4200", "https://localhost:4200"],
    "MaxRequestBodySize": 10485760,
    "RateLimitRequests": 1000,
    "RateLimitWindow": "00:01:00"
  },
  "AllowedHosts": "*"
}
EOF

    print_status "SUCCESS" "appsettings.Development.json criado"
else
    print_status "INFO" "appsettings.Development.json já existe"
fi

# 5. Instalar ferramentas globais .NET
echo ""
echo "🛠️ Instalando ferramentas .NET..."
echo "================================"

print_status "INFO" "Instalando Entity Framework Core Tools..."
dotnet tool install --global dotnet-ef 2>/dev/null || dotnet tool update --global dotnet-ef

print_status "INFO" "Instalando ReportGenerator para cobertura..."
dotnet tool install --global dotnet-reportgenerator-globaltool 2>/dev/null || dotnet tool update --global dotnet-reportgenerator-globaltool

print_status "INFO" "Instalando ferramentas de desenvolvimento..."
dotnet tool install --global dotnet-outdated-tool 2>/dev/null || dotnet tool update --global dotnet-outdated-tool

# 6. Restaurar dependências e compilar
echo ""
echo "📦 Preparando projeto..."
echo "======================="

print_status "INFO" "Restaurando dependências NuGet..."
dotnet restore

print_status "INFO" "Compilando solução..."
dotnet build --configuration Development

if [ $? -eq 0 ]; then
    print_status "SUCCESS" "Compilação concluída com sucesso"
else
    print_status "ERROR" "Falha na compilação"
    exit 1
fi

# 7. Executar migrations (se existirem)
echo ""
echo "🗃️ Configurando banco de dados..."
echo "==============================="

print_status "INFO" "Aguardando MySQL estar pronto..."
sleep 15

# Tentar conectar ao MySQL
for i in {1..10}; do
    if docker exec diario-mysql mysql -u diario_user -pDiarioPass123! -e "SELECT 1;" &>/dev/null; then
        print_status "SUCCESS" "Conexão com MySQL estabelecida"
        break
    else
        print_status "INFO" "Tentativa $i/10 - aguardando MySQL..."
        sleep 3
    fi
done

# 8. Executar testes para validar configuração
echo ""
echo "🧪 Validando configuração..."
echo "==========================="

print_status "INFO" "Executando testes básicos..."
if [ -f "./run-tests.sh" ]; then
    ./run-tests.sh
else
    print_status "WARNING" "Script de testes não encontrado. Execute: dotnet test"
fi

# 9. Criar scripts úteis
echo ""
echo "📜 Criando scripts utilitários..."
echo "==============================="

# Script para iniciar a aplicação
cat > "start-dev.sh" << 'EOF'
#!/bin/bash
echo "🚀 Iniciando Sistema Diário de Bordo Digital..."
echo "============================================="

# Verificar se containers estão rodando
if ! docker ps | grep -q "diario-mysql"; then
    echo "📦 Iniciando MySQL..."
    docker start diario-mysql
fi

if ! docker ps | grep -q "diario-redis"; then
    echo "📦 Iniciando Redis..."
    docker start diario-redis
fi

echo "⏳ Aguardando serviços..."
sleep 5

echo "🌐 Iniciando API..."
cd src/DiarioBordo.API
dotnet run --launch-profile Development
EOF

chmod +x start-dev.sh

# Script para parar serviços
cat > "stop-dev.sh" << 'EOF'
#!/bin/bash
echo "🛑 Parando serviços de desenvolvimento..."
docker stop diario-mysql diario-redis 2>/dev/null || true
echo "✅ Serviços parados"
EOF

chmod +x stop-dev.sh

# Script para reset do ambiente
cat > "reset-dev.sh" << 'EOF'
#!/bin/bash
echo "🔄 Resetando ambiente de desenvolvimento..."
docker stop diario-mysql diario-redis 2>/dev/null || true
docker rm diario-mysql diario-redis 2>/dev/null || true
docker volume prune -f
echo "✅ Ambiente resetado. Execute ./setup-dev.sh para reconfigurar"
EOF

chmod +x reset-dev.sh

print_status "SUCCESS" "Scripts criados: start-dev.sh, stop-dev.sh, reset-dev.sh"

# 10. Resumo final
echo ""
echo "🎉 CONFIGURAÇÃO CONCLUÍDA!"
echo "========================="
echo ""
print_status "SUCCESS" "Ambiente de desenvolvimento configurado com sucesso!"
echo ""
echo "📋 RESUMO DOS SERVIÇOS:"
echo "• MySQL 8.0: localhost:3306 (diario_bordo)"
echo "• Redis: localhost:6379"
echo "• .NET 9.0 SDK instalado"
echo "• Ferramentas de desenvolvimento instaladas"
echo ""
echo "🚀 PRÓXIMOS PASSOS:"
echo "1. Para iniciar o desenvolvimento: ./start-dev.sh"
echo "2. Para executar testes: ./run-tests.sh"
echo "3. Para parar serviços: ./stop-dev.sh"
echo "4. Para resetar ambiente: ./reset-dev.sh"
echo ""
echo "🌐 URLS DE DESENVOLVIMENTO:"
echo "• API: https://localhost:5001 ou http://localhost:5000"
echo "• Swagger: https://localhost:5001/swagger"
echo "• Health Check: https://localhost:5001/health"
echo ""
echo "📚 DOCUMENTAÇÃO:"
echo "• Instruções completas: .github/copilot-instructions.md"
echo "• Arquitetura: ARQUITETURA.md"
echo "• Resoluções ANAC: resolucao 457.txt, resolucao 458.txt"
echo ""
print_status "INFO" "Sistema Diário de Bordo Digital pronto para desenvolvimento! 🛩️"