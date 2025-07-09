# Script para testar o comando exato do TLauncher
Write-Host "=== TESTE DO COMANDO EXATO DO TLAUNCHER ===" -ForegroundColor Green

# Verificar se o Java do TLauncher existe
$tlJavaPath = "C:\Users\Delatro\AppData\Roaming\.minecraft\runtime\jre-legacy\windows\jre-legacy\bin\javaw.exe"
$altJavaPath = "C:\Program Files\Java\jre1.8.0_431\bin\java.exe"

if (Test-Path $tlJavaPath) {
    $javaPath = $tlJavaPath
    Write-Host "✓ Usando Java do TLauncher: $javaPath" -ForegroundColor Green
} elseif (Test-Path $altJavaPath) {
    $javaPath = $altJavaPath
    Write-Host "✓ Usando Java do sistema: $javaPath" -ForegroundColor Green
} else {
    Write-Host "✗ Java não encontrado!" -ForegroundColor Red
    exit 1
}

# Verificar se o cliente Minecraft existe
$minecraftClient = "C:\Users\Delatro\AppData\Roaming\.minecraft\versions\ForgeOptiFine 1.12.2\ForgeOptiFine 1.12.2.jar"
if (Test-Path $minecraftClient) {
    Write-Host "✓ Cliente Minecraft encontrado: $((Get-Item $minecraftClient).Length) bytes" -ForegroundColor Green
} else {
    Write-Host "✗ Cliente Minecraft não encontrado: $minecraftClient" -ForegroundColor Red
    
    # Tentar usar o cliente 1.12.2-forge-14.23.5.2854
    $altClient = "C:\Users\Delatro\AppData\Roaming\.minecraft\versions\1.12.2-forge-14.23.5.2854\1.12.2-forge-14.23.5.2854.jar"
    if (Test-Path $altClient) {
        $minecraftClient = $altClient
        Write-Host "✓ Usando cliente alternativo: $minecraftClient" -ForegroundColor Yellow
    } else {
        Write-Host "✗ Cliente alternativo também não encontrado" -ForegroundColor Red
        exit 1
    }
}

# Construir classpath essencial
$libraries = @(
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\net\minecraftforge\forge\1.12.2-14.23.5.2854\forge-1.12.2-14.23.5.2854.jar",
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\org\ow2\asm\asm-debug-all\5.2\asm-debug-all-5.2.jar",
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\net\minecraft\launchwrapper\1.12\launchwrapper-1.12.jar",
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\net\sf\jopt-simple\jopt-simple\5.0.3\jopt-simple-5.0.3.jar",
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\org\apache\logging\log4j\log4j-api\2.15.0\log4j-api-2.15.0.jar",
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\org\apache\logging\log4j\log4j-core\2.15.0\log4j-core-2.15.0.jar",
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\com\google\guava\guava\21.0\guava-21.0.jar",
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\org\apache\commons\commons-lang3\3.5\commons-lang3-3.5.jar",
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\commons-io\commons-io\2.5\commons-io-2.5.jar",
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\com\google\code\gson\gson\2.8.0\gson-2.8.0.jar",
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\com\mojang\authlib\1.5.21\authlib-1.5.21.jar",
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\org\lwjgl\lwjgl\lwjgl\2.9.4-nightly-20150209\lwjgl-2.9.4-nightly-20150209.jar",
    "C:\Users\Delatro\AppData\Roaming\.minecraft\libraries\org\lwjgl\lwjgl\lwjgl_util\2.9.4-nightly-20150209\lwjgl_util-2.9.4-nightly-20150209.jar",
    $minecraftClient
)

# Verificar bibliotecas essenciais
Write-Host "`nVerificando bibliotecas essenciais..." -ForegroundColor Yellow
$missingLibs = @()
foreach ($lib in $libraries) {
    if (Test-Path $lib) {
        Write-Host "✓ $(Split-Path $lib -Leaf)" -ForegroundColor Green
    } else {
        Write-Host "✗ $(Split-Path $lib -Leaf)" -ForegroundColor Red
        $missingLibs += $lib
    }
}

if ($missingLibs.Count -gt 0) {
    Write-Host "`n⚠️  Bibliotecas ausentes encontradas, mas continuando..." -ForegroundColor Yellow
}

# Construir classpath
$classpath = ($libraries | Where-Object { Test-Path $_ }) -join ";"

# Construir comando completo
$gameDir = "C:\Users\Delatro\AppData\Roaming\.minecraft"
$assetsDir = "$gameDir\assets"
$nativesDir = "$gameDir\versions\1.12.2-forge-14.23.5.2854\natives"

# Criar diretório natives se não existir
if (!(Test-Path $nativesDir)) {
    Write-Host "Criando diretório natives: $nativesDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $nativesDir -Force | Out-Null
}

$arguments = @(
    "-Dos.name=Windows 10",
    "-Dos.version=10.0",
    "-Djava.library.path=`"$nativesDir`"",
    "-cp `"$classpath`"",
    "-Xmx4096M",
    "-XX:+UnlockExperimentalVMOptions",
    "-XX:+UseG1GC",
    "-XX:G1NewSizePercent=20",
    "-XX:G1ReservePercent=20",
    "-XX:MaxGCPauseMillis=50",
    "-XX:G1HeapRegionSize=32M",
    "-Dfml.ignoreInvalidMinecraftCertificates=true",
    "-Dfml.ignorePatchDiscrepancies=true",
    "-Djava.net.preferIPv4Stack=true",
    "-Dminecraft.applet.TargetDirectory=`"$gameDir`"",
    "-DlibraryDirectory=`"$gameDir\libraries`"",
    "net.minecraft.launchwrapper.Launch",
    "--username TestPlayer",
    "--version 1.12.2-forge-14.23.5.2854",
    "--gameDir `"$gameDir`"",
    "--assetsDir `"$assetsDir`"",
    "--assetIndex 1.12",
    "--uuid 12345678-1234-1234-1234-123456789012",
    "--accessToken null",
    "--userType mojang",
    "--tweakClass net.minecraftforge.fml.common.launcher.FMLTweaker",
    "--versionType Forge",
    "--width 925",
    "--height 530"
)

$fullCommand = "`"$javaPath`" " + ($arguments -join " ")

Write-Host "`n=== COMANDO COMPLETO ===" -ForegroundColor Cyan
Write-Host $fullCommand -ForegroundColor White

Write-Host "`n=== INICIANDO MINECRAFT ===" -ForegroundColor Green
Write-Host "Diretório de trabalho: $gameDir" -ForegroundColor Gray

try {
    # Executar o comando
    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = $javaPath
    $processInfo.Arguments = ($arguments -join " ")
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
