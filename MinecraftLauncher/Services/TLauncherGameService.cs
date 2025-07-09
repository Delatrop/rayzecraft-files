using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MinecraftLauncher.Models;
using Newtonsoft.Json;

namespace MinecraftLauncher.Services
{
    public class TLauncherGameService
    {
        private readonly string _gameDirectory;
        private readonly ConfigService _configService;
        private readonly LogService _logService;
        private readonly string _launcherVersion = "1.12.2-forge-14.23.5.2854";

        public TLauncherGameService()
        {
            _gameDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            _configService = new ConfigService();
            _logService = new LogService();
        }

        public async Task<bool> VerifyGameIntegrityAsync()
        {
            await _logService.LogInfoAsync("=== VERIFICAÇÃO DE INTEGRIDADE ===");
            
            var issues = new List<string>();
            
            // 1. Verificar Java
            var javaPath = GetJavaPath();
            if (string.IsNullOrEmpty(javaPath))
            {
                issues.Add("Java não encontrado");
            }
            else
            {
                await _logService.LogInfoAsync($"✓ Java encontrado: {javaPath}");
            }

            // 2. Verificar cliente Minecraft
            var clientPath = GetMinecraftClientPath();
            if (string.IsNullOrEmpty(clientPath))
            {
                issues.Add("Cliente Minecraft não encontrado");
            }
            else
            {
                await _logService.LogInfoAsync($"✓ Cliente Minecraft: {clientPath}");
            }

            // 3. Verificar bibliotecas essenciais
            var missingLibs = await VerifyEssentialLibrariesAsync();
            if (missingLibs.Count > 0)
            {
                issues.Add($"{missingLibs.Count} bibliotecas essenciais ausentes");
                foreach (var lib in missingLibs.Take(5))
                {
                    await _logService.LogWarningAsync($"Biblioteca ausente: {Path.GetFileName(lib)}");
                }
            }

            // 4. Verificar diretórios necessários
            await EnsureDirectoriesExistAsync();

            // 5. Verificar arquivo de versão JSON
            await VerifyVersionJsonAsync();

            if (issues.Count > 0)
            {
                await _logService.LogErrorAsync("Problemas encontrados na verificação:", string.Join(", ", issues));
                return false;
            }

            await _logService.LogSuccessAsync("Verificação de integridade concluída com sucesso");
            return true;
        }

        public async Task LaunchGameAsync()
        {
            await _logService.LogInfoAsync("=== INICIANDO MINECRAFT (PADRÃO TLAUNCHER) ===");
            
            var config = _configService.GetConfiguration();
            await _logService.LogInfoAsync($"Jogador: {config.PlayerName}");
            await _logService.LogInfoAsync($"Memória: {config.MaxMemory}MB");

            try
            {
                // 1. Verificar integridade antes de lançar
                bool integrityOk = await VerifyGameIntegrityAsync();
                if (!integrityOk)
                {
                    await _logService.LogWarningAsync("Problemas de integridade detectados, mas continuando...");
                }

                // 2. Obter paths necessários
                var javaPath = GetJavaPath();
                var clientPath = GetMinecraftClientPath();
                
                if (string.IsNullOrEmpty(javaPath))
                {
                    throw new FileNotFoundException("Java não encontrado. Instale o Java 8 ou superior.");
                }
                
                if (string.IsNullOrEmpty(clientPath))
                {
                    throw new FileNotFoundException("Cliente Minecraft não encontrado. Execute uma atualização primeiro.");
                }

                // 3. Construir classpath
                var classpath = await BuildClasspathAsync();
                await _logService.LogInfoAsync($"Classpath construído com {classpath.Split(';').Length} componentes");

                // 4. Construir argumentos
                var jvmArgs = BuildJvmArguments(config, classpath);
                var mcArgs = BuildMinecraftArguments(config);

                // 5. Comando completo
                var fullArgs = $"{jvmArgs} net.minecraft.launchwrapper.Launch {mcArgs}";

                // 6. Log do comando para debug
                await LogLaunchCommandAsync(javaPath, fullArgs);

                // 7. Executar
                await ExecuteMinecraftAsync(javaPath, fullArgs);

                await _logService.LogSuccessAsync("Minecraft lançado com sucesso!");
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("Erro ao lançar Minecraft", ex.Message);
                throw;
            }
        }

