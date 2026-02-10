using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MiniDashboard.App.ViewModels;

namespace MiniDashboard.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var vm = App.Services.GetRequiredService<DashboardViewModel>();
        DataContext = vm;

        Loaded += async (_, _) => await vm.LoadDataCommand.ExecuteAsync(null);
    }
}
