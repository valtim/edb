#!/bin/bash

# 🧪 Script para executar todos os testes do Sistema Diário de Bordo Digital
# Executa testes de conformidade ANAC, unitários, integração e performance

set -e  # Parar em caso de erro

echo "🚀 Iniciando execução completa de testes..."
echo "📅 $(date)"
echo "==============================================="

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Função para imprimir com cores
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

# Verificar se .NET está instalado
if ! command -v dotnet &> /dev/null; then
    print_status "ERROR" ".NET SDK não encontrado. Instale o .NET 9.0 SDK"
    exit 1
fi

# Verificar versão do .NET
DOTNET_VERSION=$(dotnet --version)
print_status "INFO" "Usando .NET SDK versão: $DOTNET_VERSION"

# Criar diretório para resultados
RESULTS_DIR="./test-results"
mkdir -p $RESULTS_DIR/{domain,infrastructure,api,coverage}

print_status "INFO" "Restaurando dependências..."
dotnet restore

print_status "INFO" "Compilando solução..."
dotnet build --configuration Release --no-restore

if [ $? -ne 0 ]; then
    print_status "ERROR" "Falha na compilação"
    exit 1
fi

print_status "SUCCESS" "Compilação concluída com sucesso"

echo ""
echo "🏛️ TESTES DE CONFORMIDADE ANAC"
echo "==============================="

print_status "INFO" "Executando testes de conformidade Resolução 457/2017 (17 campos obrigatórios)..."
dotnet test tests/DiarioBordo.Domain.Tests \
    --filter "FullyQualifiedName~AnacCompliance" \
    --configuration Release \
    --no-build \
    --logger "trx;LogFileName=anac-compliance.trx" \
    --results-directory $RESULTS_DIR/domain \
    --verbosity normal

if [ $? -eq 0 ]; then
    print_status "SUCCESS" "Conformidade Resolução 457/2017 ✅"
else
    print_status "ERROR" "Falha nos testes de conformidade 457/2017"
    exit 1
fi

print_status "INFO" "Executando testes de assinatura digital Resolução 458/2017..."
dotnet test tests/DiarioBordo.Domain.Tests \
    --filter "FullyQualifiedName~AssinaturaDigital" \
    --configuration Release \
    --no-build \
    --logger "trx;LogFileName=digital-signature.trx" \
    --results-directory $RESULTS_DIR/domain \
    --verbosity normal

if [ $? -eq 0 ]; then
    print_status "SUCCESS" "Conformidade Resolução 458/2017 ✅"
else
    print_status "ERROR" "Falha nos testes de assinatura digital 458/2017"
    exit 1
fi

echo ""
echo "🧪 TESTES UNITÁRIOS"
echo "==================="

print_status "INFO" "Executando testes unitários do Domain..."
dotnet test tests/DiarioBordo.Domain.Tests \
    --configuration Release \
    --no-build \
    --logger "trx;LogFileName=domain-tests.trx" \
    --collect:"XPlat Code Coverage" \
    --results-directory $RESULTS_DIR/domain

DOMAIN_EXIT_CODE=$?

print_status "INFO" "Executando testes de repositório (Infrastructure)..."
dotnet test tests/DiarioBordo.Infrastructure.Tests \
    --configuration Release \
    --no-build \
    --logger "trx;LogFileName=infrastructure-tests.trx" \
    --collect:"XPlat Code Coverage" \
    --results-directory $RESULTS_DIR/infrastructure

INFRA_EXIT_CODE=$?

echo ""
echo "🌐 TESTES DE INTEGRAÇÃO"
echo "======================="

print_status "INFO" "Executando testes de integração da API..."
dotnet test tests/DiarioBordo.API.Tests \
    --configuration Release \
    --no-build \
    --logger "trx;LogFileName=api-tests.trx" \
    --collect:"XPlat Code Coverage" \
    --results-directory $RESULTS_DIR/api

API_EXIT_CODE=$?

echo ""
echo "⚡ TESTES DE PERFORMANCE"
echo "======================"

print_status "INFO" "Verificando performance da consulta dos últimos 30 dias..."
print_status "INFO" "Requisito: < 500ms conforme Art. 8º II Resolução 457/2017"

dotnet test tests/DiarioBordo.Infrastructure.Tests \
    --filter "FullyQualifiedName~UltimosTrindaDias" \
    --configuration Release \
    --no-build \
    --logger "trx;LogFileName=performance-tests.trx" \
    --results-directory $RESULTS_DIR/infrastructure \
    --verbosity detailed

