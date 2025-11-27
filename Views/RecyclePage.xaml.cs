using GreenCoinMovil.ViewModels;

namespace GreenCoinMovil.Views;

public partial class RecyclePage : ContentPage
{
	public RecyclePage(RecyclingViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Recargar materiales cada vez que aparezca la página
        // En caso de que el usuario haya iniciado sesión después de la primera carga
        var viewModel = BindingContext as RecyclingViewModel;
        if (viewModel != null)
        {
            await viewModel.CargarMaterialesCommand.ExecuteAsync(null);
        }
    }
}