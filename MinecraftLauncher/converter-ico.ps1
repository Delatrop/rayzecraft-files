# Script para converter PNG para ICO
# Use: .\converter-ico.ps1

Add-Type -AssemblyName System.Drawing

function Convert-PngToIco {
    param(
        [string]$PngPath = "Resources\rayze2.png",
        [string]$IcoPath = "Resources\icon.ico"
    )
    
    try {
        # Carregar a imagem PNG
        $bitmap = [System.Drawing.Image]::FromFile((Resolve-Path $PngPath))
        
        # Redimensionar para 48x48 (tamanho padrão de ícone)
        $iconBitmap = New-Object System.Drawing.Bitmap($bitmap, 48, 48)
        
        # Converter para ícone
        $icon = [System.Drawing.Icon]::FromHandle($iconBitmap.GetHicon())
        
        # Salvar como ICO
        $fileStream = [System.IO.File]::Create($IcoPath)
        $icon.Save($fileStream)
        $fileStream.Close()
        
        # Limpar recursos
        $bitmap.Dispose()
        $iconBitmap.Dispose()
        $icon.Dispose()
        
        Write-Host "✅ Ícone convertido com sucesso: $IcoPath" -ForegroundColor Green
        
        return $true
    }
    catch {
        Write-Host "❌ Erro ao converter: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Executar conversão
Write-Host "🔄 Convertendo PNG para ICO..." -ForegroundColor Yellow
$success = Convert-PngToIco

if ($success) {
    Write-Host "🎉 Conversão concluída! Agora você pode descomentar a linha do ApplicationIcon no .csproj" -ForegroundColor Green
} else {
    Write-Host "💡 Você pode usar ferramentas online como https://convertio.co/png-ico/ para converter manualmente" -ForegroundColor Yellow
}
