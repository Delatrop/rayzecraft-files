# Script de teste para verificar o funcionamento do launcher

Write-Host "🔍 Verificando arquivos do RayzeCraft Launcher..." -ForegroundColor Yellow
Write-Host ""

# Diretório do launcher
$launcherDir = "$env:APPDATA\.rayzecraftlauncher"
$gameDir = "$launcherDir\minecraft"

Write-Host "📁 Verificando diretórios:" -ForegroundColor Cyan
Write-Host "   Launcher: $launcherDir"
Write-Host "   Jogo: $gameDir"
Write-Host ""

# Verificar se os diretórios existem
if (Test-Path $launcherDir) {
    Write-Host "✅ Diretório do launcher encontrado" -ForegroundColor Green
} else {
    Write-Host "❌ Diretório do launcher não encontrado" -ForegroundColor Red
}

if (Test-Path $gameDir) {
    Write-Host "✅ Diretório do jogo encontrado" -ForegroundColor Green
} else {
    Write-Host "❌ Diretório do jogo não encontrado" -ForegroundColor Red
}

Write-Host ""
Write-Host "📄 Verificando arquivos:" -ForegroundColor Cyan

# Verificar arquivos específicos
$files = @(
    @{ Path = "$launcherDir\config.json"; Name = "Configuração" },
    @{ Path = "$launcherDir\version.json"; Name = "Versão" },
    @{ Path = "$gameDir\exemplo.txt"; Name = "Exemplo" },
    @{ Path = "$gameDir\minecraft.jar"; Name = "Minecraft JAR" }
)

foreach ($file in $files) {
    if (Test-Path $file.Path) {
        $size = (Get-Item $file.Path).Length
        Write-Host "   ✅ $($file.Name): $size bytes" -ForegroundColor Green
    } else {
        Write-Host "   ❌ $($file.Name): Não encontrado" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "🎮 Verificando Java:" -ForegroundColor Cyan

try {
    $javaVersion = java -version 2>&1 | Select-String "version"
    Write-Host "   ✅ Java encontrado: $javaVersion" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Java não encontrado" -ForegroundColor Red
}

Write-Host ""
Write-Host "📋 Conteúdo do diretório do launcher:" -ForegroundColor Cyan
if (Test-Path $launcherDir) {
    Get-ChildItem $launcherDir -Recurse | Format-Table Name, Length, LastWriteTime -AutoSize
}

Write-Host ""
Write-Host "💡 Instruções:" -ForegroundColor Magenta
Write-Host "   1. Execute o launcher com: dotnet run"
Write-Host "   2. Clique em 'Atualizar' para criar os arquivos"
Write-Host "   3. Clique em 'Jogar' para testar"
Write-Host ""
Write-Host "🎉 Teste concluído!" -ForegroundColor Green
