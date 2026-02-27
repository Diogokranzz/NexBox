# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar arquivos de projeto
COPY ["ProductManagementSystem.Api/ProductManagementSystem.Api.csproj", "ProductManagementSystem.Api/"]
COPY ["ProductManagementSystem.Application/ProductManagementSystem.Application.csproj", "ProductManagementSystem.Application/"]
COPY ["ProductManagementSystem.Domain/ProductManagementSystem.Domain.csproj", "ProductManagementSystem.Domain/"]
COPY ["ProductManagementSystem.Infrastructure/ProductManagementSystem.Infrastructure.csproj", "ProductManagementSystem.Infrastructure/"]

# Restaurar dependências
RUN dotnet restore "ProductManagementSystem.Api/ProductManagementSystem.Api.csproj"

# Copiar código fonte
COPY . .
WORKDIR "/src/ProductManagementSystem.Api"

# Build
RUN dotnet build "ProductManagementSystem.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "ProductManagementSystem.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Criar diretório de logs e db
RUN mkdir -p /app/logs
RUN mkdir -p /app/data

# Copiar arquivos publicados
COPY --from=publish /app/publish .

# Expor porta
EXPOSE 8080
EXPOSE 8443

# Healthcheck
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entrypoint
ENTRYPOINT ["dotnet", "ProductManagementSystem.Api.dll"]
