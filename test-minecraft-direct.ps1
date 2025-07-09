# Teste direto do Minecraft com bibliotecas corretas
Write-Host "=== TESTE DIRETO DO MINECRAFT ===" -ForegroundColor Green

$javaPath = "C:\Program Files\Java\jre1.8.0_431\bin\java.exe"
$minecraftDir = "$env:APPDATA\.minecraft"
$minecraftJar = "$minecraftDir\minecraft.jar"
$joptSimpleJar = "$minecraftDir\libraries\net\sf\jopt-simple\jopt-simple\4.6\jopt-simple-4.6.jar"

# Verificar se os arquivos existem
Write-Host "Verificando arquivos necessários..." -ForegroundColor Yellow

if (Test-Path $javaPath) {
    Write-Host "✓ Java encontrado: $javaPath" -ForegroundColor Green
} else {
    Write-Host "✗ Java não encontrado: $javaPath" -ForegroundColor Red
    $javaPath = "java"
}

if (Test-Path $minecraftJar) {
    $size = (Get-Item $minecraftJar).Length
    Write-Host "✓ Minecraft JAR encontrado: $size bytes" -ForegroundColor Green
} else {
    Write-Host "✗ Minecraft JAR não encontrado: $minecraftJar" -ForegroundColor Red
}

if (Test-Path $joptSimpleJar) {
    $size = (Get-Item $joptSimpleJar).Length
    Write-Host "✓ JOptSimple encontrado: $size bytes" -ForegroundColor Green
} else {
    Write-Host "✗ JOptSimple não encontrado: $joptSimpleJar" -ForegroundColor Red
}

# Montar classpath com JOptSimple
$classpath = "`"$minecraftJar`";`"$joptSimpleJar`""

# Argumentos para Minecraft vanilla
$arguments = "-Xmx2048M -Xms512M -cp $classpath net.minecraft.client.main.Main --username TestPlayer --gameDir `"$minecraftDir`" --version 1.12.2"

Write-Host ""
Write-Host "Comando completo:" -ForegroundColor Cyan
Write-Host "`"$javaPath`" $arguments" -ForegroundColor White

Write-Host ""
Write-Host "Tentando iniciar Minecraft..." -ForegroundColor Yellow

try {
    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = $javaPath
    $processInfo.Arguments = $arguments
    $processInfo.WorkingDirectory = $minecraftDir
    $processInfo.UseShellExecute = $false
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true
    $processInfo.CreateNoWindow = $false
    
    $process = [System.Diagnostics.Process]::Start($processInfo)
    
    Write-Host "✓ Processo iniciado com PID: $($process.Id)" -ForegroundColor Green
    
    # Aguardar um pouco
    Start-Sleep -Seconds 5
    
    if ($process.HasExited) {
        $output = $process.StandardOutput.ReadToEnd()
        $error = $process.StandardError.ReadToEnd()
        
        Write-Host ""
        Write-Host "Processo saiu com código: $($process.ExitCode)" -ForegroundColor Yellow
        
        if ($error) {
            Write-Host "ERRO:" -ForegroundColor Red
            Write-Host $error -ForegroundColor Red
        }
        
        if ($output) {
            Write-Host "SAÍDA:" -ForegroundColor Cyan
            Write-Host $output -ForegroundColor Gray
        }
    } else {
        Write-Host "✓ Processo ainda está rodando - SUCESSO!" -ForegroundColor Green
        Write-Host "Minecraft deve estar iniciando..." -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "✗ Erro ao iniciar processo: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== TESTE CONCLUÍDO ===" -ForegroundColor Green
