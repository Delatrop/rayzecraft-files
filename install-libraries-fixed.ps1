# Script para instalar as bibliotecas essenciais do Minecraft
Write-Host "=== INSTALANDO BIBLIOTECAS ESSENCIAIS DO MINECRAFT ===" -ForegroundColor Green

$minecraftDir = "$env:APPDATA\.minecraft"
$librariesDir = "$minecraftDir\libraries"

# Criar diretórios necessários
Write-Host "Criando diretórios..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $librariesDir -Force | Out-Null

# Baixar JOptSimple (biblioteca essencial para Minecraft)
Write-Host "Baixando JOptSimple..." -ForegroundColor Yellow
$joptSimpleDir = "$librariesDir\net\sf\jopt-simple\jopt-simple\4.6"
New-Item -ItemType Directory -Path $joptSimpleDir -Force | Out-Null

try {
    Invoke-WebRequest -Uri "https://repo1.maven.org/maven2/net/sf/jopt-simple/jopt-simple/4.6/jopt-simple-4.6.jar" -OutFile "$joptSimpleDir\jopt-simple-4.6.jar"
    Write-Host "✓ JOptSimple baixado com sucesso!" -ForegroundColor Green
} catch {
    Write-Host "✗ Erro ao baixar JOptSimple: $($_.Exception.Message)" -ForegroundColor Red
}

# Baixar Gson (biblioteca JSON)
Write-Host "Baixando Gson..." -ForegroundColor Yellow
$gsonDir = "$librariesDir\com\google\code\gson\gson\2.8.0"
New-Item -ItemType Directory -Path $gsonDir -Force | Out-Null

try {
    Invoke-WebRequest -Uri "https://repo1.maven.org/maven2/com/google/code/gson/gson/2.8.0/gson-2.8.0.jar" -OutFile "$gsonDir\gson-2.8.0.jar"
    Write-Host "✓ Gson baixado com sucesso!" -ForegroundColor Green
} catch {
    Write-Host "✗ Erro ao baixar Gson: $($_.Exception.Message)" -ForegroundColor Red
}

# Baixar Guava (biblioteca utilitária)
Write-Host "Baixando Guava..." -ForegroundColor Yellow
$guavaDir = "$librariesDir\com\google\guava\guava\21.0"
New-Item -ItemType Directory -Path $guavaDir -Force | Out-Null

try {
    Invoke-WebRequest -Uri "https://repo1.maven.org/maven2/com/google/guava/guava/21.0/guava-21.0.jar" -OutFile "$guavaDir\guava-21.0.jar"
    Write-Host "✓ Guava baixado com sucesso!" -ForegroundColor Green
} catch {
    Write-Host "✗ Erro ao baixar Guava: $($_.Exception.Message)" -ForegroundColor Red
}

# Baixar Commons IO
Write-Host "Baixando Commons IO..." -ForegroundColor Yellow
$commonsIODir = "$librariesDir\commons-io\commons-io\2.5"
New-Item -ItemType Directory -Path $commonsIODir -Force | Out-Null

try {
    Invoke-WebRequest -Uri "https://repo1.maven.org/maven2/commons-io/commons-io/2.5/commons-io-2.5.jar" -OutFile "$commonsIODir\commons-io-2.5.jar"
    Write-Host "✓ Commons IO baixado com sucesso!" -ForegroundColor Green
} catch {
    Write-Host "✗ Erro ao baixar Commons IO: $($_.Exception.Message)" -ForegroundColor Red
}

# Baixar Apache Commons Lang
Write-Host "Baixando Apache Commons Lang..." -ForegroundColor Yellow
$commonsLangDir = "$librariesDir\org\apache\commons\commons-lang3\3.5"
New-Item -ItemType Directory -Path $commonsLangDir -Force | Out-Null

try {
    Invoke-WebRequest -Uri "https://repo1.maven.org/maven2/org/apache/commons/commons-lang3/3.5/commons-lang3-3.5.jar" -OutFile "$commonsLangDir\commons-lang3-3.5.jar"
    Write-Host "✓ Apache Commons Lang baixado com sucesso!" -ForegroundColor Green
} catch {
    Write-Host "✗ Erro ao baixar Apache Commons Lang: $($_.Exception.Message)" -ForegroundColor Red
}

# Baixar Minecraft 1.12.2 client
Write-Host "Baixando Minecraft 1.12.2 client..." -ForegroundColor Yellow
$minecraftJar = "$minecraftDir\minecraft.jar"

try {
    Invoke-WebRequest -Uri "https://piston-data.mojang.com/v1/objects/0f275bc1547d01fa5f56ba34bdc87d981ee12daf/client.jar" -OutFile $minecraftJar
    Write-Host "✓ Minecraft 1.12.2 client baixado com sucesso!" -ForegroundColor Green
} catch {
    Write-Host "✗ Erro ao baixar Minecraft client: $($_.Exception.Message)" -ForegroundColor Red
}

# Verificar se as bibliotecas foram baixadas
Write-Host ""
Write-Host "=== VERIFICANDO BIBLIOTECAS INSTALADAS ===" -ForegroundColor Cyan
$libraries = @(
    "$joptSimpleDir\jopt-simple-4.6.jar",
    "$gsonDir\gson-2.8.0.jar", 
    "$guavaDir\guava-21.0.jar",
    "$commonsIODir\commons-io-2.5.jar",
    "$commonsLangDir\commons-lang3-3.5.jar",
    $minecraftJar
)

foreach ($lib in $libraries) {
    if (Test-Path $lib) {
        $size = (Get-Item $lib).Length
        Write-Host "✓ $(Split-Path $lib -Leaf) - $($size) bytes" -ForegroundColor Green
    } else {
        Write-Host "✗ $(Split-Path $lib -Leaf) - NÃO ENCONTRADO" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== INSTALAÇÃO CONCLUÍDA ===" -ForegroundColor Green
Write-Host "Agora você pode executar o launcher novamente." -ForegroundColor Yellow
Write-Host "As bibliotecas foram instaladas em: $librariesDir" -ForegroundColor Gray
