using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace HabboCustomLauncher;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var desktop = ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (desktop != null)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            string customWindowScale = Environment.GetCommandLineArgs().FirstOrDefault(x => x.StartsWith("-scale="), "");
            customWindowScale = customWindowScale.Replace("-scale=", "");
            if (IsNumeric(customWindowScale) == false)
            {
                customWindowScale = "0";
            }
            Singleton.GetCurrentInstance().CustomWindowScale = double.Parse(customWindowScale, CultureInfo.InvariantCulture);

            if (desktop.Args!.Contains("already_running"))
            {
                HandleAppAlreadyRunning();
            }
            else
            {
                desktop.MainWindow = null;
                var launcherMainWindow = new MainWindow(); // MainWindow will decide which window should be shown
            }

            // base.OnFrameworkInitializationCompleted();
        }
    }

    public async void HandleAppAlreadyRunning()
    {
        string habboProtocol = Environment.GetCommandLineArgs().FirstOrDefault(x => x.StartsWith("habbo://"), "");
        var emptyWindow = new Window();
        if (habboProtocol == "")
        {
            habboProtocol = await emptyWindow.Clipboard!.GetTextAsync() ?? "";
        }
        await emptyWindow.Clipboard!.SetTextAsync("hcl_main_focus_" + habboProtocol);
        Process.GetCurrentProcess().Kill();
    }

    /// <summary>
    /// Equivalente al IsNumeric de VB: true si la cadena se puede interpretar como número
    /// (cultura invariante, igual que el uso original al parsear el argumento -scale).
    /// </summary>
    private static bool IsNumeric(string? value) =>
        double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
}