        private string GetJavaPath()
        {
            var config = _configService.GetConfiguration();
            
            // Se o usuário configurou um caminho específico, usar ele primeiro
            if (!string.IsNullOrEmpty(config.JavaPath) && File.Exists(config.JavaPath))
            {
                return config.JavaPath;
            }
            
            // Caso contrário, usar detecção automática
            var javaPaths = new[]
            {
                // Java do TLauncher (primeira prioridade)
                Path.Combine(_gameDirectory, "runtime", "jre-legacy", "windows", "jre-legacy", "bin", "javaw.exe"),
                
                // Java do sistema (versões mais recentes primeiro)
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
                @"C:\Program Files (x86)\Java\jre1.8.0_401\bin\java.exe",
                
                // Java do PATH (último recurso)
                "javaw.exe",
                "java.exe"
            };

            foreach (var path in javaPaths)
            {
                if (path.Contains("\\") && File.Exists(path))
                {
                    return path;
                }
                else if (!path.Contains("\\"))
                {
                    try
                    {
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = path,
                            Arguments = "-version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        using (var process = Process.Start(processInfo))
                        {
                            process.WaitForExit();
                            if (process.ExitCode == 0)
                            {
                                return path;
                            }
                        }
                    }
                    catch { }
                }
            }

            return null;
        }

