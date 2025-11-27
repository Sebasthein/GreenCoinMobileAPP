using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenCoinMovil.DTO;
using GreenCoinMovil.Models;
using GreenCoinMovil.UsuarioDTO;
using GreenCoinMovil.Views;
using Microsoft.Maui.Controls;      // Para Shell y Routing


namespace GreenCoinMovil.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly AuthService _authService;
        private readonly HttpClient _httpClient;
        private readonly ApiService _apiService;

        [ObservableProperty]
        private UsuarioDto _usuario;

        [ObservableProperty]
        private string _nivelActual = "Semilla";

        [ObservableProperty]
        private int _puntosTotales;

        [ObservableProperty]
        private int _logrosDesbloqueados = 0;

        [ObservableProperty]
        private int _totalLogros = 15;

        [ObservableProperty]
        private int _totalReciclajes;

        [ObservableProperty]
        private int _ranking;

        [ObservableProperty]
        private int _diasActivos = 0;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private List<ActividadDTO> _actividadesRecientes;

        [ObservableProperty]
        private string _welcomeMessage;

        [ObservableProperty]
        private string _logrosTexto;

        [ObservableProperty]
        private string _errorMessage;

        public DashboardViewModel(AuthService authService, HttpClient httpClient, ApiService apiService)
        {
            _authService = authService;
            _httpClient = httpClient;
            _apiService = apiService;

            // Inicializar primero con valores por defecto
            Usuario = new UsuarioDto();
            InicializarValoresPorDefecto();

            // Solo una llamada a LoadDashboardData con delay
            Task.Delay(1500).ContinueWith(_ =>
                MainThread.BeginInvokeOnMainThread(async () =>
                    await LoadDashboardData()));
        }

        [RelayCommand]
        public async Task LoadDashboardData()
        {
            if (IsBusy)
            {
                Console.WriteLine("⚠️ LoadDashboardData ignorado - ya está busy");
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            // DEBUG: Verificar estado inicial
            Console.WriteLine("=== DEBUG DASHBOARD ===");
            Console.WriteLine($"IsBusy: {IsBusy}");

            #if MACCATALYST
            var token = Preferences.Get("auth_token", string.Empty);
            #else
            var token = await SecureStorage.GetAsync("auth_token");
            #endif

            Console.WriteLine($"Token presente: {!string.IsNullOrEmpty(token)}");
            Console.WriteLine($"Token length: {token?.Length ?? 0}");
            Console.WriteLine($"API Service: {_apiService != null}");
            Console.WriteLine("=======================");

            try
            {
                // Cargar solo datos reales desde el API
                await CargarDatosReales();

                // Actualizar textos de la UI
                ActualizarTextos();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error cargando datos: {ex.Message}";
                Console.WriteLine($"💥 Error en LoadDashboardData: {ex.Message}");

                // En lugar de datos simulados, mostramos solo el error
                await Shell.Current.DisplayAlert("Error", ErrorMessage, "OK");

                // Inicializar con valores por defecto vacíos
                InicializarValoresPorDefecto();
                ActualizarTextos();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CargarDatosReales()
        {
            try
            {
                Console.WriteLine("🔍 Iniciando carga de datos del dashboard...");

                // Verificar token primero
                #if MACCATALYST
                var token = Preferences.Get("auth_token", string.Empty);
                #else
                var token = await SecureStorage.GetAsync("auth_token");
                #endif

                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("❌ No hay token de autenticación");
                    throw new Exception("No hay token de autenticación. Por favor, inicia sesión nuevamente.");
                }

                Console.WriteLine($"✅ Token encontrado, llamando al API...");

                // Llamamos al endpoint unificado del dashboard
                var datos = await _apiService.ObtenerDatosDashboardAsync();

                if (datos != null)
                {
                    Console.WriteLine("✅ Datos del dashboard recibidos correctamente");
                    Console.WriteLine($"📊 Datos recibidos - Nombre: {datos.UsuarioNombre}, Puntos: {datos.PuntosTotales}");

                    // 1. Actualizar el objeto UsuarioDto existente
                    Usuario.Nombre = datos.UsuarioNombre ?? "Usuario";
                    Usuario.Direccion = datos.Direccion ?? "Sin dirección";
                    Usuario.AvatarUrl = ProcesarAvatarUrl(datos.AvatarUrl);

                    // 2. Actualizar propiedades individuales
                    NivelActual = datos.NivelActual ?? "Semilla";
                    PuntosTotales = datos.PuntosTotales?? 0;
                    TotalReciclajes = (int)datos.TotalReciclajes;
                    DiasActivos = (int)datos.DiasActivos;
                    LogrosDesbloqueados = (int)datos.LogrosDesbloqueados;
                    Ranking = datos.Ranking ?? 0;

                    // 3. Actualizar lista de actividades recientes
                    if (datos.ActividadesRecientes != null && datos.ActividadesRecientes.Any())
                    {
                        ActividadesRecientes = new List<ActividadDTO>(datos.ActividadesRecientes);
                        Console.WriteLine($"✅ {ActividadesRecientes.Count} actividades cargadas");
                    }
                    else
                    {
                        ActividadesRecientes = new List<ActividadDTO>();
                        Console.WriteLine("ℹ️ No hay actividades recientes");
                    }

                    Console.WriteLine($"📊 Resumen: {PuntosTotales} pts, {TotalReciclajes} reciclajes, Nivel: {NivelActual}");
                }
                else
                {
                    Console.WriteLine("❌ No se recibieron datos del dashboard (null)");
                    throw new Exception("No se pudieron cargar los datos del dashboard");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error en CargarDatosReales: {ex.Message}");
                Console.WriteLine($"💥 StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void InicializarValoresPorDefecto()
        {
            // Intentar cargar información básica del usuario desde el almacenamiento
            try
            {
                #if MACCATALYST
                var email = Preferences.Get("user_email", "usuario@ejemplo.com");
                #else
                // Para otras plataformas, intentar obtener de forma síncrona (puede no funcionar)
                var email = "usuario@ejemplo.com"; // Fallback por ahora
                #endif

                // Extraer nombre del email o usar valor por defecto
                var nombreBase = email.Split('@')[0];
                Usuario.Nombre = nombreBase.Length > 0 ? char.ToUpper(nombreBase[0]) + nombreBase.Substring(1) : "Usuario";
                Usuario.Direccion = "Información pendiente";
                Usuario.AvatarUrl = $"https://api.dicebear.com/7.x/bottts/png?seed={email}";
            }
            catch
            {
                Usuario.Nombre = "Usuario";
                Usuario.Direccion = "Información pendiente";
                Usuario.AvatarUrl = "https://api.dicebear.com/7.x/bottts/png?seed=default";
            }

            PuntosTotales = 0;
            TotalReciclajes = 0;
            DiasActivos = 0;
            LogrosDesbloqueados = 0;
            Ranking = 0;
            NivelActual = "Semilla";

            ActividadesRecientes = new List<ActividadDTO>();

            // Inicializar textos básicos
            WelcomeMessage = $"¡Hola {Usuario.Nombre}! Estás contribuyendo a un planeta más verde.";
            LogrosTexto = "Comienza a reciclar para desbloquear tus primeros logros.";
        }

        private string ProcesarAvatarUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                // Avatar por defecto usando DiceBear
                #if MACCATALYST
                var email = Preferences.Get("user_email", "user");
                #else
                var email = SecureStorage.GetAsync("user_email").Result ?? "user";
                #endif
                return $"https://api.dicebear.com/7.x/bottts/png?seed={email}";
            }

            if (url.StartsWith("http"))
                return url; // Es DiceBear o web

            // Si es local, construir URL completa
            if (url.StartsWith("/uploads/"))
                return $"http://192.168.1.8:8080{url}";

            return url;
        }

        private void ActualizarTextos()
        {
            try
            {
                WelcomeMessage = $"¡Hola {Usuario?.Nombre ?? "Usuario"}! Estás contribuyendo a un planeta más verde. Tienes {PuntosTotales} puntos acumulados.";

                LogrosTexto = LogrosDesbloqueados > 0
                    ? $"Has desbloqueado {LogrosDesbloqueados} de {TotalLogros} logros. ¡Sigue así!"
                    : "Comienza a reciclar para desbloquear tus primeros logros.";

                Console.WriteLine($"✅ Textos actualizados: {WelcomeMessage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error actualizando textos: {ex.Message}");
                WelcomeMessage = "Bienvenido a GreenCoin";
                LogrosTexto = "Inicia tu viaje de reciclaje";
            }
        }

        // ... (los demás métodos permanecen igual)
        [RelayCommand]
        private async Task ShowQRCode()
        {
            await Shell.Current.DisplayAlert("Código QR",
                "Tu código QR personal se generará aquí para escanear en centros de reciclaje.",
                "OK");
        }

        [RelayCommand]
        private async Task RegisterRecycling()
        {
            try
            {
                await Shell.Current.GoToAsync("RecyclingPage");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error",
                    $"No se pudo abrir la página de reciclaje: {ex.Message}",
                    "OK");
            }
        }

        [RelayCommand]
        private async Task ViewAllHistory()
        {
            await Shell.Current.DisplayAlert("Historial Completo",
                "Aquí verás tu historial completo de actividades de reciclaje.",
                "OK");
        }

        [RelayCommand]
        private async Task ViewAllAchievements()
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(AchievementsPage));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Logros",
                    "Funcionalidad de logros en desarrollo.",
                    "OK");
            }
        }

        [RelayCommand]
        private async Task NavigateToProfile()
        {
            await Shell.Current.DisplayAlert("Perfil",
                "Ya te encuentras en tu perfil principal. Usa 'Editar perfil' para modificar tu información.",
                "OK");
        }

        [RelayCommand]
        public async Task CerrarSesion()
        {
            try
            {
                bool confirmar = await Application.Current.MainPage.DisplayAlert(
                    "Cerrar Sesión",
                    "¿Estás seguro de que quieres cerrar sesión?",
                    "Sí", "No");

                if (!confirmar) return;

                // Limpiar almacenamiento
                #if MACCATALYST
                Preferences.Remove("auth_token");
                Preferences.Remove("user_email");
                #else
                SecureStorage.Remove("auth_token");
                SecureStorage.Remove("user_email");
                #endif

                // Mostrar mensaje de confirmación
                await Application.Current.MainPage.DisplayAlert(
                    "Sesión cerrada",
                    "Has cerrado sesión correctamente",
                    "OK");

                // Redirigir al Login
                await Shell.Current.GoToAsync("//LoginPage");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Error al cerrar sesión: {ex.Message}",
                    "OK");
            }
        }

        private async Task VerificarYConfigurarToken()
        {
            try
            {
                // Verificar en el almacenamiento correcto según la plataforma
                #if MACCATALYST
                var token = Preferences.Get("auth_token", "");
                var email = Preferences.Get("user_email", "");
                Console.WriteLine("=== VERIFICACIÓN TOKEN DASHBOARD [MACCATALYST] ===");
                Console.WriteLine($"Token Preferences: {!string.IsNullOrEmpty(token)}");
                #else
                var token = await SecureStorage.GetAsync("auth_token");
                var email = await SecureStorage.GetAsync("user_email");
                Console.WriteLine("=== VERIFICACIÓN TOKEN DASHBOARD ===");
                Console.WriteLine($"Token SecureStorage: {!string.IsNullOrEmpty(token)}");
                #endif

                Console.WriteLine($"Token final: {!string.IsNullOrEmpty(token)}");
                Console.WriteLine($"Email: {email}");
                Console.WriteLine("===================================");

                if (string.IsNullOrEmpty(token))
                {
                    ErrorMessage = "No hay sesión activa. Por favor, inicia sesión nuevamente.";
                    await Shell.Current.DisplayAlert("Sesión Expirada", ErrorMessage, "OK");
                    await Shell.Current.GoToAsync("//LoginPage");
                    return;
                }

                // Configurar HttpClient con el token
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // Esperar un poco y cargar datos
                await Task.Delay(1000);
                await LoadDashboardData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error en VerificarYConfigurarToken: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task RefreshData()
        {
            await LoadDashboardData();
        }

        // Propiedad computada para mostrar actividades formateadas
        public string ActividadesResumen =>
            ActividadesRecientes?.Any() == true
                ? $"{ActividadesRecientes.Count} actividades recientes"
                : "No hay actividades recientes";

        // Propiedad para el progreso de logros
        public double ProgresoLogros =>
            TotalLogros > 0 ? (double)LogrosDesbloqueados / TotalLogros : 0;
    

    [RelayCommand]
        private async Task DebugStorage()
        {
            #if MACCATALYST
            var tokenPrefs = Preferences.Get("auth_token", "");
            var emailPrefs = Preferences.Get("user_email", "");

            await Shell.Current.DisplayAlert("Debug Storage [MACCATALYST]",
                $"Preferences Token: {!string.IsNullOrEmpty(tokenPrefs)}\n" +
                $"Preferences Email: {emailPrefs}\n" +
                $"Token Preview: {tokenPrefs?.Substring(0, Math.Min(20, tokenPrefs?.Length ?? 0))}...",
                "OK");
            #else
            var tokenSecure = await SecureStorage.GetAsync("auth_token");
            var tokenPrefs = Preferences.Get("auth_token", "");
            var emailSecure = await SecureStorage.GetAsync("user_email");
            var emailPrefs = Preferences.Get("user_email", "");

            await Shell.Current.DisplayAlert("Debug Storage",
                $"SecureStorage Token: {!string.IsNullOrEmpty(tokenSecure)}\n" +
                $"Preferences Token: {!string.IsNullOrEmpty(tokenPrefs)}\n" +
                $"SecureStorage Email: {emailSecure}\n" +
                $"Preferences Email: {emailPrefs}\n" +
                $"Token Preview: {(tokenSecure ?? tokenPrefs)?.Substring(0, Math.Min(20, (tokenSecure ?? tokenPrefs)?.Length ?? 0))}...",
                "OK");
            #endif
        }
    }
}