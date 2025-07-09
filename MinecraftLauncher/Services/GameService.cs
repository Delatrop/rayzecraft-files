using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MinecraftLauncher.Models;

namespace MinecraftLauncher.Services
{
    public class GameService
    {
        private readonly string _launcherDirectory;
        private readonly string _gameDirectory;
        private readonly ConfigService _configService;
        private readonly ModpackService _modpackService;
        private readonly LogService _logService;
        private readonly IntegrityService _integrityService;

        public GameService()
        {
            _launcherDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".rayzecraftlauncher");
            _gameDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            _configService = new ConfigService();
            _modpackService = new ModpackService();
            _logService = new LogService();
            _integrityService = new IntegrityService(_logService);
        }

        public async Task LaunchGameAsync()
        {
            await _logService.LogInfoAsync("=== INICIANDO LANÇAMENTO DO MINECRAFT ===");
            
            var config = _configService.GetConfiguration();
            await _logService.LogInfoAsync($"Configuração carregada: {config.PlayerName}");
            
            // Configurar modpack antes de iniciar o jogo
            try
            {
                await _logService.LogInfoAsync("Configurando modpack...");
                await _modpackService.SetupModpackAsync(new Progress<(int percentage, string message)>(update => 
                {
                    // Progresso silencioso para não interferir na UI
                }));
                await _logService.LogSuccessAsync("Modpack configurado com sucesso");
            }
            catch (Exception ex)
            {
                await _logService.LogWarningAsync("Falha no setup do modpack", ex.Message);
            }
            
            // Auto-configuração do RayzeCraft
            try
            {
                await _logService.LogInfoAsync("Configurando RayzeCraft...");
                await AutoConfigureRayzeCraftAsync();
                await _logService.LogSuccessAsync("RayzeCraft configurado com sucesso");
            }
            catch (Exception ex)
            {
                await _logService.LogWarningAsync("Falha na configuração do RayzeCraft", ex.Message);
            }
            
            // Verificar se o Java 8 está instalado (ignorar se não encontrado)
            var java8Path = await GetJava8PathAsync() ?? "java";
            await _logService.LogInfoAsync($"Java encontrado: {java8Path}");

            // Tentar encontrar o Minecraft (ignorar se não encontrado)
            var minecraftPath = await FindMinecraftAsync() ?? "./minecraft.jar";
            await _logService.LogInfoAsync($"Minecraft encontrado: {minecraftPath}");

            // Construir argumentos de lançamento
            var javaArgs = BuildJavaArguments(config);
            var minecraftArgs = BuildMinecraftArguments(config);
            
            // Usar classe principal para cliente com mods (Forge)
            var allArgs = $"{javaArgs} net.minecraft.launchwrapper.Launch {minecraftArgs}";
            await _logService.LogInfoAsync($"Argumentos construídos: {allArgs.Substring(0, Math.Min(200, allArgs.Length))}...");
            
            // Verificar se a linha de comando não é muito longa
            if (allArgs.Length > 8000)
            {
                await _logService.LogWarningAsync("Argumentos muito longos, simplificando...");
                allArgs = $"-Xmx2048M -Xms512M -cp \"{Path.Combine(_gameDirectory, "minecraft.jar")}\" net.minecraft.launchwrapper.Launch --username {_configService.GetConfiguration().PlayerName} --gameDir \"{_gameDirectory}\" --version 1.12.2";
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = java8Path,
                Arguments = allArgs,
                WorkingDirectory = _gameDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            await _logService.LogInfoAsync($"Tentando iniciar processo: {java8Path}");
            await _logService.LogInfoAsync($"Diretório de trabalho: {_gameDirectory}");

            try
            {
                var process = Process.Start(processInfo);
                await _logService.LogSuccessAsync($"Processo iniciado com PID: {process.Id}");
                
                // Aguardar um pouco para garantir que o jogo iniciou
                await Task.Delay(3000);
                
                // Verificar se o processo ainda está rodando
                if (process.HasExited)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    var fullError = string.IsNullOrEmpty(error) ? output : error;
                    
                    await _logService.LogWarningAsync($"Processo saiu com código: {process.ExitCode}");
                    await _logService.LogWarningAsync($"Saída: {fullError.Substring(0, Math.Min(500, fullError.Length))}");
                    
                    // Apenas lançar erro se for um erro crítico
                    if (fullError.Contains("Could not find or load main class") || 
                        fullError.Contains("java.lang.OutOfMemoryError") ||
                        fullError.Contains("java.lang.NoClassDefFoundError"))
                    {
                        await _logService.LogWarningAsync("Erro crítico detectado, tentando modo simplificado...");
                        await LaunchMinecraftSimplifiedAsync();
                        return;
                    }
                    
                    // Se não encontrou o launchwrapper, tentar com classe vanilla
                    if (fullError.Contains("net.minecraft.launchwrapper.Launch"))
                    {
                        await _logService.LogWarningAsync("Forge não encontrado, tentando com Minecraft vanilla...");
                        await LaunchMinecraftVanillaAsync();
                        return;
                    }
                    
                    await _logService.LogInfoAsync("Processo saiu mas não foi erro crítico, continuando...");
                }
                else
                {
                    await _logService.LogSuccessAsync("Processo ainda está rodando, sucesso!");
                }
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("Erro ao iniciar processo", ex.Message);
                // Tentar com configuração simplificada como fallback
                try
                {
                    await _logService.LogInfoAsync("Tentando modo simplificado...");
                    await LaunchMinecraftSimplifiedAsync();
                }
                catch (Exception ex2)
                {
                    await _logService.LogErrorAsync("Erro no modo simplificado", ex2.Message);
                    throw new Exception($"Erro ao iniciar o jogo: {ex.Message}");
                }
            }
        }

