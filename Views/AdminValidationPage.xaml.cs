using GreenCoinMovil.ViewModels;
using GreenCoinMovil.DTO;
using System.Diagnostics;

namespace GreenCoinMovil.Views;

public partial class AdminValidationPage : ContentPage
{
	public AdminValidationPage(AdminValidationViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }

    private async void OnImageTapped(object sender, TappedEventArgs e)
    {
        if ((sender is Image || sender is Frame) && (sender as BindableObject)?.BindingContext is ReciclajeDTO reciclaje)
        {
            if (!string.IsNullOrEmpty(reciclaje.ImagenUrl))
            {
                // Mostrar opciones para la imagen
                var action = await DisplayActionSheet(
                    "Imagen del reciclaje",
                    "Cancelar",
                    null,
                    "Ver imagen completa",
                    "Copiar URL");

                switch (action)
                {
                    case "Ver imagen completa":
                        // Abrir la imagen en un navegador o app externa
                        try
                        {
                            await Launcher.OpenAsync(new Uri(reciclaje.ImagenUrl));
                        }
                        catch (Exception ex)
                        {
                            await DisplayAlert("Error", $"No se pudo abrir la imagen: {ex.Message}", "OK");
                        }
                        break;

                    case "Copiar URL":
                        // Copiar la URL al portapapeles
                        await Clipboard.SetTextAsync(reciclaje.ImagenUrl);
                        await DisplayAlert("Copiado", "URL de la imagen copiada al portapapeles", "OK");
                        break;
                }
            }
            else
            {
                await DisplayAlert("Imagen no disponible",
                    "La imagen requiere autenticación del servidor. Contacta al administrador para hacer público el endpoint de imágenes.",
                    "OK");
            }
        }
    }
}