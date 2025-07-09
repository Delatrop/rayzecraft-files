using System;
using System.IO;
using Newtonsoft.Json;
using MinecraftLauncher.Models;

namespace MinecraftLauncher.Services
{
    public class ConfigService
    {
        private readonly string _configPath;
        private LauncherConfig _config;

        public ConfigService()
        {
            var launcherDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".rayzecraftlauncher");
            _configPath = Path.Combine(launcherDirectory, "config.json");
            
            // Garantir que o diretório existe
            Directory.CreateDirectory(launcherDirectory);
            
            LoadConfiguration();
        }

        public LauncherConfig GetConfiguration()
        {
            return _config;
        }

        public void SaveConfiguration(LauncherConfig config)
        {
            _config = config;
            
            try
            {
                var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao salvar configuração: {ex.Message}");
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _config = JsonConvert.DeserializeObject<LauncherConfig>(json);
                }
                else
                {
                    _config = CreateDefaultConfiguration();
                    SaveConfiguration(_config);
                }
            }
            catch
            {
                _config = CreateDefaultConfiguration();
                SaveConfiguration(_config);
            }
        }

        private LauncherConfig CreateDefaultConfiguration()
        {
            return new LauncherConfig
            {
                PlayerName = "Jogador",
                GameVersion = "1.12.2",
                MaxMemory = 2048,
                MinMemory = 512,
                WindowWidth = 854,
                WindowHeight = 480,
                JvmArguments = "-XX:+UnlockExperimentalVMOptions -XX:+UseG1GC -XX:G1NewSizePercent=20 -XX:G1ReservePercent=20 -XX:MaxGCPauseMillis=50 -XX:G1HeapRegionSize=32M",
                LauncherTitle = "RayzeCraft Launcher",
                LauncherSubtitle = "Launcher personalizado para Minecraft",
                FooterText = "RayzeCraft Launcher v1.0 - Desenvolvido com ♥",
                ServerUrl = "https://seuservidor.com",
                JavaPath = "" // Vazio para detecção automática
            };
        }
    }
}