        private string GetMinecraftClientPath()
        {
            var clientPaths = new[]
            {
                // Cliente personalizado (primeira prioridade)
                Path.Combine(_gameDirectory, "versions", "ForgeOptiFine 1.12.2", "ForgeOptiFine 1.12.2.jar"),
                
                // Cliente Forge padrão
                Path.Combine(_gameDirectory, "versions", _launcherVersion, $"{_launcherVersion}.jar"),
                Path.Combine(_gameDirectory, "versions", "1.12.2-forge", "1.12.2-forge.jar"),
                
                // Cliente vanilla
                Path.Combine(_gameDirectory, "versions", "1.12.2", "1.12.2.jar"),
                
                // Cliente direto na pasta .minecraft
                Path.Combine(_gameDirectory, "minecraft.jar")
            };

            foreach (var path in clientPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private async Task<List<string>> VerifyEssentialLibrariesAsync()
        {
            var essentialLibraries = new[]
            {
                "net/minecraftforge/forge/1.12.2-14.23.5.2854/forge-1.12.2-14.23.5.2854.jar",
                "org/ow2/asm/asm-debug-all/5.2/asm-debug-all-5.2.jar",
                "net/minecraft/launchwrapper/1.12/launchwrapper-1.12.jar",
                "net/sf/jopt-simple/jopt-simple/5.0.3/jopt-simple-5.0.3.jar",
                "org/apache/logging/log4j/log4j-api/2.15.0/log4j-api-2.15.0.jar",
                "org/apache/logging/log4j/log4j-core/2.15.0/log4j-core-2.15.0.jar",
                "com/google/guava/guava/21.0/guava-21.0.jar",
                "org/apache/commons/commons-lang3/3.5/commons-lang3-3.5.jar",
                "commons-io/commons-io/2.5/commons-io-2.5.jar",
                "com/google/code/gson/gson/2.8.0/gson-2.8.0.jar",
                "com/mojang/authlib/1.5.21/authlib-1.5.21.jar",
                "org/lwjgl/lwjgl/lwjgl/2.9.4-nightly-20150209/lwjgl-2.9.4-nightly-20150209.jar",
                "org/lwjgl/lwjgl/lwjgl_util/2.9.4-nightly-20150209/lwjgl_util-2.9.4-nightly-20150209.jar"
            };

            var missingLibs = new List<string>();
            var librariesDir = Path.Combine(_gameDirectory, "libraries");

            foreach (var lib in essentialLibraries)
            {
                var fullPath = Path.Combine(librariesDir, lib);
                if (!File.Exists(fullPath))
                {
                    missingLibs.Add(lib);
                }
            }

            return missingLibs;
        }

        private async Task<string> BuildClasspathAsync()
        {
            var classpathComponents = new List<string>();
            var librariesDir = Path.Combine(_gameDirectory, "libraries");

            // Bibliotecas essenciais do Forge (ordem específica)
            var essentialLibraries = new[]
            {
                "net/minecraftforge/forge/1.12.2-14.23.5.2854/forge-1.12.2-14.23.5.2854.jar",
                "org/ow2/asm/asm-debug-all/5.2/asm-debug-all-5.2.jar",
                "net/minecraft/launchwrapper/1.12/launchwrapper-1.12.jar",
                "org/jline/jline/3.5.1/jline-3.5.1.jar",
                "org/scala-lang/scala-library/2.11.1/scala-library-2.11.1.jar",
                "org/scala-lang/scala-compiler/2.11.1/scala-compiler-2.11.1.jar",
                "lzma/lzma/0.0.1/lzma-0.0.1.jar",
                "java3d/vecmath/1.5.2/vecmath-1.5.2.jar",
                "net/sf/trove4j/trove4j/3.0.3/trove4j-3.0.3.jar",
                "org/apache/maven/maven-artifact/3.5.3/maven-artifact-3.5.3.jar",
                "net/sf/jopt-simple/jopt-simple/5.0.3/jopt-simple-5.0.3.jar",
                "org/apache/logging/log4j/log4j-api/2.15.0/log4j-api-2.15.0.jar",
                "org/apache/logging/log4j/log4j-core/2.15.0/log4j-core-2.15.0.jar",
                "oshi-project/oshi-core/1.1/oshi-core-1.1.jar",
                "net/java/dev/jna/jna/4.4.0/jna-4.4.0.jar",
                "net/java/dev/jna/platform/3.4.0/platform-3.4.0.jar",
                "com/ibm/icu/icu4j-core-mojang/51.2/icu4j-core-mojang-51.2.jar",
                "com/paulscode/codecjorbis/20101023/codecjorbis-20101023.jar",
                "com/paulscode/codecwav/20101023/codecwav-20101023.jar",
                "com/paulscode/libraryjavasound/20101123/libraryjavasound-20101123.jar",
                "com/paulscode/librarylwjglopenal/20100824/librarylwjglopenal-20100824.jar",
                "com/paulscode/soundsystem/20120107/soundsystem-20120107.jar",
                "io/netty/netty-all/4.1.9.Final/netty-all-4.1.9.Final.jar",
                "com/google/guava/guava/21.0/guava-21.0.jar",
                "org/apache/commons/commons-lang3/3.5/commons-lang3-3.5.jar",
                "commons-io/commons-io/2.5/commons-io-2.5.jar",
                "commons-codec/commons-codec/1.10/commons-codec-1.10.jar",
                "net/java/jinput/jinput/2.0.5/jinput-2.0.5.jar",
                "net/java/jutils/jutils/1.0.0/jutils-1.0.0.jar",
                "com/google/code/gson/gson/2.8.0/gson-2.8.0.jar",
                "com/mojang/authlib/1.5.21/authlib-1.5.21.jar",
                "com/mojang/realms/1.10.22/realms-1.10.22.jar",
                "org/apache/httpcomponents/httpclient/4.3.3/httpclient-4.3.3.jar",
                "commons-logging/commons-logging/1.1.3/commons-logging-1.1.3.jar",
                "org/apache/httpcomponents/httpcore/4.3.2/httpcore-4.3.2.jar",
                "it/unimi/dsi/fastutil/7.1.0/fastutil-7.1.0.jar",
                "org/lwjgl/lwjgl/lwjgl/2.9.4-nightly-20150209/lwjgl-2.9.4-nightly-20150209.jar",
                "org/lwjgl/lwjgl/lwjgl_util/2.9.4-nightly-20150209/lwjgl_util-2.9.4-nightly-20150209.jar",
                "com/mojang/text2speech/1.10.3/text2speech-1.10.3.jar"
            };

            // Adicionar bibliotecas essenciais que existem
            foreach (var lib in essentialLibraries)
            {
                var fullPath = Path.Combine(librariesDir, lib);
                if (File.Exists(fullPath))
                {
                    classpathComponents.Add(fullPath);
                }
            }

            // Adicionar outras bibliotecas encontradas
            if (Directory.Exists(librariesDir))
            {
                var allJars = Directory.GetFiles(librariesDir, "*.jar", SearchOption.AllDirectories);
                foreach (var jar in allJars)
                {
                    if (!classpathComponents.Contains(jar))
                    {
                        classpathComponents.Add(jar);
                    }
                }
            }

            // Adicionar cliente Minecraft
            var clientPath = GetMinecraftClientPath();
            if (!string.IsNullOrEmpty(clientPath))
            {
                classpathComponents.Add(clientPath);
            }

            // Criar string do classpath
            var classpath = string.Join(";", classpathComponents.Select(path => $"\"{path}\""));
            
            // Se o classpath for muito longo, tentar otimizar
            if (classpath.Length > 8000)
            {
                await _logService.LogWarningAsync("Classpath muito longo, otimizando...");
                return await OptimizeClasspathAsync(classpathComponents);
            }

            return classpath;
        }

        private async Task<string> OptimizeClasspathAsync(List<string> components)
        {
            // Criar um arquivo de manifesto JAR para contornar o limite de linha de comando
            var manifestJarPath = Path.Combine(_gameDirectory, "launcher-classpath.jar");
            
            try
            {
                await _logService.LogInfoAsync("Criando arquivo de manifesto JAR para classpath...");
                
                // Criar conteúdo do manifest
                var manifestContent = "Manifest-Version: 1.0\n";
                manifestContent += "Main-Class: net.minecraft.launchwrapper.Launch\n";
                manifestContent += "Class-Path: ";
                
                // Adicionar todas as bibliotecas ao Class-Path do manifest
                var relativePaths = components.Select(path => 
                {
                    // Converter para caminho relativo ou URI
                    var uri = new Uri(path);
                    return uri.ToString().Replace("file:///", "");
                }).ToList();
                
                // Dividir em linhas de no máximo 70 caracteres (limite do manifest)
                var classPathLines = new List<string>();
                var currentLine = "";
                
                foreach (var path in relativePaths)
                {
                    if (currentLine.Length + path.Length + 1 > 70)
                    {
                        classPathLines.Add(currentLine);
                        currentLine = " " + path; // Continuação com espaço
                    }
                    else
                    {
                        currentLine += (currentLine.Length > 0 ? " " : "") + path;
                    }
                }
                if (!string.IsNullOrEmpty(currentLine))
                {
                    classPathLines.Add(currentLine);
                }
                
                manifestContent += string.Join("\n", classPathLines) + "\n";
                
                // Escrever arquivo manifest
                var manifestPath = Path.Combine(_gameDirectory, "MANIFEST.MF");
                await File.WriteAllTextAsync(manifestPath, manifestContent);
                
                // Criar JAR com o manifest
                var jarCommand = $"cfm \"{manifestJarPath}\" \"{manifestPath}\"";
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = "jar",
                    Arguments = jarCommand,
                    WorkingDirectory = _gameDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(processInfo))
                {
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode == 0)
                    {
                        await _logService.LogSuccessAsync($"Arquivo de manifesto JAR criado: {manifestJarPath}");
                        return $"\"{manifestJarPath}\"";
                    }
                    else
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        await _logService.LogWarningAsync($"Falha ao criar JAR de manifesto: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                await _logService.LogWarningAsync($"Erro ao criar arquivo de manifesto: {ex.Message}");
            }
            
            // Fallback: usar apenas os componentes mais importantes
            await _logService.LogInfoAsync("Usando fallback com componentes essenciais...");
            
            // Bibliotecas OBRIGATÓRIAS para o Minecraft funcionar
            var mandatoryLibs = new[]
            {
                "launchwrapper", 
                "jopt-simple", 
                "lwjgl", 
                "guava", 
                "gson", 
                "authlib"
            };
            
            var essentialComponents = new List<string>();
            
            // Primeiro, adicionar bibliotecas obrigatórias
            foreach (var mandatory in mandatoryLibs)
            {
                var found = components.FirstOrDefault(c => c.Contains(mandatory));
                if (found != null && !essentialComponents.Contains(found))
                {
                    essentialComponents.Add(found);
                }
            }
            
            // Depois, adicionar outras bibliotecas importantes
            var otherImportant = components.Where(c => 
                c.Contains("log4j") ||
                c.Contains("commons") ||
                c.Contains("netty") ||
                c.Contains("asm")
            ).Where(c => !essentialComponents.Contains(c)).Take(20);
            
            essentialComponents.AddRange(otherImportant);
            
            await _logService.LogInfoAsync($"Classpath essencial: {essentialComponents.Count} componentes");
            return string.Join(";", essentialComponents.Select(path => $"\"{path}\""));
        }

        private string BuildJvmArguments(LauncherConfig config, string classpath)
        {
            var nativesDir = Path.Combine(_gameDirectory, "versions", _launcherVersion, "natives");
            var assetsDir = Path.Combine(_gameDirectory, "assets");
            var librariesDir = Path.Combine(_gameDirectory, "libraries");

            var args = new List<string>
            {
                "-Dos.name=Windows 10",
                "-Dos.version=10.0",
                $"-Djava.library.path=\"{nativesDir}\"",
                $"-cp {classpath}",
                $"-Xmx{config.MaxMemory}M",
                $"-Xms{config.MinMemory}M",
                "-XX:+UnlockExperimentalVMOptions",
                "-XX:+UseG1GC",
                "-XX:G1NewSizePercent=20",
                "-XX:G1ReservePercent=20",
                "-XX:MaxGCPauseMillis=50",
                "-XX:G1HeapRegionSize=32M",
                "-Dfml.ignoreInvalidMinecraftCertificates=true",
                "-Dfml.ignorePatchDiscrepancies=true",
                "-Djava.net.preferIPv4Stack=true",
                $"-Dminecraft.applet.TargetDirectory=\"{_gameDirectory}\"",
                $"-DlibraryDirectory=\"{librariesDir}\"",
                $"-Dlog4j.configurationFile=\"{Path.Combine(assetsDir, "log_configs", "client-1.12.xml")}\""
            };

            return string.Join(" ", args);
        }

        private string BuildMinecraftArguments(LauncherConfig config)
        {
            var assetsDir = Path.Combine(_gameDirectory, "assets");
            var uuid = Guid.NewGuid().ToString("N");

            var args = new List<string>
            {
                $"--username {config.PlayerName}",
                $"--version {_launcherVersion}",
                $"--gameDir \"{_gameDirectory}\"",
                $"--assetsDir \"{assetsDir}\"",
                "--assetIndex 1.12",
                $"--uuid {uuid}",
                "--accessToken fake",
                "--userType legacy",
                "--tweakClass net.minecraftforge.fml.common.launcher.FMLTweaker",
                "--versionType Forge"
            };

            // Adicionar resolução se especificada
            if (config.WindowWidth > 0 && config.WindowHeight > 0)
            {
                args.Add($"--width {config.WindowWidth}");
                args.Add($"--height {config.WindowHeight}");
            }

            return string.Join(" ", args);
        }

        private async Task EnsureDirectoriesExistAsync()
        {
            var directories = new[]
            {
                _gameDirectory,
                Path.Combine(_gameDirectory, "assets"),
                Path.Combine(_gameDirectory, "libraries"),
                Path.Combine(_gameDirectory, "versions"),
                Path.Combine(_gameDirectory, "versions", _launcherVersion),
                Path.Combine(_gameDirectory, "versions", _launcherVersion, "natives"),
                Path.Combine(_gameDirectory, "mods"),
                Path.Combine(_gameDirectory, "config"),
                Path.Combine(_gameDirectory, "logs")
            };

            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    await _logService.LogInfoAsync($"Diretório criado: {dir}");
                }
            }
        }

