# Teste para verificar se os arquivos de mod estão sendo encontrados
Write-Host "=== TESTE DE MODS ===" -ForegroundColor Green

$minecraftDir = "$env:APPDATA\.minecraft"
$modsDir = "$minecraftDir\mods"

Write-Host "Diretório Minecraft: $minecraftDir"
Write-Host "Diretório Mods: $modsDir"

if (Test-Path $modsDir) {
    Write-Host "✓ Pasta mods existe" -ForegroundColor Green
    
    $modFiles = Get-ChildItem -Path $modsDir -Filter "*.jar" | Measure-Object
    Write-Host "Arquivos .jar encontrados: $($modFiles.Count)" -ForegroundColor Yellow
    
    $actuallyAdditions = "$modsDir\ActuallyAdditions-1.12.2-r151-2.jar"
    if (Test-Path $actuallyAdditions) {
        Write-Host "✓ ActuallyAdditions encontrado" -ForegroundColor Green
        $fileInfo = Get-Item $actuallyAdditions
        Write-Host "  Tamanho: $($fileInfo.Length) bytes"
        Write-Host "  Caminho: $actuallyAdditions"
    } else {
        Write-Host "✗ ActuallyAdditions NÃO encontrado" -ForegroundColor Red
    }
    
    # Listar alguns mods para verificar
    Write-Host "`nPrimeiros 10 mods encontrados:" -ForegroundColor Yellow
    Get-ChildItem -Path $modsDir -Filter "*.jar" | Select-Object -First 10 | ForEach-Object {
        Write-Host "  - $($_.Name)"
    }
} else {
    Write-Host "✗ Pasta mods NÃO existe" -ForegroundColor Red
}

Write-Host "`n=== TESTE CONCLUÍDO ===" -ForegroundColor Green
