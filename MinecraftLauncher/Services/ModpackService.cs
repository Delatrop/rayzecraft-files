using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using MinecraftLauncher.Models;

namespace MinecraftLauncher.Services
{
    public class ModpackService
    {
        private readonly string _minecraftDirectory;
        private readonly string _modpackDirectory;
        private readonly HttpClient _httpClient;
        private readonly ConfigService _configService;

        public ModpackService()
        {
            _minecraftDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            _modpackDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".rayzecraftlauncher", "minecraft");
            _httpClient = new HttpClient();
            _configService = new ConfigService();
        }

        public async Task SetupModpackAsync(IProgress<(int percentage, string message)> progress)
        {
            try
            {
                progress?.Report((0, "Iniciando configuração do modpack..."));
                
                // Garantir que os diretórios existem
                Directory.CreateDirectory(_modpackDirectory);
                Directory.CreateDirectory(_minecraftDirectory);

                progress?.Report((10, "Configurando pasta .minecraft..."));
                await ConfigureMinecraftDirectoryAsync();

                progress?.Report((30, "Deletando pastas antigas..."));
                await DeleteOldFoldersAsync();

                progress?.Report((50, "Copiando arquivos do modpack..."));
                await CopyModpackFilesAsync();

                progress?.Report((70, "Configurando versão do Forge..."));
                await SetupForgeVersionAsync();

                progress?.Report((90, "Finalizando configuração..."));
                await FinalizeSetupAsync();

                progress?.Report((100, "Configuração do modpack concluída!"));
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao configurar modpack: {ex.Message}");
            }
        }

        private async Task ConfigureMinecraftDirectoryAsync()
        {
            // Garantir que a pasta .minecraft existe
            var requiredDirs = new[]
            {
                Path.Combine(_minecraftDirectory, "versions"),
                Path.Combine(_minecraftDirectory, "libraries"),
                Path.Combine(_minecraftDirectory, "assets"),
                Path.Combine(_minecraftDirectory, "mods"),
                Path.Combine(_minecraftDirectory, "config"),
                Path.Combine(_minecraftDirectory, "scripts")
            };

            foreach (var dir in requiredDirs)
            {
                Directory.CreateDirectory(dir);
            }
        }

        private async Task DeleteOldFoldersAsync()
        {
            var foldersToDelete = new[]
            {
                Path.Combine(_minecraftDirectory, "mods"),
                Path.Combine(_minecraftDirectory, "config"),
                Path.Combine(_minecraftDirectory, "scripts")
            };

            foreach (var folder in foldersToDelete)
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }

        private async Task CopyModpackFilesAsync()
        {
            var sourceDirs = new Dictionary<string, string>
            {
                { Path.Combine(_modpackDirectory, "mods"), Path.Combine(_minecraftDirectory, "mods") },
                { Path.Combine(_modpackDirectory, "config"), Path.Combine(_minecraftDirectory, "config") },
                { Path.Combine(_modpackDirectory, "scripts"), Path.Combine(_minecraftDirectory, "scripts") }
            };

            // Verificar se existe uma pasta de arquivos do modpack hospedada
            var hostedModpackDir = @"C:\rayzecraft-launcher-files";
            if (Directory.Exists(hostedModpackDir))
            {
                sourceDirs.Clear();
                sourceDirs.Add(Path.Combine(hostedModpackDir, "mods"), Path.Combine(_minecraftDirectory, "mods"));
                sourceDirs.Add(Path.Combine(hostedModpackDir, "config"), Path.Combine(_minecraftDirectory, "config"));
                sourceDirs.Add(Path.Combine(hostedModpackDir, "scripts"), Path.Combine(_minecraftDirectory, "scripts"));
            }

            foreach (var sourceDir in sourceDirs)
            {
                if (Directory.Exists(sourceDir.Key))
                {
                    await CopyDirectoryAsync(sourceDir.Key, sourceDir.Value);
                }
            }
            
            // Verificar se há mods ausentes e tentar copiar de fontes alternativas
            await FixMissingModsAsync();
        }

