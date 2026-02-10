using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MiniDashboard.DAL.Classes;
using MiniDashboard.DAL.Interfaces;
using Xunit;

namespace MiniDashboard.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private string _tempDataDir = null!;

    public Task InitializeAsync()
    {
        _tempDataDir = Path.Combine(Path.GetTempPath(), "MiniDashboardTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDataDir);

        var options = new JsonSerializerOptions { WriteIndented = true };

        var types = new[]
        {
            new { Id = 1, Name = "Electronics" },
            new { Id = 2, Name = "Furniture" }
        };

        var categories = new[]
        {
            new { Id = 1, Name = "Phones", Prefix = "PHN", TypeId = 1 },
            new { Id = 2, Name = "Laptops", Prefix = "LPT", TypeId = 1 },
            new { Id = 3, Name = "Chairs", Prefix = "CHR", TypeId = 2 }
        };

        File.WriteAllText(Path.Combine(_tempDataDir, "types.json"), JsonSerializer.Serialize(types, options));
        File.WriteAllText(Path.Combine(_tempDataDir, "categories.json"), JsonSerializer.Serialize(categories, options));
        File.WriteAllText(Path.Combine(_tempDataDir, "items.json"), "[]");
        File.WriteAllText(Path.Combine(_tempDataDir, "deleted-items.json"), "[]");

        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptors = services
                .Where(d => d.ServiceType == typeof(IItemRepository)
                         || d.ServiceType == typeof(ICategoryRepository)
                         || d.ServiceType == typeof(ITypeRepository))
                .ToList();

            foreach (var d in descriptors)
                services.Remove(d);

            var itemsPath = Path.Combine(_tempDataDir, "items.json");
            var deletedItemsPath = Path.Combine(_tempDataDir, "deleted-items.json");
            var typesPath = Path.Combine(_tempDataDir, "types.json");
            var categoriesPath = Path.Combine(_tempDataDir, "categories.json");

            services.AddSingleton<IItemRepository>(_ => new JsonItemRepository(itemsPath, deletedItemsPath));
            services.AddSingleton<ITypeRepository>(_ => new JsonTypeRepository(typesPath));
            services.AddSingleton<ICategoryRepository>(_ => new JsonCategoryRepository(categoriesPath));
        });
    }

    public new Task DisposeAsync()
    {
        if (Directory.Exists(_tempDataDir))
        {
            try { Directory.Delete(_tempDataDir, true); }
            catch { /* cleanup best effort */ }
        }

        return base.DisposeAsync().AsTask();
    }
}
