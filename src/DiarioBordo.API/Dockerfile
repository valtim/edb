# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copiar arquivos de projeto
COPY ["src/DiarioBordo.API/DiarioBordo.API.csproj", "DiarioBordo.API/"]
COPY ["src/DiarioBordo.Application/DiarioBordo.Application.csproj", "DiarioBordo.Application/"]
COPY ["src/DiarioBordo.Domain/DiarioBordo.Domain.csproj", "DiarioBordo.Domain/"]
COPY ["src/DiarioBordo.Infrastructure/DiarioBordo.Infrastructure.csproj", "DiarioBordo.Infrastructure/"]

# Restaurar dependências
RUN dotnet restore "DiarioBordo.API/DiarioBordo.API.csproj"

# Copiar código fonte
COPY src/ .

# Build da aplicação
WORKDIR "/src/DiarioBordo.API"
RUN dotnet build "DiarioBordo.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DiarioBordo.API.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

# Instalar dependências do sistema
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Criar usuário não-root
RUN addgroup --system --gid 1001 dotnet \
    && adduser --system --uid 1001 --gid 1001 --shell /bin/false dotnet

# Copiar aplicação
COPY --from=publish /app/publish .

# Configurar permissões
RUN chown -R dotnet:dotnet /app
USER dotnet

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

# Expor porta
EXPOSE 8080

# Comando de inicialização
ENTRYPOINT ["dotnet", "DiarioBordo.API.dll"]
