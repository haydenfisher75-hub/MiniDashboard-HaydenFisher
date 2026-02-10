using System.ComponentModel;
using System.Windows;
using MiniDashboard.App.ViewModels;

namespace MiniDashboard.App.Views;

public partial class DiscountEntryWindow : Window
{
    public DiscountEntryWindow(DiscountEntryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DiscountEntryViewModel.IsSaved) &&
            DataContext is DiscountEntryViewModel vm && vm.IsSaved)
        {
            DialogResult = true;
            Close();
        }
    }
}
