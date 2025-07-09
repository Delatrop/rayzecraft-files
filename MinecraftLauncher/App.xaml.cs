using System;
using System.IO;
using System.Windows;

namespace MinecraftLauncher
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Garantir que o diretório do launcher existe
            string launcherDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".rayzecraftlauncher");
            if (!Directory.Exists(launcherDir))
            {
                Directory.CreateDirectory(launcherDir);
            }
        }
    }
}