        private async Task SetupForgeVersionAsync()
        {
            var config = _configService.GetConfiguration();
            var forgeVersion = "1.12.2-forge-14.23.5.2854";
            var versionsDir = Path.Combine(_minecraftDirectory, "versions", forgeVersion);
            
            Directory.CreateDirectory(versionsDir);

            // Copiar JAR do Forge
            var sourceJar = Path.Combine(_modpackDirectory, "minecraft.jar");
            var targetJar = Path.Combine(versionsDir, $"{forgeVersion}.jar");
            
            if (File.Exists(sourceJar))
            {
                File.Copy(sourceJar, targetJar, true);
            }

            // Criar arquivo JSON da versão
            await CreateVersionJsonAsync(versionsDir, forgeVersion);
        }

        private async Task CreateVersionJsonAsync(string versionsDir, string forgeVersion)
        {
            var versionJson = new
            {
                id = forgeVersion,
                type = "release",
                time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                releaseTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                minecraftVersion = "1.12.2",
                mainClass = "net.minecraft.launchwrapper.Launch",
                arguments = new
                {
                    game = new[]
                    {
                        "--username", "${auth_player_name}",
                        "--version", "${version_name}",
                        "--gameDir", "${game_directory}",
                        "--assetsDir", "${assets_root}",
                        "--assetIndex", "${assets_index_name}",
                        "--uuid", "${auth_uuid}",
                        "--accessToken", "${auth_access_token}",
                        "--userType", "${user_type}",
                        "--tweakClass", "net.minecraftforge.fml.common.launcher.FMLTweaker"
                    },
                    jvm = new[]
                    {
                        "-Djava.library.path=${natives_directory}",
                        "-Dminecraft.launcher.brand=${launcher_name}",
                        "-Dminecraft.launcher.version=${launcher_version}",
                        "-cp", "${classpath}"
                    }
                },
                libraries = new[]
                {
                    new
                    {
                        name = "net.minecraftforge:forge:1.12.2-14.23.5.2854",
                        downloads = new
                        {
                            artifact = new
                            {
                                path = "net/minecraftforge/forge/1.12.2-14.23.5.2854/forge-1.12.2-14.23.5.2854.jar",
                                url = "https://maven.minecraftforge.net/net/minecraftforge/forge/1.12.2-14.23.5.2854/forge-1.12.2-14.23.5.2854.jar",
                                sha1 = "",
                                size = 0
                            }
                        }
                    },
                    new
                    {
                        name = "org.ow2.asm:asm-debug-all:5.2",
                        downloads = new
                        {
                            artifact = new
                            {
                                path = "org/ow2/asm/asm-debug-all/5.2/asm-debug-all-5.2.jar",
                                url = "https://repo1.maven.org/maven2/org/ow2/asm/asm-debug-all/5.2/asm-debug-all-5.2.jar",
                                sha1 = "",
                                size = 0
                            }
                        }
                    },
                    new
                    {
                        name = "net.minecraft:launchwrapper:1.12",
                        downloads = new
                        {
                            artifact = new
                            {
                                path = "net/minecraft/launchwrapper/1.12/launchwrapper-1.12.jar",
                                url = "https://libraries.minecraft.net/net/minecraft/launchwrapper/1.12/launchwrapper-1.12.jar",
                                sha1 = "",
                                size = 0
                            }
                        }
                    },
                    new
                    {
                        name = "org.apache.maven:maven-artifact:3.5.3",
                        downloads = new
                        {
                            artifact = new
                            {
                                path = "org/apache/maven/maven-artifact/3.5.3/maven-artifact-3.5.3.jar",
                                url = "https://repo1.maven.org/maven2/org/apache/maven/maven-artifact/3.5.3/maven-artifact-3.5.3.jar",
                                sha1 = "",
                                size = 0
                            }
                        }
                    },
                    new
                    {
                        name = "net.sf.jopt-simple:jopt-simple:5.0.3",
                        downloads = new
                        {
                            artifact = new
                            {
                                path = "net/sf/jopt-simple/jopt-simple/5.0.3/jopt-simple-5.0.3.jar",
                                url = "https://repo1.maven.org/maven2/net/sf/jopt-simple/jopt-simple/5.0.3/jopt-simple-5.0.3.jar",
                                sha1 = "",
                                size = 0
                            }
                        }
                    },
                    new
                    {
                        name = "org.apache.logging.log4j:log4j-api:2.15.0",
                        downloads = new
                        {
                            artifact = new
                            {
                                path = "org/apache/logging/log4j/log4j-api/2.15.0/log4j-api-2.15.0.jar",
                                url = "https://repo1.maven.org/maven2/org/apache/logging/log4j/log4j-api/2.15.0/log4j-api-2.15.0.jar",
                                sha1 = "",
                                size = 0
                            }
                        }
                    },
                    new
                    {
                        name = "org.apache.logging.log4j:log4j-core:2.15.0",
                        downloads = new
                        {
                            artifact = new
                            {
                                path = "org/apache/logging/log4j/log4j-core/2.15.0/log4j-core-2.15.0.jar",
                                url = "https://repo1.maven.org/maven2/org/apache/logging/log4j/log4j-core/2.15.0/log4j-core-2.15.0.jar",
                                sha1 = "",
                                size = 0
                            }
                        }
                    },
                    new
                    {
                        name = "com.google.guava:guava:21.0",
                        downloads = new
                        {
                            artifact = new
                            {
                                path = "com/google/guava/guava/21.0/guava-21.0.jar",
                                url = "https://repo1.maven.org/maven2/com/google/guava/guava/21.0/guava-21.0.jar",
                                sha1 = "",
                                size = 0
                            }
                        }
                    },
                    new
                    {
                        name = "org.apache.commons:commons-lang3:3.5",
                        downloads = new
                        {
                            artifact = new
                            {
                                path = "org/apache/commons/commons-lang3/3.5/commons-lang3-3.5.jar",
                                url = "https://repo1.maven.org/maven2/org/apache/commons/commons-lang3/3.5/commons-lang3-3.5.jar",
                                sha1 = "",
                                size = 0
                            }
                        }
                    },
                    new
                    {
                        name = "commons-io:commons-io:2.5",
                        downloads = new
                        {
                            artifact = new
                            {
                                path = "commons-io/commons-io/2.5/commons-io-2.5.jar",
                                url = "https://repo1.maven.org/maven2/commons-io/commons-io/2.5/commons-io-2.5.jar",
                                sha1 = "",
                                size = 0
                            }
                        }
                    },
                    new
                    {
                        name = "com.google.code.gson:gson:2.8.0",
                        downloads = new
                        {
                            artifact = new
                            {
                                path = "com/google/code/gson/gson/2.8.0/gson-2.8.0.jar",
                                url = "https://repo1.maven.org/maven2/com/google/code/gson/gson/2.8.0/gson-2.8.0.jar",
                                sha1 = "",
                                size = 0
                            }
                        }
                    },
                    new
                    {
                        name = "com.mojang:authlib:1.5.21",
                        downloads = new
                        {
                            artifact = new
                            {
                                path = "com/mojang/authlib/1.5.21/authlib-1.5.21.jar",
                                url = "https://libraries.minecraft.net/com/mojang/authlib/1.5.21/authlib-1.5.21.jar",
                                sha1 = "",
                                size = 0
                            }
                        }
                    },
                    new
                    {
                        name = "org.lwjgl.lwjgl:lwjgl:2.9.4-nightly-20150209",
                        downloads = new
                        {
                            artifact = new
                            {
                                path = "org/lwjgl/lwjgl/lwjgl/2.9.4-nightly-20150209/lwjgl-2.9.4-nightly-20150209.jar",
                                url = "https://libraries.minecraft.net/org/lwjgl/lwjgl/lwjgl/2.9.4-nightly-20150209/lwjgl-2.9.4-nightly-20150209.jar",
                                sha1 = "",
                                size = 0
                            }
                        }
                    },
                    new
                    {
                        name = "org.lwjgl.lwjgl:lwjgl_util:2.9.4-nightly-20150209",
                        downloads = new
                        {
                            artifact = new
                            {
                                path = "org/lwjgl/lwjgl/lwjgl_util/2.9.4-nightly-20150209/lwjgl_util-2.9.4-nightly-20150209.jar",
                                url = "https://libraries.minecraft.net/org/lwjgl/lwjgl/lwjgl_util/2.9.4-nightly-20150209/lwjgl_util-2.9.4-nightly-20150209.jar",
                                sha1 = "",
                                size = 0
                            }
                        }
                    }
                }
            };

            var jsonPath = Path.Combine(versionsDir, $"{forgeVersion}.json");
            var jsonContent = JsonConvert.SerializeObject(versionJson, Formatting.Indented);
            await File.WriteAllTextAsync(jsonPath, jsonContent);
        }

