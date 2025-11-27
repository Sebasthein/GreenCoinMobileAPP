using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GreenCoinMovil.DTO;
using GreenCoinMovil.Models;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;

namespace GreenCoinMovil.Models
{
    public class ApiService
    {

            private readonly HttpClient _httpClient;
            private static string BaseUrl
            {
                get
                {
                    // Forzar la URL correcta para testing
                    var envUrl = Environment.GetEnvironmentVariable("API_BASE_URL");
                    System.Diagnostics.Debug.WriteLine($"🔍 Environment API_BASE_URL: {envUrl}");

#if ANDROID
                    var finalUrl = envUrl ?? "http://192.168.1.8:8080"; // Cambiado para dispositivo físico
                    System.Diagnostics.Debug.WriteLine($"📱 ANDROID BaseUrl: {finalUrl}");
                    return finalUrl;
#elif MACCATALYST
                    var finalUrl = envUrl ?? "http://192.168.1.8:8080";
                    System.Diagnostics.Debug.WriteLine($"🍎 MACCATALYST BaseUrl: {finalUrl}");
                    return finalUrl;
#else
                    var finalUrl = envUrl ?? "http://192.168.1.8:8080";
                    System.Diagnostics.Debug.WriteLine($"💻 OTHER BaseUrl: {finalUrl}");
                    return finalUrl;
#endif
                }
            }

            // Método público para obtener la URL base actual (útil para debugging)
            public static string GetBaseUrl() => BaseUrl;

            public ApiService()
            {
                var handler = new HttpClientHandler();

                // SOLO PARA DESARROLLO - Deshabilitar SSL
#if DEBUG
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif

                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(BaseUrl),
                    Timeout = TimeSpan.FromSeconds(30)
                };

                // Log para debugging
                System.Diagnostics.Debug.WriteLine($"🚀 ApiService inicializado con BaseUrl: {BaseUrl}");
            }

