using System.ComponentModel;
using System.Windows;
using MiniDashboard.App.ViewModels;

namespace MiniDashboard.App.Views;

public partial class ItemDialogWindow : Window
{
    public ItemDialogWindow(ItemDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        Loaded += async (_, _) => await viewModel.LoadDataCommand.ExecuteAsync(null);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ItemDialogViewModel.IsSaved) &&
            DataContext is ItemDialogViewModel vm && vm.IsSaved)
        {
            DialogResult = true;
            Close();
        }
    }
}
