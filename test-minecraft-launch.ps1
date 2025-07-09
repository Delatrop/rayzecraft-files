# Script para testar o lançamento do Minecraft
Write-Host "=== TESTE DE LANÇAMENTO DO MINECRAFT ===" -ForegroundColor Green

# Verificar se o Java existe
$javaPath = "C:\Users\Delatro\AppData\Roaming\.minecraft\runtime\jre-legacy\windows\jre-legacy\bin\javaw.exe"
$altJavaPath = "C:\Program Files\Java\jre1.8.0_431\bin\java.exe"

if (Test-Path $javaPath) {
    Write-Host "✓ Java encontrado: $javaPath" -ForegroundColor Green
} elseif (Test-Path $altJavaPath) {
    $javaPath = $altJavaPath
    Write-Host "✓ Java alternativo encontrado: $javaPath" -ForegroundColor Green
} else {
    Write-Host "✗ Java não encontrado!" -ForegroundColor Red
    exit 1
}

# Verificar cliente Minecraft
$minecraftClient = "C:\Users\Delatro\AppData\Roaming\.minecraft\versions\ForgeOptiFine 1.12.2\ForgeOptiFine 1.12.2.jar"
if (Test-Path $minecraftClient) {
    Write-Host "✓ Cliente Minecraft encontrado" -ForegroundColor Green
} else {
    Write-Host "✗ Cliente Minecraft não encontrado: $minecraftClient" -ForegroundColor Red
    exit 1
}

# Diretórios
$gameDir = "C:\Users\Delatro\AppData\Roaming\.minecraft"
$assetsDir = "$gameDir\assets"
$nativesDir = "$gameDir\versions\1.12.2-forge-14.23.5.2854\natives"

# Criar diretório natives se não existir
if (!(Test-Path $nativesDir)) {
    Write-Host "Criando diretório natives: $nativesDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $nativesDir -Force | Out-Null
}

# Bibliotecas essenciais
$libraries = @(
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\net\minecraft\launchwrapper\1.12\launchwrapper-1.12.jar",
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\org\lwjgl\lwjgl\lwjgl\2.9.4-nightly-20150209\lwjgl-2.9.4-nightly-20150209.jar",
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\org\lwjgl\lwjgl\lwjgl_util\2.9.4-nightly-20150209\lwjgl_util-2.9.4-nightly-20150209.jar",
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\com\google\guava\guava\21.0\guava-21.0.jar",
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\com\google\code\gson\gson\2.8.0\gson-2.8.0.jar",
    $minecraftClient
)

# Verificar bibliotecas
Write-Host "`nVerificando bibliotecas..." -ForegroundColor Yellow
$existingLibs = @()
foreach ($lib in $libraries) {
    if (Test-Path $lib) {
        Write-Host "✓ $(Split-Path $lib -Leaf)" -ForegroundColor Green
        $existingLibs += $lib
    } else {
        Write-Host "✗ $(Split-Path $lib -Leaf)" -ForegroundColor Red
    }
}

if ($existingLibs.Count -eq 0) {
    Write-Host "✗ Nenhuma biblioteca encontrada!" -ForegroundColor Red
    exit 1
}

# Construir classpath
$classpath = $existingLibs -join ";"

# Argumentos do Java
$javaArgs = @(
    "-Xmx4096M",
    "-Xms1024M",
    "-Djava.library.path=`"$nativesDir`"",
    "-cp `"$classpath`"",
    "-Dfml.ignoreInvalidMinecraftCertificates=true",
    "-Dfml.ignorePatchDiscrepancies=true"
)

# Argumentos do Minecraft
$mcArgs = @(
    "net.minecraft.launchwrapper.Launch",
    "--username TestPlayer",
    "--version 1.12.2-forge",
    "--gameDir `"$gameDir`"",
    "--assetsDir `"$assetsDir`"",
    "--assetIndex 1.12",
    "--uuid 12345678-1234-1234-1234-123456789012",
    "--accessToken fake",
    "--userType legacy",
    "--tweakClass net.minecraftforge.fml.common.launcher.FMLTweaker"
)

# Comando completo
$allArgs = $javaArgs + $mcArgs
$argumentString = $allArgs -join " "

Write-Host "`n=== COMANDO COMPLETO ===" -ForegroundColor Cyan
Write-Host "`"$javaPath`" $argumentString" -ForegroundColor White

Write-Host "`n=== INICIANDO MINECRAFT ===" -ForegroundColor Green
Write-Host "Diretório de trabalho: $gameDir" -ForegroundColor Gray

try {
    # Executar o comando
    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = $javaPath
    $processInfo.Arguments = $argumentString
    $processInfo.WorkingDirectory = $gameDir
    $processInfo.UseShellExecute = $false
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true
    $processInfo.CreateNoWindow = $false
    
    $process = [System.Diagnostics.Process]::Start($processInfo)
    
    Write-Host "✓ Processo iniciado com PID: $($process.Id)" -ForegroundColor Green
    
    # Aguardar um pouco para ver se o processo continua rodando
    Start-Sleep -Seconds 10
    
    if ($process.HasExited) {
        $output = $process.StandardOutput.ReadToEnd()
        $error = $process.StandardError.ReadToEnd()
        
        Write-Host "`n=== SAÍDA DO PROCESSO ===" -ForegroundColor Red
        Write-Host "Código de saída: $($process.ExitCode)" -ForegroundColor Red
        
        if ($error) {
            Write-Host "ERRO:" -ForegroundColor Red
            Write-Host $error -ForegroundColor Red
        }
        
        if ($output) {
            Write-Host "SAÍDA:" -ForegroundColor Yellow
            Write-Host $output -ForegroundColor Gray
        }
    } else {
        Write-Host "✓ Processo ainda está rodando - MINECRAFT INICIADO COM SUCESSO!" -ForegroundColor Green
        Write-Host "Você pode fechar este script, o Minecraft continuará rodando." -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "✗ Erro ao iniciar o processo: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== TESTE CONCLUIDO ===" -ForegroundColor Green
