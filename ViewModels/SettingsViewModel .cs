using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenCoinMovil.Models;
using GreenCoinMovil.UsuarioDTO;
using GreenCoinMovil.Views;

namespace GreenCoinMovil.ViewModels
{

    public partial class SettingsViewModel : ObservableObject
    {
        private readonly AuthService _authService;
    private readonly HttpClient _httpClient;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _notificacionesActivadas = true;

    [ObservableProperty]
    private bool _notificacionesEmail = true;

    [ObservableProperty]
    private bool _notificacionesPush = true;

    [ObservableProperty]
    private UsuarioDto _usuario;

    [ObservableProperty]
    private string _versionApp = "1.0.0";

    public SettingsViewModel(AuthService authService, HttpClient httpClient)
    {
        _authService = authService;
        _httpClient = httpClient;
        CargarDatosUsuario();
    }

    private void CargarDatosUsuario()
    {
        Usuario = AuthService.CurrentUsuario;
    }

    [RelayCommand]
    private async Task EditarPerfil()
    {
        try
        {
            await Shell.Current.DisplayAlert(
                "Editar Perfil",
                "Función para editar información del perfil",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al editar perfil: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task CambiarContrasena()
    {
        try
        {
            var confirmacion = await Shell.Current.DisplayAlert(
                "Cambiar Contraseña",
                "¿Deseas cambiar tu contraseña?",
                "Sí, cambiar",
                "Cancelar");

            if (confirmacion)
            {
                // Aquí iría la lógica para cambiar contraseña
                await Shell.Current.DisplayAlert(
                    "Cambio de Contraseña",
                    "Se ha enviado un enlace para cambiar tu contraseña a tu email",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al cambiar contraseña: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task GestionarPrivacidad()
    {
        try
        {
            await Shell.Current.DisplayAlert(
                "Privacidad y Seguridad",
                "Configuración de privacidad y opciones de seguridad",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al cargar privacidad: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task VerTerminosCondiciones()
    {
        try
        {
            await Shell.Current.DisplayAlert(
                "Términos y Condiciones",
                "Mostrando términos y condiciones de uso",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al cargar términos: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task VerPoliticaPrivacidad()
    {
        try
        {
            await Shell.Current.DisplayAlert(
                "Política de Privacidad",
                "Mostrando política de privacidad",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al cargar política: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task ContactarSoporte()
    {
        try
        {
            var asunto = "Soporte - GreenCoin App";
            var cuerpo = $"Usuario: {Usuario?.Email ?? "No identificado"}\n\nDescripción del problema:\n";

            await Shell.Current.DisplayAlert(
                "Contactar Soporte",
                "Por favor, envía un email a soporte@greencoin.com",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al contactar soporte: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task CalificarApp()
    {
        try
        {
            await Shell.Current.DisplayAlert(
                "Calificar App",
                "¡Gracias por calificar nuestra aplicación!",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al calificar app: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task ExportarDatos()
    {
        IsBusy = true;
        try
        {
            // Simular exportación de datos
            await Task.Delay(1500);

            await Shell.Current.DisplayAlert(
                "Exportar Datos",
                "Tus datos han sido exportados correctamente",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al exportar datos: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EliminarCuenta()
    {
        try
        {
            var confirmacion = await Shell.Current.DisplayAlert(
                "Eliminar Cuenta",
                "¿Estás seguro de que quieres eliminar tu cuenta? Esta acción no se puede deshacer.",
                "Sí, eliminar",
                "Cancelar");

            if (confirmacion)
            {
                var confirmacionFinal = await Shell.Current.DisplayAlert(
                    "Confirmación Final",
                    "Esta acción eliminará todos tus datos permanentemente. ¿Continuar?",
                    "Eliminar definitivamente",
                    "Cancelar");

                if (confirmacionFinal)
                {
                    IsBusy = true;
                    // Aquí iría la llamada a la API para eliminar la cuenta
                    await Task.Delay(2000);
                    await CerrarSesion();
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al eliminar cuenta: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CerrarSesion()
    {
        var confirmacion = await Shell.Current.DisplayAlert(
            "Cerrar Sesión",
            "¿Estás seguro de que quieres cerrar sesión?",
            "Sí, cerrar sesión",
            "Cancelar");

        if (confirmacion)
        {
            IsBusy = true;
            try
            {
                _authService.Logout();
                await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "No se pudo cerrar la sesión", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    private async Task ActualizarConfiguracionNotificaciones()
    {
        try
        {
            // Aquí iría la llamada a la API para guardar las preferencias
            var configuracion = new
            {
                NotificacionesActivadas = NotificacionesActivadas,
                NotificacionesEmail = NotificacionesEmail,
                NotificacionesPush = NotificacionesPush
            };

            // Simular guardado en API
            await Task.Delay(500);

            await Shell.Current.DisplayAlert(
                "Configuración Guardada",
                "Tus preferencias de notificaciones han sido actualizadas",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al guardar configuración: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task SincronizarDatos()
    {
        IsBusy = true;
        try
        {
            // Simular sincronización con el servidor
            await Task.Delay(2000);

            await Shell.Current.DisplayAlert(
                "Sincronización Completa",
                "Todos tus datos han sido sincronizados con el servidor",
                "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al sincronizar datos: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task VerAcercaDe()
    {
        try
        {
            var mensaje = $@"
GreenCoin - Reciclaje Inteligente

Versión: {VersionApp}
Desarrollado por Semilla Verde

¡Juntos por un planeta más limpio!";

            await Shell.Current.DisplayAlert("Acerca de", mensaje, "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al cargar información: {ex.Message}", "OK");
        }
    }

    // Comando para manejar cambios en las notificaciones
    partial void OnNotificacionesActivadasChanged(bool value)
    {
        if (!value)
        {
            // Si se desactivan las notificaciones principales, desactivar las específicas también
            NotificacionesEmail = false;
            NotificacionesPush = false;
        }

        // Guardar automáticamente los cambios
        ActualizarConfiguracionNotificacionesCommand.Execute(null);
    }

    partial void OnNotificacionesEmailChanged(bool value)
    {
        ActualizarConfiguracionNotificacionesCommand.Execute(null);
    }

    partial void OnNotificacionesPushChanged(bool value)
    {
        ActualizarConfiguracionNotificacionesCommand.Execute(null);
    }
}
}