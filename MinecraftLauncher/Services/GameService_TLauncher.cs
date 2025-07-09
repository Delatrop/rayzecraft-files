using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MinecraftLauncher.Models;

namespace MinecraftLauncher.Services
{
    public class GameService_TLauncher
    {
        private readonly string _gameDirectory;
        private readonly ConfigService _configService;
        private readonly LogService _logService;

        public GameService_TLauncher()
        {
            _gameDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            _configService = new ConfigService();
            _logService = new LogService();
        }

        public async Task LaunchGameAsync()
        {
            await _logService.LogInfoAsync("=== INICIANDO MINECRAFT COM ESTRUTURA TLAUNCHER ===");
            
            var config = _configService.GetConfiguration();
            await _logService.LogInfoAsync($"Configuração carregada: {config.PlayerName}");

            try
            {
                // 1. Verificar Java
                var javaPath = GetJavaPath();
                await _logService.LogInfoAsync($"Java encontrado: {javaPath}");

                // 2. Verificar cliente Minecraft
                var minecraftClient = GetMinecraftClient();
                await _logService.LogInfoAsync($"Cliente Minecraft: {minecraftClient}");

                // 3. Construir classpath baseado no TLauncher
                var classpath = BuildTLauncherClasspath();
                await _logService.LogInfoAsync($"Classpath construído com {classpath.Split(';').Length} bibliotecas");

                // 4. Preparar diretórios
                await PrepareDirectoriesAsync();

                // 5. Construir argumentos JVM
                var jvmArgs = BuildJvmArguments(config, classpath);

                // 6. Construir argumentos do Minecraft
                var mcArgs = BuildMinecraftArguments(config);

                // 7. Comando completo
                var fullArgs = $"{jvmArgs} net.minecraft.launchwrapper.Launch {mcArgs}";

                await _logService.LogInfoAsync($"Argumentos construídos: {fullArgs.Substring(0, Math.Min(200, fullArgs.Length))}...");

                // 8. Executar
                var processInfo = new ProcessStartInfo
                {
                    FileName = javaPath,
                    Arguments = fullArgs,
                    WorkingDirectory = _gameDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false
                };

                await _logService.LogInfoAsync("Iniciando processo...");
                var process = Process.Start(processInfo);
                await _logService.LogSuccessAsync($"Processo iniciado com PID: {process.Id}");

                // Aguardar um pouco para verificar se o processo não sai imediatamente
                await Task.Delay(5000);

                if (process.HasExited)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    var fullError = string.IsNullOrEmpty(error) ? output : error;

                    await _logService.LogWarningAsync($"Processo saiu com código: {process.ExitCode}");
                    await _logService.LogWarningAsync($"Saída: {fullError.Substring(0, Math.Min(500, fullError.Length))}");
                    
                    throw new Exception($"Minecraft falhou ao iniciar: {fullError.Substring(0, Math.Min(200, fullError.Length))}");
                }
                else
                {
                    await _logService.LogSuccessAsync("Minecraft iniciado com sucesso!");
                }
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("Erro ao iniciar Minecraft", ex.Message);
                throw;
            }
        }

