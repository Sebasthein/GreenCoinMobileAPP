using System.Collections.ObjectModel;
using GreenCoinMovil.DTO;
using GreenCoinMovil.Models;
using GreenCoinMovil.ViewModels;

namespace GreenCoinMovil.Views;

public partial class AdminDashboardPage : ContentPage
{
    public AdminDashboardPage(AdminDashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is AdminDashboardViewModel viewModel)
        {
            // Recargar datos cuando la página aparece
            Task.Run(async () => await viewModel.CargarReciclajesPendientes());
        }
    }
}