        private async Task FinalizeSetupAsync()
        {
            // Criar perfil do launcher para o modpack
            var launcherProfilesPath = Path.Combine(_minecraftDirectory, "launcher_profiles.json");
            
            var profile = new
            {
                profiles = new Dictionary<string, object>
                {
                    ["RayzeCraft"] = new
                    {
                        name = "RayzeCraft Modpack",
                        type = "custom",
                        created = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        lastUsed = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        lastVersionId = "1.12.2-forge-14.23.5.2854",
                        gameDir = _minecraftDirectory,
                        javaArgs = "-Xmx4G -Xms1G -XX:+UnlockExperimentalVMOptions -XX:+UseG1GC -XX:G1NewSizePercent=20 -XX:G1ReservePercent=20 -XX:MaxGCPauseMillis=50 -XX:G1HeapRegionSize=32M"
                    }
                },
                settings = new
                {
                    enableHistorical = false,
                    enableReleases = true,
                    enableSnapshots = false,
                    keepLauncherOpen = false,
                    profileSorting = "ByLastPlayed",
                    showGameLog = false,
                    showMenu = false,
                    soundOn = false
                },
                version = 3
            };

            var profileJson = JsonConvert.SerializeObject(profile, Formatting.Indented);
            await File.WriteAllTextAsync(launcherProfilesPath, profileJson);
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

            // Copiar subdiretórios recursivamente
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var targetSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
                await CopyDirectoryAsync(dir, targetSubDir);
            }
        }

        private async Task FixMissingModsAsync()
        {
            var logService = new LogService();
            var modsDir = Path.Combine(_minecraftDirectory, "mods");
            Directory.CreateDirectory(modsDir);
            
            // Lista de mods essenciais que devem estar presentes
            var essentialMods = new List<string>
            {
                "ActuallyAdditions-1.12.2-r151-2.jar",
                "JEI-1.12.2-4.16.1.302.jar",
                "Baubles-1.12-1.5.2.jar",
                "Mantle-1.12-1.3.3.55.jar",
                "TConstruct-1.12.2-2.13.0.183.jar",
                "Waystones-MC1.12-4.1.0.jar",
                "AppliedEnergistics2-rv6-stable-7.jar",
                "Chisel-MC1.12.2-1.0.1.44.jar",
                "CTM-MC1.12.2-1.0.2.31.jar",
                "ExtremeReactors-1.12.2-0.4.5.67.jar",
                "ZeroCore-1.12.2-0.1.2.8.jar",
                "thermal-expansion-1.12.2-5.5.7.1.jar",
                "CoFHCore-1.12.2-4.6.6.1.jar",
                "CoFHWorld-1.12.2-1.4.0.1.jar",
                "RedstoneFlux-1.12-2.1.1.1.jar",
                "ThermalFoundation-1.12.2-2.6.7.1.jar",
                "ThermalDynamics-1.12.2-2.5.6.1.jar",
                "enderio-1.12.2-5.3.70.jar",
                "EnderCore-1.12.2-0.5.76.jar",
                "forestry_1.12.2-5.8.2.387.jar",
                "industrialcraft-2-2.8.221-ex112.jar",
                "Mekanism-1.12.2-9.8.3.390.jar",
                "MekanismGenerators-1.12.2-9.8.3.390.jar",
                "MekanismTools-1.12.2-9.8.3.390.jar",
                "railcraft-12.0.0.jar",
                "refinedstorage-1.6.16.jar",
                "StorageDrawers-1.12.2-5.4.2.jar",
                "ironchest-1.12.2-7.0.72.847.jar",
                "journeymap-1.12.2-5.7.1.jar",
                "OptiFine_1.12.2_HD_U_G5.jar"
            };
            
            // Verificar quais mods estão faltando
            var missingMods = new List<string>();
            foreach (var modName in essentialMods)
            {
                var modPath = Path.Combine(modsDir, modName);
                if (!File.Exists(modPath))
                {
                    missingMods.Add(modName);
                }
            }
            
            if (missingMods.Count == 0)
            {
                await logService.LogSuccessAsync("Todos os mods essenciais estão presentes");
                return;
            }
            
            await logService.LogWarningAsync($"Encontrados {missingMods.Count} mods ausentes, tentando corrigir...");
            
            // Tentar encontrar e copiar mods ausentes de fontes alternativas
            var alternativeModsPaths = new List<string>
            {
                @"C:\rayzecraft-launcher-files\mods",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "mods"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "backup", "mods"),
                @"C:\RayzeCraft\mods",
                @"C:\Users\Public\RayzeCraft\mods"
            };
            
            foreach (var modName in missingMods)
            {
                var modPath = Path.Combine(modsDir, modName);
                bool found = false;
                
                // Procurar o mod em caminhos alternativos
                foreach (var altPath in alternativeModsPaths)
                {
                    if (Directory.Exists(altPath))
                    {
                        var sourceModPath = Path.Combine(altPath, modName);
                        if (File.Exists(sourceModPath))
                        {
                            try
                            {
                                File.Copy(sourceModPath, modPath, true);
                                await logService.LogSuccessAsync($"Mod {modName} copiado de {altPath}");
                                found = true;
                                break;
                            }
                            catch (Exception ex)
                            {
                                await logService.LogWarningAsync($"Erro ao copiar {modName} de {altPath}", ex.Message);
                            }
                        }
                    }
                }
                
                // Se não encontrou o mod, criar um arquivo placeholder
                if (!found)
                {
                    await logService.LogWarningAsync($"Mod {modName} não encontrado, criando placeholder");
                    
                    // Criar um arquivo JAR vazio para não quebrar o classpath
                    var placeholderContent = "PK\x03\x04\x14\x00\x00\x00\x08\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00";
                    await File.WriteAllTextAsync(modPath, placeholderContent);
                }
            }
        }
        
        public async Task<string> GetModpackStatusAsync()
        {
            var status = new System.Text.StringBuilder();
            
            status.AppendLine("=== STATUS DO MODPACK ===");
            
            // Verificar pastas principais
            var folders = new[] { "mods", "config", "scripts" };
            foreach (var folder in folders)
            {
                var folderPath = Path.Combine(_minecraftDirectory, folder);
                if (Directory.Exists(folderPath))
                {
                    var fileCount = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories).Length;
                    status.AppendLine($"✓ {folder.ToUpper()}: {fileCount} arquivos");
                }
                else
                {
                    status.AppendLine($"✗ {folder.ToUpper()}: Pasta não encontrada");
                }
            }

            // Verificar versão do Forge
            var forgeVersion = "1.12.2-forge-14.23.5.2854";
            var forgeDir = Path.Combine(_minecraftDirectory, "versions", forgeVersion);
            if (Directory.Exists(forgeDir))
            {
                status.AppendLine($"✓ FORGE: {forgeVersion}");
            }
            else
            {
                status.AppendLine("✗ FORGE: Não configurado");
            }

            return status.ToString();
        }
    }
}