PERF_EXIT_CODE=$?

echo ""
echo "🔒 VERIFICAÇÕES DE SEGURANÇA"
echo "============================"

print_status "INFO" "Verificando vulnerabilidades em pacotes NuGet..."
dotnet list package --vulnerable --include-transitive > $RESULTS_DIR/vulnerability-report.txt 2>&1

if grep -q "Critical\|High" $RESULTS_DIR/vulnerability-report.txt; then
    print_status "WARNING" "Vulnerabilidades encontradas - veja $RESULTS_DIR/vulnerability-report.txt"
else
    print_status "SUCCESS" "Nenhuma vulnerabilidade crítica encontrada"
fi

print_status "INFO" "Verificando configurações de segurança..."
if find src/ -name "*.cs" -o -name "*.json" | xargs grep -l "password\|secret\|key" | grep -v "Password" | grep -v "Key" > /dev/null 2>&1; then
    print_status "WARNING" "Possíveis secrets hardcoded encontrados"
else
    print_status "SUCCESS" "Nenhum secret hardcoded encontrado"
fi

echo ""
echo "📊 RELATÓRIO DE COBERTURA"
echo "========================="

# Verificar se ReportGenerator está instalado
if ! command -v reportgenerator &> /dev/null; then
    print_status "INFO" "Instalando ReportGenerator..."
    dotnet tool install -g dotnet-reportgenerator-globaltool
fi

print_status "INFO" "Gerando relatório de cobertura..."
reportgenerator \
    -reports:"$RESULTS_DIR/**/coverage.cobertura.xml" \
    -targetdir:"$RESULTS_DIR/coverage" \
    -reporttypes:"HtmlInline_AzurePipelines;TextSummary" \
    -historydir:"$RESULTS_DIR/coverage/history"

if [ -f "$RESULTS_DIR/coverage/Summary.txt" ]; then
    print_status "INFO" "Resumo da cobertura:"
    cat "$RESULTS_DIR/coverage/Summary.txt"
    print_status "INFO" "Relatório completo disponível em: $RESULTS_DIR/coverage/index.html"
else
    print_status "WARNING" "Não foi possível gerar relatório de cobertura"
fi

echo ""
echo "📋 RESUMO DOS RESULTADOS"
echo "======================="

# Verificar resultados
TOTAL_FAILURES=0

if [ $DOMAIN_EXIT_CODE -eq 0 ]; then
    print_status "SUCCESS" "Testes Domain: PASSOU"
else
    print_status "ERROR" "Testes Domain: FALHOU"
    TOTAL_FAILURES=$((TOTAL_FAILURES + 1))
fi

if [ $INFRA_EXIT_CODE -eq 0 ]; then
    print_status "SUCCESS" "Testes Infrastructure: PASSOU"
else
    print_status "ERROR" "Testes Infrastructure: FALHOU"
    TOTAL_FAILURES=$((TOTAL_FAILURES + 1))
fi

if [ $API_EXIT_CODE -eq 0 ]; then
    print_status "SUCCESS" "Testes API: PASSOU"
else
    print_status "ERROR" "Testes API: FALHOU"
    TOTAL_FAILURES=$((TOTAL_FAILURES + 1))
fi

if [ $PERF_EXIT_CODE -eq 0 ]; then
    print_status "SUCCESS" "Testes Performance: PASSOU"
else
    print_status "ERROR" "Testes Performance: FALHOU"
    TOTAL_FAILURES=$((TOTAL_FAILURES + 1))
fi

echo ""
echo "==============================================="

if [ $TOTAL_FAILURES -eq 0 ]; then
    print_status "SUCCESS" "🎉 TODOS OS TESTES PASSARAM!"
    print_status "SUCCESS" "✅ Sistema em conformidade com ANAC 457/2017 e 458/2017"
    print_status "SUCCESS" "✅ Qualidade e segurança verificadas"
    echo ""
    echo "🚀 Sistema Diário de Bordo Digital pronto para deployment!"
    exit 0
else
    print_status "ERROR" "❌ $TOTAL_FAILURES conjunto(s) de testes falharam"
    print_status "ERROR" "🔍 Verifique os logs em $RESULTS_DIR/"
    echo ""
    echo "⚠️  Corrija os problemas antes do deployment"
    exit 1
fi