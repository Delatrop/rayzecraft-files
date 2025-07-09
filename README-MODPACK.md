# RayzeCraft Launcher - Configuração para Modpack

## Funcionalidades Implementadas

Seu launcher agora funciona de forma similar ao TLauncher, com as seguintes características:

### 🎮 Funcionalidades Principais

1. **Exclusão e Substituição Automática**
   - Deleta automaticamente as pastas `mods`, `config` e `scripts` da `.minecraft`
   - Substitui pelos seus arquivos personalizados hospedados

2. **Configuração do Forge**
   - Configura automaticamente o Forge 1.12.2-14.23.5.2854
   - Cria perfil personalizado no `launcher_profiles.json`
   - Usa a pasta `versions` corretamente

3. **Gestão de Arquivos**
   - Busca arquivos em `C:\rayzecraft-launcher-files\`
   - Copia mods, configs e scripts automaticamente
   - Cria estrutura de pastas necessária

### 📁 Estrutura de Arquivos Necessária

Para o launcher funcionar corretamente, você precisa organizar os arquivos do seu modpack em:

```
C:\rayzecraft-launcher-files\
├── mods\                 <- Seus arquivos .jar dos mods
├── config\               <- Arquivos de configuração
├── scripts\              <- Scripts do CraftTweaker
└── bin\
    └── minecraft.jar     <- JAR do Minecraft com Forge
```

### 🔧 Como Usar

1. **Preparar Arquivos**
   - Coloque todos os mods (.jar) na pasta `C:\rayzecraft-launcher-files\mods\`
   - Coloque configurações na pasta `C:\rayzecraft-launcher-files\config\`
   - Coloque scripts na pasta `C:\rayzecraft-launcher-files\scripts\`

2. **Executar o Launcher**
   - Execute o launcher
   - Clique em "Atualizar" se necessário
   - Clique em "Jogar"

3. **Primeiro Uso**
   - O launcher configurará automaticamente a pasta `.minecraft`
   - Criará o perfil "RayzeCraft Modpack"
   - Copiará todos os arquivos necessários

### ⚙️ Configurações Técnicas

- **Versão do Minecraft**: 1.12.2
- **Versão do Forge**: 14.23.5.2854
- **Classe Principal**: `net.minecraft.launchwrapper.Launch`
- **Diretório de Trabalho**: `%APPDATA%\.minecraft`

### 🔍 Resolução de Problemas

**Erro "Could not find a part of the path"**
- Verifique se a pasta `C:\rayzecraft-launcher-files\mods\` existe
- Certifique-se de que os arquivos .jar estão na pasta
- Execute o launcher como administrador se necessário

**Erro "Java 8 não encontrado"**
- Instale o Java 8 (JRE ou JDK)
- O launcher procura automaticamente em locais padrão

**Erro de inicialização do Minecraft**
- Verifique se o arquivo `minecraft.jar` está em `C:\rayzecraft-launcher-files\bin\`
- Execute uma atualização no launcher
- Verifique se há espaço suficiente no disco

### 📋 Status do Modpack

O launcher mostra o status atual do modpack na interface:
- ✓ MODS: Número de arquivos .jar encontrados
- ✓ CONFIG: Arquivos de configuração
- ✓ SCRIPTS: Scripts do CraftTweaker
- ✓ FORGE: Versão instalada

### 🚀 Próximos Passos

1. **Hospedagem de Arquivos**: Configure um servidor web para hospedar os arquivos
2. **Atualização Automática**: Os arquivos serão baixados automaticamente
3. **Profiles Customizados**: Configure diferentes perfis para diferentes modpacks

### 💡 Dicas

- Mantenha os arquivos organizados nas pastas corretas
- Use nomes de arquivo sem espaços ou caracteres especiais
- Teste o modpack localmente antes de distribuir
- Faça backup das configurações importantes

---

**Desenvolvido para RayzeCraft Server**  
Versão: 1.0.0  
Data: Julho 2025
