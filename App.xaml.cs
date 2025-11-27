using System.IO;
using GreenCoinMovil.ViewModels;

namespace GreenCoinMovil
{
    public partial class App : Application
    {
        private readonly AppShellViewModel _appShellViewModel;

        public App(AppShellViewModel appShellViewModel)
        {
            _appShellViewModel = appShellViewModel;
            LoadEnvironmentVariables();
            InitializeComponent();
        }

        private void LoadEnvironmentVariables()
        {
            try
            {
                Console.WriteLine("🔍 Loading environment variables...");

                // Forzar la URL directamente por ahora
                Environment.SetEnvironmentVariable("API_BASE_URL", "http://192.168.1.8:8080");
                Console.WriteLine("✅ API_BASE_URL set to: http://192.168.1.8:8080");

                // Verificar que se configuró
                var apiUrl = Environment.GetEnvironmentVariable("API_BASE_URL");
                Console.WriteLine($"🎯 API_BASE_URL verified: {apiUrl}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error setting environment variables: {ex.Message}");
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell(_appShellViewModel));
        }
    }
}