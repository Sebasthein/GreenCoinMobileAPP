using GreenCoinMovil.ViewModels;

namespace GreenCoinMovil.Views;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(RegisterViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnTermsLabelTapped(object sender, TappedEventArgs e)
    {
        if (BindingContext is RegisterViewModel viewModel)
        {
            // Alternar el valor de AcceptTerms
            viewModel.AcceptTerms = !viewModel.AcceptTerms;

            // NOTA: Con Community Toolkit MVVM, AcceptTerms notifica automáticamente a CanRegister, 
            // por lo que el botón se habilitará/deshabilitará.
        }
    }
}