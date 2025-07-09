using System;
using System.Threading.Tasks;
using MinecraftLauncher.Services;

namespace MinecraftLauncher.Test
{
    class TestLaunch
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== TESTE DE LANÇAMENTO ===");
            
            try
            {
                var gameService = new GameService();
                
                Console.WriteLine("Tentando lançar o Minecraft...");
                await gameService.LaunchGameAsync();
                
                Console.WriteLine("Comando executado com sucesso!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
            }
            
            Console.WriteLine("Pressione qualquer tecla para sair...");
            Console.ReadKey();
        }
    }
}
