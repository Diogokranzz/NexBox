param (
    [string]$OutputPath = ".\Exportar\Pendrive"
)

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "        EXPORTANDO NEXBOX 100% STANDALONE     " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "O aplicativo e o banco de dados (API local) serao"
Write-Host "compilados e agrupados nesta pasta: $OutputPath"
Write-Host "Eles poderao rodar direto do pendrive sem instalacao!"
Write-Host ""

if (Test-Path $OutputPath) {
    Remove-Item -Recurse -Force $OutputPath
}
New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

Write-Host "[1/3] Compilando e compactando o Servidor (Banco de Dados API)..." -ForegroundColor Yellow

# Publish API as self-contained SingleFile for Windows x64
dotnet publish .\ProductManagementSystem.Api\ProductManagementSystem.Api.csproj `
    -c Release -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $OutputPath | Out-Null

Write-Host "[2/3] Compilando e compactando a Interface (NexBox Desktop)..." -ForegroundColor Yellow

# Publish DesktopClient as self-contained SingleFile for Windows x64
dotnet publish .\ProductManagementSystem.DesktopClient\ProductManagementSystem.DesktopClient.csproj `
    -c Release -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $OutputPath | Out-Null

Write-Host "[3/3] Limpando arquivos desnecessarios da pasta final..." -ForegroundColor Yellow

# The single file publish usually drops some .pdb, let's remove them to keep it 100% clean
Get-ChildItem -Path $OutputPath -Filter "*.pdb" | Remove-Item -Force

Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host " CONCLUIDO COM SUCESSO!                      " -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Para usar no pendrive:"
Write-Host "1. Copie todo o conteudo da pasta '$OutputPath'"
Write-Host "   direto para a raiz ou para uma pasta no seu pendrive."
Write-Host "2. Deixe o 'ProductManagementSystem.Api.exe' na mesma pasta."
Write-Host "3. Basta executar o 'NexBox.exe'!"
Write-Host ""