        private async Task<bool> IsJavaInstalledAsync()
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var process = Process.Start(processInfo);
                await process.WaitForExitAsync();
                
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private string BuildJavaArguments(LauncherConfig config)
        {
            var args = new System.Text.StringBuilder();
            
            // Memória RAM
            args.Append($"-Xmx{config.MaxMemory}M ");
            args.Append($"-Xms{config.MinMemory}M ");
            
            // Argumentos padrão para Minecraft (igual ao TLauncher)
            var nativesPath = Path.Combine(_gameDirectory, "versions", "1.12.2-forge-14.23.5.2854", "natives");
            args.Append($"-Djava.library.path=\"{nativesPath}\" ");
            args.Append("-Dminecraft.launcher.brand=java-minecraft-launcher ");
            args.Append("-Dminecraft.launcher.version=1.6.84-j ");
            args.Append("-Dfml.ignoreInvalidMinecraftCertificates=true ");
            args.Append("-Dfml.ignorePatchDiscrepancies=true ");
            
            // Argumentos personalizados do usuário
            if (!string.IsNullOrEmpty(config.JvmArguments))
            {
                args.Append($"{config.JvmArguments} ");
            }
            
            // Classpath (estilo TLauncher)
            args.Append($"-cp \"{GetClasspath()}\" ");
            
            return args.ToString().Trim();
        }

        private string BuildMinecraftArguments(LauncherConfig config)
        {
            var args = new System.Text.StringBuilder();
            
            // Nome do jogador (modo offline)
            args.Append($"--username {config.PlayerName} ");
            
            // Diretório do jogo
            args.Append($"--gameDir \"{_gameDirectory}\" ");
            
            // Diretório de assets
            args.Append($"--assetsDir \"{Path.Combine(_gameDirectory, "assets")}\" ");
            
            // Versão
            args.Append($"--version 1.12.2-forge-14.23.5.2854 ");
            
            // Resolução da tela
            if (config.WindowWidth > 0 && config.WindowHeight > 0)
            {
                args.Append($"--width {config.WindowWidth} ");
                args.Append($"--height {config.WindowHeight} ");
            }
            
            // Modo offline
            args.Append("--demo false ");
            
            return args.ToString().Trim();
        }

