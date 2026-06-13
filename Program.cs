using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Media;

namespace HabboCustomLauncher;

internal static class Program
{
    private static Mutex? appMutex;

    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            const string mutexName = "HabboCustomLauncherBeta";
            appMutex = new Mutex(true, mutexName, out bool isNewInstance);
            if (!isNewInstance)
            {
                args = new string[] { "already_running" };
            }
            StartAvaloniaApp(args);
        }
        catch
        {
            // App startup error
        }
        try
        {
            appMutex!.ReleaseMutex();
        }
        catch
        {
            // Error while releasing mutex
        }
        Environment.Exit(0);
    }

    private static void StartAvaloniaApp(string[] newWindowArgs)
    {
        var avaloniaApp = BuildAvaloniaApp();
        var osVersion = Environment.OSVersion.Version;
        // Usando Windows 7 se define renderizado por software debido a que el usuario probablemente
        // tenga una gpu demasiado antigua para soportar opengl de forma adecuada (gma3600 por ejemplo da problemas)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && osVersion.Major == 6 && osVersion.Minor == 1)
        {
            var win32Options = new Win32PlatformOptions
            {
                RenderingMode = new[] { Win32RenderingMode.Software },
                CompositionMode = new[] { Win32CompositionMode.RedirectionSurface }
            };
            avaloniaApp.With(win32Options);
        }
        avaloniaApp.StartWithClassicDesktopLifetime(newWindowArgs);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        string fontFamilyName = "avares://" + Assembly.GetExecutingAssembly().GetName().Name + "/Assets/Rajdhani-Regular.ttf#Rajdhani";
        var fontOptions = new FontManagerOptions
        {
            DefaultFamilyName = fontFamilyName,
            FontFallbacks = new[]
            {
                new FontFallback
                {
                    FontFamily = new FontFamily(fontFamilyName)
                }
            }
        };
        // Alternativa:
        // var fontOptions = new FontManagerOptions { DefaultFamilyName = null };
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .With(fontOptions)
            .WithSystemFontSource(new Uri(fontFamilyName, UriKind.Absolute));
    }
}
