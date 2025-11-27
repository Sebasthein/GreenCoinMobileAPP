using GreenCoinMovil.Models;
using GreenCoinMovil.ViewModels;
using GreenCoinMovil.Views;
using Microsoft.Extensions.Logging;
using GreenCoinMovil.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;


namespace GreenCoinMovil
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });



            // ✅ FORMA CORRECTA - HttpClient básico
            builder.Services.AddSingleton<HttpClient>(serviceProvider =>
            {
                var client = new HttpClient();
                client.BaseAddress = new Uri("http://192.168.1.8:8080/api"); // Tu URL real
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                return client;
            });

            builder.Services.AddSingleton<ApiService>();
            // ✅ REGISTRAR SERVICIOS
            builder.Services.AddSingleton<AuthService>(serviceProvider =>
            {
                var apiService = serviceProvider.GetRequiredService<ApiService>();
                return new AuthService(apiService);
            });

            // ✅ REGISTRAR VIEWMODELS
            builder.Services.AddSingleton<AppShellViewModel>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterViewModel>();
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<RecyclingViewModel>();
            builder.Services.AddTransient<HistoryViewModel>();
            builder.Services.AddTransient<AchievementsViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<AdminValidationViewModel>();
            builder.Services.AddTransient<AdminDashboardViewModel>();


            // ✅ REGISTRAR PÁGINAS
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<RecyclePage>();
            builder.Services.AddTransient<HistoryPage>();
            builder.Services.AddTransient<AchievementsPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<AdminValidationPage>();
            builder.Services.AddTransient<AdminDashboardPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
