using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GreenCoinMovil.Models;
using GreenCoinMovil.DTO;
using GreenCoinMovil.UsuarioDTO;
using Microsoft.Maui.Storage;

namespace GreenCoinMovil.Models
{
    public class AuthService
    {
        // ✅ CORRECCIÓN: Usar la IP correcta para Android/emulador
        private readonly HttpClient _httpClient;
        private readonly ApiService _apiService;

        // Para Android Emulator usa: http://10.0.2.2:8080/
        // Para dispositivo físico usa la IP de tu PC: http://192.168.1.8:8080/
        private static string BaseUrl
        {
            get
            {
#if ANDROID
                return "http://192.168.1.8:8080";
#elif MACCATALYST
                return "http://192.168.1.8:8080";
#else
                return "http://192.168.1.8:8080";
#endif
            }
        }

        private const string LoginEndpoint = "auth/login";
        private const string RegisterEndpoint = "api/registro"; // ✅ Endpoint actualizado según nueva documentación

        // CLAVES PARA SECURESTORAGE
        private const string UserDataKey = "currentUserData";
        private const string AuthTokenKey = "authToken";

        // Propiedad estática para el usuario actual
        public static UsuarioDto CurrentUsuario { get; private set; }

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public AuthService(ApiService apiService)
        {
            _apiService = apiService;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            // ✅ Inicializar sesión si existe
            _ = InitializeSessionAsync();
        }

