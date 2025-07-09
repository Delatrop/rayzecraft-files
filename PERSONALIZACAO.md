# 🎨 Como Personalizar o RayzeCraft Launcher

## 📁 Estrutura de Pastas

```
MinecraftLauncher/
├── Resources/                  ← COLOQUE SUAS IMAGENS AQUI
│   ├── icon.ico               ← Ícone do launcher
│   ├── logo.png               ← Logo principal
│   ├── background.jpg         ← Imagem de fundo
│   └── splash.png             ← Imagem do splash screen
├── Styles/
│   └── AppStyles.xaml         ← CORES E ESTILOS
└── Services/
    └── ConfigService.cs       ← TEXTOS PADRÃO
```

## 🖼️ Imagens (Pasta Resources/)

### 1. **icon.ico** (Ícone do launcher)
- **Tamanho**: 32x32 ou 64x64 pixels
- **Formato**: .ico
- **Uso**: Ícone na barra de tarefas e janela

### 2. **logo.png** (Logo principal)
- **Tamanho**: 200x80 pixels (recomendado)
- **Formato**: .png (com fundo transparente)
- **Uso**: Logo no topo do launcher

### 3. **background.jpg** (Fundo principal)
- **Tamanho**: 800x600 pixels ou maior
- **Formato**: .jpg ou .png
- **Uso**: Imagem de fundo da janela principal

### 4. **splash.png** (Splash screen)
- **Tamanho**: 300x300 pixels
- **Formato**: .png (com fundo transparente)
- **Uso**: Tela de carregamento ao iniciar o jogo

## 🎨 Cores (Arquivo AppStyles.xaml)

Edite as cores na linha 6-11 do arquivo `Styles/AppStyles.xaml`:

```xml
<SolidColorBrush x:Key="PrimaryColor" Color="#4CAF50"/>      ← Cor principal (botões)
<SolidColorBrush x:Key="SecondaryColor" Color="#2196F3"/>    ← Cor secundária
<SolidColorBrush x:Key="AccentColor" Color="#FF9800"/>       ← Cor de destaque
<SolidColorBrush x:Key="BackgroundColor" Color="#1E1E1E"/>   ← Cor do painel
<SolidColorBrush x:Key="TextColor" Color="White"/>           ← Cor do texto
<SolidColorBrush x:Key="SubtleTextColor" Color="#CCCCCC"/>   ← Cor do texto secundário
```

### Exemplos de Cores:
- **Verde**: `#4CAF50`
- **Azul**: `#2196F3`
- **Vermelho**: `#F44336`
- **Roxo**: `#9C27B0`
- **Laranja**: `#FF9800`

## 📝 Textos (Arquivo ConfigService.cs)

Edite os textos padrão nas linhas 58-64 do arquivo `Services/ConfigService.cs`:

```csharp
LauncherTitle = "RayzeCraft Launcher",           ← Título principal
LauncherSubtitle = "Launcher personalizado...",  ← Subtítulo
FooterText = "RayzeCraft Launcher v1.0...",     ← Texto do rodapé
ServerUrl = "https://seuservidor.com"           ← URL do servidor
```

## 🔧 Configuração de Atualização

No arquivo `UpdateService.cs`, linha 23, altere a URL do version.json:

```csharp
private const string VERSION_URL = "https://seuservidor.com/version.json";
```

## 🎵 Sons (Opcional)

Para adicionar sons, você pode:
1. Adicionar arquivos .wav na pasta Resources/
2. Reproduzir sons nos eventos usando `System.Media.SoundPlayer`

## 🚀 Como Compilar

1. Abra o projeto no Visual Studio
2. Clique com botão direito na solução
3. Selecione "Build Solution" (F6)
4. O .exe será gerado em `bin/Debug/` ou `bin/Release/`

## 📋 Checklist de Personalização

- [ ] Substituir `icon.ico` pelo ícone do seu servidor
- [ ] Substituir `logo.png` pelo logo do seu servidor  
- [ ] Substituir `background.jpg` por uma imagem temática
- [ ] Substituir `splash.png` por uma imagem de loading
- [ ] Alterar cores no `AppStyles.xaml`
- [ ] Alterar textos no `ConfigService.cs`
- [ ] Alterar URL do version.json no `UpdateService.cs`
- [ ] Testar compilação

## 🆘 Resolução de Problemas

### Imagem não aparece:
- Verifique se o arquivo está na pasta Resources/
- Verifique se o nome do arquivo está correto
- Clique com botão direito no arquivo → Properties → Build Action: "Resource"

### Cores não mudam:
- Salve o arquivo AppStyles.xaml
- Recompile o projeto (F6)

### Textos não mudam:
- Salve o arquivo ConfigService.cs
- Recompile o projeto (F6)

---

**💡 Dica**: Sempre faça backup dos arquivos originais antes de modificar!
