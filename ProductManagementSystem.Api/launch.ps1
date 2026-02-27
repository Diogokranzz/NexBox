Write-Host ""
Write-Host "ProductManagementSystem - API Launcher" -ForegroundColor Cyan
Write-Host ""

Write-Host "Escolha uma opcao:" -ForegroundColor Yellow
Write-Host "1. Executar API (Debug)" -ForegroundColor Gray
Write-Host "2. Executar API (Release)" -ForegroundColor Gray
Write-Host "3. Aplicar Migrations" -ForegroundColor Gray
Write-Host "4. Rodar Testes" -ForegroundColor Gray
Write-Host "5. Docker Compose Up" -ForegroundColor Gray
Write-Host ""

$choice = Read-Host "Opcao (1-5)"

switch ($choice) {
    "1" {
        Write-Host "Iniciando em Debug..." -ForegroundColor Cyan
        dotnet run
    }
    "2" {
        Write-Host "Iniciando em Release..." -ForegroundColor Cyan
        dotnet run -c Release
    }
    "3" {
        Write-Host "Aplicando migrations..." -ForegroundColor Cyan
        dotnet ef database update
    }
    "4" {
        Write-Host "Rodando testes..." -ForegroundColor Cyan
        cd ../ProductManagementSystem.UnitTests
        dotnet test
        cd ../ProductManagementSystem.Api
    }
    "5" {
        Write-Host "Iniciando Docker Compose..." -ForegroundColor Cyan
        cd ..
        docker-compose up -d
        Write-Host ""
        Write-Host "Containers iniciados" -ForegroundColor Green
        Write-Host "API: http://localhost:8080" -ForegroundColor Gray
        Write-Host "Swagger: http://localhost:8080/swagger" -ForegroundColor Gray
    }
    default {
        Write-Host "Opcao invalida" -ForegroundColor Red
    }
}

Write-Host ""