        public async Task<bool> EsAdministradorAsync()
        {
            try
            {
                #if MACCATALYST
                var email = Preferences.Get("user_email", string.Empty);
                #else
                var email = await SecureStorage.GetAsync("user_email");
                #endif
                return email == "admin@gmail.com";
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> ObtenerEmailUsuarioAsync()
        {
            #if MACCATALYST
            return Preferences.Get("user_email", string.Empty);
            #else
            return await SecureStorage.GetAsync("user_email");
            #endif
        }

        public async Task<AuthResponse> AttemptLoginAsync(LoginRequest request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔐 AUTHSERVICE: Preparando solicitud de login...");
                System.Diagnostics.Debug.WriteLine($"🔐 AUTHSERVICE: URL completa: {BaseUrl}{LoginEndpoint}");

                var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"🔐 AUTHSERVICE: Enviando login a: {BaseUrl}{LoginEndpoint}");
                System.Diagnostics.Debug.WriteLine($"📧 AUTHSERVICE: Email: {request.Email}");
                System.Diagnostics.Debug.WriteLine($"📦 AUTHSERVICE: JSON enviado: {jsonRequest}");

                System.Diagnostics.Debug.WriteLine($"🌐 AUTHSERVICE: Intentando conexión HTTP...");
                HttpResponseMessage response = await _httpClient.PostAsync(LoginEndpoint, content);

                System.Diagnostics.Debug.WriteLine($"📥 AUTHSERVICE: Respuesta HTTP recibida - Código: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"📥 AUTHSERVICE: Headers de respuesta: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");

                string jsonResponse = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"📦 AUTHSERVICE: JSON recibido: {jsonResponse}");

                System.Diagnostics.Debug.WriteLine($"🔍 AUTHSERVICE: Intentando deserializar respuesta JSON...");
                var authResult = JsonSerializer.Deserialize<AuthResponse>(jsonResponse, _jsonOptions);

                if (authResult == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ AUTHSERVICE: ERROR - No se pudo deserializar la respuesta JSON");
                    return new AuthResponse
                    {
                        Success = false,
                        Error = "❌ ERROR: El servidor devolvió una respuesta en formato incorrecto (JSON inválido)"
                    };
                }

                System.Diagnostics.Debug.WriteLine($"🔍 AUTHSERVICE: Respuesta deserializada - Success: {authResult.Success}, Error: {authResult.Error}");

                if (response.IsSuccessStatusCode && authResult.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ AUTHSERVICE: Login exitoso - Guardando sesión...");

                    // ✅ GUARDAR SESIÓN COMPLETA
                    await SaveSessionAsync(authResult.Usuario, authResult.Token);

                    // ✅ CONFIGURAR TOKEN EN ApiService para todas las llamadas autenticadas
                    _apiService.SetAuthToken(authResult.Token);

                    // Para desarrollo en Mac Catalyst, usar Preferences en lugar de SecureStorage
                    #if MACCATALYST
                    Preferences.Set("auth_token", authResult.Token);
                    Preferences.Set("user_email", authResult.Usuario?.Email ?? string.Empty);
                    #endif

                    System.Diagnostics.Debug.WriteLine($"✅ AUTHSERVICE: Sesión guardada - Usuario: {authResult.Usuario?.Email}");
                    System.Diagnostics.Debug.WriteLine($"✅ AUTHSERVICE: Token configurado en ApiService: {!string.IsNullOrEmpty(authResult.Token)}");

                    return authResult;
                }
                else
                {
                    // Determinar el tipo de error específico
                    string errorMessage = authResult?.Error ?? $"Error HTTP {response.StatusCode}";

                    System.Diagnostics.Debug.WriteLine($"❌ AUTHSERVICE: Login fallido - Código HTTP: {response.StatusCode}, Error: {errorMessage}");

                    // Mensajes específicos según código HTTP
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        errorMessage = "❌ ERROR 401: Credenciales incorrectas. Verifica tu email y contraseña.";
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        errorMessage = "❌ ERROR 403: Acceso denegado. Tu cuenta puede estar bloqueada o suspendida.";
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        errorMessage = "❌ ERROR 404: Endpoint de login no encontrado. Verifica que el servidor esté ejecutándose correctamente.";
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    {
                        errorMessage = "❌ ERROR 500: Error interno del servidor. Contacta al administrador.";
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        errorMessage = "❌ ERROR 400: Datos enviados incorrectos. Verifica el formato del email.";
                    }
                    else if ((int)response.StatusCode >= 500)
                    {
                        errorMessage = $"❌ ERROR DEL SERVIDOR ({(int)response.StatusCode}): Problema en el backend. Revisa los logs del servidor.";
                    }
                    else if (!response.IsSuccessStatusCode)
                    {
                        errorMessage = $"❌ ERROR HTTP {(int)response.StatusCode}: {errorMessage}";
                    }

                    return new AuthResponse
                    {
                        Success = false,
                        Error = errorMessage
                    };
                }
            }
            catch (HttpRequestException httpEx)
            {
                System.Diagnostics.Debug.WriteLine($"💥 AUTHSERVICE: ERROR HttpRequestException: {httpEx.Message}");
                System.Diagnostics.Debug.WriteLine($"💥 AUTHSERVICE: Inner exception: {httpEx.InnerException?.Message}");

                string errorMsg = "❌ ERROR DE CONEXIÓN: No se puede conectar al servidor.\n\n💡 POSIBLES CAUSAS:\n";
                errorMsg += "• El servidor no está ejecutándose\n";
                errorMsg += "• La URL del servidor es incorrecta\n";
                errorMsg += $"• URL actual: {BaseUrl}\n";
                errorMsg += "• Firewall bloqueando la conexión\n";
                errorMsg += "• Problemas de red/internet";

                return new AuthResponse
                {
                    Success = false,
                    Error = errorMsg
                };
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine($"💥 AUTHSERVICE: ERROR JsonException: {jsonEx.Message}");
                System.Diagnostics.Debug.WriteLine($"💥 AUTHSERVICE: JSON problemático: {jsonEx.Path}");

                return new AuthResponse
                {
                    Success = false,
                    Error = "❌ ERROR DE FORMATO: El servidor devolvió datos en un formato incorrecto.\n💡 POSIBLE CAUSA: Versión incompatible entre app y servidor."
                };
            }
            catch (TaskCanceledException timeoutEx)
            {
                System.Diagnostics.Debug.WriteLine($"💥 AUTHSERVICE: ERROR TaskCanceledException (timeout): {timeoutEx.Message}");

                return new AuthResponse
                {
                    Success = false,
                    Error = "❌ ERROR DE TIEMPO: El servidor tardó demasiado en responder.\n💡 POSIBLE CAUSA: Servidor sobrecargado o problemas de red."
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 AUTHSERVICE: ERROR GENERAL: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"💥 AUTHSERVICE: Tipo de excepción: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"💥 AUTHSERVICE: Stack trace: {ex.StackTrace}");

                return new AuthResponse
                {
                    Success = false,
                    Error = $"❌ ERROR INESPERADO: {ex.Message}\n💡 POSIBLE CAUSA: Error interno de la aplicación."
                };
            }
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string name, string email, string password, string telefono, string direccion)
        {
            try
            {
                var request = new RegistroRequest
                {
                    Nombre = name,
                    Email = email,
                    Password = password,
                    Telefono = telefono,
                    Direccion = direccion,
                    AvatarId = "12345678-abcd-efgh-ijkl-0123456789ab"
                };

                var jsonRequest = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"📤 Enviando registro a: {BaseUrl}{RegisterEndpoint}");

                var response = await _httpClient.PostAsync(RegisterEndpoint, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"📥 Respuesta HTTP: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var registerResponse = JsonSerializer.Deserialize<RegisterResponse>(responseJson, _jsonOptions);

                    if (registerResponse != null && registerResponse.Success)
                    {
                        return (true, registerResponse.Message ?? "Usuario registrado exitosamente");
                    }
                    else
                    {
                        return (false, registerResponse?.Error ?? "Error desconocido del servidor");
                    }
                }
                else
                {
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<RegisterResponse>(responseJson, _jsonOptions);
                        return (false, errorResponse?.Error ?? $"Error HTTP: {response.StatusCode}");
                    }
                    catch
                    {
                        return (false, $"Error HTTP: {response.StatusCode} - {responseJson}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error de conexión: {ex.Message}");
                return (false, "Error de conexión con el servidor. Verifica que la API esté ejecutándose.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error inesperado: {ex.Message}");
                return (false, $"Error: {ex.Message}");
            }
        }

        public void Logout()
        {
            #if MACCATALYST
            // Para Mac Catalyst limpiar Preferences
            Preferences.Remove("auth_token");
            Preferences.Remove("user_email");
            Preferences.Remove("user_id");
            Preferences.Remove("user_data");
            System.Diagnostics.Debug.WriteLine("🚪 [MACCATALYST] Sesión cerrada - Preferences limpiados");
            #else
            // Para otras plataformas usar SecureStorage
            SecureStorage.Remove(UserDataKey);
            SecureStorage.Remove(AuthTokenKey);
            System.Diagnostics.Debug.WriteLine("🚪 Sesión cerrada - SecureStorage limpiado");
            #endif

            // Limpiar token de ApiService
            _apiService.SetAuthToken(null);

            CurrentUsuario = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("test");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Test conexión fallido: {ex.Message}");
                return false;
            }
        }

