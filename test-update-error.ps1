Write-Host "=== TESTE DE ATUALIZAÇÃO - DIAGNÓSTICO DE ERRO ===" -ForegroundColor Green
Write-Host ""

# Verificar se o launcher está executando
$launcher = Get-Process -Name "MinecraftLauncher" -ErrorAction SilentlyContinue
if ($launcher) {
    Write-Host "✅ Launcher executando (PID: $($launcher.Id))" -ForegroundColor Green
} else {
    Write-Host "❌ Launcher não está executando" -ForegroundColor Red
}

Write-Host ""
Write-Host "📋 Logs disponíveis:" -ForegroundColor Yellow
Get-ChildItem -Path "$env:APPDATA\.rayzecraftlauncher\" -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 5 | Format-Table Name, LastWriteTime, Length

Write-Host ""
Write-Host "📋 Logs em /logs/:" -ForegroundColor Yellow
Get-ChildItem -Path "$env:APPDATA\.rayzecraftlauncher\logs\" -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 3 | Format-Table Name, LastWriteTime, Length

Write-Host ""
Write-Host "🔍 Verificando últimas 20 linhas dos logs:" -ForegroundColor Yellow
$logFile = Get-ChildItem -Path "$env:APPDATA\.rayzecraftlauncher\logs\" -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($logFile) {
    Write-Host "Log mais recente: $($logFile.Name)" -ForegroundColor Cyan
    Get-Content $logFile.FullName | Select-Object -Last 20
} else {
    Write-Host "❌ Nenhum log encontrado" -ForegroundColor Red
}

Write-Host ""
Write-Host "💡 Para testar a atualização:" -ForegroundColor Yellow
Write-Host "1. Abra o launcher"
Write-Host "2. Clique em 'Atualizar'"
Write-Host "3. Observe qual erro aparece"
Write-Host "4. Me informe o erro específico"

Write-Host ""
Write-Host "🔧 Verificando configuração atual:" -ForegroundColor Yellow
$configFile = "$env:APPDATA\.rayzecraftlauncher\config.json"
if (Test-Path $configFile) {
    Write-Host "✅ Arquivo de configuração encontrado" -ForegroundColor Green
    $config = Get-Content $configFile | ConvertFrom-Json
    Write-Host "Jogador: $($config.PlayerName)"
    Write-Host "Memória: $($config.MaxMemory)MB"
    Write-Host "Java: $($config.JavaPath)"
} else {
    Write-Host "❌ Arquivo de configuração não encontrado" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== TESTE CONCLUIDO ===" -ForegroundColor Green