        private string GetJavaPath()
        {
            // Ordem de prioridade: TLauncher -> Sistema -> Padrão
            var javaPaths = new[]
            {
                Path.Combine(_gameDirectory, "runtime", "jre-legacy", "windows", "jre-legacy", "bin", "javaw.exe"),
                @"C:\Program Files\Java\jre1.8.0_431\bin\java.exe",
                @"C:\Program Files\Java\jre1.8.0_421\bin\java.exe",
                @"C:\Program Files\Java\jdk1.8.0_431\bin\java.exe",
                @"C:\Program Files (x86)\Java\jre1.8.0_431\bin\java.exe",
                "java"
            };

            foreach (var path in javaPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return "java"; // Fallback
        }

        private string GetMinecraftClient()
        {
            var clients = new[]
            {
                Path.Combine(_gameDirectory, "versions", "ForgeOptiFine 1.12.2", "ForgeOptiFine 1.12.2.jar"),
                Path.Combine(_gameDirectory, "versions", "1.12.2-forge-14.23.5.2854", "1.12.2-forge-14.23.5.2854.jar"),
                Path.Combine(_gameDirectory, "versions", "1.12.2-forge", "1.12.2-forge.jar"),
                Path.Combine(_gameDirectory, "versions", "1.12.2", "1.12.2.jar")
            };

            foreach (var client in clients)
            {
                if (File.Exists(client))
                {
                    return client;
                }
            }

            throw new FileNotFoundException("Cliente Minecraft não encontrado");
        }

        private string BuildTLauncherClasspath()
        {
            var libraries = new List<string>();
            var librariesDir = Path.Combine(_gameDirectory, "libraries");

            // Bibliotecas essenciais na ordem exata do TLauncher
            var essentialLibraries = new[]
            {
                // Forge principal
                "net/minecraftforge/forge/1.12.2-14.23.5.2854/forge-1.12.2-14.23.5.2854.jar",
                
                // ASM
                "org/ow2/asm/asm-debug-all/5.2/asm-debug-all-5.2.jar",
                
                // Launcher wrapper
                "net/minecraft/launchwrapper/1.12/launchwrapper-1.12.jar",
                
                // JLine
                "org/jline/jline/3.5.1/jline-3.5.1.jar",
                
                // Scala libraries
                "org/scala-lang/scala-library/2.11.1/scala-library-2.11.1.jar",
                "org/scala-lang/scala-compiler/2.11.1/scala-compiler-2.11.1.jar",
                "org/scala-lang/scala-parser-combinators_2.11/1.0.1/scala-parser-combinators_2.11-1.0.1.jar",
                "org/scala-lang/scala-swing_2.11/1.0.1/scala-swing_2.11-1.0.1.jar",
                "org/scala-lang/scala-xml_2.11/1.0.2/scala-xml_2.11-1.0.2.jar",
                
                // Compression
                "lzma/lzma/0.0.1/lzma-0.0.1.jar",
                
                // Math
                "java3d/vecmath/1.5.2/vecmath-1.5.2.jar",
                
                // Collections
                "net/sf/trove4j/trove4j/3.0.3/trove4j-3.0.3.jar",
                
                // Maven
                "org/apache/maven/maven-artifact/3.5.3/maven-artifact-3.5.3.jar",
                
                // JOpt Simple
                "net/sf/jopt-simple/jopt-simple/5.0.3/jopt-simple-5.0.3.jar",
                
                // Log4j
                "org/apache/logging/log4j/log4j-api/2.15.0/log4j-api-2.15.0.jar",
                "org/apache/logging/log4j/log4j-core/2.15.0/log4j-core-2.15.0.jar",
                
                // Patchy
                "org/tlauncher/patchy/1.3.9/patchy-1.3.9.jar",
                
                // System info
                "oshi-project/oshi-core/1.1/oshi-core-1.1.jar",
                
                // JNA
                "net/java/dev/jna/jna/4.4.0/jna-4.4.0.jar",
                "net/java/dev/jna/platform/3.4.0/platform-3.4.0.jar",
                
                // ICU
                "com/ibm/icu/icu4j-core-mojang/51.2/icu4j-core-mojang-51.2.jar",
                
                // Audio
                "com/paulscode/codecjorbis/20101023/codecjorbis-20101023.jar",
                "com/paulscode/codecwav/20101023/codecwav-20101023.jar",
                "com/paulscode/libraryjavasound/20101123/libraryjavasound-20101123.jar",
                "com/paulscode/librarylwjglopenal/20100824/librarylwjglopenal-20100824.jar",
                "com/paulscode/soundsystem/20120107/soundsystem-20120107.jar",
                
                // Netty
                "io/netty/netty-all/4.1.9.Final/netty-all-4.1.9.Final.jar",
                
                // Google Guava
                "com/google/guava/guava/21.0/guava-21.0.jar",
                
                // Apache Commons
                "org/apache/commons/commons-lang3/3.5/commons-lang3-3.5.jar",
                "commons-io/commons-io/2.5/commons-io-2.5.jar",
                "commons-codec/commons-codec/1.10/commons-codec-1.10.jar",
                
                // Input
                "net/java/jinput/jinput/2.0.5/jinput-2.0.5.jar",
                "net/java/jutils/jutils/1.0.0/jutils-1.0.0.jar",
                
                // JSON
                "com/google/code/gson/gson/2.8.0/gson-2.8.0.jar",
                
                // Auth
                "org/tlauncher/authlib/1.6.251/authlib-1.6.251.jar",
                
                // Realms
                "com/mojang/realms/1.10.22/realms-1.10.22.jar",
                
                // HTTP
                "org/apache/httpcomponents/httpclient/4.3.3/httpclient-4.3.3.jar",
                "commons-logging/commons-logging/1.1.3/commons-logging-1.1.3.jar",
                "org/apache/httpcomponents/httpcore/4.3.2/httpcore-4.3.2.jar",
                
                // FastUtil
                "it/unimi/dsi/fastutil/7.1.0/fastutil-7.1.0.jar",
                
                // LWJGL
                "org/lwjgl/lwjgl/lwjgl/2.9.4-nightly-20150209/lwjgl-2.9.4-nightly-20150209.jar",
                "org/lwjgl/lwjgl/lwjgl_util/2.9.4-nightly-20150209/lwjgl_util-2.9.4-nightly-20150209.jar",
                
                // Text to Speech
                "com/mojang/text2speech/1.10.3/text2speech-1.10.3.jar"
            };

            // Adicionar bibliotecas essenciais
            foreach (var lib in essentialLibraries)
            {
                var fullPath = Path.Combine(librariesDir, lib);
                if (File.Exists(fullPath))
                {
                    libraries.Add(fullPath);
                }
            }

            // Adicionar cliente Minecraft
            var minecraftClient = GetMinecraftClient();
            libraries.Add(minecraftClient);

            return string.Join(";", libraries.Select(lib => $"\"{lib}\""));
        }

        private string BuildJvmArguments(LauncherConfig config, string classpath)
        {
            var nativesDir = Path.Combine(_gameDirectory, "versions", "1.12.2-forge-14.23.5.2854", "natives");
            var assetsDir = Path.Combine(_gameDirectory, "assets");

            var args = new List<string>
            {
                $"-Dos.name=Windows 10",
                $"-Dos.version=10.0",
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
                $"-DlibraryDirectory=\"{Path.Combine(_gameDirectory, "libraries")}\"",
                $"-Dlog4j.configurationFile=\"{Path.Combine(assetsDir, "log_configs", "client-1.12.xml")}\""
            };

            return string.Join(" ", args);
        }

        private string BuildMinecraftArguments(LauncherConfig config)
        {
            var assetsDir = Path.Combine(_gameDirectory, "assets");
            var uuid = Guid.NewGuid().ToString();

            var args = new List<string>
            {
                $"--username {config.PlayerName}",
                "--version 1.12.2-forge-14.23.5.2854",
                $"--gameDir \"{_gameDirectory}\"",
                $"--assetsDir \"{assetsDir}\"",
                "--assetIndex 1.12",
                $"--uuid {uuid}",
                "--accessToken null",
                "--userType mojang",
                "--tweakClass net.minecraftforge.fml.common.launcher.FMLTweaker",
                "--versionType Forge"
            };

            if (config.WindowWidth > 0 && config.WindowHeight > 0)
            {
                args.Add($"--width {config.WindowWidth}");
                args.Add($"--height {config.WindowHeight}");
            }

            return string.Join(" ", args);
        }

        private Task PrepareDirectoriesAsync()
        {
            var directories = new[]
            {
                _gameDirectory,
                Path.Combine(_gameDirectory, "assets"),
                Path.Combine(_gameDirectory, "libraries"),
                Path.Combine(_gameDirectory, "versions"),
                Path.Combine(_gameDirectory, "versions", "1.12.2-forge-14.23.5.2854"),
                Path.Combine(_gameDirectory, "versions", "1.12.2-forge-14.23.5.2854", "natives"),
                Path.Combine(_gameDirectory, "mods"),
                Path.Combine(_gameDirectory, "config")
            };

            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            
            return Task.CompletedTask;
        }
    }
}