        // ✅ MÉTODO CLAVE: Inicializar sesión al abrir la app
        public async Task<bool> InitializeSessionAsync()
        {
            try
            {
                string token = null;
                string userDataJson = null;

                #if MACCATALYST
                // Para Mac Catalyst usar Preferences
                token = Preferences.Get("auth_token", string.Empty);
                userDataJson = Preferences.Get("user_data", string.Empty);
                System.Diagnostics.Debug.WriteLine($"🔄 [MACCATALYST] Intentando inicializar sesión desde Preferences");
                #else
                // Para otras plataformas usar SecureStorage
                token = await SecureStorage.GetAsync(AuthTokenKey);
                userDataJson = await SecureStorage.GetAsync(UserDataKey);
                System.Diagnostics.Debug.WriteLine($"🔄 Intentando inicializar sesión desde SecureStorage");
                #endif

                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userDataJson))
                {
                    CurrentUsuario = JsonSerializer.Deserialize<UsuarioDto>(userDataJson, _jsonOptions);
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    // Configurar token en ApiService para todas las llamadas autenticadas
                    _apiService.SetAuthToken(token);

                    System.Diagnostics.Debug.WriteLine($"🔄 Sesión inicializada: {CurrentUsuario?.Email}");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"🔄 No se encontró sesión guardada (token: {!string.IsNullOrEmpty(token)}, userData: {!string.IsNullOrEmpty(userDataJson)})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al inicializar sesión: {ex.Message}");
            }