        private async Task VerifyVersionJsonAsync()
        {
            var versionJsonPath = Path.Combine(_gameDirectory, "versions", _launcherVersion, $"{_launcherVersion}.json");
            
            if (!File.Exists(versionJsonPath))
            {
                await _logService.LogWarningAsync($"Arquivo de versão não encontrado: {versionJsonPath}");
                await CreateVersionJsonAsync(versionJsonPath);
            }
        }

        private async Task CreateVersionJsonAsync(string jsonPath)
        {
            var versionData = new
            {
                id = _launcherVersion,
                time = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                releaseTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                type = "release",
                minecraftArguments = "--username ${auth_player_name} --version ${version_name} --gameDir ${game_directory} --assetsDir ${assets_root} --assetIndex ${assets_index_name} --uuid ${auth_uuid} --accessToken ${auth_access_token} --userType ${user_type} --tweakClass net.minecraftforge.fml.common.launcher.FMLTweaker --versionType ${version_type}",
                mainClass = "net.minecraft.launchwrapper.Launch",
                inheritsFrom = "1.12.2",
                jar = "1.12.2",
                logging = new
                {
                    client = new
                    {
                        file = new
                        {
                            id = "client-1.12.xml",
                            sha1 = "ef4f57b922df243d0cef096efe808c72db042149",
                            size = 877,
                            url = "https://launcher.mojang.com/v1/objects/ef4f57b922df243d0cef096efe808c72db042149/client-1.12.xml"
                        },
                        argument = "-Dlog4j.configurationFile=${path}",
                        type = "log4j2-xml"
                    }
                }
            };

            var jsonContent = JsonConvert.SerializeObject(versionData, Formatting.Indented);
            await File.WriteAllTextAsync(jsonPath, jsonContent);
            await _logService.LogInfoAsync($"Arquivo de versão criado: {jsonPath}");
        }

