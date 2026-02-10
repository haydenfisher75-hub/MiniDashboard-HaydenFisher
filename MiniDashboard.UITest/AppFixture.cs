using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using Xunit;

namespace MiniDashboard.UITest;

public class AppFixture : IAsyncLifetime
{
    private Process? _apiProcess;
    private Application? _app;
    private UIA3Automation? _automation;

    public Window MainWindow { get; private set; } = null!;
    public UIA3Automation Automation => _automation!;

    private static string SolutionDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    public async Task InitializeAsync()
    {
        // Build the solution
        var buildProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{Path.Combine(SolutionDir, "MiniDashboard.slnx")}\" -c Debug",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        })!;
        await buildProcess.WaitForExitAsync();
        if (buildProcess.ExitCode != 0)
            throw new Exception("Solution build failed");

        // Start the API on http only (avoids cert issues)
        var apiProjectDir = Path.Combine(SolutionDir, "MiniDashboard.Api");
        _apiProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --no-build --urls https://localhost:7233",
            WorkingDirectory = apiProjectDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            Environment = { ["ASPNETCORE_ENVIRONMENT"] = "Development" }
        })!;

        // Wait for the API to be ready
        await WaitForApiAsync("https://localhost:7233/api/types", TimeSpan.FromSeconds(30));

        // Clean up any leftover test items from previous runs
        await CleanupTestItemsAsync("https://localhost:7233/api/items");

        // Launch the WPF app
        var appExePath = Path.Combine(SolutionDir,
            "MiniDashboard.App", "bin", "Debug", "net8.0-windows", "MiniDashboard.App.exe");
        _automation = new UIA3Automation();
        _app = Application.Launch(appExePath);

        // Wait for the main window with a generous timeout
        var mainWindowResult = Retry.WhileNull(
            () => _app.GetMainWindow(_automation),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromMilliseconds(500));
        MainWindow = mainWindowResult.Result
            ?? throw new Exception("Main window did not appear within timeout");

        // Wait for initial data load to finish (loading spinners to disappear)
        await Task.Delay(3000);
    }

    public async Task DisposeAsync()
    {
        try { _app?.Close(); } catch { /* ignore */ }

        // Give the app a moment to close gracefully
        await Task.Delay(1000);

        if (_app != null && !_app.HasExited)
        {
            try { _app.Kill(); } catch { /* ignore */ }
        }

        _automation?.Dispose();

        if (_apiProcess is { HasExited: false })
        {
            try
            {
                _apiProcess.Kill(entireProcessTree: true);
                await _apiProcess.WaitForExitAsync();
            }
            catch { /* ignore */ }
        }
        _apiProcess?.Dispose();
    }

    private static async Task WaitForApiAsync(string url, TimeSpan timeout)
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode) return;
            }
            catch { /* API not ready yet */ }
            await Task.Delay(500);
        }

        throw new TimeoutException($"API at {url} did not become ready within {timeout.TotalSeconds}s");
    }

    /// <summary>
    /// Deletes any items whose name starts with "UI Test" or "Diag" to clean up from previous runs.
    /// </summary>
    private static async Task CleanupTestItemsAsync(string baseUrl)
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

        try
        {
            var response = await client.GetAsync(baseUrl);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<JsonElement[]>(json);
            if (items == null) return;

            foreach (var item in items)
            {
                var name = item.GetProperty("name").GetString() ?? "";
                if (name.StartsWith("UI Test", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("Diag", StringComparison.OrdinalIgnoreCase))
                {
                    var id = item.GetProperty("id").GetInt32();
                    await client.DeleteAsync($"{baseUrl}/{id}");
                }
            }
        }
        catch { /* best-effort cleanup */ }
    }

    /// <summary>
    /// Finds an element by AutomationId within the main window, retrying until found.
    /// </summary>
    public AutomationElement FindById(string automationId, TimeSpan? timeout = null)
    {
        var result = Retry.WhileNull(
            () => MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
            timeout ?? TimeSpan.FromSeconds(10),
            TimeSpan.FromMilliseconds(250));
        return result.Result
            ?? throw new Exception($"Element with AutomationId '{automationId}' not found");
    }

    /// <summary>
    /// Gets the AllItemsGrid DataGrid element.
    /// </summary>
    public AutomationElement GetAllItemsGrid() => FindById("AllItemsGrid");

    /// <summary>
    /// Gets the DiscountedGrid DataGrid element.
    /// </summary>
    public AutomationElement GetDiscountedGrid() => FindById("DiscountedGrid");

    /// <summary>
    /// Gets all DataItem rows from a WPF DataGrid.
    /// </summary>
    public AutomationElement[] GetGridRows(AutomationElement grid)
    {
        return grid.FindAllDescendants(cf => cf.ByControlType(ControlType.DataItem));
    }

    /// <summary>
    /// Checks whether any row in the grid contains the specified text.
    /// </summary>
    public bool GridContainsItem(AutomationElement grid, string text)
    {
        var rows = GetGridRows(grid);
        return rows.Any(row => RowContainsText(row, text));
    }

    /// <summary>
    /// Finds the first row containing the specified text.
    /// </summary>
    public AutomationElement? FindRowByText(AutomationElement grid, string text)
    {
        var rows = GetGridRows(grid);
        return rows.FirstOrDefault(row => RowContainsText(row, text));
    }

    private static bool RowContainsText(AutomationElement row, string text)
    {
        // Check the row's Name property first (often contains cell values)
        if (row.Name?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
            return true;

        // Fall back to checking individual cell text values
        var cells = row.FindAllChildren();
        return cells.Any(cell =>
        {
            var cellText = cell.Name ?? "";
            return cellText.Contains(text, StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// Finds a modal dialog window by waiting for a window with the given title.
    /// </summary>
    public Window? WaitForDialog(string titleContains, TimeSpan? timeout = null)
    {
        var result = Retry.WhileNull(
            () =>
            {
                // Check modal windows on the main window first
                var modalWindows = MainWindow.ModalWindows;
                var modal = modalWindows.FirstOrDefault(w =>
                    w.Title?.Contains(titleContains, StringComparison.OrdinalIgnoreCase) == true);
                if (modal != null) return modal;

                // Fall back to checking all top-level windows
                var allWindows = _app!.GetAllTopLevelWindows(_automation!);
                return allWindows.FirstOrDefault(w =>
                    w.Title?.Contains(titleContains, StringComparison.OrdinalIgnoreCase) == true);
            },
            timeout ?? TimeSpan.FromSeconds(10),
            TimeSpan.FromMilliseconds(250));
        return result.Result;
    }

    /// <summary>
    /// Finds a MessageBox window by title.
    /// </summary>
    public Window? WaitForMessageBox(string title, TimeSpan? timeout = null)
    {
        var result = Retry.WhileNull(
            () =>
            {
                var allWindows = _app!.GetAllTopLevelWindows(_automation!);
                return allWindows.FirstOrDefault(w =>
                    w.Title?.Equals(title, StringComparison.OrdinalIgnoreCase) == true);
            },
            timeout ?? TimeSpan.FromSeconds(10),
            TimeSpan.FromMilliseconds(250));
        return result.Result;
    }

    /// <summary>
    /// Refreshes the main window reference (useful after dialogs close).
    /// </summary>
    public void RefreshMainWindow()
    {
        MainWindow = _app!.GetMainWindow(_automation!)!;
    }
}
