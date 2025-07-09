using System;
using System.Threading.Tasks;
using System.Windows;
using MinecraftLauncher.Services;
using MinecraftLauncher.Windows;

namespace MinecraftLauncher
{
    public partial class MainWindow : Window
    {
        private readonly UpdateService _updateService;
        private readonly TLauncherGameServiceFixed _gameService;
        private readonly ConfigService _configService;
        private readonly ModpackService _modpackService;
        private readonly LogService _logService;
        private readonly IntegrityService _integrityService;

        public MainWindow()
        {
            InitializeComponent();
            
            _updateService = new UpdateService();
            _gameService = new TLauncherGameServiceFixed();
            _configService = new ConfigService();
            _modpackService = new ModpackService();
            _logService = new LogService();
            _integrityService = new IntegrityService(_logService);
            
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckForUpdates();
        }

        private async Task CheckForUpdates()
        {
            UpdateStatusText.Text = "Verificando atualizações...";
            UpdateDetailText.Text = "Conectando ao servidor...";
            
            try
            {
                bool needsUpdate = await _updateService.CheckForUpdatesAsync();
                
                if (needsUpdate)
                {
                    UpdateStatusText.Text = "Atualizações disponíveis";
                    UpdateDetailText.Text = "Clique em 'Atualizar' para baixar os arquivos";
                    UpdateButton.Content = "Atualizar";
                    UpdateButton.IsEnabled = true;
                }
                else
                {
                    UpdateStatusText.Text = "Tudo atualizado!";
                    UpdateDetailText.Text = "Seus arquivos estão atualizados";
                    await ShowGamePanel();
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = "Erro ao verificar atualizações";
                UpdateDetailText.Text = $"Erro: {ex.Message}";
                UpdateButton.Content = "Tentar novamente";
                UpdateButton.IsEnabled = true;
            }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateButton.IsEnabled = false;
            UpdateProgressBar.Value = 0;
            UpdateProgressBar.IsIndeterminate = false;
            
            try
            {
                var progress = new Progress<(int percentage, string message)>(update =>
                {
                    UpdateProgressBar.Value = update.percentage;
                    UpdateDetailText.Text = update.message;
                });
                
                await _updateService.UpdateAsync(progress);
                
                UpdateStatusText.Text = "Atualização concluída!";
                UpdateDetailText.Text = "Todos os arquivos foram atualizados";
                
                await Task.Delay(2000);
                await ShowGamePanel();
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = "Erro durante a atualização";
                UpdateDetailText.Text = $"Erro: {ex.Message}";
                UpdateButton.Content = "Tentar novamente";
                UpdateButton.IsEnabled = true;
            }
        }

        private async Task ShowGamePanel()
        {
            UpdatePanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Visible;
            
            // Atualizar informações da versão
            var config = _configService.GetConfiguration();
            GameVersionText.Text = "Versão: RayzeCraft Modpack 1.12.2-Forge";
            
            // Mostrar status do modpack
            try
            {
                var status = await _modpackService.GetModpackStatusAsync();
                UpdateDetailText.Text = status;
            }
            catch (Exception ex)
            {
                UpdateDetailText.Text = $"Erro ao verificar modpack: {ex.Message}";
            }
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSplashScreen();
            
            try
            {
                await _gameService.LaunchGameAsync();
                HideSplashScreen();
            }
            catch (Exception ex)
            {
                HideSplashScreen();
                // Mostrar aviso mas não impedir o lançamento
                var result = MessageBox.Show($"Aviso: {ex.Message}\n\nDeseja tentar lançar mesmo assim?", "Aviso", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Tentar lançar com configuração simplificada
                        ShowSplashScreen();
                        await _gameService.LaunchGameAsync();
                        HideSplashScreen();
                    }
                    catch
                    {
                        HideSplashScreen();
                        MessageBox.Show("Não foi possível iniciar o jogo. Verifique se o Java e o Minecraft estão instalados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void IntegrityButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatusText.Text = "Verificando integridade...";
            UpdateDetailText.Text = "";
            
            try
            {
                // Usar o novo serviço corrigido
                bool integrityOk = await _gameService.VerifyGameIntegrityAsync();
                
                if (integrityOk)
                {
                    UpdateStatusText.Text = "✅ Integridade verificada com sucesso!";
                    UpdateDetailText.Text = "Todos os arquivos estão corretos.";
                }
                else
                {
                    UpdateStatusText.Text = "❌ Problemas de integridade detectados!";
                    UpdateDetailText.Text = "Alguns arquivos estão faltando ou corrompidos.";
                    
                    // Mostrar relatório detalhado
                    var report = await _gameService.GetIntegrityReportAsync();
                    MessageBox.Show(report, "Relatório de Integridade", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = "❌ Erro na verificação de integridade";
                UpdateDetailText.Text = $"Erro: {ex.Message}";
                MessageBox.Show($"Erro ao verificar integridade:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            var configWindow = new ConfigWindow();
            configWindow.ShowDialog();
        }

        private void ShowSplashScreen()
        {
            SplashScreen.Visibility = Visibility.Visible;
        }

        private void HideSplashScreen()
        {
            SplashScreen.Visibility = Visibility.Collapsed;
        }
    }
}
