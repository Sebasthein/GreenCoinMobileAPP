
using GreenCoinMovil.ViewModels;

namespace GreenCoinMovil.Views;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Opcional: Limpiar campos al mostrar la página
        if (BindingContext is LoginViewModel viewModel)
        {
            viewModel.Email = string.Empty;
            viewModel.Password = string.Empty;
            viewModel.ErrorMessage = string.Empty;
        }
    }
}