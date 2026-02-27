param(
    [switch]$Docker = $false,
    [switch]$SkipTests = $false
)

Write-Host "Iniciando setup do ProductManagementSystem..." -ForegroundColor Cyan
Write-Host ""

Write-Host "Restaurando dependencias..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro ao restaurar" -ForegroundColor Red
    exit 1
}
Write-Host "Dependencias restauradas" -ForegroundColor Green
Write-Host ""

Write-Host "Compilando solucao..." -ForegroundColor Yellow
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro no build" -ForegroundColor Red
    exit 1
}
Write-Host "Build concluido" -ForegroundColor Green
Write-Host ""

if (-not $SkipTests) {
    Write-Host "Executando testes..." -ForegroundColor Yellow
    dotnet test ProductManagementSystem.UnitTests/ProductManagementSystem.UnitTests.csproj -v quiet
    Write-Host "Testes concluidos" -ForegroundColor Green
    Write-Host ""
}

Write-Host "Aplicando migrations..." -ForegroundColor Yellow
cd ProductManagementSystem.Api
dotnet ef database update
if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro ao aplicar migrations" -ForegroundColor Red
    exit 1
}
Write-Host "Migrations aplicadas" -ForegroundColor Green
cd ..
Write-Host ""

if ($Docker) {
    Write-Host "Construindo imagem Docker..." -ForegroundColor Yellow
    docker-compose build
    Write-Host "Imagem construida" -ForegroundColor Green
    Write-Host ""
    Write-Host "Para iniciar os containers:" -ForegroundColor Cyan
    Write-Host "  docker-compose up -d" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "SETUP CONCLUIDO COM SUCESSO" -ForegroundColor Green
Write-Host ""
Write-Host "Para iniciar a API:" -ForegroundColor Cyan
Write-Host "  cd ProductManagementSystem.Api && dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "Swagger disponivel em:" -ForegroundColor Cyan
Write-Host "  http://localhost:5000" -ForegroundColor Gray
Write-Host ""
