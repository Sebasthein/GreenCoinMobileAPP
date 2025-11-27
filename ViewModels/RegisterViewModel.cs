using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenCoinMovil.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GreenCoinMovil.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        private readonly AuthService _authService;


        // --- CAMPOS DE ENTRADA ---
        [ObservableProperty]
        private string nombre;

        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private string confirmPassword; // Campo de validación local

        [ObservableProperty]
        private string telefono;

        [ObservableProperty]
        private string direccion;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private bool acceptTerms;

        // --- ESTADO Y ERRORES ---
        [ObservableProperty]
        private string errorMessage;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private bool isBusy;

        // --- CONSTRUCTOR ---
        public RegisterViewModel(AuthService authService)
        {
            _authService = authService;
        }


        // --- LÓGICA DE VALIDACIÓN ---

        private bool CanRegister()
        {
            // 1. Validar que la aplicación no esté ocupada
            if (IsBusy) return false;

            // 2. Validar que todos los campos requeridos tengan contenido
            if (string.IsNullOrWhiteSpace(Nombre) ||
        string.IsNullOrWhiteSpace(Email) ||
        string.IsNullOrWhiteSpace(Password) ||
        string.IsNullOrWhiteSpace(Telefono) ||
        string.IsNullOrWhiteSpace(Direccion) ||
        string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                return false;
            }

            // 3. Validar que la contraseña tenga una longitud mínima (ejemplo)
            if (Password.Length < 6)
            {
                return false;
            }

            // 4. Validar que las contraseñas coincidan
            if (Password != ConfirmPassword)
            {
                return false;
            }

            // 5. Validar que los términos hayan sido aceptados
            if (!AcceptTerms)
            {
                return false;
            }

            // Si todas las validaciones pasan
            return true;
        }

        // --- COMANDO DE REGISTRO ---
        [RelayCommand(CanExecute = nameof(CanRegister))]
        private async Task Register()
        {
            Debug.WriteLine("--- COMMAND: RegisterCommand iniciado ---");

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Las contraseñas no coinciden.";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                Debug.WriteLine($"Datos a enviar: Email={Email}, Nombre={Nombre}");

                // 🛑 CORRECCIÓN: Solo se pasan 5 argumentos a AuthService.RegisterAsync
                var result = await _authService.RegisterAsync(
          Nombre,
          Email,
          Password,
          Telefono,
          Direccion
        );

                if (result.Success)
                {
                    Debug.WriteLine("--- ÉXITO: Usuario registrado correctamente. ---");

                    await Application.Current.MainPage.DisplayAlert(
                      "¡Éxito!",
                      result.Message ?? "Tu cuenta se ha creado satisfactoriamente.",
                      "OK");

                    // Navegar al login
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    ErrorMessage = result.Message;
                    Debug.WriteLine($"❌ Error del servidor: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"*** ERROR CRÍTICO: {ex.Message} ***");
                ErrorMessage = "Error de conexión. Revisa que el servidor Java esté activo.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // --- COMANDO PARA VOLVER AL LOGIN ---
        [RelayCommand]
        private async Task GoBackToLogin()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
