# Imagens Necessárias para o RayzeCraft Launcher

Para que o launcher funcione completamente com todas as imagens, você precisa adicionar os seguintes arquivos na pasta `Resources`:

## Imagens Obrigatórias:

1. **icon.ico** - Ícone da aplicação (32x32 ou 48x48 pixels)
   - Usado como ícone da janela e do executável

2. **background.jpg** - Imagem de fundo da aplicação
   - Recomendado: 1920x1080 pixels ou maior
   - Formato: JPG ou PNG

3. **logo.png** - Logo do launcher
   - Recomendado: 200x80 pixels
   - Formato: PNG com fundo transparente

4. **splash.png** - Imagem da tela de carregamento
   - Recomendado: 300x300 pixels
   - Formato: PNG com fundo transparente

## Status Atual:
- ✅ As referências às imagens foram temporariamente comentadas no código
- ✅ O launcher funciona sem as imagens (apenas com textos)
- ⚠️ Para ativar as imagens, descomente as linhas no arquivo `MainWindow.xaml`

## Como Ativar as Imagens:
1. Adicione os arquivos de imagem na pasta `Resources`
2. No arquivo `MainWindow.xaml`, descomente as linhas que contêm as referências às imagens
3. No arquivo `MinecraftLauncher.csproj`, descomente a linha: `<ApplicationIcon>Resources\icon.ico</ApplicationIcon>`
4. Recompile o projeto

## Resolução de Problemas:
Se você receber erros relacionados a imagens:
- Verifique se os arquivos estão na pasta `Resources`
- Confirme que os nomes dos arquivos estão corretos
- Verifique se as imagens não estão corrompidas
