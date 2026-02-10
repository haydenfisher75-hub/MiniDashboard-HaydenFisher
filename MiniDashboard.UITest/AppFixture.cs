using System.Diagnostics;
using System.IO;
using System.Net.Http;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
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
        // Build both projects first
        var buildProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{Path.Combine(SolutionDir, "MiniDashboard.slnx")}\" -c Debug --no-restore",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        })!;
        await buildProcess.WaitForExitAsync();

        // Start the API
        var apiProjectDir = Path.Combine(SolutionDir, "MiniDashboard.Api");
        _apiProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --no-build --launch-profile https",
            WorkingDirectory = apiProjectDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        })!;

        // Wait for the API to be ready
        await WaitForApiAsync("https://localhost:7233/api/types", TimeSpan.FromSeconds(30));

        // Launch the WPF app
        var appExePath = Path.Combine(SolutionDir,
            "MiniDashboard.App", "bin", "Debug", "net8.0-windows", "MiniDashboard.App.exe");
        _automation = new UIA3Automation();
        _app = Application.Launch(appExePath);

        // Wait for the main window
        MainWindow = Retry.WhileNull(
            () => _app.GetMainWindow(_automation),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromMilliseconds(500)).Result;

        // Wait for initial data load to complete
        await Task.Delay(2000);
    }

    public async Task DisposeAsync()
    {
        _app?.Close();
        _automation?.Dispose();

        if (_apiProcess is { HasExited: false })
        {
            _apiProcess.Kill(entireProcessTree: true);
            await _apiProcess.WaitForExitAsync();
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
            catch
            {
                // API not ready yet
            }
            await Task.Delay(500);
        }

        throw new TimeoutException($"API at {url} did not become ready within {timeout.TotalSeconds}s");
    }

    /// <summary>
    /// Finds an element by AutomationId within the main window.
    /// </summary>
    public AutomationElement FindById(string automationId)
    {
        return Retry.WhileNull(
            () => MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(250)).Result;
    }

    /// <summary>
    /// Finds an element by AutomationId within a given parent element.
    /// </summary>
    public AutomationElement FindById(AutomationElement parent, string automationId)
    {
        return Retry.WhileNull(
            () => parent.FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(250)).Result;
    }

    /// <summary>
    /// Waits until an element with the given AutomationId exists in the window.
    /// Returns the element or null if not found within timeout.
    /// </summary>
    public AutomationElement? WaitForElement(string automationId, TimeSpan? timeout = null)
    {
        try
        {
            return Retry.WhileNull(
                () => MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
                timeout ?? TimeSpan.FromSeconds(5),
                TimeSpan.FromMilliseconds(250)).Result;
        }
        catch
        {
            return null;
        }
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
    /// Finds a modal dialog window (e.g., ItemDialogWindow) by waiting for a window
    /// with the given title.
    /// </summary>
    public Window WaitForDialog(string titleContains, TimeSpan? timeout = null)
    {
        return Retry.WhileNull<Window>(
            () =>
            {
                var windows = _app!.GetAllTopLevelWindows(_automation!);
                return windows.FirstOrDefault(w =>
                    w.Title.Contains(titleContains, StringComparison.OrdinalIgnoreCase))!;
            },
            timeout ?? TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(250)).Result;
    }

    /// <summary>
    /// Finds a MessageBox window by title.
    /// </summary>
    public Window WaitForMessageBox(string title, TimeSpan? timeout = null)
    {
        return Retry.WhileNull<Window>(
            () =>
            {
                var windows = _app!.GetAllTopLevelWindows(_automation!);
                return windows.FirstOrDefault(w =>
                    w.Title.Equals(title, StringComparison.OrdinalIgnoreCase))!;
            },
            timeout ?? TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(250)).Result;
    }
}
