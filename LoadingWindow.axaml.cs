using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Path = System.IO.Path;

namespace HabboCustomLauncher;

public partial class LoadingWindow : Window
{
    public bool MainMenuRequested;
    private Window? Window;
    private Label? TitleBarLabel;
    public TextBlock? StatusLabel;
    private CustomButton? CloseButton;
    private CustomButton? MainMenuButton;
    public int CurrentLanguageInt = 0;
    public string PreviousLauncherFilename = "";

    public LoadingWindow()
    {
        InitializeComponent(); // This call is required by the designer
    }

    // Auto-wiring does not work for VB, so do it manually (se conserva ese patrón en C#)
    private void InitializeComponent(bool loadXaml = true)
    {
        if (CultureInfo.CurrentCulture.Name.ToLower().StartsWith("es"))
        {
            CurrentLanguageInt = 1;
        }
        if (loadXaml)
        {
            AvaloniaXamlLoader.Load(this);
        }
        // Example: Control = FindNameScope().Find("Control_Name")
        Window = this.FindNameScope()!.Find<Window>("Window");
        TitleBarLabel = Window!.FindNameScope()!.Find<Label>("TitleBarLabel");
        StatusLabel = this.FindNameScope()!.Find<TextBlock>("StatusLabel");
        CloseButton = this.FindNameScope()!.Find<CustomButton>("CloseButton");
        MainMenuButton = this.FindNameScope()!.Find<CustomButton>("MainMenuButton");

        CloseButton!.Click += CloseButton_Click;
        MainMenuButton!.Click += MainMenuButton_Click;
        TitleBarLabel!.PointerPressed += TitleBarLabel_PointerPressed;
        Closing += LoadingWindow_Closing;

        Singleton.GetCurrentInstance().ScaleMainGrid(Window!);

        MainMenuRequested = false;

        MainMenuButton.Text = LauncherUpdaterTranslator.ReturnToMainMenu[CurrentLanguageInt];

        return;

        // ---------------------------------------------------------------------------------
        //  A partir del Return anterior el código del actualizador queda DESACTIVADO, igual
        //  que en el VB original (allí estas líneas seguían al Return y nunca se ejecutaban).
        //  Se conserva la lógica más abajo en métodos definidos pero no invocados
        //  (StartUpdateProcess, ReemplazarCuandoSeLibere, MakeUnixExecutable) para mantener
        //  la conversión 1:1 sin introducir código inalcanzable.
        //
        //  for (int argumentIndex = 0; argumentIndex <= Environment.GetCommandLineArgs().Length - 1; argumentIndex++)
        //  {
        //      if (Environment.GetCommandLineArgs()[argumentIndex].ToLower().StartsWith("-updater"))
        //      {
        //          if (argumentIndex + 1 <= Environment.GetCommandLineArgs().Length - 1)
        //          {
        //              PreviousLauncherFilename = Environment.GetCommandLineArgs()[argumentIndex + 1];
        //          }
        //      }
        //  }
        //  StartUpdateProcess();
    }

    private async void StartUpdateProcess()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(PreviousLauncherFilename))
            {
                throw new Exception("Unknown launcher filename!");
            }
            string applicationLocation = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName)!;
            string launcherUpdatePath = Process.GetCurrentProcess().MainModule!.FileName; // HCL_Update (full path)
            string previousLauncherPath = Path.Combine(applicationLocation, PreviousLauncherFilename); // HabboCustomLauncher (full path)
            StatusLabel!.Text = LauncherUpdaterTranslator.VerifyingUpdate[CurrentLanguageInt];

            ReemplazarCuandoSeLibere(launcherUpdatePath, previousLauncherPath);
            await Task.Delay(5000);
            Environment.Exit(0);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
            {
                MakeUnixExecutable(previousLauncherPath);
            }
            Process.Start(previousLauncherPath);
            Environment.Exit(0);
        }
        catch
        {
            StatusLabel!.Text = LauncherUpdaterTranslator.UpdateError[CurrentLanguageInt];
        }
    }

    private void MakeUnixExecutable(string filePath)
    {
        var process = new Process();
        process.StartInfo.FileName = "chmod";
        process.StartInfo.Arguments = $"+x \"{filePath}\"";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        process.WaitForExit();
    }

    private void ReemplazarCuandoSeLibere(string archivoNuevo, string archivoViejo)
    {
        var psi = new ProcessStartInfo();
        psi.CreateNoWindow = true;
        psi.WindowStyle = ProcessWindowStyle.Hidden;
        psi.UseShellExecute = false;

        if (OperatingSystem.IsWindows())
        {
            psi.FileName = "cmd.exe";

            string script =
                $"for /L %i in (1,1,60) do ( " +
                $"  timeout /t 1 /nobreak >nul & " +
                $"  move /y \"{archivoNuevo}\" \"{archivoViejo}\" >nul 2>&1 & " +
                $"  if not errorlevel 1 ( " +
                $"    start \"\" \"{archivoViejo}\" & " +
                $"    exit /b 0 " +
                $"  ) " +
                $")";

            psi.Arguments = "/c " + script;
        }
        else
        {
            // Linux / macOS
            psi.FileName = "sh";

            var pid = Process.GetCurrentProcess().Id;
            psi.Arguments =
                $"-c \"" +
                $"i=0; ok=0; " +
                $"while [ $i -lt 60 ]; do " +
                $"  if mv -f '{archivoNuevo}' '{archivoViejo}' >/dev/null 2>&1; then ok=1; break; fi; " +
                $"  i=$((i+1)); " +
                $"  sleep 1; " +
                $"done; " +
                $"[ $ok -ne 1 ] && exit 1; " + // <-- Esto hace que salga si no se movió
                $"kill -9 {pid} >/dev/null 2>&1; " +
                $"while kill -0 {pid} 2>/dev/null; do sleep 1; done; " +
                $"exec '{archivoViejo}'" +
                "\"";
        }

        Process.Start(psi);

        // Cerrar el proceso actual
        // Environment.Exit(0)
    }

    private void CloseButton_Click(object? sender, EventArgs e)
    {
        Window!.Close();
    }

    private MainWindow? GetMainWindow()
    {
        return Singleton.GetCurrentInstance().MainWindow;
    }

    private void MainMenuButton_Click(object? sender, EventArgs e)
    {
        MainMenuRequested = true;
        Window!.Close();
    }

    private void TitleBarLabel_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Solo con botón izquierdo
        if (e.GetCurrentPoint(TitleBarLabel).Properties.IsLeftButtonPressed)
        {
            // Avalonia se encarga de DPI y límites automáticamente
            BeginMoveDrag(e);
        }
    }

    private void LoadingWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (MainMenuRequested)
        {
            GetMainWindow()!.Show();
            GetMainWindow()!.LoadingWindowChild = null;
        }
        else
        {
            Process.GetCurrentProcess().Kill();
        }
    }
}
