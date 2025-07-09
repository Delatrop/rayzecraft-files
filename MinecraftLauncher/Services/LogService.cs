using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MinecraftLauncher.Services
{
    public class LogService
    {
        private readonly string _logDirectory;
        private readonly string _logFile;
        private readonly List<string> _logBuffer;

        public LogService()
        {
            _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".rayzecraftlauncher", "logs");
            _logFile = Path.Combine(_logDirectory, $"launcher-{DateTime.Now:yyyy-MM-dd}.log");
            _logBuffer = new List<string>();
            
            Directory.CreateDirectory(_logDirectory);
        }

        public async Task LogAsync(string level, string message, string details = null)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logEntry = $"[{timestamp}] [{level}] {message}";
            
            if (!string.IsNullOrEmpty(details))
            {
                logEntry += $" - {details}";
            }

            _logBuffer.Add(logEntry);
            
            // Escrever no arquivo
            await File.AppendAllTextAsync(_logFile, logEntry + Environment.NewLine);
            
            // Também exibir no console para debug
            Console.WriteLine(logEntry);
        }

        public async Task LogInfoAsync(string message, string details = null)
        {
            await LogAsync("INFO", message, details);
        }

        public async Task LogWarningAsync(string message, string details = null)
        {
            await LogAsync("WARN", message, details);
        }

        public async Task LogErrorAsync(string message, string details = null)
        {
            await LogAsync("ERROR", message, details);
        }

        public async Task LogSuccessAsync(string message, string details = null)
        {
            await LogAsync("SUCCESS", message, details);
        }

        public List<string> GetRecentLogs()
        {
            return new List<string>(_logBuffer);
        }

        public async Task<string> GetLogContentsAsync()
        {
            if (File.Exists(_logFile))
            {
                return await File.ReadAllTextAsync(_logFile);
            }
            return "Nenhum log encontrado.";
        }

        public async Task ClearLogsAsync()
        {
            _logBuffer.Clear();
            if (File.Exists(_logFile))
            {
                File.Delete(_logFile);
            }
        }
    }
}