            return false;
        }

        // 🧪 MÉTODO DE PRUEBA: Test completo registro + login + dashboard + historial
        public async Task<string> TestFullFlowAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🧪 === INICIANDO PRUEBA COMPLETA: REGISTRO + LOGIN + DASHBOARD + HISTORIAL ===");

                // 0. Intentar registro de usuario de prueba
                System.Diagnostics.Debug.WriteLine("📝 Paso 0: Intentando registro de usuario de prueba...");
                var registroRequest = new RegistroRequest
                {
                    Nombre = "Usuario Test API",
                    Email = "testapi@example.com",
                    Password = "password123",
                    Telefono = "0999999999",
                    Direccion = "Dirección de prueba",
                    AvatarId = "test-avatar-id"
                };

                // Intentar registro (puede fallar si ya existe, pero eso está bien)
                var registroResult = await RegisterAsync(
                    registroRequest.Nombre,
                    registroRequest.Email,
                    registroRequest.Password,
                    registroRequest.Telefono,
                    registroRequest.Direccion
                );

                if (registroResult.Success)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Registro exitoso - Usuario creado");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Registro falló (posiblemente usuario ya existe): {registroResult.Message}");
                }

                // 1. Intentar login con usuario de prueba
                var loginRequest = new LoginRequest
                {
                    Email = "test@example.com", // Usar el usuario que ya existe
                    Password = "password123"
                };

                System.Diagnostics.Debug.WriteLine("🔐 Paso 1: Intentando login...");
                var loginResult = await AttemptLoginAsync(loginRequest);

                if (!loginResult.Success)
                {
                    return $"❌ LOGIN FALLÓ: {loginResult.Error}";
                }

                System.Diagnostics.Debug.WriteLine("✅ Login exitoso - Token obtenido");

                // 2. Intentar cargar datos del dashboard
                System.Diagnostics.Debug.WriteLine("📊 Paso 2: Intentando cargar dashboard...");
                var apiService = new ApiService(); // Crear nueva instancia
                apiService.SetAuthToken(loginResult.Token);

                System.Diagnostics.Debug.WriteLine("🔍 TEST: Llamando directamente al endpoint del dashboard...");

                // Llamar directamente al endpoint para ver la respuesta raw
                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(BaseUrl);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult.Token);

                try
                {
                    var dashboardResponse = await httpClient.GetAsync("/api/dashboard/datos");
                    System.Diagnostics.Debug.WriteLine($"📡 TEST: Dashboard response status: {dashboardResponse.StatusCode}");

                    var rawJson = await dashboardResponse.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"📦 TEST: Raw JSON response ({rawJson.Length} chars):");
                    System.Diagnostics.Debug.WriteLine(rawJson);

                    if (!dashboardResponse.IsSuccessStatusCode)
                    {
                        return $"❌ DASHBOARD FALLÓ: HTTP {dashboardResponse.StatusCode} - {rawJson}";
                    }

                    // Intentar deserializar manualmente para ver el error exacto
                    System.Diagnostics.Debug.WriteLine("🔍 TEST: Intentando deserializar JSON manualmente...");

                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };

                        var dashboardData = JsonSerializer.Deserialize<DashboardResponseDTO>(rawJson, options);

                        if (dashboardData == null)
                        {
                            return $"❌ DASHBOARD FALLÓ: Deserialización retornó null. JSON: {rawJson}";
                        }

                        System.Diagnostics.Debug.WriteLine("✅ Deserialización manual exitosa");
                        System.Diagnostics.Debug.WriteLine($"📊 Datos deserializados: Usuario={dashboardData.UsuarioNombre}, Puntos={dashboardData.PuntosTotales}");

                        // Ahora probar el método normal
                        System.Diagnostics.Debug.WriteLine("🔍 TEST: Probando método ObtenerDatosDashboardAsync()...");
                        var dashboardData2 = await apiService.ObtenerDatosDashboardAsync();

                        if (dashboardData2 == null)
                        {
                            return $"❌ DASHBOARD FALLÓ: Método ObtenerDatosDashboardAsync() retornó null, pero deserialización manual funcionó. JSON: {rawJson}";
                        }

                        System.Diagnostics.Debug.WriteLine("✅ Dashboard cargado exitosamente");
                        System.Diagnostics.Debug.WriteLine($"📊 Datos finales: Usuario={dashboardData2.UsuarioNombre}, Puntos={dashboardData2.PuntosTotales}");

                    }
                    catch (JsonException jsonEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"💥 TEST: Error de JSON en deserialización: {jsonEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"💥 TEST: Ruta del error: {jsonEx.Path}");
                        System.Diagnostics.Debug.WriteLine($"💥 TEST: Línea: {jsonEx.LineNumber}, Columna: {jsonEx.BytePositionInLine}");
                        return $"❌ DASHBOARD FALLÓ: Error JSON - {jsonEx.Message} en {jsonEx.Path}. JSON: {rawJson}";
                    }
                    catch (Exception desEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"💥 TEST: Error general en deserialización: {desEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"💥 TEST: Tipo: {desEx.GetType().Name}");
                        return $"❌ DASHBOARD FALLÓ: Excepción en deserialización - {desEx.Message}. JSON: {rawJson}";
                    }

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"💥 TEST: Error al llamar dashboard: {ex.Message}");
                    return $"❌ DASHBOARD FALLÓ: Excepción - {ex.Message}";
                }

                // 3. Intentar obtener historial de reciclajes
                System.Diagnostics.Debug.WriteLine("📋 Paso 3: Intentando obtener historial de reciclajes...");

                try
                {
                    var historial = await apiService.ObtenerMisReciclajesAsync();

                    if (historial == null || !historial.Any())
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ Historial retornó null o vacío");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Historial de reciclajes obtenido exitosamente");
                        System.Diagnostics.Debug.WriteLine($"📋 Tipo de historial: {historial.GetType().Name}, Items: {historial.Count}");
                    }

                    // También probar el endpoint de reciclajes por usuario
                    System.Diagnostics.Debug.WriteLine("🔍 TEST: Probando endpoint de reciclajes por usuario...");
                    var reciclajesUsuario = await apiService.ObtenerReciclajesPorUsuarioAsync(49); // ID del usuario test

                    if (reciclajesUsuario == null || !reciclajesUsuario.Any())
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ Reciclajes por usuario retornó null o vacío");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Reciclajes por usuario obtenidos exitosamente");
                        System.Diagnostics.Debug.WriteLine($"📋 Tipo: {reciclajesUsuario.GetType().Name}, Items: {reciclajesUsuario.Count}");
                    }

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"💥 TEST: Error al obtener historial: {ex.Message}");
                    return $"❌ HISTORIAL FALLÓ: {ex.Message}";
                }

                System.Diagnostics.Debug.WriteLine("🎉 === TODAS LAS PRUEBAS COMPLETADAS EXITOSAMENTE ===");
                return "✅ PRUEBA COMPLETA EXITOSA: Registro + Login + Dashboard + Historial funcionando";

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 Error en prueba completa: {ex.Message}");
                return $"❌ ERROR EN PRUEBA: {ex.Message}";
            }
        }

        // ✅ MÉTODO CLAVE: Guardar sesión completa
        private async Task SaveSessionAsync(UsuarioDto usuario, string token)
        {
            try
            {
                #if MACCATALYST
                // Para Mac Catalyst usar solo Preferences (SecureStorage requiere entitlements)
                Preferences.Set("auth_token", token);
                Preferences.Set("user_email", usuario?.Email ?? string.Empty);
                Preferences.Set("user_id", usuario?.Id.ToString() ?? string.Empty);
                // Guardar datos del usuario serializados
                var userDataJson = JsonSerializer.Serialize(usuario, _jsonOptions);
                Preferences.Set("user_data", userDataJson);
                System.Diagnostics.Debug.WriteLine($"💾 [MACCATALYST] Sesión guardada en Preferences - Token: {!string.IsNullOrEmpty(token)}, Email: {usuario?.Email}, UserId: {usuario?.Id}");
                #else
                // Para otras plataformas usar SecureStorage
                await SecureStorage.SetAsync("auth_token", token);
                await SecureStorage.SetAsync("user_email", usuario?.Email ?? string.Empty);
                await SecureStorage.SetAsync("user_id", usuario?.Id.ToString() ?? string.Empty);

                // También guardar en Preferences como backup
                Preferences.Set("auth_token", token);
                Preferences.Set("user_email", usuario?.Email ?? string.Empty);
                System.Diagnostics.Debug.WriteLine($"💾 Sesión guardada - Token: {!string.IsNullOrEmpty(token)}, Email: {usuario?.Email}");
                #endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 Error guardando sesión: {ex.Message}");
            }
        }
    }
}
