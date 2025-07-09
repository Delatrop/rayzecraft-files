using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MinecraftLauncher.Services
{
    public class IntegrityService
    {
        private readonly LogService _logService;
        private readonly string _sourceDirectory;
        private readonly string _targetDirectory;

        public IntegrityService(LogService logService)
        {
            _logService = logService;
            _sourceDirectory = @"C:\rayzecraft-launcher-files";
            _targetDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
        }

        public async Task<IntegrityResult> VerifyIntegrityAsync(IProgress<(int percentage, string message)> progress = null)
        {
            await _logService.LogInfoAsync("Iniciando verificação de integridade dos arquivos");
            
            var result = new IntegrityResult();
            var foldersToCheck = new[] { "mods", "config", "scripts" };
            
            for (int i = 0; i < foldersToCheck.Length; i++)
            {
                var folder = foldersToCheck[i];
                var percentage = (int)((i + 1) * 100.0 / foldersToCheck.Length);
                
                progress?.Report((percentage, $"Verificando pasta {folder}..."));
                
                var folderResult = await VerifyFolderIntegrityAsync(folder);
                result.FolderResults.Add(folder, folderResult);
                
                await _logService.LogInfoAsync($"Verificação da pasta {folder} concluída", 
                    $"Corretos: {folderResult.CorrectFiles.Count}, Faltando: {folderResult.MissingFiles.Count}, Corrompidos: {folderResult.CorruptedFiles.Count}");
            }
            
            result.IsValid = result.FolderResults.All(fr => fr.Value.IsValid);
            
            if (result.IsValid)
            {
                await _logService.LogSuccessAsync("Verificação de integridade concluída com sucesso");
            }
            else
            {
                await _logService.LogWarningAsync("Verificação de integridade encontrou problemas");
            }
            
            return result;
        }

        private async Task<FolderIntegrityResult> VerifyFolderIntegrityAsync(string folderName)
        {
            var result = new FolderIntegrityResult();
            var sourceFolder = Path.Combine(_sourceDirectory, folderName);
            var targetFolder = Path.Combine(_targetDirectory, folderName);

            if (!Directory.Exists(sourceFolder))
            {
                await _logService.LogWarningAsync($"Pasta fonte não encontrada: {sourceFolder}");
                return result;
            }

            if (!Directory.Exists(targetFolder))
            {
                await _logService.LogWarningAsync($"Pasta destino não encontrada: {targetFolder}");
                Directory.CreateDirectory(targetFolder);
            }

            var sourceFiles = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);
            
            foreach (var sourceFile in sourceFiles)
            {
                var relativePath = Path.GetRelativePath(sourceFolder, sourceFile);
                var targetFile = Path.Combine(targetFolder, relativePath);
                
                if (!File.Exists(targetFile))
                {
                    result.MissingFiles.Add(relativePath);
                    await _logService.LogWarningAsync($"Arquivo faltando: {relativePath}");
                }
                else
                {
                    var sourceHash = await GetFileHashAsync(sourceFile);
                    var targetHash = await GetFileHashAsync(targetFile);
                    
                    if (sourceHash == targetHash)
                    {
                        result.CorrectFiles.Add(relativePath);
                    }
                    else
                    {
                        result.CorruptedFiles.Add(relativePath);
                        await _logService.LogWarningAsync($"Arquivo corrompido: {relativePath}");
                    }
                }
            }

            result.IsValid = result.MissingFiles.Count == 0 && result.CorruptedFiles.Count == 0;
            return result;
        }

        public async Task<bool> RepairIntegrityAsync(IntegrityResult integrityResult, IProgress<(int percentage, string message)> progress = null)
        {
            await _logService.LogInfoAsync("Iniciando reparo de integridade dos arquivos");
            
            var totalFiles = integrityResult.FolderResults.SelectMany(fr => fr.Value.MissingFiles.Concat(fr.Value.CorruptedFiles)).Count();
            var processedFiles = 0;
            
            foreach (var folderResult in integrityResult.FolderResults)
            {
                var folderName = folderResult.Key;
                var folder = folderResult.Value;
                
                var sourceFolder = Path.Combine(_sourceDirectory, folderName);
                var targetFolder = Path.Combine(_targetDirectory, folderName);
                
                // Reparar arquivos faltando
                foreach (var missingFile in folder.MissingFiles)
                {
                    var sourceFile = Path.Combine(sourceFolder, missingFile);
                    var targetFile = Path.Combine(targetFolder, missingFile);
                    
                    try
                    {
                        var targetDir = Path.GetDirectoryName(targetFile);
                        Directory.CreateDirectory(targetDir);
                        
                        File.Copy(sourceFile, targetFile, true);
                        await _logService.LogSuccessAsync($"Arquivo reparado: {missingFile}");
                        
                        processedFiles++;
                        progress?.Report((processedFiles * 100 / totalFiles, $"Reparando {missingFile}..."));
                    }
                    catch (Exception ex)
                    {
                        await _logService.LogErrorAsync($"Erro ao reparar arquivo: {missingFile}", ex.Message);
                        return false;
                    }
                }
                
                // Reparar arquivos corrompidos
                foreach (var corruptedFile in folder.CorruptedFiles)
                {
                    var sourceFile = Path.Combine(sourceFolder, corruptedFile);
                    var targetFile = Path.Combine(targetFolder, corruptedFile);
                    
                    try
                    {
                        File.Copy(sourceFile, targetFile, true);
                        await _logService.LogSuccessAsync($"Arquivo corrompido reparado: {corruptedFile}");
                        
                        processedFiles++;
                        progress?.Report((processedFiles * 100 / totalFiles, $"Reparando {corruptedFile}..."));
                    }
                    catch (Exception ex)
                    {
                        await _logService.LogErrorAsync($"Erro ao reparar arquivo corrompido: {corruptedFile}", ex.Message);
                        return false;
                    }
                }
            }
            
            await _logService.LogSuccessAsync("Reparo de integridade concluído com sucesso");
            return true;
        }

        public async Task<bool> ForceResyncAsync(IProgress<(int percentage, string message)> progress = null)
        {
            await _logService.LogInfoAsync("Iniciando ressincronização completa dos arquivos");
            
            var foldersToSync = new[] { "mods", "config", "scripts" };
            
            for (int i = 0; i < foldersToSync.Length; i++)
            {
                var folder = foldersToSync[i];
                var percentage = (int)((i + 1) * 100.0 / foldersToSync.Length);
                
                progress?.Report((percentage, $"Ressincronizando pasta {folder}..."));
                
                var sourceFolder = Path.Combine(_sourceDirectory, folder);
                var targetFolder = Path.Combine(_targetDirectory, folder);
                
                if (!Directory.Exists(sourceFolder))
                {
                    await _logService.LogWarningAsync($"Pasta fonte não encontrada: {sourceFolder}");
                    continue;
                }
                
                try
                {
                    // Deletar pasta destino
                    if (Directory.Exists(targetFolder))
                    {
                        Directory.Delete(targetFolder, true);
                        await _logService.LogInfoAsync($"Pasta {folder} deletada");
                    }
                    
                    // Recriar pasta destino
                    Directory.CreateDirectory(targetFolder);
                    
                    // Copiar todos os arquivos
                    await CopyDirectoryAsync(sourceFolder, targetFolder);
                    
                    await _logService.LogSuccessAsync($"Pasta {folder} ressincronizada com sucesso");
                }
                catch (Exception ex)
                {
                    await _logService.LogErrorAsync($"Erro ao ressincronizar pasta {folder}", ex.Message);
                    return false;
                }
            }
            
            await _logService.LogSuccessAsync("Ressincronização completa concluída");
            return true;
        }

        private async Task CopyDirectoryAsync(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            
            // Copiar arquivos
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var targetFile = Path.Combine(targetDir, fileName);
                File.Copy(file, targetFile, true);
                await _logService.LogInfoAsync($"Arquivo copiado: {fileName}");
            }
            
            // Copiar subdiretórios recursivamente
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                var targetSubDir = Path.Combine(targetDir, dirName);
                await CopyDirectoryAsync(dir, targetSubDir);
            }
        }

        private async Task<string> GetFileHashAsync(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = await Task.Run(() => md5.ComputeHash(stream));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    public class IntegrityResult
    {
        public Dictionary<string, FolderIntegrityResult> FolderResults { get; set; } = new();
        public bool IsValid { get; set; }
    }

    public class FolderIntegrityResult
    {
        public List<string> CorrectFiles { get; set; } = new();
        public List<string> MissingFiles { get; set; } = new();
        public List<string> CorruptedFiles { get; set; } = new();
        public bool IsValid { get; set; }
    }
}
