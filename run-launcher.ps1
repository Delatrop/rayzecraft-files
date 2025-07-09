# Script para executar o RayzeCraft Launcher com logs
Write-Host "=== RAYZECRAFT LAUNCHER - INICIALIZANDO ===" -ForegroundColor Green
Write-Host "Verificando se o launcher está atualizado..." -ForegroundColor Yellow

# Compilar o projeto se necessário
if (-not (Test-Path "MinecraftLauncher\bin\Debug\net6.0-windows\MinecraftLauncher.exe")) {
    Write-Host "Compilando o projeto..." -ForegroundColor Yellow
    dotnet build MinecraftLauncher.sln
}

# Executar o launcher
Write-Host "Iniciando o launcher..." -ForegroundColor Green
Start-Process -FilePath "MinecraftLauncher\bin\Debug\net6.0-windows\MinecraftLauncher.exe" -WorkingDirectory "MinecraftLauncher\bin\Debug\net6.0-windows"

# Aguardar um pouco e mostrar os logs
Start-Sleep -Seconds 3

$logPath = "$env:APPDATA\.rayzecraftlauncher\logs\launcher-$(Get-Date -Format 'yyyy-MM-dd').log"

if (Test-Path $logPath) {
    Write-Host "`n=== LOGS RECENTES ===" -ForegroundColor Cyan
    Get-Content $logPath -Tail 20
    
    Write-Host "`n=== MONITORAMENTO DE LOGS ===" -ForegroundColor Cyan
    Write-Host "Pressione Ctrl+C para sair do monitoramento" -ForegroundColor Yellow
    
    try {
        Get-Content $logPath -Wait -Tail 10
    } catch {
        Write-Host "Monitoramento interrompido." -ForegroundColor Red
    }
} else {
    Write-Host "Arquivo de log não encontrado em: $logPath" -ForegroundColor Red
}
