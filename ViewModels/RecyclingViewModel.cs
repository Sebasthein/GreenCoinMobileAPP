using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO; // Necesario para leer el archivo de la foto
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenCoinMovil.DTO;
using GreenCoinMovil.Models; // Asegúrate de que esto apunte a donde está tu ApiService
using GreenCoinMovil.Views;

namespace GreenCoinMovil.ViewModels
{
    public partial class RecyclingViewModel : ObservableObject
    {
        // Inyectamos ApiService para la lógica nueva y HttpClient para la antigua (historial/validaciones)
        private readonly ApiService _apiService;
        private readonly HttpClient _httpClient;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private ObservableCollection<MaterialReciclaje> _materiales;

        [ObservableProperty]
        private bool _hasScanResult;

        [ObservableProperty]
        private string _scanResult;

        [ObservableProperty]
        private bool _hasPhoto;

        [ObservableProperty]
        private ImageSource _capturedPhoto;

        [ObservableProperty]
        private string _photoPath;

        // --- CAMBIO IMPORTANTE: Propiedades para la UI ---
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NombreMaterialSeleccionado))] // Actualiza el Label
        [NotifyPropertyChangedFor(nameof(IsMaterialSelected))]       // Habilita el botón
        private MaterialReciclaje _materialSeleccionado;

        public string NombreMaterialSeleccionado => MaterialSeleccionado?.Tipo;
        public bool IsMaterialSelected => MaterialSeleccionado != null;
        // ------------------------------------------------

        [ObservableProperty]
        private string _accionPendiente;

        [ObservableProperty]
        private int _validacionesPendientes;

        [ObservableProperty]
        private bool _tieneNotificaciones;

        [ObservableProperty]
        private string _observaciones;

        private Timer _estadoTimer;

        // Constructor Actualizado: Recibe ApiService y HttpClient
        public RecyclingViewModel(ApiService apiService, HttpClient httpClient)
        {
            _apiService = apiService;
            _httpClient = httpClient; // Mantenemos esto para tus métodos de historial/validaciones existentes

            Materiales = new ObservableCollection<MaterialReciclaje>();

            // Cargar materiales al inicio
            CargarMaterialesCommand.Execute(null);

            // Iniciar verificación automática
            IniciarVerificacionAutomatica();
        }

        private void IniciarVerificacionAutomatica()
        {
            _estadoTimer = new Timer(async _ =>
            {
                await VerificarEstadoValidaciones();
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        }

        #region Métodos de Estado y Validación (Lógica Antigua mantenida con HttpClient)

        [RelayCommand]
        private async Task VerificarEstadoValidaciones()
        {
            try
            {
                // Nota: Idealmente deberías mover esto al ApiService en el futuro
                var response = await _httpClient.GetAsync("api/reciclajes/estado-validaciones");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var estado = JsonSerializer.Deserialize<EstadoValidacionesDTO>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ValidacionesPendientes = estado?.Pendientes ?? 0;
                        TieneNotificaciones = estado?.TieneAprobadosRecientes ?? false;

                        if (estado?.TieneAprobadosRecientes == true)
                        {
                            MostrarNotificacionAprobados(estado);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verificando estado: {ex.Message}");
            }
        }

        private async void MostrarNotificacionAprobados(EstadoValidacionesDTO estado)
        {
            await Shell.Current.DisplayAlert("🎉 ¡Validaciones Aprobadas!",
                $"Tienes {estado.AprobadosRecientes} reciclajes aprobados recientemente.\n\n" +
                $"Total de puntos ganados: +{estado.PuntosGanados}", "Ver Detalles");

            await VerEstadoValidaciones();
        }

        [RelayCommand]
        private async Task VerEstadoValidaciones()
        {
            try
            {
                IsBusy = true;
                var response = await _httpClient.GetAsync("api/reciclajes/mis-validaciones");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var validaciones = JsonSerializer.Deserialize<List<ReciclajeDTO>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (validaciones != null && validaciones.Any())
                    {
                        await MostrarEstadoValidaciones(validaciones);
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("📋 Mis Validaciones",
                            "No tienes reciclajes pendientes de validación.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("❌ Error", $"Error al cargar validaciones: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task MostrarEstadoValidaciones(List<ReciclajeDTO> validaciones)
        {
            var mensaje = new StringBuilder();
            mensaje.AppendLine("📋 **Estado de tus validaciones:**\n");

            var pendientes = validaciones.Where(v => v.Estado == "Pendiente").ToList();
            var aprobados = validaciones.Where(v => v.Estado == "Aprobado").ToList();
            var rechazados = validaciones.Where(v => v.Estado == "Rechazado").ToList();

            if (pendientes.Any())
            {
                mensaje.AppendLine($"⏳ **Pendientes:** {pendientes.Count}");
                foreach (var item in pendientes.Take(3))
                    mensaje.AppendLine($"   • {item.MaterialNombre} - {item.Fecha:dd/MM/yyyy}");

                if (pendientes.Count > 3) mensaje.AppendLine($"   ... y {pendientes.Count - 3} más");
                mensaje.AppendLine();
            }

            if (aprobados.Any())
            {
                mensaje.AppendLine($"✅ **Aprobados:** {aprobados.Count}");
                foreach (var item in aprobados.Take(2))
                    mensaje.AppendLine($"   • {item.MaterialNombre} - +{item.PuntosGanados} pts");
                mensaje.AppendLine();
            }

            if (rechazados.Any())
            {
                mensaje.AppendLine($"❌ **Rechazados:** {rechazados.Count}");
                foreach (var item in rechazados.Take(2))
                {
                    var motivo = string.IsNullOrEmpty(item.ObservacionesAdmin) ? "Sin motivo" : item.ObservacionesAdmin;
                    mensaje.AppendLine($"   • {item.MaterialNombre} - {motivo}");
                }
            }

            await Shell.Current.DisplayAlert("📊 Estado de Validaciones", mensaje.ToString(), "OK");
        }

        [RelayCommand]
        private async Task VerHistorialCompleto()
        {
            try
            {
                Console.WriteLine("📊 Navegando a la página de historial...");
                await Shell.Current.GoToAsync($"///{nameof(HistoryPage)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error navegando al historial: {ex.Message}");
                await Shell.Current.DisplayAlert("❌ Error", $"Error al abrir el historial: {ex.Message}", "OK");
            }
        }

        private async Task MostrarHistorialDetallado(List<ReciclajeDTO> historial)
        {
            var opcion = await Shell.Current.DisplayActionSheet(
                "📊 Ver Historial por:", "Cancelar", null,
                "✅ Todos los Aprobados", "⏳ Pendientes", "❌ Rechazados", "📅 Este Mes", "🏆 Mis Puntos");

            IEnumerable<ReciclajeDTO> filtrado = historial;

            switch (opcion)
            {
                case "✅ Todos los Aprobados": filtrado = historial.Where(h => h.Estado == "Aprobado"); break;
                case "⏳ Pendientes": filtrado = historial.Where(h => h.Estado == "Pendiente"); break;
                case "❌ Rechazados": filtrado = historial.Where(h => h.Estado == "Rechazado"); break;
                case "📅 Este Mes": filtrado = historial.Where(h => h.Fecha.Month == DateTime.Now.Month); break;
                case "🏆 Mis Puntos":
                    var totalPuntos = historial.Where(h => h.Estado == "Aprobado").Sum(h => h.PuntosGanados);
                    await Shell.Current.DisplayAlert("🏆 Total de Puntos", $"Tienes un total de: {totalPuntos} puntos", "OK");
                    return;
            }

            var mensaje = new StringBuilder();
            mensaje.AppendLine($"📊 **Historial ({filtrado.Count()} items):**\n");

            foreach (var item in filtrado.OrderByDescending(h => h.Fecha).Take(10))
            {
                var icono = item.Estado switch { "Aprobado" => "✅", "Pendiente" => "⏳", "Rechazado" => "❌", _ => "📝" };
                mensaje.AppendLine($"{icono} {item.MaterialNombre}");
                mensaje.AppendLine($"   📅 {item.Fecha:dd/MM/yyyy}");

                if (item.Estado == "Aprobado") mensaje.AppendLine($"   🏆 +{item.PuntosGanados} puntos");
                else if (item.Estado == "Rechazado" && !string.IsNullOrEmpty(item.ObservacionesAdmin)) mensaje.AppendLine($"   💬 {item.ObservacionesAdmin}");

                mensaje.AppendLine();
            }

            await Shell.Current.DisplayAlert("📋 Historial Detallado", mensaje.ToString(), "OK");
        }

        #endregion

        #region Métodos de Carga de Materiales (ACTUALIZADO para usar ApiService)

        [RelayCommand]
        private async Task CargarMateriales()
        {
            IsBusy = true;
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Iniciando carga de materiales desde API...");
                var materialesApi = await _apiService.GetMaterialsAsync();
                System.Diagnostics.Debug.WriteLine($"📦 Materiales obtenidos de API: {materialesApi?.Count ?? 0}");

                if (materialesApi != null && materialesApi.Any())
                {
                    Materiales.Clear();
                    foreach (var materialApi in materialesApi)
                    {
                        System.Diagnostics.Debug.WriteLine($"➕ Agregando material: {materialApi.Nombre} - {materialApi.PuntosPorUnidad} pts");
                        var material = new MaterialReciclaje
                        {
                            Id = materialApi.Id,
                            // ✅ CORREGIDO: Usa las propiedades correctas según tu DTO
                            Tipo = materialApi.Nombre ?? "Material",
                            Puntos = materialApi.PuntosPorUnidad, // Ajusta según tu DTO
                            Icono = ObtenerIconoPorTipo(materialApi.Nombre),
                            Color = ObtenerColorPorTipo(materialApi.Nombre),
                            Descripcion = materialApi.Descripcion ?? "Material reciclable"
                        };
                        Materiales.Add(material);
                    }
                    System.Diagnostics.Debug.WriteLine($"✅ Materiales cargados desde API: {Materiales.Count}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ No se obtuvieron materiales de API, cargando por defecto...");
                    CargarMaterialesPorDefecto();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando materiales: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                CargarMaterialesPorDefecto();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void CargarMaterialesPorDefecto()
        {
            Materiales.Clear();
            var materialesDefault = new List<MaterialReciclaje>
            {
                new MaterialReciclaje { Id = 1, Tipo = "Plástico", Puntos = 50, Icono = "🥤", Color = "#00b894", Descripcion = "Botellas, envases y otros plásticos" },
                new MaterialReciclaje { Id = 2, Tipo = "Vidrio", Puntos = 30, Icono = "🍶", Color = "#00cec9", Descripcion = "Botellas y frascos de vidrio" },
                new MaterialReciclaje { Id = 3, Tipo = "Papel", Puntos = 20, Icono = "📄", Color = "#74b9ff", Descripcion = "Periódicos, revistas y cartón" },
                new MaterialReciclaje { Id = 4, Tipo = "Metal", Puntos = 40, Icono = "🥫", Color = "#a29bfe", Descripcion = "Latas y objetos metálicos" }
            };
            foreach (var m in materialesDefault) Materiales.Add(m);
        }

        private string ObtenerIconoPorTipo(string tipo) => tipo?.ToLower() switch
        {
            "plástico" or "plastic" => "🥤",
            "vidrio" or "glass" => "🍶",
            "papel" or "paper" => "📄",
            "metal" or "metal" => "🥫",
            "cartón" or "cardboard" => "📦",
            "orgánico" or "organic" => "🍂",
            _ => "♻️"
        };

        private string ObtenerColorPorTipo(string tipo) => tipo?.ToLower() switch
        {
            "plástico" or "plastic" => "#00b894",
            "vidrio" or "glass" => "#00cec9",
            "papel" or "paper" => "#74b9ff",
            "metal" or "metal" => "#a29bfe",
            "cartón" or "cardboard" => "#fd79a8",
            "orgánico" or "organic" => "#e17055",
            _ => "#636e72"
        };

        #endregion

        #region Métodos de Cámara y Fotos

        [RelayCommand]
        private async Task TakePhoto()
        {
            try
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await Shell.Current.DisplayAlert("Error", "Cámara no disponible", "OK");
                    return;
                }

                var cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                if (cameraStatus != PermissionStatus.Granted)
                {
                    await Shell.Current.DisplayAlert("Permiso requerido", "Se necesita permiso de cámara", "OK");
                    return;
                }

                var photoFile = await MediaPicker.Default.CapturePhotoAsync();

                if (photoFile != null)
                {
                    var savedFilePath = await SavePhotoToFolder(photoFile);

                    if (!string.IsNullOrEmpty(savedFilePath))
                    {
                        CapturedPhoto = ImageSource.FromFile(savedFilePath);
                        HasPhoto = true;
                        PhotoPath = savedFilePath;

                        AccionPendiente = "Foto lista para validación";
                        // Limpiar selección previa para obligar a elegir de nuevo
                        MaterialSeleccionado = null;
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Error con la cámara: {ex.Message}", "OK");
            }
        }

        private async Task<string> SavePhotoToFolder(FileResult photo)
        {
            try
            {
                var fecha = DateTime.Now.ToString("yyyy-MM");
                var folderPath = Path.Combine(FileSystem.AppDataDirectory, "RecyclingPhotos", fecha);
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"material_{timestamp}.jpg";
                var fullPath = Path.Combine(folderPath, fileName);

                using (var sourceStream = await photo.OpenReadAsync())
                using (var fileStream = File.OpenWrite(fullPath))
                {
                    await sourceStream.CopyToAsync(fileStream);
                }
                return fullPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error guardando foto: {ex.Message}");
                return null;
            }
        }

        [RelayCommand]
        private async Task UsePhoto()
        {
            if (!string.IsNullOrEmpty(PhotoPath))
            {
                var materialOptions = Materiales.Select(m => m.Tipo).ToArray();
                var selectedMaterial = await Shell.Current.DisplayActionSheet(
                    "📋 ¿Qué material estás reciclando?", "Cancelar", null, materialOptions);

                if (!string.IsNullOrEmpty(selectedMaterial) && selectedMaterial != "Cancelar")
                {
                    var material = Materiales.FirstOrDefault(m => m.Tipo == selectedMaterial);
                    if (material != null)
                    {
                        // Esto activará la propiedad IsMaterialSelected automáticamente
                        MaterialSeleccionado = material;

                        // Pedimos observaciones (Opcional)
                        Observaciones = await Shell.Current.DisplayPromptAsync(
                            "💬 Información extra",
                            "Agrega detalles para el admin:", "OK", "Saltar", "Ej: Botellas limpias") ?? "";

                        AccionPendiente = $"✅ {material.Tipo} listo para enviar";
                    }
                }
            }
        }

        // --- MÉTODO PRINCIPAL ARREGLADO: Registrar con Foto ---
        [RelayCommand]
        private async Task RegisterRecyclingWithPhoto()
        {
            // 1. Validaciones Previas
            if (MaterialSeleccionado == null)
            {
                await Shell.Current.DisplayAlert("Falta información", "Por favor selecciona el tipo de material.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(PhotoPath) || !File.Exists(PhotoPath))
            {
                await Shell.Current.DisplayAlert("Error", "No se encuentra la foto.", "OK");
                return;
            }

            // 2. Confirmación
            var confirmacion = await Shell.Current.DisplayAlert(
                 "📤 Enviar para Validación",
                 $"¿Enviar reciclaje de **{MaterialSeleccionado.Tipo}**?\n\n" +
                 $"El administrador revisará la foto.",
                 "Sí, Enviar", "Cancelar");

            if (!confirmacion) return;

            try
            {
                IsBusy = true;

                // 3. Leer bytes del archivo
                byte[] fotoBytes = await File.ReadAllBytesAsync(PhotoPath);

                Console.WriteLine($"📸 Foto leída - Tamaño: {fotoBytes.Length} bytes");
                Console.WriteLine($"🎯 Material seleccionado - ID: {MaterialSeleccionado.Id}, Nombre: {MaterialSeleccionado.Tipo}");

                // 4. Enviar usando ApiService
                bool exito = await _apiService.RegistrarReciclajeConFotoAsync(
                    MaterialSeleccionado.Id,
                    fotoBytes,
                    1.0 // Cantidad por defecto
                );

                if (exito)
                {
                    await Shell.Current.DisplayAlert("✅ ¡Enviado!",
                        "Tu reciclaje ha sido enviado al administrador para validación.\n\n" +
                        "Recibirás una notificación cuando sea validado.",
                        "Genial");

                    ResetearEstado();
                    await CargarMateriales(); // Refrescar materiales
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error",
                        "No se pudo enviar el reciclaje. Verifica tu conexión e intenta nuevamente.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error en RegisterRecyclingWithPhoto: {ex.Message}");
                await Shell.Current.DisplayAlert("Error",
                    $"No se pudo completar la operación: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RetakePhoto()
        {
            HasPhoto = false;
            CapturedPhoto = null;
            PhotoPath = null;
            MaterialSeleccionado = null;
            AccionPendiente = null;
        }

        #endregion

        #region Métodos de QR (Mantenidos Igual)

        [RelayCommand]
        private async Task ScanQR()
        {
            var option = await Shell.Current.DisplayActionSheet(
                "¿Cómo quieres escanear?", "Cancelar", null, "📷 Tomar Foto del QR", "🔢 Ingresar Manualmente");

            if (option == "📷 Tomar Foto del QR") await TakePhotoForQR();
            else if (option == "🔢 Ingresar Manualmente") await ProcessManualQRInput();
        }

        private async Task TakePhotoForQR()
        {
            try
            {
                if (!MediaPicker.Default.IsCaptureSupported) return;
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    // Aquí iría tu lógica de decodificación de QR real si la tuvieras
                    // Por ahora simulamos que pide el código
                    var qrCode = await Shell.Current.DisplayPromptAsync("QR Detectado", "Ingresa el código leído:", "OK", "Cancelar");
                    if (!string.IsNullOrEmpty(qrCode)) await ProcesarCodigoQR(qrCode);
                }
            }
            catch { }
        }

        private async Task ProcessManualQRInput()
        {
            var qrCode = await Shell.Current.DisplayPromptAsync("Código Manual", "Ingresa el código:", "OK", "Cancelar");
            if (!string.IsNullOrEmpty(qrCode)) await ProcesarCodigoQR(qrCode);
        }

        private async Task ProcesarCodigoQR(string qrCode)
        {
            if (qrCode.StartsWith("MATERIAL_"))
            {
                var id = qrCode.Replace("MATERIAL_", "");
                var material = Materiales.FirstOrDefault(m => m.Id.ToString() == id);
                if (material != null)
                {
                    ScanResult = qrCode;
                    HasScanResult = true;
                    MaterialSeleccionado = material;
                    AccionPendiente = $"QR: {material.Tipo}";
                    await Shell.Current.DisplayAlert("QR", $"Material: {material.Tipo}", "OK");
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("QR", "Código QR procesado", "OK");
                ScanResult = qrCode;
                HasScanResult = true;
            }
        }

        #endregion

        #region Métodos de Registro General (Sin Foto)

        [RelayCommand]
        private async Task RegisterRecycling()
        {
            if (MaterialSeleccionado != null)
            {
                // Usar lógica antigua para registros simples sin foto
                // Aquí podrías actualizarlo para usar ApiService también si quisieras
                await RegistrarReciclajeEnAPI(MaterialSeleccionado.Id);
                ResetearEstado();
            }
            else
            {
                await Shell.Current.DisplayAlert("Info", "Selecciona un material o escanea un QR", "OK");
            }
        }

        private async Task RegistrarReciclajeEnAPI(long materialId)
        {
            try
            {
                IsBusy = true;
                // Usamos la lógica existente con HttpClient para no romper lo que ya servía
                var response = await _httpClient.PostAsync($"api/reciclajes/registrar?materialId={materialId}&cantidad=1", null);
                if (response.IsSuccessStatusCode)
                    await Shell.Current.DisplayAlert("¡Éxito!", "Puntos ganados por reciclar", "OK");
                else
                    await Shell.Current.DisplayAlert("Error", "No se pudo registrar", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Conexión: {ex.Message}", "OK");
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void SeleccionarMaterial(MaterialReciclaje material)
        {
            if (material == null) return;
            MaterialSeleccionado = material;
            AccionPendiente = $"{material.Tipo} seleccionado";
        }

        #endregion

        #region Métodos Auxiliares

        private void ResetearEstado()
        {
            HasScanResult = false;
            ScanResult = null;
            HasPhoto = false;
            CapturedPhoto = null;
            PhotoPath = null;
            MaterialSeleccionado = null;
            AccionPendiente = null;
            Observaciones = null;
        }

        private string ObtenerNombreUsuario() => "Usuario Demo";

        [RelayCommand]
        private void CancelarAccion() => ResetearEstado();

        #endregion
    }
}