        private async Task LogLaunchCommandAsync(string javaPath, string arguments)
        {
            var commandLog = $"=== COMANDO DE LANÇAMENTO ===\n";
            commandLog += $"Java: {javaPath}\n";
            commandLog += $"Diretório: {_gameDirectory}\n";
            commandLog += $"Argumentos: {arguments.Substring(0, Math.Min(500, arguments.Length))}...\n";
            commandLog += $"Comprimento total: {arguments.Length} caracteres\n";
            
            await _logService.LogInfoAsync(commandLog);
        }

        private async Task ExecuteMinecraftAsync(string javaPath, string arguments)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = javaPath,
                Arguments = arguments,
                WorkingDirectory = _gameDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            };

            await _logService.LogInfoAsync("Iniciando processo do Minecraft...");
            
            var process = Process.Start(processInfo);
            await _logService.LogInfoAsync($"Processo iniciado com PID: {process.Id}");

            // Aguardar um tempo para verificar se o processo não sai imediatamente
            await Task.Delay(5000);

            if (process.HasExited)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                var fullOutput = string.IsNullOrEmpty(error) ? output : error;

                await _logService.LogWarningAsync($"Processo saiu com código: {process.ExitCode}");
                await _logService.LogWarningAsync($"Saída: {fullOutput.Substring(0, Math.Min(1000, fullOutput.Length))}");

