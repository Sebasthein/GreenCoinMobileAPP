using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenCoinMovil.DTO; // Asegúrate de tener tus DTOs aquí
using GreenCoinMovil.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;

namespace GreenCoinMovil.ViewModels
{
    public partial class AdminValidationViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private ObservableCollection<ReciclajeDTO> _pendientes;

        public AdminValidationViewModel(ApiService apiService)
        {
            _apiService = apiService;
            _pendientes = new ObservableCollection<ReciclajeDTO>();
            _ = LoadInitialDataAsync();
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                Debug.WriteLine("🔄 AdminValidationViewModel: Cargando datos iniciales...");
                await CargarPendientesCommand.ExecuteAsync(null);
                Debug.WriteLine("✅ AdminValidationViewModel: Datos iniciales cargados");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 AdminValidationViewModel: Error cargando datos iniciales: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task CargarPendientes()
        {
            IsBusy = true;
            try
            {
                Debug.WriteLine("🔄 AdminValidationViewModel: Cargando reciclajes pendientes...");
                var lista = await _apiService.ObtenerPendientesAsync();

                if (lista != null)
                {
                    Debug.WriteLine($"📊 AdminValidationViewModel: {lista.Count} reciclajes pendientes encontrados");
                    Pendientes.Clear();
                    foreach (var item in lista)
                    {
                        // Debug: Verificar información del usuario
                        Debug.WriteLine($"🔍 Reciclaje ID {item.Id}: Usuario={item.UsuarioNombre}, Material={item.MaterialNombre}");

                        // TRUCO: Si la URL viene como ruta de archivo local (C:\...),
                        // necesitamos convertirla a URL http para que el celular la vea.
                        // Si tu backend ya devuelve URL http, borra esta línea.
                        item.ImagenUrl = ConvertirRutaAUrl(item.ImagenUrl);
                        Pendientes.Add(item);
                    }
                    Debug.WriteLine($"✅ AdminValidationViewModel: {Pendientes.Count} reciclajes agregados a la lista");
                }
                else
                {
                    Debug.WriteLine("⚠️ AdminValidationViewModel: Lista de pendientes es null");
                    Pendientes.Clear();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 AdminValidationViewModel: Error cargando pendientes: {ex.Message}");
                Debug.WriteLine($"💥 AdminValidationViewModel: StackTrace: {ex.StackTrace}");
                await Shell.Current.DisplayAlert("Error", $"Error al cargar reciclajes pendientes: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task Aprobar(ReciclajeDTO reciclaje)
        {
            if (reciclaje == null) return;

            try
            {
                bool confirm = await Shell.Current.DisplayAlert("Validar",
                    $"¿Aprobar reciclaje de {reciclaje.MaterialNombre}?", "Sí", "Cancelar");

                if (confirm)
                {
                    Debug.WriteLine($"🔄 AdminValidationViewModel: Aprobando reciclaje ID {reciclaje.Id}");
                    var response = await _apiService.ValidarReciclajeAsync(reciclaje.Id);
                    if (response.Success)
                    {
                        Pendientes.Remove(reciclaje); // Lo quitamos de la lista
                        Debug.WriteLine($"✅ AdminValidationViewModel: Reciclaje ID {reciclaje.Id} aprobado");
                        await Shell.Current.DisplayAlert("Éxito", response.Message ?? "Reciclaje validado y puntos asignados.", "OK");
                    }
                    else
                    {
                        Debug.WriteLine($"❌ AdminValidationViewModel: Error aprobando reciclaje ID {reciclaje.Id}: {response.Message}");
                        await Shell.Current.DisplayAlert("Error", response.Message ?? "No se pudo validar.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 AdminValidationViewModel: Error en Aprobar: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", $"Error al aprobar reciclaje: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task Rechazar(ReciclajeDTO reciclaje)
        {
            if (reciclaje == null)
            {
                Debug.WriteLine("⚠️ AdminValidationViewModel: Reciclaje es null, cancelando operación");
                return;
            }

            try
            {
                Debug.WriteLine($"🔄 AdminValidationViewModel: Solicitando motivo para rechazar reciclaje ID {reciclaje.Id}");
                string motivo = await Shell.Current.DisplayPromptAsync("Rechazar Reciclaje",
                    "Ingrese el motivo del rechazo:", "Rechazar", "Cancelar");

                if (!string.IsNullOrWhiteSpace(motivo))
                {
                    Debug.WriteLine($"🔄 AdminValidationViewModel: Rechazando reciclaje ID {reciclaje.Id} con motivo: '{motivo}'");
                    var response = await _apiService.RechazarReciclajeAsync(reciclaje.Id, motivo);

                    if (response.Success)
                    {
                        Pendientes.Remove(reciclaje); // Lo quitamos de la lista
                        Debug.WriteLine($"✅ AdminValidationViewModel: Reciclaje ID {reciclaje.Id} rechazado exitosamente");
                        await Shell.Current.DisplayAlert("Éxito", response.Message ?? "Reciclaje rechazado.", "OK");
                    }
                    else
                    {
                        Debug.WriteLine($"❌ AdminValidationViewModel: Error rechazando - API retornó fallo: {response.Message}");
                        Debug.WriteLine($"❌ AdminValidationViewModel: Error detallado: {response.Error}");
                        await Shell.Current.DisplayAlert("Error", response.Message ?? "No se pudo rechazar.", "OK");
                    }
                }
                else
                {
                    Debug.WriteLine("⚠️ AdminValidationViewModel: Usuario canceló el rechazo o no ingresó motivo");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 AdminValidationViewModel: Error en Rechazar: {ex.Message}");
                Debug.WriteLine($"💥 AdminValidationViewModel: StackTrace: {ex.StackTrace}");
                await Shell.Current.DisplayAlert("Error", $"Error al rechazar reciclaje: {ex.Message}", "OK");
            }
        }

        // Método auxiliar para convertir rutas de imagen a URLs absolutas
        private string ConvertirRutaAUrl(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.WriteLine("⚠️ AdminValidationViewModel: ImagenUrl es null o vacío");
                return null; // Devolver null para que el converter muestre el placeholder
            }

            Debug.WriteLine($"🔄 AdminValidationViewModel: Convirtiendo imagen path: '{path}'");

            // NOTA: El endpoint de imágenes requiere autenticación, pero el control Image de MAUI
            // no puede enviar headers de auth. Por ahora, retornamos null para mostrar placeholder.
            // Para solucionar: hacer el endpoint /api/reciclajes/imagen/{filename} público en el backend.

            Debug.WriteLine("⚠️ AdminValidationViewModel: Endpoint de imágenes requiere autenticación - mostrando placeholder");
            return null; // Forzar mostrar placeholder hasta que el backend haga el endpoint público
        }
    }
}