            // 🔐 MÉTODO CRÍTICO: Configurar el token después del login
            public void SetAuthToken(string token)
            {
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }
            }

        public async Task<DashboardResponseDTO> ObtenerDatosDashboardAsync()
        {
            try
            {
                await EnsureTokenAsync();
                System.Diagnostics.Debug.WriteLine("📊 Pidiendo datos del Dashboard...");

                // Usamos la ruta correcta con /api para consistencia
                var response = await _httpClient.GetAsync("/api/dashboard/datos");

                var jsonString = await response.Content.ReadAsStringAsync();

                // 👇 ESTO TE SALVARÁ LA VIDA: Verás el JSON exacto en la consola

                System.Diagnostics.Debug.WriteLine($"📦 JSON DASHBOARD RECIBIDO:\n{jsonString}");

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<DashboardResponseDTO>(jsonString, options);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error Servidor: {response.StatusCode}");
                    return null;
                }
            }
            catch (JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR DE FORMATO JSON: {jsonEx.Message}");
                System.Diagnostics.Debug.WriteLine($"   Ruta del error: {jsonEx.Path}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 Error General: {ex.Message}");
                return null;
            }
        }


        // 📱 ESCANEAR QR
        public async Task<Material> ScanQRAsync(string qrData)
            {
                try
                {
                    await EnsureTokenAsync();

                    var requestData = new { qrData };
                    var response = await _httpClient.PostAsJsonAsync("/api/materiales/crear-desde-qr", requestData);

                    System.Diagnostics.Debug.WriteLine($"📡 QR Scan Response: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<ApiResponse<Material>>();
                        System.Diagnostics.Debug.WriteLine($"✅ QR Scan Success: {result.Message}");
                        return result.Data;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"❌ QR Scan Error: {errorContent}");
                        throw new Exception($"Error: {response.StatusCode} - {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"💥 QR Scan Exception: {ex.Message}");
                    throw;
                }
            }

        // 📸 NUEVO MÉTODO: Registrar Reciclaje con FOTO (Multipart)
        public async Task<bool> RegistrarReciclajeConFotoAsync(long materialId, byte[] fotoBytes, double cantidad = 1.0)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚀 INICIANDO SUBIDA DE FOTO DESDE APISERVICE...");
                System.Diagnostics.Debug.WriteLine($"📦 Datos a enviar - MaterialId: {materialId}, Cantidad: {cantidad}");

                // 1. Obtener el token fresco del almacenamiento
                string token = null;

                #if MACCATALYST
                token = Preferences.Get("auth_token", string.Empty);
                #else
                token = await SecureStorage.GetAsync("auth_token");
                #endif

                if (string.IsNullOrEmpty(token))
                {
                    System.Diagnostics.Debug.WriteLine("❌ ERROR: No hay token guardado. El usuario debe loguearse.");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"✅ Token obtenido: {token.Substring(0, Math.Min(20, token.Length))}...");

                // 2. Crear la petición
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{BaseUrl}/api/reciclajes/registrar-con-foto"
                );

                // 3. Agregar el token de autorización
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // 4. Construir el contenido multipart
                var content = new MultipartFormDataContent();

                // ✅ Enviar datos como strings
                content.Add(new StringContent(materialId.ToString()), "materialId");
                content.Add(new StringContent(cantidad.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)), "cantidad");

                // ✅ La imagen - usar el nombre correcto "foto" que espera tu backend
                var imageContent = new ByteArrayContent(fotoBytes);
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "foto", "reciclaje.jpg");

                request.Content = content;

                // 5. Enviar la petición
                System.Diagnostics.Debug.WriteLine("📤 Enviando petición al servidor...");
                var response = await _httpClient.SendAsync(request);

                // 6. Procesar respuesta
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"✅ ¡RECICLAJE REGISTRADO CON ÉXITO! Respuesta: {responseContent}");

                    // Parsear la respuesta para verificar éxito
                    try
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        if (doc.RootElement.TryGetProperty("success", out JsonElement successElement) &&
                            successElement.ValueKind == JsonValueKind.True)
                        {
                            System.Diagnostics.Debug.WriteLine("🎉 Reciclaje guardado en base de datos correctamente");
                            return true;
                        }
                    }
                    catch (Exception jsonEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Respuesta exitosa pero no pudo parsearse: {jsonEx.Message}");
                        // Si no se puede parsear pero el status es exitoso, asumimos éxito
                        return true;
                    }

                    return true;
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ ERROR DEL SERVIDOR ({response.StatusCode}): {errorResponse}");

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ El token venció o es inválido.");
                        // Limpiar token expirado
                        #if MACCATALYST
                        Preferences.Remove("auth_token");
                        #else
                        SecureStorage.Remove("auth_token");
                        #endif

                        // Mostrar alerta en el hilo principal
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await Application.Current.MainPage.DisplayAlert(
                                "Sesión Expirada",
                                "Por favor, inicia sesión nuevamente",
                                "OK");
                        });
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await Application.Current.MainPage.DisplayAlert(
                                "Error",
                                "Datos inválidos enviados al servidor",
                                "OK");
                        });
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 EXCEPCIÓN CRÍTICA: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"💥 StackTrace: {ex.StackTrace}");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error de Conexión",
                        "No se pudo conectar con el servidor. Verifica tu conexión a internet.",
                        "OK");
                });

                return false;
            }
        }


        // 📋 OBTENER MATERIALES
        public async Task<List<Material>> GetMaterialsAsync()
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("🔍 ApiService: Obteniendo materiales...");
                    await EnsureTokenAsync();

                    var fullUrl = $"{BaseUrl}/api/materiales";
                    System.Diagnostics.Debug.WriteLine($"🌐 ApiService: Llamando a {fullUrl}");

                    var response = await _httpClient.GetAsync("/api/materiales");
                    System.Diagnostics.Debug.WriteLine($"📡 ApiService: Response status: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"📦 ApiService: JSON recibido ({jsonString.Length} chars): {jsonString}");

                        var materials = await response.Content.ReadFromJsonAsync<List<Material>>();
                        System.Diagnostics.Debug.WriteLine($"✅ ApiService: Materiales deserializados: {materials?.Count ?? 0}");
                        return materials;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"❌ ApiService: Error HTTP {response.StatusCode}: {errorContent}");
                        throw new Exception($"Error getting materials: {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"💥 GetMaterials error: {ex.Message}");
                    throw;
                }
            }

            // 🔄 REGISTRAR RECICLAJE (Solo Datos, sin foto - Método antiguo si lo necesitas)
            public async Task<MaterialScanResponse> RegisterRecyclingAsync(string qrData, int quantity = 1)
            {
                try
                {
                    await EnsureTokenAsync();

                    var request = new { qrData, quantity };
                    var response = await _httpClient.PostAsJsonAsync("/api/reciclajes/crear-desde-qr", request);

                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadFromJsonAsync<MaterialScanResponse>();
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Error registering recycling: {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"💥 RegisterRecycling error: {ex.Message}");
                    throw;
                }
            }

            // 🔍 MÉTODO PRIVADO: Asegurar que tenemos token
            private async Task EnsureTokenAsync()
            {
                // Si no hay token en los headers, intentar recuperarlo
                if (_httpClient.DefaultRequestHeaders.Authorization == null)
                {
                    string savedToken = null;

                    #if MACCATALYST
                    // Para Mac Catalyst usar Preferences
                    savedToken = Preferences.Get("auth_token", string.Empty);
                    System.Diagnostics.Debug.WriteLine($"🔄 [MACCATALYST] Recuperando token desde Preferences");
                    #else
                    // Para otras plataformas usar SecureStorage
                    savedToken = await SecureStorage.GetAsync("auth_token");
                    System.Diagnostics.Debug.WriteLine($"🔄 Recuperando token desde SecureStorage");
                    #endif

                    if (!string.IsNullOrEmpty(savedToken))
                    {
                        SetAuthToken(savedToken);
                        System.Diagnostics.Debug.WriteLine($"✅ Token JWT configurado en headers: {savedToken.Substring(0, Math.Min(20, savedToken.Length))}...");
                    }
                    else
                    {
                        // Opcional: Lanzar excepción o manejar logout
                        // throw new Exception("Usuario no autenticado.");
                        System.Diagnostics.Debug.WriteLine("⚠️ Advertencia: No hay token guardado.");
                    }
                }
            }

        // 1. Obtener lista de pendientes
        public async Task<List<ReciclajeDTO>> ObtenerPendientesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 ApiService: Obteniendo reciclajes pendientes...");
                await EnsureTokenAsync();

                var response = await _httpClient.GetAsync("/api/reciclajes/pendientes");
                System.Diagnostics.Debug.WriteLine($"📡 ApiService: Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"📦 ApiService: JSON recibido ({jsonString.Length} chars): {jsonString.Substring(0, Math.Min(500, jsonString.Length))}...");

                    // Intentar deserializar con opciones para manejar JSON complejos
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            MaxDepth = 128 // Aumentar la profundidad máxima para JSON complejos
                        };
                        var result = JsonSerializer.Deserialize<List<ReciclajeDTO>>(jsonString, options);
                        System.Diagnostics.Debug.WriteLine($"✅ ApiService: Reciclajes pendientes deserializados correctamente - {result?.Count ?? 0} items");
                        return result ?? new List<ReciclajeDTO>();
                    }
                    catch (JsonException jsonEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"💥 ApiService: Error de deserialización JSON: {jsonEx.Message}");
                        // Retornar lista vacía como fallback
                        return new List<ReciclajeDTO>();
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ ApiService: Error HTTP {response.StatusCode}: {errorContent}");
                    return new List<ReciclajeDTO>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ObtenerPendientes error: {ex.Message}");
                return new List<ReciclajeDTO>();
            }
        }

        // 2. Validar (Aprobar) un reciclaje
        public async Task<ApiResponse<string>> ValidarReciclajeAsync(long id)
        {
            try
            {
                await EnsureTokenAsync();
                // Tu backend tiene este endpoint PUT: /validar/{id}
                var response = await _httpClient.PutAsync($"/api/reciclajes/validar/{id}", null);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Error validando: {response.StatusCode} - {errorContent}");
                    return new ApiResponse<string> { Success = false, Message = "Error al validar reciclaje", Error = errorContent };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validando: {ex.Message}");
                return new ApiResponse<string> { Success = false, Message = "Error al validar reciclaje", Error = ex.Message };
            }
        }
        // 🧪 TEST DE CONEXIÓN
        public async Task<bool> TestConnectionAsync()
            {
                try
                {
                    var response = await _httpClient.GetAsync("/api/materiales");
                    System.Diagnostics.Debug.WriteLine($"🔗 Connection test: {response.StatusCode}");
                    return response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"🔗 Connection test failed: {ex.Message}");
                    return false;
                }
            }

        // 📋 MÉTODOS PARA ADMINISTRADOR
        public async Task<List<ReciclajeDTO>> ObtenerReciclajesPendientesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 ApiService: Obteniendo reciclajes pendientes (admin)...");
                await EnsureTokenAsync();

                // Este endpoint debe coincidir con tu ReciclajeController en Java (@GetMapping("/pendientes"))
                var response = await _httpClient.GetAsync("/api/reciclajes/pendientes");
                System.Diagnostics.Debug.WriteLine($"📡 ApiService: Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"📦 ApiService: JSON recibido ({jsonString.Length} chars): {jsonString.Substring(0, Math.Min(500, jsonString.Length))}...");

                    // Intentar deserializar con opciones para manejar JSON complejos
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            MaxDepth = 128 // Aumentar la profundidad máxima para JSON complejos
                        };
                        var result = JsonSerializer.Deserialize<List<ReciclajeDTO>>(jsonString, options);
                        System.Diagnostics.Debug.WriteLine($"✅ ApiService: Reciclajes pendientes (admin) deserializados correctamente - {result?.Count ?? 0} items");
                        return result ?? new List<ReciclajeDTO>();
                    }
                    catch (JsonException jsonEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"💥 ApiService: Error de deserialización JSON: {jsonEx.Message}");
                        // Retornar lista vacía como fallback
                        return new List<ReciclajeDTO>();
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ ApiService: Error HTTP {response.StatusCode}: {errorContent}");
                    return new List<ReciclajeDTO>(); // Retorna lista vacía si falla
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ObtenerReciclajesPendientes error: {ex.Message}");
                return new List<ReciclajeDTO>();
            }
        }

        public async Task<ApiResponse<string>> AprobarReciclajeAsync(long id)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 ApiService: Aprobando reciclaje ID {id}");
                await EnsureTokenAsync();

                // Endpoint: @PutMapping("/validar/{id}")
                var response = await _httpClient.PutAsync($"/api/reciclajes/validar/{id}", null);
                System.Diagnostics.Debug.WriteLine($"📡 ApiService: Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"✅ ApiService: Aprobación exitosa - Respuesta: {responseContent}");

                    // El backend probablemente devuelve {"message":"Reciclaje aprobado exitosamente"}
                    // Crear un ApiResponse manualmente ya que el formato no coincide exactamente
                    var result = new ApiResponse<string>
                    {
                        Success = true,
                        Message = "Reciclaje aprobado exitosamente",
                        Data = null
                    };

                    System.Diagnostics.Debug.WriteLine($"✅ ApiService: Reciclaje ID {id} aprobado correctamente");
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ ApiService: Error aprobando - Status {response.StatusCode}: {errorContent}");
                    return new ApiResponse<string> { Success = false, Message = "Error al aprobar reciclaje", Error = errorContent };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ApiService: Error aprobando reciclaje ID {id}: {ex.Message}");
                return new ApiResponse<string> { Success = false, Message = "Error al aprobar reciclaje", Error = ex.Message };
            }
        }

        // 3. Rechazar Reciclaje
        public async Task<ApiResponse<string>> RechazarReciclajeAsync(long id, string motivo)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 ApiService: Rechazando reciclaje ID {id} con motivo: '{motivo}'");
                await EnsureTokenAsync();

                var requestBody = new { motivoRechazo = motivo };
                System.Diagnostics.Debug.WriteLine($"📤 ApiService: Enviando PUT a /api/reciclajes/rechazar/{id}");

                var response = await _httpClient.PutAsJsonAsync($"/api/reciclajes/rechazar/{id}", requestBody);
                System.Diagnostics.Debug.WriteLine($"📡 ApiService: Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"✅ ApiService: Rechazo exitoso - Respuesta: {responseContent}");

                    // El backend devuelve {"message":"Reciclaje rechazado exitosamente"}
                    // Crear un ApiResponse manualmente ya que el formato no coincide exactamente
                    var result = new ApiResponse<string>
                    {
                        Success = true,
                        Message = "Reciclaje rechazado exitosamente",
                        Data = null
                    };

                    System.Diagnostics.Debug.WriteLine($"✅ ApiService: Reciclaje ID {id} rechazado correctamente");
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ ApiService: Error rechazando - Status {response.StatusCode}: {errorContent}");
                    return new ApiResponse<string> { Success = false, Message = "Error al rechazar reciclaje", Error = errorContent };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ApiService: Error rechazando reciclaje ID {id}: {ex.Message}");
                return new ApiResponse<string> { Success = false, Message = "Error al rechazar reciclaje", Error = ex.Message };
            }
        }

        // Verificar si el usuario actual es admin
        public async Task<bool> EsAdministradorAsync()
        {
            try
            {
                string email = null;

                #if MACCATALYST
                email = Preferences.Get("user_email", string.Empty);
                #else
                email = await SecureStorage.GetAsync("user_email");
                #endif

                return email == "admin@gmail.com";
            }
            catch
            {
                return false;
            }
        }

        // 📝 ACTUALIZAR MATERIAL
        public async Task<Material> ActualizarMaterialAsync(long id, Material material)
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.PutAsJsonAsync($"/api/materiales/{id}", material);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Material>();
                }
                else
                {
                    throw new Exception($"Error updating material: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ActualizarMaterial error: {ex.Message}");
                throw;
            }
        }

        // ➕ CREAR MATERIAL
        public async Task<Material> CrearMaterialAsync(Material material)
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.PostAsJsonAsync("/api/materiales", material);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Material>();
                }
                else
                {
                    throw new Exception($"Error creating material: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 CrearMaterial error: {ex.Message}");
                throw;
            }
        }

        // 🔍 BUSCAR MATERIAL POR CÓDIGO
        public async Task<object> BuscarMaterialPorCodigoAsync(object request)
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.PostAsJsonAsync("/api/materiales/buscar-por-codigo", request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<object>();
                }
                else
                {
                    throw new Exception($"Error searching material by code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 BuscarMaterialPorCodigo error: {ex.Message}");
                throw;
            }
        }

        // 👤 OBTENER PERFIL DE USUARIO POR ID
        public async Task<Usuario> ObtenerPerfilUsuarioAsync(long id)
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.GetAsync($"/api/usuarios/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Usuario>();
                }
                else
                {
                    throw new Exception($"Error getting user profile: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ObtenerPerfilUsuario error: {ex.Message}");
                throw;
            }
        }

        // 📋 RECICLAJES POR USUARIO
        public async Task<List<ReciclajeDTO>> ObtenerReciclajesPorUsuarioAsync(long usuarioId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 ApiService: Obteniendo reciclajes para usuario {usuarioId}");
                await EnsureTokenAsync();

                var url = $"/api/reciclajes/usuario/{usuarioId}";
                System.Diagnostics.Debug.WriteLine($"🌐 ApiService: Llamando a {url}");

                var response = await _httpClient.GetAsync(url);
                System.Diagnostics.Debug.WriteLine($"📡 ApiService: Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"📦 ApiService: JSON recibido ({jsonString.Length} chars): {jsonString}");

                    // Intentar deserializar como List<ReciclajeDTO>
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            MaxDepth = 128 // Aumentar la profundidad máxima para JSON complejos
                        };
                        var result = JsonSerializer.Deserialize<List<ReciclajeDTO>>(jsonString, options);
                        System.Diagnostics.Debug.WriteLine($"✅ ApiService: Deserializado correctamente - {result?.Count ?? 0} items");
                        return result ?? new List<ReciclajeDTO>();
                    }
                    catch (JsonException jsonEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"💥 ApiService: Error de deserialización JSON: {jsonEx.Message}");
                        // Retornar lista vacía como fallback
                        return new List<ReciclajeDTO>();
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ ApiService: Error HTTP {response.StatusCode}: {errorContent}");
                    throw new Exception($"Error getting recyclings by user: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ObtenerReciclajesPorUsuario error: {ex.Message}");
                throw;
            }
        }

        // 📋 TODOS LOS RECICLAJES
        public async Task<object> ObtenerTodosReciclajesAsync()
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.GetAsync("/api/reciclajes/todos");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<object>();
                }
                else
                {
                    throw new Exception($"Error getting all recyclings: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ObtenerTodosReciclajes error: {ex.Message}");
                throw;
            }
        }

        // 🔍 BUSCAR MATERIALES POR TIPO
        public async Task<List<Material>> BuscarMaterialesPorTipoAsync(string tipo)
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.GetAsync($"/api/materiales/tipo/{tipo}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Material>>();
                }
                else
                {
                    throw new Exception($"Error searching materials by type: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 BuscarMaterialesPorTipo error: {ex.Message}");
                throw;
            }
        }

        // 👤 REGISTRAR USUARIO API
        public async Task<object> RegistrarUsuarioApiAsync(RegistroRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/usuarios/api/registro", request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<object>();
                }
                else
                {
                    throw new Exception($"Error registering user: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 RegistrarUsuarioApi error: {ex.Message}");
                throw;
            }
        }

        // 📝 REGISTRAR RECICLAJE SIMPLE (sin foto)
        public async Task<object> RegistrarReciclajeSimpleAsync(long materialId, int cantidad = 1)
        {
            try
            {
                await EnsureTokenAsync();
                var request = new
                {
                    materialId = materialId,
                    cantidad = cantidad
                };
                var response = await _httpClient.PostAsJsonAsync("/api/reciclajes/registrar", request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<object>();
                }
                else
                {
                    throw new Exception($"Error registering recycling: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 RegistrarReciclajeSimple error: {ex.Message}");
                throw;
            }
        }

        // 📱 MIS RECICLAJES
        public async Task<List<ReciclajeDTO>> ObtenerMisReciclajesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 ApiService: Obteniendo mis reciclajes...");
                await EnsureTokenAsync();

                var response = await _httpClient.GetAsync("/api/reciclajes/mis-reciclajes");
                System.Diagnostics.Debug.WriteLine($"📡 ApiService: Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"📦 ApiService: JSON recibido ({jsonString.Length} chars): {jsonString}");

                    // Intentar deserializar como List<ReciclajeDTO>
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            MaxDepth = 128 // Aumentar la profundidad máxima para JSON complejos
                        };
                        var result = JsonSerializer.Deserialize<List<ReciclajeDTO>>(jsonString, options);
                        System.Diagnostics.Debug.WriteLine($"✅ ApiService: Mis reciclajes deserializados correctamente - {result?.Count ?? 0} items");
                        return result ?? new List<ReciclajeDTO>();
                    }
                    catch (JsonException jsonEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"💥 ApiService: Error de deserialización JSON: {jsonEx.Message}");
                        // Retornar lista vacía como fallback
                        return new List<ReciclajeDTO>();
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ ApiService: Error HTTP {response.StatusCode}: {errorContent}");
                    throw new Exception($"Error getting my recyclings: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ObtenerMisReciclajes error: {ex.Message}");
                throw;
            }
        }

        // 🏆 OBTENER LOGROS
        public async Task<List<Logro>> ObtenerLogrosAsync()
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.GetAsync("/api/logros");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Logro>>();
                }
                else
                {
                    throw new Exception($"Error getting achievements: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ObtenerLogros error: {ex.Message}");
                throw;
            }
        }

        // 📊 OBTENER NIVELES
        public async Task<List<Nivel>> ObtenerNivelesAsync()
        {
            try
            {
                await EnsureTokenAsync();
                var response = await _httpClient.GetAsync("/api/niveles");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Nivel>>();
                }
                else
                {
                    throw new Exception($"Error getting levels: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ObtenerNiveles error: {ex.Message}");
                throw;
            }
        }

    }
    }