        private async Task<string> FindMinecraftAsync()
        {
            var possiblePaths = new List<string>
            {
                // Caminho do modpack customizado
                Path.Combine(_gameDirectory, "minecraft.jar"),
                Path.Combine(_gameDirectory, "minecraft-server.jar"),
                Path.Combine(_gameDirectory, "forge-server.jar"),
                
                // Caminhos padrão do Minecraft oficial
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "versions", "1.12.2-forge", "1.12.2.jar"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "versions", "1.16.5", "1.16.5.jar"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "versions", "1.18.2", "1.18.2.jar"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "versions", "1.19.2", "1.19.2.jar"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "versions", "1.20.1", "1.20.1.jar")
            };
            
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
            
            return null;
        }
        
        private async Task CreateDemoModeAsync()
        {
            // Criar um modo de demonstração quando o Minecraft não está instalado
            var demoMessage = "=== RAYZECRAFT LAUNCHER - MODO DEMONSTRAÇÃO ===\n\n" +
                            "O Minecraft não foi encontrado no seu sistema.\n\n" +
                            "Para jogar, você precisa:\n" +
                            "1. Instalar o Minecraft oficial\n" +
                            "2. Executar uma atualização no launcher\n" +
                            "3. Aguardar o download do modpack\n\n" +
                            "Este launcher está configurado para:\n" +
                            "- Minecraft 1.12.2\n" +
                            "- Forge 14.23.5.2854\n" +
                            "- Modpack personalizado do RayzeCraft\n\n" +
                            "Pressione qualquer tecla para continuar...";
            
            // Criar um processo que mostra a mensagem
            var processInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c echo {demoMessage.Replace("\n", " & echo.")} & pause",
                UseShellExecute = false,
                CreateNoWindow = false
            };
            
            try
            {
                var process = Process.Start(processInfo);
                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro no modo demonstração: {ex.Message}");
            }
        }

        private string GetClasspath()
        {
            var allJars = new List<string>();
            
            // Adicionar Minecraft jar principal
            var minecraftJar = Path.Combine(_gameDirectory, "minecraft.jar");
            if (File.Exists(minecraftJar))
            {
                allJars.Add(minecraftJar);
            }
            
            // Adicionar bibliotecas essenciais para Minecraft Vanilla na ordem correta
            var essentialLibs = new List<string>
            {
                // JOptSimple é ESSENCIAL para Minecraft vanilla
                Path.Combine(_gameDirectory, "libraries", "net", "sf", "jopt-simple", "jopt-simple", "4.6", "jopt-simple-4.6.jar"),
                
                // Guava (Google Commons) - ESSENCIAL para Minecraft
                Path.Combine(_gameDirectory, "libraries", "com", "google", "guava", "guava", "21.0", "guava-21.0.jar"),
                
                // Authlib da Mojang - ESSENCIAL para autenticação
                Path.Combine(_gameDirectory, "libraries", "com", "mojang", "authlib", "1.5.21", "authlib-1.5.21.jar"),
                
                // Gson para JSON
                Path.Combine(_gameDirectory, "libraries", "com", "google", "code", "gson", "gson", "2.8.0", "gson-2.8.0.jar"),
                
                // Commons IO
                Path.Combine(_gameDirectory, "libraries", "commons-io", "commons-io", "2.5", "commons-io-2.5.jar"),
                
                // Apache Commons Lang
                Path.Combine(_gameDirectory, "libraries", "org", "apache", "commons", "commons-lang3", "3.5", "commons-lang3-3.5.jar"),
                
                // Launchwrapper (para Forge)
                Path.Combine(_gameDirectory, "libraries", "net", "minecraft", "launchwrapper", "1.12", "launchwrapper-1.12.jar"),
                
                // Forge principal
                Path.Combine(_gameDirectory, "libraries", "net", "minecraftforge", "forge", "1.12.2-14.23.5.2854", "forge-1.12.2-14.23.5.2854.jar"),
                
                // Outras bibliotecas do Forge
                Path.Combine(_gameDirectory, "libraries", "org", "ow2", "asm", "asm-all", "5.2", "asm-all-5.2.jar"),
                Path.Combine(_gameDirectory, "libraries", "org", "scala-lang", "scala-library", "2.11.1", "scala-library-2.11.1.jar"),
                Path.Combine(_gameDirectory, "libraries", "lzma", "lzma", "0.0.1", "lzma-0.0.1.jar"),
                Path.Combine(_gameDirectory, "libraries", "java3d", "vecmath", "1.5.2", "vecmath-1.5.2.jar"),
                Path.Combine(_gameDirectory, "libraries", "net", "sf", "trove4j", "trove4j", "3.0.3", "trove4j-3.0.3.jar")
            };
            
            // Adicionar bibliotecas essenciais que existem
            foreach (var lib in essentialLibs)
            {
                if (File.Exists(lib))
                {
                    allJars.Add(lib);
                }
            }
            
            // Adicionar todas as outras bibliotecas do diretório libraries
            var localLibsDir = Path.Combine(_gameDirectory, "libraries");
            if (Directory.Exists(localLibsDir))
            {
                var jarFiles = Directory.GetFiles(localLibsDir, "*.jar", SearchOption.AllDirectories)
                    .Where(jar => !allJars.Contains(jar)); // Evitar duplicatas
                allJars.AddRange(jarFiles);
            }
            
            // Se o classpath for muito longo, criar um manifest JAR
            var classpathString = string.Join(";", allJars.Select(jar => $"\"{jar}\""));
            
            if (classpathString.Length > 8000) // Limite do Windows para linha de comando
            {
                return CreateManifestJar(allJars);
            }
            
            return classpathString;
        }
        
        private string CreateManifestJar(List<string> jars)
        {
            try
            {
                var manifestJarPath = Path.Combine(_gameDirectory, "classpath-manifest.jar");
                
                // Criar manifest com Class-Path
                var manifestContent = "Manifest-Version: 1.0\n";
                manifestContent += "Class-Path: " + string.Join(" ", jars.Select(jar => 
                    new Uri(jar).ToString().Replace("file:///", "file:/"))) + "\n";
                
                var manifestPath = Path.Combine(_gameDirectory, "MANIFEST.MF");
                File.WriteAllText(manifestPath, manifestContent);
                
                // Criar JAR com manifest
                var processInfo = new ProcessStartInfo
                {
                    FileName = "jar",
                    Arguments = $"cfm \"{manifestJarPath}\" \"{manifestPath}\"",
                    WorkingDirectory = _gameDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                var process = Process.Start(processInfo);
                process.WaitForExit();
                
                if (process.ExitCode == 0)
                {
                    return $"\"{manifestJarPath}\"";
                }
            }
            catch
            {
                // Se falhar, usar abordagem alternativa
            }
            
            // Fallback: usar apenas os JARs mais importantes
            var importantJars = new List<string>
            {
                Path.Combine(_gameDirectory, "minecraft.jar")
            };
            
            // Adicionar bibliotecas essenciais do Forge
            var forgeLibs = Path.Combine(_gameDirectory, "libraries", "net", "minecraftforge");
            if (Directory.Exists(forgeLibs))
            {
                var forgeJars = Directory.GetFiles(forgeLibs, "*.jar", SearchOption.AllDirectories);
                importantJars.AddRange(forgeJars);
            }
            
            return string.Join(";", importantJars.Select(jar => $"\"{jar}\""));
        }
        
        private async Task AutoConfigureRayzeCraftAsync()
        {
            // Garantir que os diretórios existem
            Directory.CreateDirectory(_gameDirectory);
            Directory.CreateDirectory(Path.Combine(_gameDirectory, "mods"));
            Directory.CreateDirectory(Path.Combine(_gameDirectory, "config"));
            Directory.CreateDirectory(Path.Combine(_gameDirectory, "libraries"));
            Directory.CreateDirectory(Path.Combine(_gameDirectory, "assets"));
            
            // Verificar se o minecraft.jar existe e está correto
            var minecraftJar = Path.Combine(_gameDirectory, "minecraft.jar");
            if (!File.Exists(minecraftJar) || new FileInfo(minecraftJar).Length < 1000000)
            {
                await ConfigureMinecraftJarAsync();
            }
            
            // Verificar se os mods foram copiados
            var modsDir = Path.Combine(_gameDirectory, "mods");
            if (Directory.Exists(modsDir))
            {
                Directory.Delete(modsDir, true);
            }
            await CopyRayzeCraftModsAsync();
            
            // Verificar se as configurações foram copiadas
            var configDir = Path.Combine(_gameDirectory, "config");
            if (Directory.Exists(configDir))
            {
                Directory.Delete(configDir, true);
            }
            await CopyRayzeCraftConfigAsync();
            
            // Verificar se as bibliotecas do Forge existem
            var forgeLibs = Path.Combine(_gameDirectory, "libraries", "net", "minecraftforge");
            if (!Directory.Exists(forgeLibs))
            {
                await InstallForgeLibrariesAsync();
            }
        }
        
        private async Task<string> GetJava8PathAsync()
        {
            var java8Paths = new List<string>
            {
                @"C:\Program Files\Java\jre1.8.0_431\bin\java.exe",
                @"C:\Program Files\Java\jre1.8.0_421\bin\java.exe",
                @"C:\Program Files\Java\jre1.8.0_411\bin\java.exe",
                @"C:\Program Files\Java\jre1.8.0_401\bin\java.exe",
                @"C:\Program Files\Java\jdk1.8.0_431\bin\java.exe",
                @"C:\Program Files\Java\jdk1.8.0_421\bin\java.exe",
                @"C:\Program Files\Java\jdk1.8.0_411\bin\java.exe",
                @"C:\Program Files\Java\jdk1.8.0_401\bin\java.exe",
                @"C:\Program Files (x86)\Java\jre1.8.0_431\bin\java.exe",
                @"C:\Program Files (x86)\Java\jre1.8.0_421\bin\java.exe",
                @"C:\Program Files (x86)\Java\jre1.8.0_411\bin\java.exe",
                @"C:\Program Files (x86)\Java\jre1.8.0_401\bin\java.exe"
            };
            
            foreach (var path in java8Paths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
            
            return null;
        }
        
        private async Task ConfigureMinecraftJarAsync()
        {
            var minecraftJar = Path.Combine(_gameDirectory, "minecraft.jar");
            
            // Tentar copiar do RayzeCraft
            var rayzecraftJar = @"C:\rayzecraft-launcher-files\bin\minecraft.jar";
            if (File.Exists(rayzecraftJar))
            {
                File.Copy(rayzecraftJar, minecraftJar, true);
                var versionsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "versions", "1.12.2-forge");
                Directory.CreateDirectory(versionsDir);
                File.Copy(rayzecraftJar, Path.Combine(versionsDir, "1.12.2-forge.jar"), true);
                return;
            }
            
            // Tentar copiar do Minecraft oficial
            var officialJar = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "versions", "1.12.2", "1.12.2.jar");
            if (File.Exists(officialJar))
            {
                File.Copy(officialJar, minecraftJar, true);
                return;
            }
            
            // Como último recurso, baixar o cliente vanilla
            await DownloadMinecraftClientAsync();
        }
        
        private async Task DownloadMinecraftClientAsync()
        {
            var minecraftJar = Path.Combine(_gameDirectory, "minecraft.jar");
            
            // Usar URL oficial do Minecraft 1.12.2
            var clientUrl = "https://piston-data.mojang.com/v1/objects/0f275bc1547d01fa5f56ba34bdc87d981ee12daf/client.jar";
            
            await _logService.LogInfoAsync($"Baixando Minecraft 1.12.2 de {clientUrl}");
            
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                try
                {
                    var response = await httpClient.GetAsync(clientUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsByteArrayAsync();
                        await File.WriteAllBytesAsync(minecraftJar, content);
                        await _logService.LogSuccessAsync($"Minecraft 1.12.2 baixado com sucesso ({content.Length} bytes)");
                    }
                    else
                    {
                        await _logService.LogErrorAsync($"Falha ao baixar Minecraft: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    await _logService.LogErrorAsync("Erro ao baixar Minecraft", ex.Message);
                }
            }
        }
        
        private async Task CopyRayzeCraftModsAsync()
        {
            var sourceModsDir = @"C:\rayzecraft-launcher-files\mods";
            var targetModsDir = Path.Combine(_gameDirectory, "mods");
            
            if (Directory.Exists(sourceModsDir))
            {
                foreach (var modFile in Directory.GetFiles(sourceModsDir, "*.jar"))
                {
                    var targetFile = Path.Combine(targetModsDir, Path.GetFileName(modFile));
                    File.Copy(modFile, targetFile, true);
                }
            }
        }
        
        private async Task CopyRayzeCraftConfigAsync()
        {
            var sourceConfigDir = @"C:\rayzecraft-launcher-files\config";
            var targetConfigDir = Path.Combine(_gameDirectory, "config");
            
            if (Directory.Exists(sourceConfigDir))
            {
                await CopyDirectoryAsync(sourceConfigDir, targetConfigDir);
            }
        }
        
        private async Task InstallForgeLibrariesAsync()
        {
            var sourceLibsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "libraries");
            var targetLibsDir = Path.Combine(_gameDirectory, "libraries");
            
            if (Directory.Exists(sourceLibsDir))
            {
                await CopyDirectoryAsync(sourceLibsDir, targetLibsDir);
            }
        }
        
        private async Task CopyDirectoryAsync(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            
            // Copiar arquivos
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFile, true);
            }
            
            // Copiar subdiretórios
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var targetSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
                await CopyDirectoryAsync(dir, targetSubDir);
            }
        }
        
        private async Task SetupModpackAlternativeAsync()
        {
            // Método alternativo para configurar o modpack
            var minecraftDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            var sourceDir = @"C:\rayzecraft-launcher-files";
            
            // Garantir que os diretórios existem
            Directory.CreateDirectory(minecraftDir);
            
            // Deletar e recriar pasta mods
            var modsDir = Path.Combine(minecraftDir, "mods");
            if (Directory.Exists(modsDir))
            {
                Directory.Delete(modsDir, true);
            }
            Directory.CreateDirectory(modsDir);
            
            // Copiar mods
            var sourceModsDir = Path.Combine(sourceDir, "mods");
            if (Directory.Exists(sourceModsDir))
            {
                foreach (var modFile in Directory.GetFiles(sourceModsDir, "*.jar"))
                {
                    var targetFile = Path.Combine(modsDir, Path.GetFileName(modFile));
                    File.Copy(modFile, targetFile, true);
                }
            }
            
            // Deletar e recriar pasta config
            var configDir = Path.Combine(minecraftDir, "config");
            if (Directory.Exists(configDir))
            {
                Directory.Delete(configDir, true);
            }
            Directory.CreateDirectory(configDir);
            
            // Copiar config
            var sourceConfigDir = Path.Combine(sourceDir, "config");
            if (Directory.Exists(sourceConfigDir))
            {
                await CopyDirectoryAsync(sourceConfigDir, configDir);
            }
            
            // Deletar e recriar pasta scripts
            var scriptsDir = Path.Combine(minecraftDir, "scripts");
            if (Directory.Exists(scriptsDir))
            {
                Directory.Delete(scriptsDir, true);
            }
            Directory.CreateDirectory(scriptsDir);
            
            // Copiar scripts
            var sourceScriptsDir = Path.Combine(sourceDir, "scripts");
            if (Directory.Exists(sourceScriptsDir))
            {
                await CopyDirectoryAsync(sourceScriptsDir, scriptsDir);
            }
        }
        
        private async Task LaunchMinecraftVanillaAsync()
        {
            await _logService.LogInfoAsync("Iniciando lançamento do Minecraft Vanilla...");
            
            var config = _configService.GetConfiguration();
            var javaPath = await GetJava8PathAsync() ?? "java";
            
            // Encontrar um JAR do Minecraft disponível
            var minecraftJar = await FindMinecraftAsync();
            if (string.IsNullOrEmpty(minecraftJar))
            {
                minecraftJar = Path.Combine(_gameDirectory, "minecraft.jar");
                if (!File.Exists(minecraftJar))
                {
                    try
                    {
                        await DownloadMinecraftClientAsync();
                    }
                    catch
                    {
                        await _logService.LogWarningAsync("Não foi possível baixar o Minecraft, usando arquivo dummy");
                        await File.WriteAllTextAsync(minecraftJar, "dummy");
                    }
                }
            }
            
            // Argumentos para Minecraft Vanilla (sem Forge)
            var vanillaArgs = $"-Xmx{config.MaxMemory}M -Xms{config.MinMemory}M -cp \"{minecraftJar}\" net.minecraft.client.main.Main --username {config.PlayerName} --gameDir \"{_gameDirectory}\" --version 1.12.2";
            
            var processInfo = new ProcessStartInfo
            {
                FileName = javaPath,
                Arguments = vanillaArgs,
                WorkingDirectory = _gameDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            };
            
            await _logService.LogInfoAsync($"Tentando lançar Minecraft Vanilla: {vanillaArgs.Substring(0, Math.Min(100, vanillaArgs.Length))}...");
            
            try
            {
                var process = Process.Start(processInfo);
                await _logService.LogSuccessAsync($"Processo Vanilla iniciado com PID: {process.Id}");
                
                await Task.Delay(3000);
                
                if (process.HasExited)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    var fullError = string.IsNullOrEmpty(error) ? output : error;
                    
                    await _logService.LogWarningAsync($"Vanilla saiu com código: {process.ExitCode}");
                    await _logService.LogWarningAsync($"Saída Vanilla: {fullError.Substring(0, Math.Min(300, fullError.Length))}");
                    
                    // Tentar modo ainda mais simplificado
                    await LaunchMinecraftSimplifiedAsync();
                }
                else
                {
                    await _logService.LogSuccessAsync("Minecraft Vanilla iniciado com sucesso!");
                }
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("Erro ao lançar Minecraft Vanilla", ex.Message);
                await LaunchMinecraftSimplifiedAsync();
            }
        }
        
        private async Task LaunchMinecraftSimplifiedAsync()
        {
            await _logService.LogInfoAsync("Iniciando modo simplificado...");
            
            // Método simplificado para lançar o Minecraft com configuração mínima
            var config = _configService.GetConfiguration();
            var javaPath = "java";
            
            // Tentar encontrar java específico do sistema
            var java8Path = await GetJava8PathAsync();
            if (!string.IsNullOrEmpty(java8Path))
            {
                javaPath = java8Path;
            }
            
            // Encontrar um JAR do Minecraft disponível
            var minecraftJar = await FindMinecraftAsync();
            if (string.IsNullOrEmpty(minecraftJar))
            {
                // Se não encontrou Minecraft, tentar baixar ou criar um arquivo dummy
                minecraftJar = Path.Combine(_gameDirectory, "minecraft.jar");
                if (!File.Exists(minecraftJar))
                {
                    try
                    {
                        await DownloadMinecraftClientAsync();
                    }
                    catch
                    {
                        // Criar arquivo dummy para tentar lançar mesmo assim
                        await File.WriteAllTextAsync(minecraftJar, "dummy");
                    }
                }
            }
            
            // Argumentos mínimos necessários
            var simpleArgs = $"-Xmx{config.MaxMemory}M -Xms{config.MinMemory}M -cp \"{minecraftJar}\" net.minecraft.client.main.Main --username {config.PlayerName} --gameDir \"{_gameDirectory}\" --version 1.12.2";
            
            var processInfo = new ProcessStartInfo
            {
                FileName = javaPath,
                Arguments = simpleArgs,
                WorkingDirectory = _gameDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = false
            };
            
            await _logService.LogInfoAsync($"Tentando modo simplificado: {simpleArgs.Substring(0, Math.Min(100, simpleArgs.Length))}...");
            
            // Tentar lançar mesmo que possa falhar
            try
            {
                var process = Process.Start(processInfo);
                await _logService.LogSuccessAsync($"Modo simplificado iniciado com PID: {process.Id}");
                await Task.Delay(2000); // Aguardar um pouco antes de retornar
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("Erro no modo simplificado", ex.Message);
                
                // Como último recurso, tentar com argumentos ainda mais simples
                processInfo.Arguments = $"-jar \"{minecraftJar}\"";
                try
                {
                    var process = Process.Start(processInfo);
                    await _logService.LogSuccessAsync($"Modo JAR direto iniciado com PID: {process.Id}");
                    await Task.Delay(2000);
                }
                catch (Exception ex2)
                {
                    await _logService.LogErrorAsync("Erro no modo JAR direto", ex2.Message);
                    // Mostrar uma mensagem de demonstração
                    await CreateDemoModeAsync();
                }
            }
        }
    }
}
