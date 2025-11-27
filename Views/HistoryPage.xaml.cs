using GreenCoinMovil.ViewModels;

namespace GreenCoinMovil.Views;

public partial class HistoryPage : ContentPage
{
    public HistoryPage(HistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}