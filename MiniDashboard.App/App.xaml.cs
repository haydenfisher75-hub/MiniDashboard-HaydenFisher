using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MiniDashboard.App.Services;
using MiniDashboard.App.ViewModels;

namespace MiniDashboard.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();

        services.AddHttpClient<ItemApiService>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7233");
        });

        var cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiniDashboard", "Cache");

        services.AddTransient<IItemApiService>(sp =>
        {
            var inner = sp.GetRequiredService<ItemApiService>();
            return new CachedItemApiService(inner, cacheDir);
        });

        services.AddTransient<DashboardViewModel>();

        Services = services.BuildServiceProvider();
        base.OnStartup(e);
    }
}
