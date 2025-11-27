using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenCoinMovil.Models;
using GreenCoinMovil.Views;
using Microsoft.Maui.Controls;



namespace GreenCoinMovil.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private string errorMessage;

        [ObservableProperty]
        private bool isBusy;

        private readonly AuthService _authService;
        private readonly AppShellViewModel _appShellViewModel;

        public LoginViewModel(AuthService authService, AppShellViewModel appShellViewModel)
        {
            _authService = authService;
            _appShellViewModel = appShellViewModel;
        }

        [RelayCommand]
        private async Task IrAlAdmin()
        {
            await Shell.Current.GoToAsync(nameof(AdminDashboardPage));
        }

        [RelayCommand]
        private async Task NavigateToRegister()
        {
            await Shell.Current.GoToAsync("RegisterPage");
        }

        [RelayCommand]
        private async Task Login()
        {
            if (IsBusy) return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                // ✅ Validaciones básicas
                if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
                {
                    ErrorMessage = "❌ ERROR: Por favor, completa todos los campos (email y contraseña)";
                    System.Diagnostics.Debug.WriteLine("❌ VALIDACIÓN: Campos vacíos");
                    return;
                }

                if (!Email.Contains("@"))
                {
                    ErrorMessage = "❌ ERROR: El email debe tener un formato válido (@)";
                    System.Diagnostics.Debug.WriteLine("❌ VALIDACIÓN: Email sin @");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"🔐 LOGIN: Iniciando sesión para email: {Email}");
                System.Diagnostics.Debug.WriteLine($"🔐 LOGIN: Longitud de contraseña: {Password?.Length ?? 0} caracteres");

                // 1. Crear la solicitud
                var request = new LoginRequest { Email = Email, Password = Password };

                // 2. Llamar al servicio
                System.Diagnostics.Debug.WriteLine("🔐 LOGIN: Llamando al servicio de autenticación...");
                var response = await _authService.AttemptLoginAsync(request);

                if (response.Success)
                {
                    System.Diagnostics.Debug.WriteLine("✅✅✅ LOGIN: ¡Login exitoso! Verificando token...");

                    // ✅✅✅ CORRECCIÓN CRÍTICA: Guardar el token manualmente
                    if (!string.IsNullOrEmpty(response.Token))
                    {
                        #if MACCATALYST
                        // Para Mac Catalyst usar Preferences (SecureStorage requiere entitlements)
                        System.Diagnostics.Debug.WriteLine("💾 LOGIN [MACCATALYST]: Guardando token en Preferences...");
                        Preferences.Set("auth_token", response.Token);
                        Preferences.Set("user_email", Email);

                        // Verificar que se guardó correctamente
                        var savedToken = Preferences.Get("auth_token", string.Empty);
                        var savedEmail = Preferences.Get("user_email", string.Empty);

                        System.Diagnostics.Debug.WriteLine($"✅ LOGIN [MACCATALYST]: Token guardado correctamente: {!string.IsNullOrEmpty(savedToken)}");
                        System.Diagnostics.Debug.WriteLine($"✅ LOGIN [MACCATALYST]: Email guardado: {savedEmail}");
                        System.Diagnostics.Debug.WriteLine($"✅ LOGIN [MACCATALYST]: Longitud del token: {savedToken?.Length ?? 0} caracteres");
                        #else
                        // Para otras plataformas usar SecureStorage
                        System.Diagnostics.Debug.WriteLine("💾 LOGIN: Guardando token en SecureStorage...");
                        await SecureStorage.SetAsync("auth_token", response.Token);
                        await SecureStorage.SetAsync("user_email", Email);

                        // Verificar que se guardó correctamente
                        #if MACCATALYST
                        var savedToken = Preferences.Get("auth_token", string.Empty);
                        var savedEmail = Preferences.Get("user_email", string.Empty);
                        #else
                        var savedToken = await SecureStorage.GetAsync("auth_token");
                        var savedEmail = await SecureStorage.GetAsync("user_email");
                        #endif

                        System.Diagnostics.Debug.WriteLine($"✅ LOGIN: Token guardado correctamente: {!string.IsNullOrEmpty(savedToken)}");
                        System.Diagnostics.Debug.WriteLine($"✅ LOGIN: Email guardado: {savedEmail}");
                        System.Diagnostics.Debug.WriteLine($"✅ LOGIN: Longitud del token: {savedToken?.Length ?? 0} caracteres");
                        #endif

                        if (string.IsNullOrEmpty(savedToken))
                        {
                            ErrorMessage = "❌ ERROR: No se pudo guardar el token de sesión";
                            System.Diagnostics.Debug.WriteLine("❌ LOGIN: Error al guardar token");
                            return;
                        }
                    }
                    else
                    {
                        ErrorMessage = "❌ ERROR: Login exitoso pero el servidor no devolvió un token válido";
                        System.Diagnostics.Debug.WriteLine("⚠️ LOGIN: Login exitoso pero token vacío en la respuesta");
                        return;
                    }

                    // ✅ Limpiar campos sensibles
                    Email = string.Empty;
                    Password = string.Empty;

                    // ✅ Refrescar visibilidad de opciones de admin
                    System.Diagnostics.Debug.WriteLine("🔄 LOGIN: Refrescando visibilidad de opciones de admin...");
                    System.Diagnostics.Debug.WriteLine($"🔄 LOGIN: Email guardado: {Email}");
                    await _appShellViewModel.RefreshAdminVisibility();

                    // ✅ Navegación ABSOLUTA al Dashboard
                    System.Diagnostics.Debug.WriteLine("🚀 LOGIN: Navegando al Dashboard...");
                    await Shell.Current.GoToAsync($"//{nameof(DashboardPage)}");

                    System.Diagnostics.Debug.WriteLine("✅✅✅ LOGIN: ¡Proceso completado exitosamente!");
                }
                else
                {
                    // 4. Fallo: Mostrar el error específico
                    ErrorMessage = $"❌ ERROR DE LOGIN: {response.Error}";
                    System.Diagnostics.Debug.WriteLine($"❌❌❌ LOGIN FALLIDO: {response.Error}");

                    // Mensajes específicos según el error
                    if (response.Error.Contains("404"))
                    {
                        ErrorMessage += "\n💡 POSIBLE CAUSA: El endpoint de login no existe en el servidor";
                    }
                    else if (response.Error.Contains("401") || response.Error.Contains("403"))
                    {
                        ErrorMessage += "\n💡 POSIBLE CAUSA: Credenciales incorrectas o usuario no autorizado";
                    }
                    else if (response.Error.Contains("500"))
                    {
                        ErrorMessage += "\n💡 POSIBLE CAUSA: Error interno del servidor";
                    }
                    else if (response.Error.Contains("conectar") || response.Error.Contains("network"))
                    {
                        ErrorMessage += "\n💡 POSIBLE CAUSA: No hay conexión a internet o el servidor no está disponible";
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"❌ ERROR CRÍTICO: Problema de conexión con el servidor\nDetalles: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"💥💥💥 LOGIN ERROR CRÍTICO: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"💥 LOGIN: Tipo de excepción: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"💥 LOGIN: Stack trace: {ex.StackTrace}");

                // Mensajes específicos para diferentes tipos de error
                if (ex is HttpRequestException)
                {
                    ErrorMessage += "\n💡 POSIBLE CAUSA: No se puede conectar al servidor. Verifica:\n• Que el servidor esté ejecutándose\n• Que la URL http://192.168.1.8:8080 esté correcta\n• Que no haya firewall bloqueando la conexión";
                }
                else if (ex is System.Text.Json.JsonException)
                {
                    ErrorMessage += "\n💡 POSIBLE CAUSA: El servidor devolvió una respuesta en formato incorrecto";
                }
                else if (ex is TimeoutException)
                {
                    ErrorMessage += "\n💡 POSIBLE CAUSA: El servidor tardó demasiado en responder (timeout)";
                }
            }
            finally
            {
                IsBusy = false;
                System.Diagnostics.Debug.WriteLine($"🔄 LOGIN: Proceso terminado. IsBusy = {IsBusy}");
            }
        }
        [RelayCommand]
        private async Task TestConnection()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 TEST: Iniciando prueba de conexión...");

                // Mostrar mensaje de carga
                ErrorMessage = "🔍 Probando conexión con el servidor...";

                // Probar conexión básica
                bool isConnected = await _authService.TestConnectionAsync();

                if (isConnected)
                {
                    ErrorMessage = "✅ CONEXIÓN EXITOSA: El servidor responde correctamente";
                    System.Diagnostics.Debug.WriteLine("✅ TEST: Conexión exitosa");

                    await Application.Current.MainPage.DisplayAlert(
                        "✅ Conexión Exitosa",
                        $"El servidor en http://192.168.1.8:8080 está respondiendo correctamente.\n\nSi el login falla, revisa:\n• Credenciales correctas\n• Usuario registrado\n• Servidor funcionando",
                        "OK");
                }
                else
                {
                    ErrorMessage = "❌ CONEXIÓN FALLIDA: No se puede conectar al servidor";
                    System.Diagnostics.Debug.WriteLine("❌ TEST: Conexión fallida");

                    await Application.Current.MainPage.DisplayAlert(
                        "❌ Conexión Fallida",
                        $"No se puede conectar al servidor en http://192.168.1.8:8080\n\n💡 Verifica:\n• Que el servidor esté ejecutándose\n• Que la IP sea correcta\n• Que no haya firewall\n• Que tengas conexión a internet",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"❌ ERROR EN PRUEBA: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"💥 TEST ERROR: {ex.Message}");

                await Application.Current.MainPage.DisplayAlert(
                    "❌ Error en Prueba",
                    $"Error al probar conexión: {ex.Message}",
                    "OK");
            }
        }

        [RelayCommand]
        private async Task TestApiFlow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🧪 TEST: Iniciando prueba completa de API...");

                ErrorMessage = "🧪 Probando flujo completo: Login + Dashboard...";

                // Ejecutar la prueba completa
                string result = await _authService.TestFullFlowAsync();

                ErrorMessage = result;

                // Mostrar resultado en alerta
                await Application.Current.MainPage.DisplayAlert(
                    "Resultado de Prueba API",
                    result,
                    "OK");

                System.Diagnostics.Debug.WriteLine($"🧪 TEST RESULTADO: {result}");

            }
            catch (Exception ex)
            {
                ErrorMessage = $"❌ ERROR EN PRUEBA: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"💥 TEST ERROR: {ex.Message}");

                await Application.Current.MainPage.DisplayAlert(
                    "❌ Error en Prueba",
                    $"Error al ejecutar prueba: {ex.Message}",
                    "OK");
            }
        }

        [RelayCommand]
        private async Task ForgotPassword()
        {
            await Application.Current.MainPage.DisplayAlert(
                "Recuperar Contraseña",
                "Función en desarrollo",
                "OK");
        }
    }
}
