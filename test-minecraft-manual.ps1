Write-Host "=== TESTE MANUAL DO MINECRAFT ===" -ForegroundColor Green
Write-Host ""

$javaPath = "C:\Program Files\Java\jre1.8.0_431\bin\java.exe"
$mcDir = "$env:APPDATA\.minecraft"

Write-Host "1. Verificando Java..." -ForegroundColor Yellow
if (Test-Path $javaPath) {
    Write-Host "   ✅ Java encontrado: $javaPath" -ForegroundColor Green
} else {
    Write-Host "   ❌ Java não encontrado!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "2. Verificando último comando executado..." -ForegroundColor Yellow
$lastCommand = Get-Content "$env:APPDATA\.rayzecraftlauncher\launch.log" | Select-String "Argumentos:" | Select-Object -Last 1
if ($lastCommand) {
    Write-Host "   ✅ Comando encontrado" -ForegroundColor Green
$args = $lastCommand.Context.PreContext.Split("Argumentos: ")[1].Trim()
    
    Write-Host ""
    Write-Host "3. Executando Minecraft com janela visível..." -ForegroundColor Yellow
    Write-Host "   Comando: java.exe $args" -ForegroundColor Cyan
    Write-Host ""
    
    # Salvar comando em arquivo temporário
    $tempScript = "$env:TEMP\minecraft-test.bat"
    "@echo off" | Out-File -FilePath $tempScript -Encoding ASCII
    "cd /d `"$mcDir`"" | Out-File -FilePath $tempScript -Append -Encoding ASCII
    "`"$javaPath`" $args" | Out-File -FilePath $tempScript -Append -Encoding ASCII
    "pause" | Out-File -FilePath $tempScript -Append -Encoding ASCII
    
    Write-Host "   Script salvo em: $tempScript" -ForegroundColor Cyan
    Write-Host "   Executando..." -ForegroundColor Yellow
    
    # Executar com janela visível
    Start-Process -FilePath $tempScript -Wait
    
} else {
    Write-Host "   ❌ Nenhum comando encontrado nos logs!" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== TESTE CONCLUÍDO ===" -ForegroundColor Green
