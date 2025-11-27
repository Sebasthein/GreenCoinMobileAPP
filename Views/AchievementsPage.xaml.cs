using GreenCoinMovil.ViewModels;

namespace GreenCoinMovil.Views;

public partial class AchievementsPage : ContentPage
{
    public AchievementsPage(AchievementsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}