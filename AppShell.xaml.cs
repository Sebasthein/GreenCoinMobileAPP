using GreenCoinMovil.ViewModels;
using GreenCoinMovil.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace GreenCoinMovil
{
    public partial class AppShell : Shell
    {
        public AppShell(AppShellViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

            // Esto es necesario para navegar de vuelta después del logout.
            Routing.RegisterRoute("LoginPage", typeof(Views.LoginPage));

            // 2. Registrar la ruta del Dashboard
            // Esto es necesario para navegar después de un login exitoso.
            Routing.RegisterRoute("Dashboard", typeof(Views.DashboardPage));

            Routing.RegisterRoute("RegisterPage", typeof(Views.RegisterPage));

            Routing.RegisterRoute("RecyclingPage", typeof(Views.RecyclePage));
            Routing.RegisterRoute("AchievementsPage", typeof(Views.AchievementsPage));
            Routing.RegisterRoute("SettingsPage", typeof(Views.SettingsPage));
            Routing.RegisterRoute("AdminValidationPage", typeof(AdminValidationPage));
            Routing.RegisterRoute("AdminDashboardPage", typeof(AdminDashboardPage));

        }
    }
}
