Write-Host "=== TESTANDO LAUNCHER CORRIGIDO ===" -ForegroundColor Green

# Verificar se o launcher foi compilado
$launcherPath = "C:\Users\Delatro\MinecraftLauncher\MinecraftLauncher\bin\Debug\net6.0-windows\MinecraftLauncher.exe"
if (Test-Path $launcherPath) {
    Write-Host "✅ Launcher encontrado: $launcherPath" -ForegroundColor Green
} else {
    Write-Host "❌ Launcher não encontrado. Execute: dotnet build" -ForegroundColor Red
    exit 1
}

# Verificar arquivos de log
$logDir = "$env:APPDATA\.rayzecraftlauncher\logs"
if (Test-Path $logDir) {
    Write-Host "✅ Diretório de logs encontrado: $logDir" -ForegroundColor Green
} else {
    Write-Host "⚠️ Diretório de logs não encontrado (será criado automaticamente)" -ForegroundColor Yellow
}

# Verificar .minecraft
$minecraftDir = "$env:APPDATA\.minecraft"
if (Test-Path $minecraftDir) {
    Write-Host "✅ Diretório .minecraft encontrado: $minecraftDir" -ForegroundColor Green
} else {
    Write-Host "⚠️ Diretório .minecraft não encontrado (será criado automaticamente)" -ForegroundColor Yellow
}

Write-Host "`n=== INICIANDO LAUNCHER CORRIGIDO ===" -ForegroundColor Cyan
Write-Host "Aguardando o launcher abrir..." -ForegroundColor Gray
Write-Host "Você pode agora:" -ForegroundColor Yellow
Write-Host "1. Clicar em 'Verificar Integridade' para ver logs detalhados" -ForegroundColor Yellow
Write-Host "2. Ir em 'Configurações' para definir o Java" -ForegroundColor Yellow
Write-Host "3. Clicar em 'Entrar no Jogo' para testar o lançamento" -ForegroundColor Yellow
Write-Host "4. Verificar os logs em: $env:APPDATA\.rayzecraftlauncher\" -ForegroundColor Yellow

# Executar o launcher
try {
    Start-Process -FilePath $launcherPath -WorkingDirectory (Split-Path $launcherPath)
    Write-Host "✅ Launcher iniciado com sucesso!" -ForegroundColor Green
} catch {
    Write-Host "❌ Erro ao iniciar launcher: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== ARQUIVOS DE LOG IMPORTANTES ===" -ForegroundColor Cyan
Write-Host "• Logs do launcher: $env:APPDATA\.rayzecraftlauncher\logs\" -ForegroundColor Gray
Write-Host "• Comando de lançamento: $env:APPDATA\.rayzecraftlauncher\launch.log" -ForegroundColor Gray
Write-Host "• Saída do Minecraft: $env:APPDATA\.rayzecraftlauncher\minecraft_output.log" -ForegroundColor Gray