                // Analisar os erros e fornecer feedback específico
                if (fullOutput.Contains("Could not find or load main class"))
                {
                    throw new Exception("Erro: Classe principal não encontrada. Verifique se o Forge está instalado corretamente.");
                }
                else if (fullOutput.Contains("java.lang.OutOfMemoryError"))
                {
                    throw new Exception("Erro: Memória insuficiente. Aumente a quantidade de RAM nas configurações.");
                }
                else if (fullOutput.Contains("java.lang.NoClassDefFoundError"))
                {
                    throw new Exception("Erro: Bibliotecas ausentes. Clique em 'Verificar Integridade' para corrigir.");
                }
                else if (fullOutput.Contains("AccessDeniedException"))
                {
                    throw new Exception("Erro: Acesso negado. Execute o launcher como administrador.");
                }
                else if (process.ExitCode != 0)
                {
                    throw new Exception($"Minecraft falhou ao iniciar (código {process.ExitCode}). Verifique os logs para mais detalhes.");
                }
            }
            else
            {
                await _logService.LogSuccessAsync("Minecraft iniciado com sucesso e ainda está executando!");
            }
        }

        public async Task<string> GetIntegrityReportAsync()
        {
            var report = "=== RELATÓRIO DE INTEGRIDADE ===\n\n";
            
            // Java
            var javaPath = GetJavaPath();
            report += $"Java: {(string.IsNullOrEmpty(javaPath) ? "❌ NÃO ENCONTRADO" : $"✅ {javaPath}")}\n";
            
            // Cliente Minecraft
            var clientPath = GetMinecraftClientPath();
            report += $"Cliente Minecraft: {(string.IsNullOrEmpty(clientPath) ? "❌ NÃO ENCONTRADO" : $"✅ {clientPath}")}\n";
            
            // Bibliotecas
            var missingLibs = await VerifyEssentialLibrariesAsync();
            report += $"Bibliotecas essenciais: {(missingLibs.Count == 0 ? "✅ TODAS PRESENTES" : $"❌ {missingLibs.Count} AUSENTES")}\n";
            
            // Diretórios
            var requiredDirs = new[]
            {
                Path.Combine(_gameDirectory, "assets"),
                Path.Combine(_gameDirectory, "libraries"),
                Path.Combine(_gameDirectory, "versions"),
                Path.Combine(_gameDirectory, "mods")
            };
            
            var missingDirs = requiredDirs.Where(dir => !Directory.Exists(dir)).ToList();
            report += $"Diretórios necessários: {(missingDirs.Count == 0 ? "✅ TODOS PRESENTES" : $"❌ {missingDirs.Count} AUSENTES")}\n";
            
            if (missingLibs.Count > 0)
            {
                report += "\n=== BIBLIOTECAS AUSENTES ===\n";
                foreach (var lib in missingLibs)
                {
                    report += $"• {Path.GetFileName(lib)}\n";
                }
            }
            
            if (missingDirs.Count > 0)
            {
                report += "\n=== DIRETÓRIOS AUSENTES ===\n";
                foreach (var dir in missingDirs)
                {
                    report += $"• {dir}\n";
                }
            }
            
            return report;
        }
    }
}
