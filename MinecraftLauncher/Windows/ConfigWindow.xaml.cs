using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MinecraftLauncher.Services;

namespace MinecraftLauncher.Windows
{
    public partial class ConfigWindow : Window
    {
        private readonly ConfigService _configService;

        public ConfigWindow()
        {
            InitializeComponent();
            _configService = new ConfigService();
            
            LoadConfiguration();
            SetupEventHandlers();
        }

        private void LoadConfiguration()
        {
            var config = _configService.GetConfiguration();
            
            PlayerNameTextBox.Text = config.PlayerName;
            MaxMemorySlider.Value = config.MaxMemory;
            MinMemorySlider.Value = config.MinMemory;
            WindowWidthTextBox.Text = config.WindowWidth.ToString();
            WindowHeightTextBox.Text = config.WindowHeight.ToString();
            JvmArgumentsTextBox.Text = config.JvmArguments;
            JavaPathTextBox.Text = config.JavaPath ?? "";
            
            UpdateMemoryTexts();
            UpdateJavaPathDisplay();
        }

        private void SetupEventHandlers()
        {
            MaxMemorySlider.ValueChanged += (s, e) => 
            {
                MaxMemoryText.Text = $"{(int)MaxMemorySlider.Value} MB";
            };
            
            MinMemorySlider.ValueChanged += (s, e) => 
            {
                MinMemoryText.Text = $"{(int)MinMemorySlider.Value} MB";
            };
        }

        private void UpdateMemoryTexts()
        {
            MaxMemoryText.Text = $"{(int)MaxMemorySlider.Value} MB";
            MinMemoryText.Text = $"{(int)MinMemorySlider.Value} MB";
        }

        private void SetResolution_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string resolution)
            {
                var parts = resolution.Split(',');
                if (parts.Length == 2)
                {
                    WindowWidthTextBox.Text = parts[0];
                    WindowHeightTextBox.Text = parts[1];
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = _configService.GetConfiguration();
                
                // Validar e salvar configurações
                config.PlayerName = string.IsNullOrWhiteSpace(PlayerNameTextBox.Text) ? "Jogador" : PlayerNameTextBox.Text;
                config.MaxMemory = (int)MaxMemorySlider.Value;
                config.MinMemory = (int)MinMemorySlider.Value;
                config.JvmArguments = JvmArgumentsTextBox.Text;
                
                // Validar resolução
                if (int.TryParse(WindowWidthTextBox.Text, out int width) && width > 0)
                {
                    config.WindowWidth = width;
                }
                
                if (int.TryParse(WindowHeightTextBox.Text, out int height) && height > 0)
                {
                    config.WindowHeight = height;
                }
                
                // Validar memória
                if (config.MinMemory > config.MaxMemory)
                {
                    MessageBox.Show("A memória mínima não pode ser maior que a máxima!", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Salvar caminho do Java
                config.JavaPath = string.IsNullOrWhiteSpace(JavaPathTextBox.Text) ? "" : JavaPathTextBox.Text;
                
                _configService.SaveConfiguration(config);
                
                MessageBox.Show("Configurações salvas com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar configurações: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        
        private void BrowseJavaButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Selecionar Java Executable",
                Filter = "Java Executable (*.exe)|*.exe|Todos os arquivos (*.*)|*.*",
                FilterIndex = 1,
                InitialDirectory = @"C:\Program Files\Java"
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                var selectedPath = openFileDialog.FileName;
                
                // Verificar se é um executável Java válido
                if (IsValidJavaExecutable(selectedPath))
                {
                    JavaPathTextBox.Text = selectedPath;
                    UpdateJavaPathDisplay();
                }
                else
                {
                    MessageBox.Show("O arquivo selecionado não parece ser um executável Java válido.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
        
        private void DetectJavaButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var detectedPath = DetectJavaPath();
                if (!string.IsNullOrEmpty(detectedPath))
                {
                    JavaPathTextBox.Text = detectedPath;
                    UpdateJavaPathDisplay();
                    MessageBox.Show($"Java detectado automaticamente:\n{detectedPath}", "Java Detectado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Não foi possível detectar o Java automaticamente. Instale o Java 8 ou superior.", "Java Não Encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao detectar Java: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void UpdateJavaPathDisplay()
        {
            if (string.IsNullOrEmpty(JavaPathTextBox.Text))
            {
                JavaPathTextBox.Text = "";
                JavaPathTextBox.Background = System.Windows.Media.Brushes.White;
            }
            else if (IsValidJavaExecutable(JavaPathTextBox.Text))
            {
                JavaPathTextBox.Background = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                JavaPathTextBox.Background = System.Windows.Media.Brushes.LightCoral;
            }
        }
        
        private bool IsValidJavaExecutable(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;
                
            try
            {
                var fileName = Path.GetFileName(path).ToLower();
                if (fileName != "java.exe" && fileName != "javaw.exe")
                    return false;
                
                // Tentar executar java -version para verificar se funciona
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
                    process.WaitForExit(5000); // Timeout de 5 segundos
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }
        
        private string DetectJavaPath()
        {
            var javaPaths = new[]
            {
                // Java do TLauncher
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "runtime", "jre-legacy", "windows", "jre-legacy", "bin", "javaw.exe"),
                
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
                @"C:\Program Files (x86)\Java\jre1.8.0_401\bin\java.exe"
            };
            
            foreach (var path in javaPaths)
            {
                if (File.Exists(path) && IsValidJavaExecutable(path))
                {
                    return path;
                }
            }
            
            // Tentar detectar via PATH
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
                
                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit(5000);
                    if (process.ExitCode == 0)
                    {
                        return "java.exe"; // Java está no PATH
                    }
                }
            }
            catch { }
            
            return null;
        }
    }
}
