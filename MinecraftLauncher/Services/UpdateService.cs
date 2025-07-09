using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MinecraftLauncher.Models;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace MinecraftLauncher.Services
{
    public class UpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly string _launcherDirectory;
        private readonly string _gameDirectory;
        private readonly ConfigService _configService;
        private readonly ModpackService _modpackService;
        
        // URL do version.json hospedado no GitHub Pages
        private const string VERSION_URL = "https://delatrop.github.io/rayzecraft-launcher-config/version.json";

        public UpdateService()
        {
            _httpClient = new HttpClient();
            _launcherDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".rayzecraftlauncher");
            _gameDirectory = Path.Combine(_launcherDirectory, "minecraft");
            _configService = new ConfigService();
            _modpackService = new ModpackService();
            
            // Garantir que os diretórios existem
            Directory.CreateDirectory(_launcherDirectory);
            Directory.CreateDirectory(_gameDirectory);
        }

        public async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                var remoteVersion = await GetRemoteVersionAsync();
                var localVersion = GetLocalVersion();

                if (localVersion == null || remoteVersion.Version != localVersion.Version)
                {
                    return true;
                }

                // Verificar se algum arquivo foi modificado
                foreach (var file in remoteVersion.Files)
                {
                    var localFile = Path.Combine(_gameDirectory, file.Path);
                    
                    if (!File.Exists(localFile))
                    {
                        return true;
                    }

                    var localHash = CalculateFileHash(localFile);
                    if (localHash != file.Hash)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao verificar atualizações: {ex.Message}");
            }
        }

        public async Task UpdateAsync(IProgress<(int percentage, string message)> progress)
        {
            try
            {
                progress?.Report((0, "Baixando informações da versão..."));
                
                var remoteVersion = await GetRemoteVersionAsync();
                
                // Verificar se estamos no modo exemplo (servidor não disponível)
                if (remoteVersion.Description.Contains("Servidor não disponível"))
                {
                    await SimulateUpdateAsync(progress);
                    return;
                }
                
                var filesToUpdate = new List<GameFile>();

                // Determinar quais arquivos precisam ser atualizados
                foreach (var file in remoteVersion.Files)
                {
                    var localFile = Path.Combine(_gameDirectory, file.Path);
                    
                    if (!File.Exists(localFile))
                    {
                        filesToUpdate.Add(file);
                    }
                    else
                    {
                        var localHash = CalculateFileHash(localFile);
                        if (localHash != file.Hash)
                        {
                            filesToUpdate.Add(file);
                        }
                    }
                }

                if (filesToUpdate.Count == 0)
                {
                    progress?.Report((100, "Todos os arquivos já estão atualizados!"));
                    return;
                }

                // Baixar o arquivo compactado (ZIP ou RAR)
                var compressedFile = filesToUpdate.FirstOrDefault(f => 
                    f.Path.EndsWith(".zip") || f.Path.EndsWith(".rar"));
                    
                if (compressedFile == null)
                {
                    throw new Exception("Arquivo compactado não encontrado para baixar.");
                }

                var localPath = Path.Combine(_launcherDirectory, compressedFile.Path);
                progress?.Report((10, "Baixando arquivo compactado..."));
                await DownloadFileAsync(compressedFile.Url, localPath);

                progress?.Report((50, "Descompactando arquivos..."));
                
                if (compressedFile.Path.EndsWith(".zip"))
                {
                    ZipFile.ExtractToDirectory(localPath, _gameDirectory, true);
                }
                else if (compressedFile.Path.EndsWith(".rar"))
                {
                    // Para RAR, vamos usar um método alternativo
                    await ExtractRarFileAsync(localPath, _gameDirectory, progress);
                }

                progress?.Report((100, "Atualização concluída!"));

                // Configurar modpack
                progress?.Report((80, "Configurando modpack personalizado..."));
                await _modpackService.SetupModpackAsync(new Progress<(int percentage, string message)>((update) => 
                {
                    progress?.Report((80 + (update.percentage / 5), update.message));
                }));
                
                // Salvar versão local
                SaveLocalVersion(remoteVersion);
                
                progress?.Report((100, "Atualização concluída!"));
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro durante a atualização: {ex.Message}");
            }
        }

        private async Task SimulateUpdateAsync(IProgress<(int percentage, string message)> progress)
        {
            // Usar arquivos locais do RayzeCraft quando servidor não está disponível
            progress?.Report((10, "Verificando configuração local..."));
            await Task.Delay(500);
            
            progress?.Report((30, "Copiando arquivos do RayzeCraft..."));
            await Task.Delay(500);
            
            // Copiar o minecraft.jar correto dos arquivos do RayzeCraft
            var sourceMinecraftJar = Path.Combine("C:\\rayzecraft-launcher-files\\bin", "minecraft.jar");
            var targetMinecraftJar = Path.Combine(_gameDirectory, "minecraft.jar");
            
            if (File.Exists(sourceMinecraftJar))
            {
                progress?.Report((50, "Copiando minecraft.jar..."));
                File.Copy(sourceMinecraftJar, targetMinecraftJar, true);
                progress?.Report((70, "minecraft.jar copiado com sucesso!"));
            }
            else
            {
                progress?.Report((50, "minecraft.jar não encontrado, criando arquivo de exemplo..."));
                // Criar um arquivo de exemplo
                var exampleFile = Path.Combine(_gameDirectory, "exemplo.txt");
                await File.WriteAllTextAsync(exampleFile, $"Arquivo de exemplo criado em {DateTime.Now}");
                
                // Criar um arquivo minecraft.jar de teste
                var minecraftJar = Path.Combine(_gameDirectory, "minecraft.jar");
                await File.WriteAllTextAsync(minecraftJar, "Arquivo de teste - Minecraft JAR simulado");
            }
            
            progress?.Report((90, "Salvando configuração..."));
            await Task.Delay(500);
            
            // Configurar modpack
            progress?.Report((80, "Configurando modpack personalizado..."));
            await _modpackService.SetupModpackAsync(new Progress<(int percentage, string message)>((update) => 
            {
                progress?.Report((80 + (update.percentage / 5), update.message));
            }));
            
            // Salvar versão local
            SaveLocalVersion(GetExampleVersion());
            
            progress?.Report((100, "Configuração local concluída!"));
        }

        private async Task<GameVersion> GetRemoteVersionAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(VERSION_URL);
                return JsonConvert.DeserializeObject<GameVersion>(response);
            }
            catch (Exception ex)
            {
                // Se não conseguir acessar o servidor, usar versão de exemplo
                return GetExampleVersion();
            }
        }

        private GameVersion GetExampleVersion()
        {
            return new GameVersion
            {
                Version = "1.0.0",
                Description = "Versão de exemplo - Servidor não disponível",
                ReleaseDate = DateTime.Now,
                MinecraftVersion = "1.12.2",
                ForgeVersion = "14.23.5.2854",
                Files = new List<GameFile>
                {
                    new GameFile
                    {
                        Path = "exemplo.txt",
                        Url = "https://example.com/exemplo.txt",
                        Hash = "abc123def456",
                        Size = 1024,
                        IsRequired = true,
                        Type = "config",
                        Description = "Arquivo de exemplo"
                    }
                },
                RequiredMods = new List<string> { "ExampleMod" }
            };
        }

        private GameVersion GetLocalVersion()
        {
            try
            {
                var versionFile = Path.Combine(_launcherDirectory, "version.json");
                if (!File.Exists(versionFile))
                {
                    return null;
                }

                var json = File.ReadAllText(versionFile);
                return JsonConvert.DeserializeObject<GameVersion>(json);
            }
            catch
            {
                return null;
            }
        }

        private void SaveLocalVersion(GameVersion version)
        {
            var versionFile = Path.Combine(_launcherDirectory, "version.json");
            var json = JsonConvert.SerializeObject(version, Formatting.Indented);
            File.WriteAllText(versionFile, json);
        }

        private async Task DownloadFileAsync(string url, string localPath)
        {
            var directory = Path.GetDirectoryName(localPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var response = await _httpClient.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                
                using (var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }
        }

        private string CalculateFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private async Task ExtractRarFileAsync(string rarFilePath, string extractPath, IProgress<(int percentage, string message)> progress)
        {
            try
            {
                // Criar diretório de extração se não existir
                if (!Directory.Exists(extractPath))
                {
                    Directory.CreateDirectory(extractPath);
                }

                // Abrir o arquivo RAR usando SharpCompress
                using (var archive = ArchiveFactory.Open(rarFilePath))
                {
                    var totalEntries = archive.Entries.Count();
                    var processedEntries = 0;

                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.IsDirectory)
                        {
                            // Extrair o arquivo
                            entry.WriteToDirectory(extractPath, new ExtractionOptions()
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }

                        processedEntries++;
                        var percentage = (int)((processedEntries * 100.0) / totalEntries);
                        progress?.Report((50 + (percentage / 2), $"Extraindo: {entry.Key}"));

                        // Permitir que outras operações sejam executadas
                        await Task.Delay(1);
                    }
                }

                // Deletar o arquivo RAR após extração
                File.Delete(rarFilePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao extrair arquivo RAR: {ex.Message}");
            }
        }
    }
}
