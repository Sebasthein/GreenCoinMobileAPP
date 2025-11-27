using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenCoinMovil.DTO;
using GreenCoinMovil.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace GreenCoinMovil.ViewModels
{
    public partial class AdminDashboardViewModel : ObservableObject
    {
        
        private readonly ApiService _apiService;
        private readonly AuthService _authService;
        private readonly HttpClient _httpClient;

        public AdminDashboardViewModel(ApiService apiService, AuthService authService)
        {
            _apiService = apiService;
            _authService = authService;
            reciclajesPendientes = new ObservableCollection<ReciclajeDTO>();

            // Cargar datos iniciales
            Task.Run(async () => await CargarDatosIniciales());
        }

        [ObservableProperty]
        private string welcomeMessage = "Bienvenido Administrador";

        [ObservableProperty]
        private int pendientesCount;

        [ObservableProperty]
        private int porValidarCount;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isEmptyList;

        [ObservableProperty]
        private ObservableCollection<ReciclajeDTO> reciclajesPendientes;

        [ObservableProperty]
        private string errorMessage;

        private async Task CargarDatosIniciales()
        {
            await CargarReciclajesPendientes();
            await ActualizarWelcomeMessage();
        }

        private async Task ActualizarWelcomeMessage()
        {
            try
            {
                var email = await SecureStorage.GetAsync("user_email");
                WelcomeMessage = $"Bienvenido: {email ?? "Administrador"}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo email: {ex.Message}");
                WelcomeMessage = "Bienvenido Administrador";
            }
        }

        public async Task<List<ReciclajeDTO>> ObtenerReciclajesPendientesAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<ReciclajeDTO>>("/api/admin/reciclajes/pendientes");
        }

        public async Task<bool> AprobarReciclajeAsync(long reciclajeId)
        {
            var response = await _httpClient.PostAsync($"/api/admin/reciclajes/{reciclajeId}/aprobar", null);
            return response.IsSuccessStatusCode;
        }



        [RelayCommand]
        public async Task CargarReciclajesPendientes()
        {
            if (IsLoading) return;
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var reciclajes = await _apiService.ObtenerReciclajesPendientesAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ReciclajesPendientes.Clear();
                    foreach (var reciclaje in reciclajes)
                    {
                        // Convertir ruta a URL accesible
                        reciclaje.ImagenUrl = ConvertirRutaAUrl(reciclaje.ImagenUrl);

                        // Formatear datos para mejor visualización
                        reciclaje.CantidadFormateada = $"{reciclaje.Cantidad} ";
                        reciclaje.FechaFormateada = reciclaje.Fecha.ToString("dd/MM/yyyy HH:mm");

                        ReciclajesPendientes.Add(reciclaje);
                    }

                    // Actualizar contadores
                    PendientesCount = reciclajes.Count;
                    PorValidarCount = reciclajes.Count(r => !r.Validado);
                    IsEmptyList = !reciclajes.Any();

                    Console.WriteLine($"✅ Cargados {reciclajes.Count} reciclajes pendientes");
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error cargando reciclajes: {ex.Message}";
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string ConvertirRutaAUrl(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine("⚠️ Ruta de imagen vacía");
                return "no_image.png";
            }

            // Si ya es una URL completa, verificar si es accesible
            if (path.StartsWith("http"))
            {
                Console.WriteLine($"🌐 URL ya completa: {path}");
                // Para debugging, podríamos verificar si la URL es accesible
                // pero por ahora la dejamos como está
                return path;
            }

            // Tu IP local - AJUSTA ESTO SEGÚN TU SERVIDOR
            string baseUrl = "http://192.168.1.8:8080";

            // Limpiar la ruta
            string cleanPath = path.Replace("\\", "/").TrimStart('/');
            string fullUrl = $"{baseUrl}/{cleanPath}";

            Console.WriteLine($"🖼️ Convirtiendo ruta: {path} -> {fullUrl}");

            // Nota: Si las imágenes no se cargan, es porque:
            // 1. El backend no está sirviendo archivos estáticos desde /uploads/
            // 2. Las imágenes no existen en el servidor
            // 3. La IP del servidor es incorrecta

            return fullUrl;
        }

        [RelayCommand]
        public async Task AprobarReciclaje(long reciclajeId)
        {
            try
            {
                bool confirmar = await Application.Current.MainPage.DisplayAlert(
                    "✅ Aprobar Reciclaje",
                    "¿Estás seguro de aprobar este reciclaje?\n\nEl usuario recibirá los puntos correspondientes.",
                    "Sí, Aprobar", "Cancelar");

                if (!confirmar) return;

                var response = await _apiService.AprobarReciclajeAsync(reciclajeId);

                if (response.Success)
                {
                    await Application.Current.MainPage.DisplayAlert("✅ Éxito",
                        response.Message ?? "Reciclaje aprobado correctamente\n\nLos puntos han sido asignados al usuario.",
                        "OK");

                    // Remover de la lista y actualizar
                    var reciclaje = ReciclajesPendientes.FirstOrDefault(r => r.Id == reciclajeId);
                    if (reciclaje != null)
                    {
                        ReciclajesPendientes.Remove(reciclaje);
                        ActualizarContadores();
                    }

                    // Recargar para asegurar datos actualizados
                    await CargarReciclajesPendientes();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("❌ Error",
                        "No se pudo aprobar el reciclaje", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("❌ Error",
                    $"Error al aprobar: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task RechazarReciclaje(long reciclajeId)
        {
            try
            {
                string motivo = await Application.Current.MainPage.DisplayPromptAsync(
                    "❌ Rechazar Reciclaje",
                    "Ingresa el motivo del rechazo para informar al usuario:",
                    "Rechazar", "Cancelar",
                    "Ej: Material incorrecto, foto no clara...",
                    maxLength: 200);

                if (string.IsNullOrWhiteSpace(motivo))
                {
                    await Application.Current.MainPage.DisplayAlert("Info",
                        "Debes ingresar un motivo para rechazar.", "OK");
                    return;
                }

                if (motivo == "Cancelar") return;

                var response = await _apiService.RechazarReciclajeAsync(reciclajeId, motivo);

                if (response.Success)
                {
                    await Application.Current.MainPage.DisplayAlert("✅ Éxito",
                        response.Message ?? "Reciclaje rechazado correctamente\n\nEl usuario ha sido notificado.",
                        "OK");

                    // Remover de la lista
                    var reciclaje = ReciclajesPendientes.FirstOrDefault(r => r.Id == reciclajeId);
                    if (reciclaje != null)
                    {
                        ReciclajesPendientes.Remove(reciclaje);
                        ActualizarContadores();
                    }

                    await CargarReciclajesPendientes();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("❌ Error",
                        "No se pudo rechazar el reciclaje", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("❌ Error",
                    $"Error al rechazar: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task VerDetallesReciclaje(long reciclajeId)
        {
            var reciclaje = ReciclajesPendientes.FirstOrDefault(r => r.Id == reciclajeId);
            if (reciclaje != null)
            {
                await Application.Current.MainPage.DisplayAlert("📋 Detalles del Reciclaje",
                    $"👤 Usuario: {reciclaje.UsuarioNombre}\n" +
                    $"📦 Material: {reciclaje.MaterialNombre}\n" +
                    $"⚖️ Cantidad: {reciclaje.Cantidad} kg\n" +
                    $"📅 Fecha: {reciclaje.Fecha:dd/MM/yyyy HH:mm}\n" +
                    $"💬 Observaciones: {reciclaje.Observaciones ?? "Ninguna"}\n" +
                    $"🆔 ID: {reciclaje.Id}",
                    "OK");
            }
        }

        [RelayCommand]
        public async Task CerrarSesion()
        {
            bool confirmar = await Application.Current.MainPage.DisplayAlert(
                "🚪 Cerrar Sesión",
                "¿Estás seguro de que quieres cerrar sesión?",
                "Sí, Cerrar", "Cancelar");

            if (!confirmar) return;

            // Limpiar almacenamiento
            SecureStorage.Remove("auth_token");
            SecureStorage.Remove("user_email");

            // Redirigir al login
            await Shell.Current.GoToAsync("//LoginPage");
        }

        private void ActualizarContadores()
        {
            PendientesCount = ReciclajesPendientes.Count;
            PorValidarCount = ReciclajesPendientes.Count(r => !r.Validado);
            IsEmptyList = !ReciclajesPendientes.Any();
        }
    }
}