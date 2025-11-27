using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenCoinMovil.Models;
using GreenCoinMovil.Views;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GreenCoinMovil.ViewModels
{
    public partial class AppShellViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isAdminVisible;

        public AppShellViewModel()
        {
            CheckAdminVisibility();
            CheckAuthenticationAndNavigate();
        }

        private async void CheckAdminVisibility()
        {
            // Check if current user is admin
            try
            {
                var email = await SecureStorage.GetAsync("user_email");
                IsAdminVisible = email == "admin@gmail.com";
                Debug.WriteLine($"🔍 ADMIN CHECK: Email='{email}', IsAdminVisible={IsAdminVisible}");
            }
            catch (Exception ex)
            {
                IsAdminVisible = false;
                Debug.WriteLine($"❌ ADMIN CHECK ERROR: {ex.Message}");
            }
        }

        private async void CheckAuthenticationAndNavigate()
        {
            try
            {
                Debug.WriteLine("🔐 CHECKING AUTHENTICATION ON APP STARTUP...");

                // Check if user has a valid token
                string token = null;
                string email = null;

                #if MACCATALYST
                token = Preferences.Get("auth_token", string.Empty);
                email = Preferences.Get("user_email", string.Empty);
                #else
                token = await SecureStorage.GetAsync("auth_token");
                email = await SecureStorage.GetAsync("user_email");
                #endif

                bool isAuthenticated = !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(email);

                Debug.WriteLine($"🔐 AUTH CHECK: Token exists={token?.Length > 0}, Email exists={email?.Length > 0}, IsAuthenticated={isAuthenticated}");

                if (!isAuthenticated)
                {
                    Debug.WriteLine("🔐 USER NOT AUTHENTICATED - REDIRECTING TO LOGIN");
                    // User is not authenticated, redirect to login
                    await Shell.Current.GoToAsync("//LoginPage");
                }
                else
                {
                    Debug.WriteLine("🔐 USER IS AUTHENTICATED - STAYING ON DASHBOARD");
                    // User is authenticated, stay on dashboard (default behavior)
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ AUTH CHECK ERROR: {ex.Message}");
                // On error, redirect to login to be safe
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }

        // Método público para refrescar la visibilidad de admin (llamar después del login)
        public async Task RefreshAdminVisibility()
        {
            Debug.WriteLine("🔄 REFRESHING ADMIN VISIBILITY...");
            await Task.Run(CheckAdminVisibility);
            Debug.WriteLine($"✅ ADMIN VISIBILITY REFRESHED: IsAdminVisible={IsAdminVisible}");
        }

        [RelayCommand]
        private async Task NavigateToDashboard()
        {
            await Shell.Current.GoToAsync("//MainTabs");
        }

        [RelayCommand]
        private async Task NavigateToRecycle()
        {
            await Shell.Current.GoToAsync("//MainTabs/RecyclingTab");
        }

        [RelayCommand]
        private async Task NavigateToHistory()
        {
            await Shell.Current.GoToAsync("//MainTabs/HistoryTab");
        }

        [RelayCommand]
        private async Task NavigateToAchievements()
        {
            await Shell.Current.GoToAsync("//MainTabs/AchievementsTab");
        }

        [RelayCommand]
        private async Task NavigateToSettings()
        {
            await Shell.Current.GoToAsync("//MainTabs/SettingsTab");
        }


        [RelayCommand]
        private async Task Logout()
        {
            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Cerrar Sesión",
                "¿Estás seguro de que quieres cerrar sesión?",
                "Sí", "Cancelar");

            if (confirm)
            {
                // Clear stored data
                SecureStorage.Remove("auth_token");
                SecureStorage.Remove("user_email");

                // Navigate to login
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
    }
}