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
    public class TLauncherGameServiceFixed
    {
        private readonly string _gameDirectory;
        private readonly string _launcherDirectory;
        private readonly ConfigService _configService;
        private readonly LogService _logService;
        private readonly string _launcherVersion = "1.12.2-forge-14.23.5.2854";

        public TLauncherGameServiceFixed()
        {
            _gameDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            _launcherDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".rayzecraftlauncher");
            _configService = new ConfigService();
            _logService = new LogService();
        }

        public async Task<bool> VerifyGameIntegrityAsync()
        {
            await _logService.LogInfoAsync("=== VERIFICAÇÃO DE INTEGRIDADE DETALHADA ===");
            
            var issues = new List<string>();
            
            // 1. Verificar Java com logs detalhados
            var javaPath = GetJavaPath();
            if (string.IsNullOrEmpty(javaPath))
            {
                issues.Add("Java não encontrado");
                await _logService.LogErrorAsync("❌ Java não encontrado em nenhum local");
                await LogJavaSearchDetails();
            }
            else
            {
                await _logService.LogInfoAsync($"✅ Java encontrado: {javaPath}");
                await ValidateJavaVersion(javaPath);
            }

            // 2. Verificar cliente Minecraft
            var clientPath = GetMinecraftClientPath();
            if (string.IsNullOrEmpty(clientPath))
            {
                issues.Add("Cliente Minecraft não encontrado");
                await _logService.LogErrorAsync("❌ Cliente Minecraft não encontrado");
                await LogClientSearchDetails();
            }
            else
            {
                await _logService.LogInfoAsync($"✅ Cliente Minecraft: {clientPath}");
                var fileInfo = new FileInfo(clientPath);
                await _logService.LogInfoAsync($"   Tamanho: {fileInfo.Length} bytes");
            }

            // 3. Verificar bibliotecas essenciais
            var missingLibs = await VerifyEssentialLibrariesAsync();
            if (missingLibs.Count > 0)
            {
                issues.Add($"{missingLibs.Count} bibliotecas essenciais ausentes");
                await _logService.LogWarningAsync($"❌ {missingLibs.Count} bibliotecas essenciais ausentes:");
                foreach (var lib in missingLibs.Take(10))
                {
                    await _logService.LogWarningAsync($"   - {Path.GetFileName(lib)}");
                }
            }
            else
            {
                await _logService.LogInfoAsync("✅ Todas as bibliotecas essenciais estão presentes");
            }

            // 4. Verificar diretórios críticos
            await VerifyDirectoriesAsync();

            // 5. Verificar arquivo de versão JSON
            await VerifyVersionJsonAsync();

            if (issues.Count > 0)
            {
                await _logService.LogErrorAsync("❌ Problemas encontrados:", string.Join(", ", issues));
                return false;
            }

            await _logService.LogSuccessAsync("✅ Verificação de integridade concluída com sucesso");
            return true;
        }

        public async Task LaunchGameAsync()
        {
            await _logService.LogInfoAsync("=== INICIANDO MINECRAFT (PADRÃO TLAUNCHER CORRIGIDO) ===");
            
            var config = _configService.GetConfiguration();
            await _logService.LogInfoAsync($"Jogador: {config.PlayerName}");
            await _logService.LogInfoAsync($"Memória: {config.MaxMemory}MB");
            await _logService.LogInfoAsync($"Java configurado: {config.JavaPath ?? "Detecção automática"}");

            try
            {
                // 1. Verificação completa antes de lançar
                await _logService.LogInfoAsync("Executando verificação de integridade...");
                bool integrityOk = await VerifyGameIntegrityAsync();
                if (!integrityOk)
                {
                    throw new Exception("Verificação de integridade falhou. Verifique os logs para detalhes.");
                }

                // 2. Obter e validar Java
                var javaPath = GetJavaPath();
                if (string.IsNullOrEmpty(javaPath))
                {
                    throw new FileNotFoundException("Java não encontrado. Configure o caminho do Java nas configurações ou instale o Java 8.");
                }

                if (!File.Exists(javaPath))
                {
                    throw new FileNotFoundException($"Java não encontrado no caminho: {javaPath}");
                }

                await _logService.LogInfoAsync($"✅ Java validado: {javaPath}");

                // 3. Obter e validar cliente Minecraft
                var clientPath = GetMinecraftClientPath();
                if (string.IsNullOrEmpty(clientPath) || !File.Exists(clientPath))
                {
                    throw new FileNotFoundException("Cliente Minecraft não encontrado. Execute uma atualização primeiro.");
                }

                await _logService.LogInfoAsync($"✅ Cliente Minecraft validado: {clientPath}");

                // 4. Construir classpath
                var classpath = await BuildClasspathAsync();
                await _logService.LogInfoAsync($"✅ Classpath construído com {classpath.Split(';').Length} componentes");

                // 5. Construir argumentos
                var jvmArgs = BuildJvmArguments(config, classpath);
                var mcArgs = BuildMinecraftArguments(config);

                // 6. Comando completo
                var fullArgs = $"{jvmArgs} net.minecraft.launchwrapper.Launch {mcArgs}";

                // 7. Salvar comando em arquivo de log
                await SaveLaunchCommandToFile(javaPath, fullArgs);

                // 8. Executar com logs detalhados
                await ExecuteMinecraftWithDetailedLogging(javaPath, fullArgs);

                await _logService.LogSuccessAsync("✅ Minecraft lançado com sucesso!");
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("❌ Erro ao lançar Minecraft", ex.Message);
                await _logService.LogErrorAsync("Stack trace:", ex.StackTrace);
                throw;
            }
        }

        private async Task ValidateJavaVersion(string javaPath)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = javaPath,
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit(10000);
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    var versionInfo = string.IsNullOrEmpty(error) ? output : error;

                    if (process.ExitCode == 0)
                    {
                        await _logService.LogInfoAsync($"✅ Java validado com sucesso:");
                        await _logService.LogInfoAsync($"   Versão: {versionInfo.Split('\n')[0]}");
                    }
                    else
                    {
                        await _logService.LogWarningAsync($"⚠️ Java respondeu com código {process.ExitCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                await _logService.LogWarningAsync($"⚠️ Erro ao validar Java: {ex.Message}");
            }
        }

        private async Task LogJavaSearchDetails()
        {
            await _logService.LogInfoAsync("Locais verificados para Java:");
            var config = _configService.GetConfiguration();
            
            if (!string.IsNullOrEmpty(config.JavaPath))
            {
                await _logService.LogInfoAsync($"   Configurado: {config.JavaPath} - {(File.Exists(config.JavaPath) ? "✅" : "❌")}");
            }

            var javaPaths = new[]
            {
                Path.Combine(_gameDirectory, "runtime", "jre-legacy", "windows", "jre-legacy", "bin", "javaw.exe"),
                @"C:\Program Files\Java\jre1.8.0_431\bin\java.exe",
                @"C:\Program Files\Java\jdk1.8.0_431\bin\java.exe",
                @"C:\Program Files (x86)\Java\jre1.8.0_431\bin\java.exe"
            };

            foreach (var path in javaPaths)
            {
                await _logService.LogInfoAsync($"   {path} - {(File.Exists(path) ? "✅" : "❌")}");
            }
        }

        private async Task LogClientSearchDetails()
        {
            await _logService.LogInfoAsync("Locais verificados para cliente Minecraft:");
            var clientPaths = new[]
            {
                Path.Combine(_gameDirectory, "versions", "ForgeOptiFine 1.12.2", "ForgeOptiFine 1.12.2.jar"),
                Path.Combine(_gameDirectory, "versions", _launcherVersion, $"{_launcherVersion}.jar"),
                Path.Combine(_gameDirectory, "versions", "1.12.2-forge", "1.12.2-forge.jar"),
                Path.Combine(_gameDirectory, "versions", "1.12.2", "1.12.2.jar"),
                Path.Combine(_gameDirectory, "minecraft.jar")
            };

            foreach (var path in clientPaths)
            {
                await _logService.LogInfoAsync($"   {path} - {(File.Exists(path) ? "✅" : "❌")}");
            }
        }

        private async Task VerifyDirectoriesAsync()
        {
            var requiredDirs = new[]
            {
                Path.Combine(_gameDirectory, "assets"),
                Path.Combine(_gameDirectory, "libraries"),
                Path.Combine(_gameDirectory, "versions"),
                Path.Combine(_gameDirectory, "mods")
            };

            foreach (var dir in requiredDirs)
            {
                if (!Directory.Exists(dir))
                {
                    await _logService.LogWarningAsync($"❌ Diretório ausente: {dir}");
                    Directory.CreateDirectory(dir);
                    await _logService.LogInfoAsync($"✅ Diretório criado: {dir}");
                }
                else
                {
                    await _logService.LogInfoAsync($"✅ Diretório ok: {dir}");
                }
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
                @"C:\Program Files\Java\jdk1.8.0_431\bin\java.exe",
                @"C:\Program Files (x86)\Java\jre1.8.0_431\bin\java.exe",
                
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
                            process.WaitForExit(5000);
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
                "org/apache/maven/maven-artifact/3.5.3/maven-artifact-3.5.3.jar",
                "lzma/lzma/0.0.1/lzma-0.0.1.jar",
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

            // Adicionar bibliotecas essenciais que existem
            foreach (var lib in essentialLibraries)
            {
                var fullPath = Path.Combine(librariesDir, lib);
                if (File.Exists(fullPath))
                {
                    classpathComponents.Add(fullPath);
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
            
            await _logService.LogInfoAsync($"Classpath final: {classpath.Length} caracteres");
            
            return classpath;
        }

        private string BuildJvmArguments(LauncherConfig config, string classpath)
        {
            var nativesDir = Path.Combine(_gameDirectory, "versions", _launcherVersion, "natives");
            var assetsDir = Path.Combine(_gameDirectory, "assets");
            var librariesDir = Path.Combine(_gameDirectory, "libraries");

            var args = new List<string>
            {
                $"-Xmx{config.MaxMemory}M",
                $"-Xms{config.MinMemory}M",
                $"-Djava.library.path=\"{nativesDir}\"",
                $"-cp {classpath}",
                "-Dfml.ignoreInvalidMinecraftCertificates=true",
                "-Dfml.ignorePatchDiscrepancies=true",
                "-Djava.net.preferIPv4Stack=true",
                $"-Dminecraft.applet.TargetDirectory=\"{_gameDirectory}\"",
                $"-DlibraryDirectory=\"{librariesDir}\""
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

        private async Task SaveLaunchCommandToFile(string javaPath, string arguments)
        {
            try
            {
                var launchLogPath = Path.Combine(_launcherDirectory, "launch.log");
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                var logContent = $"[{timestamp}] COMANDO DE LANÇAMENTO:\n";
                logContent += $"Java: {javaPath}\n";
                logContent += $"Diretório: {_gameDirectory}\n";
                logContent += $"Argumentos: {arguments}\n";
                logContent += $"Comprimento total: {arguments.Length} caracteres\n";
                logContent += new string('=', 80) + "\n\n";

                await File.AppendAllTextAsync(launchLogPath, logContent);
                await _logService.LogInfoAsync($"✅ Comando salvo em: {launchLogPath}");
            }
            catch (Exception ex)
            {
                await _logService.LogWarningAsync($"⚠️ Erro ao salvar comando: {ex.Message}");
            }
        }

        private async Task ExecuteMinecraftWithDetailedLogging(string javaPath, string arguments)
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

            await _logService.LogInfoAsync("🚀 Iniciando processo do Minecraft...");
            await _logService.LogInfoAsync($"   Executável: {javaPath}");
            await _logService.LogInfoAsync($"   Diretório: {_gameDirectory}");
            
            var process = Process.Start(processInfo);
            await _logService.LogInfoAsync($"✅ Processo iniciado com PID: {process.Id}");

            // Aguardar um tempo para verificar se o processo não sai imediatamente
            await Task.Delay(10000); // 10 segundos para dar tempo ao Minecraft iniciar

            if (process.HasExited)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await _logService.LogErrorAsync($"❌ Processo saiu com código: {process.ExitCode}");
                
                // Salvar saída completa em arquivo
                await SaveProcessOutputToFile(process.ExitCode, output, error);
                
                if (!string.IsNullOrEmpty(error))
                {
                    await _logService.LogErrorAsync("ERRO DO JAVA:");
                    await _logService.LogErrorAsync(error);
                }
                
                if (!string.IsNullOrEmpty(output))
                {
                    await _logService.LogInfoAsync("SAÍDA DO JAVA:");
                    await _logService.LogInfoAsync(output);
                }

                // Analisar erro específico
                var errorMessage = AnalyzeJavaError(process.ExitCode, output, error);
                throw new Exception($"Minecraft falhou ao iniciar (código {process.ExitCode}): {errorMessage}");
            }
            else
            {
                await _logService.LogSuccessAsync("🎮 Minecraft iniciado com sucesso e ainda está executando!");
            }
        }

        private async Task SaveProcessOutputToFile(int exitCode, string output, string error)
        {
            try
            {
                var outputLogPath = Path.Combine(_launcherDirectory, "minecraft_output.log");
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                var logContent = $"[{timestamp}] SAÍDA DO MINECRAFT:\n";
                logContent += $"Código de saída: {exitCode}\n";
                logContent += $"ERRO:\n{error}\n";
                logContent += $"SAÍDA:\n{output}\n";
                logContent += new string('=', 80) + "\n\n";

                await File.AppendAllTextAsync(outputLogPath, logContent);
                await _logService.LogInfoAsync($"✅ Saída salva em: {outputLogPath}");
            }
            catch (Exception ex)
            {
                await _logService.LogWarningAsync($"⚠️ Erro ao salvar saída: {ex.Message}");
            }
        }

        private string AnalyzeJavaError(int exitCode, string output, string error)
        {
            var fullOutput = $"{output}\n{error}".ToLower();
            
            if (fullOutput.Contains("could not find or load main class"))
            {
                return "Classe principal não encontrada. Verifique se o launchwrapper está presente.";
            }
            else if (fullOutput.Contains("noclassdeffounderror"))
            {
                return "Bibliotecas ausentes. Execute 'Verificar Integridade' para corrigir.";
            }
            else if (fullOutput.Contains("outofmemoryerror"))
            {
                return "Memória insuficiente. Diminua a RAM nas configurações.";
            }
            else if (fullOutput.Contains("accessdeniedexception"))
            {
                return "Acesso negado. Execute o launcher como administrador.";
            }
            else if (fullOutput.Contains("joptsimple"))
            {
                return "Biblioteca jopt-simple ausente. Instale as bibliotecas necessárias.";
            }
            else if (exitCode == 1)
            {
                return "Erro geral do Java. Verifique os logs para mais detalhes.";
            }
            else
            {
                return $"Erro desconhecido (código {exitCode}). Verifique os logs.";
            }
        }

        private async Task VerifyVersionJsonAsync()
        {
            var versionJsonPath = Path.Combine(_gameDirectory, "versions", _launcherVersion, $"{_launcherVersion}.json");
            
            if (!File.Exists(versionJsonPath))
            {
                await _logService.LogWarningAsync($"❌ Arquivo de versão não encontrado: {versionJsonPath}");
                await CreateVersionJsonAsync(versionJsonPath);
            }
            else
            {
                await _logService.LogInfoAsync($"✅ Arquivo de versão encontrado: {versionJsonPath}");
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
                jar = "1.12.2"
            };

            var jsonContent = JsonConvert.SerializeObject(versionData, Formatting.Indented);
            Directory.CreateDirectory(Path.GetDirectoryName(jsonPath));
            await File.WriteAllTextAsync(jsonPath, jsonContent);
            await _logService.LogInfoAsync($"✅ Arquivo de versão criado: {jsonPath}");
        }

        public async Task<string> GetIntegrityReportAsync()
        {
            var report = "=== RELATÓRIO DE INTEGRIDADE DETALHADO ===\n\n";
            
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
