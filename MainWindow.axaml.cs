using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Win32;
using WindowsShortcutFactory;

namespace HabboCustomLauncher;

// PROBLEMA: Como se puede hacer para que se pueda definir manualmente un login code en lugar de solo leerlo desde el clipboard?
// SOLUCION: Al hacer click al boton se pregunta para introducir manualmente el codigo (aunque seria innecesario), mejor preguntar que hotel lanzar directamente? o tambien es innecesario? si todo es innecesario capaz convenga hacer que deje de ser un boton y pase a ser un label

public partial class MainWindow : Window
{
    public LoadingWindow? LoadingWindowChild;
    private bool LoadingWindowClientLaunchRequested = false;
    private Label? TitleBarLabel;
    private CustomButton? CloseButton;
    public CustomButton? StartNewInstanceButton;
    private CustomButton? StartNewInstanceButton2;
    private CustomButton? LoginCodeButton;
    private CustomButton? ChangeUpdateSourceButton;
    private CustomButton? ChangeUpdateSourceButton2;
    private Image? HabboLogoButton;
    private Image? GithubButton;
    private Image? SulakeButton;
    private CustomButton? FooterButton;
    private TextBlock? CreditsLink;
    private Border? CreditsOverlay;
    private Border? CreditsCard;
    private TextBlock? CreditsTitle;
    private TextBlock? CreditsBody;
    private TextBlock? CreditsRepoLink;
    private TextBlock? CreditsAccountLink;
    private CustomButton? CreditsCloseButton;
    public LoginCode? CurrentLoginCode;
    public JsonClientUrls? CurrentClientUrls;
    public int CurrentDownloadProgress;
    public string UpdateSource = "AIR_HFA";
    public int CurrentLanguageInt = 0;
    private readonly HttpClient HttpClient = new HttpClient();
    private bool _noSwfDownload = false;
    public string UnixPatchName = "HabboAirLinuxPatch_x64.zip"; // Depending on the platform, it can automatically become HabboAirLinuxPatch_x64.zip and HabboAirOSXPatch.zip
    public string WindowsPatchName = "HabboAirWindowsPatch_x86.zip"; // Depending on the architecture, it can automatically become HabboAirWindowsPatch_x64.zip
    public string AirPlusPatchName = "HabboAirPlusPatch.zip";
    public string LauncherShortcutOSXPatchName = "LauncherShortcutOSXPatch.zip";
    public string AirPlusClientURL = "https://github.com/LilithRainbows/HabboAirPlus/releases/download/latest/HabboAir.swf";
    // Repo de distribución de clientes HFA (generado por HfaPlus build). AIR_HFA descarga el SWF
    // ya parcheado por versión desde aquí, en lugar de exigir un build local.
    public string HfaPlusSwfBaseUrl = "https://raw.githubusercontent.com/denio4321/HfaPlusSwf/main";
    private string LauncherUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HabboLauncher/1.0.41 Chrome/87.0.4280.141 Electron/11.3.0 Safari/537.36";

    public MainWindow()
    {
        // This call is required by the designer
        InitializeComponent();
    }

    // Auto-wiring does not work for VB, so do it manually (se conserva ese patrón en C#)
    // Wires up the controls and optionally loads XAML markup and attaches dev tools (if Avalonia.Diagnostics package is referenced)
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
        // El elemento x:Name="Window" del XAML es la propia MainWindow (este Window), así que se usa "this".
        TitleBarLabel = this.FindNameScope()!.Find<Label>("TitleBarLabel");
        CloseButton = this.FindNameScope()!.Find<CustomButton>("CloseButton");
        StartNewInstanceButton = this.FindNameScope()!.Find<CustomButton>("StartNewInstanceButton");
        StartNewInstanceButton2 = this.FindNameScope()!.Find<CustomButton>("StartNewInstanceButton2");
        LoginCodeButton = this.FindNameScope()!.Find<CustomButton>("LoginCodeButton");
        ChangeUpdateSourceButton = this.FindNameScope()!.Find<CustomButton>("ChangeUpdateSourceButton");
        ChangeUpdateSourceButton2 = this.FindNameScope()!.Find<CustomButton>("ChangeUpdateSourceButton2");
        HabboLogoButton = this.FindNameScope()!.Find<Image>("HabboLogoButton");
        GithubButton = this.FindNameScope()!.Find<Image>("GithubButton");
        SulakeButton = this.FindNameScope()!.Find<Image>("SulakeButton");
        FooterButton = this.FindNameScope()!.Find<CustomButton>("FooterButton");
        CreditsLink = this.FindNameScope()!.Find<TextBlock>("CreditsLink");
        CreditsOverlay = this.FindNameScope()!.Find<Border>("CreditsOverlay");
        CreditsCard = this.FindNameScope()!.Find<Border>("CreditsCard");
        CreditsTitle = this.FindNameScope()!.Find<TextBlock>("CreditsTitle");
        CreditsBody = this.FindNameScope()!.Find<TextBlock>("CreditsBody");
        CreditsRepoLink = this.FindNameScope()!.Find<TextBlock>("CreditsRepoLink");
        CreditsAccountLink = this.FindNameScope()!.Find<TextBlock>("CreditsAccountLink");
        CreditsCloseButton = this.FindNameScope()!.Find<CustomButton>("CreditsCloseButton");

        // Textos del bloque de créditos según idioma
        CreditsLink!.Text = AppTranslator.CreditsLink[CurrentLanguageInt];
        CreditsTitle!.Text = AppTranslator.CreditsTitle[CurrentLanguageInt];
        CreditsBody!.Text = AppTranslator.CreditsBody[CurrentLanguageInt];
        CreditsRepoLink!.Text = AppTranslator.CreditsRepoLabel[CurrentLanguageInt];
        CreditsCloseButton!.Text = AppTranslator.CreditsClose[CurrentLanguageInt];

        // Suscripción manual de eventos (equivalente a los WithEvents/Handles del VB)
        CloseButton!.Click += CloseButton_Click;
        StartNewInstanceButton!.Click += StartNewInstanceButton_Click;
        StartNewInstanceButton.PropertyChanged += StartNewInstanceButton_PropertyChanged;
        StartNewInstanceButton2!.Click += StartNewInstanceButton2_Click;
        LoginCodeButton!.Click += LoginCodeButton_Click;
        ChangeUpdateSourceButton!.Click += ChangeUpdateSourceButton_Click;
        ChangeUpdateSourceButton2!.Click += ChangeUpdateSourceButton2_Click;
        FooterButton!.Click += FooterButton_Click;
        TitleBarLabel!.PointerPressed += TitleBarLabel_PointerPressed;
        HabboLogoButton!.PointerEntered += HabboLogoButton_PointerEntered;
        HabboLogoButton.PointerExited += HabboLogoButton_PointerExited;
        HabboLogoButton.PointerPressed += HabboLogoButton_PointerPressed;
        GithubButton!.PointerPressed += GithubButton_PointerPressed;
        GithubButton.PointerEntered += GithubButton_PointerEntered;
        GithubButton.PointerExited += GithubButton_PointerExited;
        SulakeButton!.PointerPressed += SulakeButton_PointerPressed;
        SulakeButton.PointerEntered += SulakeButtonButton_PointerEntered;
        SulakeButton.PointerExited += SulakeButtonButton_PointerExited;
        CreditsLink!.PointerPressed += CreditsLink_PointerPressed;
        CreditsCloseButton!.Click += CreditsCloseButton_Click;
        CreditsOverlay!.PointerPressed += CreditsOverlay_PointerPressed;
        CreditsCard!.PointerPressed += CreditsCard_PointerPressed;
        CreditsRepoLink!.PointerPressed += CreditsRepoLink_PointerPressed;
        CreditsAccountLink!.PointerPressed += CreditsAccountLink_PointerPressed;
        Closing += MainWindow_Closing;
        Activated += MainWindow_Activated;

        Singleton.GetCurrentInstance().ScaleMainGrid(this);
        Singleton.GetCurrentInstance().MainWindow = this;

        LoginCodeButton.Text = AppTranslator.ClipboardLoginCodeNotDetected[CurrentLanguageInt];
        StartNewInstanceButton.Text = AppTranslator.UnknownClientVersion[CurrentLanguageInt];

        DisplayLauncherVersionOnFooter();
        RefreshUpdateSourceText();
        FixWindowsTLS();
        RegisterHabboProtocol();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.OSArchitecture == Architecture.X64)
        {
            WindowsPatchName = "HabboAirWindowsPatch_x64.zip";
        }
        if ((RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            UnixPatchName = "HabboAirLinuxPatch_arm64.zip";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (OperatingSystem.IsMacOSVersionAtLeast(26, 0))
            {
                UnixPatchName = "HabboAirOSXTahoePatch.zip";
            }
            else
            {
                UnixPatchName = "HabboAirOSXPatch.zip";
            }
        }

        LoadSavedUpdateSource();

        _noSwfDownload = Environment.GetCommandLineArgs().Contains("--no-download");
        string habboProtocol = Environment.GetCommandLineArgs().FirstOrDefault(x => x.StartsWith("habbo://"), "");
        if (habboProtocol == "")
        {
            Show();
        }
        else
        {
            LoadingWindowChild = new LoadingWindow();
            LoadingWindowChild.Closed += LoadingWindowChild_Closed; // Handles LoadingWindowChild.Closed
            LoadingWindowChild.StatusLabel!.Text = AppTranslator.GenericLoading[CurrentLanguageInt] + " ..."; // Generic loading
            LoadingWindowChild.Show();
            _ = CopyToClipboard(habboProtocol);
        }
        StartRecursiveClipboardLoginCodeCheckAsync();
    }

    public string GetAirPatchNameForCurrentOS()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return WindowsPatchName;
        }
        else
        {
            return UnixPatchName;
        }
    }

    public async Task<bool> CopyToClipboard(string argument)
    {
        try
        {
            await Clipboard!.SetTextAsync(argument);
        }
        catch
        {
            return false;
        }
        return true;
    }

    private void DisplayLauncherVersionOnFooter()
    {
        FooterButton!.BackColor = Color.Parse("Transparent");
        FooterButton.Text = "HFA Launcher v1 (06/06/2026)";
    }

    private void DisplayCurrentUserOnFooter()
    {
        if (string.IsNullOrWhiteSpace(CurrentLoginCode!.Username))
        {
            DisplayLauncherVersionOnFooter();
        }
        else
        {
            FooterButton!.BackColor = Color.Parse("#8D31A500");
            string finalUsername = CurrentLoginCode.Username;
            if (finalUsername.Length > 15)
            {
                finalUsername = finalUsername.Remove(15) + "...";
            }
            FooterButton.Text = AppTranslator.PlayingAs[CurrentLanguageInt] + " " + finalUsername;
        }
    }

    private void StartNewInstanceButton_Click(object? sender, EventArgs e)
    {
        if (StartNewInstanceButton!.Text == AppTranslator.RetryClientUpdatesCheck[CurrentLanguageInt])
        {
            StartNewInstanceButton.IsButtonDisabled = true;
            StartNewInstanceButton2!.IsButtonDisabled = true;
            ChangeUpdateSourceButton!.IsButtonDisabled = true;
            ChangeUpdateSourceButton2!.IsButtonDisabled = true;
            FocusManager?.ClearFocus();
            _ = UpdateClientButtonStatus();
        }
        if (StartNewInstanceButton.Text.StartsWith(AppTranslator.UpdateClientVersion[CurrentLanguageInt]))
        {
            StartNewInstanceButton.IsButtonDisabled = true;
            StartNewInstanceButton2!.IsButtonDisabled = true;
            ChangeUpdateSourceButton!.IsButtonDisabled = true;
            ChangeUpdateSourceButton2!.IsButtonDisabled = true;
            FocusManager?.ClearFocus();
            _ = UpdateClient();
        }
        if (StartNewInstanceButton.Text.StartsWith(AppTranslator.LaunchClientVersion[CurrentLanguageInt]))
        {
            StartNewInstanceButton.IsButtonDisabled = true;
            StartNewInstanceButton2!.IsButtonDisabled = true;
            ChangeUpdateSourceButton!.IsButtonDisabled = true;
            ChangeUpdateSourceButton2!.IsButtonDisabled = true;
            FocusManager?.ClearFocus();
            _ = LaunchClient();
        }
    }

    public async Task LaunchClient()
    {
        try
        {
            var clientProcess = new Process();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // Windows
            {
                clientProcess.StartInfo.FileName = Path.Combine(GetPossibleClientPath(CurrentClientUrls!.FlashWindowsVersion), "Habbo.exe");
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // OSX
            {
                clientProcess.StartInfo.FileName = Path.Combine(GetPossibleClientPath(CurrentClientUrls!.FlashWindowsVersion), "Habbo.app", "Contents", "MacOS", "Habbo");
            }
            else // Linux
            {
                clientProcess.StartInfo.FileName = Path.Combine(GetPossibleClientPath(CurrentClientUrls!.FlashWindowsVersion), "Habbo");
            }
            clientProcess.StartInfo.Arguments = "-server " + CurrentLoginCode!.ServerId + " -ticket " + CurrentLoginCode.SSOTicket;
            await Task.Run(() => clientProcess.Start());
            CurrentLoginCode = null;
        }
        catch (Exception ex)
        {
            StartNewInstanceButton!.IsButtonDisabled = false;
            StartNewInstanceButton2!.IsButtonDisabled = false;
            ChangeUpdateSourceButton!.IsButtonDisabled = false;
            ChangeUpdateSourceButton2!.IsButtonDisabled = false;
            StartNewInstanceButton.Text = AppTranslator.LaunchClientVersion[CurrentLanguageInt] + " " + CurrentClientUrls!.FlashWindowsVersion;
            _ = MsgBox(AppTranslator.ErrorDebugClipboardHint[CurrentLanguageInt], AppTranslator.ClientLaunchError[CurrentLanguageInt], ex.Message);
        }
    }

    public async Task<bool> MsgBox(string title, string message, string clipboardDebugContent = "")
    {
        var errorDialog = new MessageBox();
        errorDialog.ConfigureContent(title, message, clipboardDebugContent);
        while (IsVisible == false)
        {
            await Task.Delay(100);
        }
        await errorDialog.ShowDialog(this);
        return true;
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

    private void CodesignFile(string filePath)
    {
        var p = new Process();
        p.StartInfo.FileName = "/usr/bin/codesign";
        p.StartInfo.Arguments = $"--force --timestamp=none --sign - \"{filePath}\"";
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.Start();
        p.WaitForExit();
    }

    public void UnzipFile(string sourcezip, string destinationfolder, bool overwrite, List<string>? itemsToSkip = null, bool ignoreIOExceptions = false)
    {
        string basePath = Path.GetFullPath(destinationfolder);
        using (ZipArchive archive = ZipFile.OpenRead(sourcezip))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                try
                {
                    string relativePath = entry.FullName.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

                    // ---- FILTRO DE EXCLUSIÓN ----
                    if (itemsToSkip != null)
                    {
                        string normalized = relativePath.TrimStart(Path.DirectorySeparatorChar);
                        bool skipEntry = false;

                        foreach (var skip in itemsToSkip)
                        {
                            string skipNorm = skip.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                            if (string.Equals(normalized, skipNorm, StringComparison.OrdinalIgnoreCase))
                            {
                                skipEntry = true;
                                break;
                            }
                            if (normalized.StartsWith(skipNorm + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                            {
                                skipEntry = true;
                                break;
                            }
                        }
                        if (skipEntry) continue;
                    }
                    // -----------------------------

                    string destinationFilePath = Path.GetFullPath(Path.Combine(basePath, relativePath));
                    if (!destinationFilePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new IOException("Zip slip error!");
                    }
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destinationFilePath);
                        continue;
                    }
                    string? dir = Path.GetDirectoryName(destinationFilePath);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    entry.ExtractToFile(destinationFilePath, overwrite);
                }
                catch (IOException)
                {
                    if (!ignoreIOExceptions) throw;
                }
            }
        }
    }

    private void ReplaceSwfVersion(string rutaArchivo, int nuevoValorInt)
    {
        byte[] datos = File.ReadAllBytes(rutaArchivo);
        datos[3] = (byte)nuevoValorInt;
        File.WriteAllBytes(rutaArchivo, datos);
    }

    private string GetSwfType(string swfPath)
    {
        using (var br = new BinaryReader(File.OpenRead(swfPath)))
        {
            br.BaseStream.Seek(0, SeekOrigin.Begin);
            return Encoding.UTF8.GetString(br.ReadBytes(4));
        }
        // El VB tenía aquí un Throw inalcanzable ("GetSwfType failed!"): se omite por ser código muerto.
    }

    public async Task UpdateClient()
    {
        try
        {
            string clientFolderPath = GetPossibleClientPath(CurrentClientUrls!.FlashWindowsVersion);
            string clientFilePath = Path.Combine(clientFolderPath, "ClientDownload.zip");
            if (UpdateSource == "AIR_Plus" || UpdateSource == "AIR_HFA")
            {
                clientFilePath = Path.Combine(clientFolderPath, "HabboAir.swf");
            }
            string downloadingClientHint = AppTranslator.DownloadingClient[CurrentLanguageInt];
            StartNewInstanceButton!.Text = downloadingClientHint;
            Directory.CreateDirectory(clientFolderPath);

            string clientUrl = CurrentClientUrls.FlashWindowsUrl;
            if (_noSwfDownload && File.Exists(clientFilePath))
            {
                StartNewInstanceButton.Text = downloadingClientHint + " (omitido)";
            }
            else
            {
                var umaka = DownloadRemoteFileAsync(clientUrl, clientFilePath);
                while (!umaka.IsCompleted)
                {
                    StartNewInstanceButton.Text = downloadingClientHint + " (" + CurrentDownloadProgress + "%)";
                    await Task.Delay(100);
                }
            }
            StartNewInstanceButton.Text = AppTranslator.ExtractingClient[CurrentLanguageInt];

            await Task.Run(() => CopyEmbeddedAsset(GetAirPatchNameForCurrentOS(), clientFolderPath));
            await Task.Run(() => UnzipFile(Path.Combine(clientFolderPath, GetAirPatchNameForCurrentOS()), clientFolderPath, true));
            File.Delete(Path.Combine(clientFolderPath, GetAirPatchNameForCurrentOS()));

            if (UpdateSource == "AIR_Plus" || UpdateSource == "AIR_HFA")
            {
                // AIR_HFA reutiliza el patch de AirPlus: iconos, mimetype y local_include
                // (placeholders de furni/sala) que el cliente parcheado también necesita.
                await Task.Run(() => CopyEmbeddedAsset(AirPlusPatchName, clientFolderPath));
                await Task.Run(() => UnzipFile(Path.Combine(clientFolderPath, AirPlusPatchName), clientFolderPath, true));
                File.Delete(Path.Combine(clientFolderPath, AirPlusPatchName));
            }
            else
            {
                var itemsToSkip = new List<string> { "Adobe AIR", "META-INF/signatures.xml", "META-INF/AIR/hash", "Habbo.exe" };
                await Task.Run(() => UnzipFile(clientFilePath, clientFolderPath, true, itemsToSkip));
                await Task.Run(() => File.Delete(clientFilePath));
                string clientSwfType = GetSwfType(Path.Combine(clientFolderPath, "HabboAir.swf"));
                if (clientSwfType.StartsWith("cWS") || clientSwfType.StartsWith("fWS") || clientSwfType.StartsWith("zWS"))
                {
                    // The swf is decrypted (if needed) so that it can later be edited for OSX (the user can also see/edit it)
                    await Task.Run(() => AirSwfDecryptor.FlashCrypto.DecryptFile(Path.Combine(clientFolderPath, "HabboAir.swf"), Path.Combine(clientFolderPath, "HabboAir.swf")));
                }
            }

            UpdateAirApplicationXML();

            if (File.ReadAllText(Path.Combine(clientFolderPath, "META-INF", "AIR", "application.xml")).Contains("<extensions>"))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    string habboAirExtensionsOSXPatchName = "HabboAirExtensionsOSXPatch.zip";
                    await Task.Run(() => CopyEmbeddedAsset(habboAirExtensionsOSXPatchName, clientFolderPath));
                    await Task.Run(() => UnzipFile(Path.Combine(clientFolderPath, habboAirExtensionsOSXPatchName), clientFolderPath, true));
                    File.Delete(Path.Combine(clientFolderPath, habboAirExtensionsOSXPatchName));
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string habboAirExtensionsWindowsPatchName = "HabboAirExtensionsWindowsPatch.zip";
                    await Task.Run(() => CopyEmbeddedAsset(habboAirExtensionsWindowsPatchName, clientFolderPath));
                    await Task.Run(() => UnzipFile(Path.Combine(clientFolderPath, habboAirExtensionsWindowsPatchName), clientFolderPath, true));
                    File.Delete(Path.Combine(clientFolderPath, habboAirExtensionsWindowsPatchName));
                }
            }

            string airCustomLicensePath = Path.Combine(clientFolderPath, "license.txt");
            if (File.Exists(airCustomLicensePath))
            {
                File.Move(airCustomLicensePath, Path.Combine(clientFolderPath, "META-INF", "AIR", "license.txt"), true);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (OperatingSystem.IsMacOSVersionAtLeast(26, 0))
                {
                    ReplaceSwfVersion(Path.Combine(clientFolderPath, "HabboAir.swf"), 51); // OSX Tahoe and later needs AIR 51+ to avoid keyboard shortcuts bugs
                }
                else
                {
                    ReplaceSwfVersion(Path.Combine(clientFolderPath, "HabboAir.swf"), 50); // OSX is limited to AIR version 50.2.3.8 to improve performance and provide compatibility with OSX 10.12+, so the swf version will be forced to 50
                }
            }
            else
            {
                ReplaceSwfVersion(Path.Combine(clientFolderPath, "HabboAir.swf"), 51); // Windows and Linux works with AIR 51+
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                FixOSXClientStructure();
                var executableFiles = new List<string>
                {
                    Path.Combine(clientFolderPath, "Habbo.app", "Contents", "Frameworks", "DiscordRichPresence.framework", "Versions", "A", "DiscordRichPresence"),
                    Path.Combine(clientFolderPath, "Habbo.app", "Contents", "Frameworks", "Adobe AIR.framework", "Versions", "1.0", "Adobe AIR"),
                    Path.Combine(clientFolderPath, "Habbo.app", "Contents", "MacOS", "Habbo")
                };
                foreach (var executableFile in executableFiles)
                {
                    if (File.Exists(executableFile))
                    {
                        MakeUnixExecutable(executableFile);
                    }
                }
                var codesignFilesOrDirectories = new List<string>
                {
                    Path.Combine(clientFolderPath, "Habbo.app", "Contents", "Frameworks", "DiscordRichPresence.framework"),
                    Path.Combine(clientFolderPath, "Habbo.app", "Contents", "Frameworks", "Adobe AIR.framework"),
                    Path.Combine(clientFolderPath, "Habbo.app", "Contents", "MacOS", "Habbo"),
                    Path.Combine(clientFolderPath, "Habbo.app")
                };
                foreach (var codesignFileOrDirectory in codesignFilesOrDirectories)
                {
                    if (File.Exists(codesignFileOrDirectory) || Directory.Exists(codesignFileOrDirectory))
                    {
                        CodesignFile(codesignFileOrDirectory);
                    }
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
            {
                MakeUnixExecutable(Path.Combine(clientFolderPath, "Habbo")); // Linux
            }

            if (UpdateSource == "AIR_Plus")
            {
                string airPlusClientLatestVersion = await GetRemoteLastModifiedHeaderEpoch(AirPlusClientURL);
                if ((CurrentClientUrls.FlashWindowsVersion == airPlusClientLatestVersion) == false)
                {
                    throw new Exception("AirPlus remote client version mismatch"); // Es muy poco probable pero este error ocurre si el identificador de la version remota de airplus cambio desde que se inicio el proceso de descarga
                }
            }

            File.WriteAllText(Path.Combine(clientFolderPath, "VERSION.txt"), CurrentClientUrls.FlashWindowsVersion);
            if (UpdateSource == "AIR_Official")
            {
                AddOrUpdateInstallation(CurrentClientUrls.FlashWindowsVersion, clientFolderPath, "air", 0);
            }

            StartNewInstanceButton.IsButtonDisabled = false;
            StartNewInstanceButton2!.IsButtonDisabled = false;
            ChangeUpdateSourceButton!.IsButtonDisabled = false;
            ChangeUpdateSourceButton2!.IsButtonDisabled = false;
            StartNewInstanceButton.Text = AppTranslator.LaunchClientVersion[CurrentLanguageInt] + " " + CurrentClientUrls.FlashWindowsVersion;
        }
        catch (Exception ex)
        {
            // StartNewInstanceButton.BackColor = Colors.Red
            StartNewInstanceButton!.IsButtonDisabled = false;
            StartNewInstanceButton2!.IsButtonDisabled = false;
            ChangeUpdateSourceButton!.IsButtonDisabled = false;
            ChangeUpdateSourceButton2!.IsButtonDisabled = false;
            StartNewInstanceButton.Text = AppTranslator.RetryClientUpdatesCheck[CurrentLanguageInt];
            // Clipboard.SetTextAsync(ex.ToString)
            _ = MsgBox(AppTranslator.ErrorDebugClipboardHint[CurrentLanguageInt], AppTranslator.ClientUpdateError[CurrentLanguageInt], ex.Message);
        }
    }

    public void FixOSXClientStructure()
    {
        // Rutas de origen y destino
        string origen = GetPossibleClientPath(CurrentClientUrls!.FlashWindowsVersion);
        string destino = Path.Combine(origen, "Habbo.app", "Contents", "Resources");
        Directory.CreateDirectory(destino);

        // Exclusiones
        string carpetaExcluida = "Habbo.app";
        string archivoExcluido = "README.txt";

        // Mover archivos
        foreach (var archivo in Directory.GetFiles(origen))
        {
            string nombreArchivo = Path.GetFileName(archivo);
            if (!nombreArchivo.Equals(archivoExcluido, StringComparison.OrdinalIgnoreCase))
            {
                string destinoArchivo = Path.Combine(destino, nombreArchivo);
                if (!string.Equals(archivo, destinoArchivo, StringComparison.OrdinalIgnoreCase))
                {
                    File.Move(archivo, destinoArchivo, true);
                }
            }
        }

        // Mover carpetas
        foreach (var carpeta in Directory.GetDirectories(origen))
        {
            string nombreCarpeta = Path.GetFileName(carpeta);
            if (!nombreCarpeta.Equals(carpetaExcluida, StringComparison.OrdinalIgnoreCase))
            {
                string destinoCarpeta = Path.Combine(destino, nombreCarpeta);
                MoveMerge(carpeta, destinoCarpeta);
            }
        }
    }

    private void MoveMerge(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var currFile in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(targetDir, Path.GetFileName(currFile));
            if (!string.Equals(currFile, destFile, StringComparison.OrdinalIgnoreCase))
            {
                File.Move(currFile, destFile, true);
            }
        }
        foreach (var currDir in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(targetDir, Path.GetFileName(currDir));
            MoveMerge(currDir, destSubDir);
        }
        if (Directory.Exists(sourceDir) && !Directory.EnumerateFileSystemEntries(sourceDir).Any())
        {
            Directory.Delete(sourceDir);
        }
    }

    public void UpdateAirApplicationXML()
    {
        string clientFolderPath = GetPossibleClientPath(CurrentClientUrls!.FlashWindowsVersion);
        string originalXmlPath = Path.Combine(clientFolderPath, "META-INF", "AIR", "application.xml");
        string originalXmlVersionNumber;
        XElement? originalXmlExtensionsNode;
        string newXmlPath = Path.Combine(clientFolderPath, "application.xml");
        var xmlDoc = new XDocument();
        if (File.Exists(originalXmlPath))
        {
            xmlDoc = XDocument.Load(originalXmlPath);
            originalXmlVersionNumber = xmlDoc.Root!.Elements().First(x => x.Name.LocalName == "versionLabel").Value;
            originalXmlExtensionsNode = xmlDoc.Root.Elements().FirstOrDefault(x => x.Name.LocalName == "extensions");
        }
        else
        {
            originalXmlVersionNumber = "1.0";
            originalXmlExtensionsNode = null;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false && RuntimeInformation.IsOSPlatform(OSPlatform.OSX) == false)
        {
            originalXmlExtensionsNode = null; // In the future, AIR extensions support for Linux, if possible, should be added.
        }
        xmlDoc = XDocument.Load(newXmlPath);
        xmlDoc.Root!.Elements().First(x => x.Name.LocalName == "versionLabel").Value = originalXmlVersionNumber; // Reemplaza con el nuevo valor
        xmlDoc.Root.Elements().First(x => x.Name.LocalName == "versionNumber").Value = originalXmlVersionNumber; // Reemplaza con el nuevo valor
        if (originalXmlExtensionsNode != null)
        {
            XNamespace newXmlNamespace = xmlDoc.Root.Name.Namespace;
            xmlDoc.Root.Add(new XElement(newXmlNamespace + originalXmlExtensionsNode.Name.LocalName, originalXmlExtensionsNode.Elements().Select(e => new XElement(newXmlNamespace + e.Name.LocalName, e.Value))));
        }
        xmlDoc.Save(originalXmlPath);
        File.Delete(newXmlPath);
    }

    public void CopyEmbeddedAsset(string assetName, string destinationFolder)
    {
        string resourceName = "avares://" + Assembly.GetExecutingAssembly().GetName().Name + "/Assets/" + assetName;
        Stream resourceStream = AssetLoader.Open(new Uri(resourceName));
        using (FileStream fileStream = File.Create(Path.Combine(destinationFolder, assetName)))
        {
            resourceStream.CopyTo(fileStream);
        }
    }

    public async Task<string> DownloadRemoteFileAsync(string remoteFileUrl, string downloadFilePath)
    {
        CurrentDownloadProgress = 0;
        HttpClient.DefaultRequestHeaders.Clear();
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(LauncherUserAgent);
        var response = await HttpClient.GetAsync(remoteFileUrl, HttpCompletionOption.ResponseHeadersRead);
        var totalSize = response.Content.Headers.ContentLength;
        var downloaded = 0;
        using (var stream = await response.Content.ReadAsStreamAsync())
        {
            using (var file = new FileStream(downloadFilePath, FileMode.Create, FileAccess.Write))
            {
                var buffer = new byte[1025];
                int bytesRead;
                do
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    await file.WriteAsync(buffer, 0, bytesRead);
                    downloaded += bytesRead;
                    CurrentDownloadProgress = Convert.ToInt32(downloaded / (double)totalSize!.Value * 100);
                }
                while (bytesRead > 0);
            }
        }
        return null!;
    }

    private async void StartRecursiveClipboardLoginCodeCheckAsync()
    {
        await CheckClipboardLoginCodeAsync();
        while (true)
        {
            await Task.Delay(500);
            await CheckClipboardLoginCodeAsync();
        }
    }

    public async Task<bool> EnsureCurrentWindowFocus()
    {
        if (LoadingWindowChild == null && IsActive == false)
        {
            await EnsureWindowFocus(this);
        }
        if (LoadingWindowChild != null && LoadingWindowChild.IsActive == false)
        {
            await EnsureWindowFocus(LoadingWindowChild);
        }
        return true;
    }

    private async Task<bool> CheckClipboardLoginCodeAsync()
    {
        try
        {
            string? clipboardText = await Clipboard!.GetTextAsync();

            if (clipboardText!.StartsWith("hcl_main_focus_"))
            {
                clipboardText = clipboardText.Replace("hcl_main_focus_", "");
                await EnsureCurrentWindowFocus();
                await CopyToClipboard(clipboardText);
            }
            var clipboardLoginCode = new LoginCode(clipboardText);
            if (string.IsNullOrWhiteSpace(clipboardLoginCode.ServerUrl))
            {
                throw new Exception("Invalid clipboard login code");
            }
            else
            {
                string oldLoginTicket = "";
                if (CurrentLoginCode != null)
                {
                    oldLoginTicket = CurrentLoginCode.SSOTicket;
                }
                CurrentLoginCode = clipboardLoginCode;
                await CopyToClipboard("");
                LoginCodeButton!.Text = AppTranslator.ClipboardLoginCodeDetected[CurrentLanguageInt] + " [" + clipboardLoginCode.ServerId.Replace("hh", "").ToUpper() + "]";
                if ((oldLoginTicket == clipboardLoginCode.SSOTicket) == false)
                {
                    await EnsureCurrentWindowFocus();
                    DisplayCurrentUserOnFooter();
                    await UpdateClientButtonStatus();
                    return true;
                }
                // Await Application.Current.Clipboard.SetTextAsync("ServerId: " & LoginCode.ServerId & " - ServerUrl: " & LoginCode.ServerUrl & " - SSOTicket: " & LoginCode.SSOTicket)
            }
        }
        catch
        {
            if (CurrentLoginCode != null)
            {
                return false; // Ignore invalid clipboard login codes if there is already a valid login code
            }
            CurrentLoginCode = null;
            StartNewInstanceButton!.IsButtonDisabled = true;
            StartNewInstanceButton2!.IsButtonDisabled = true;
            ChangeUpdateSourceButton!.IsButtonDisabled = true;
            ChangeUpdateSourceButton2!.IsButtonDisabled = true;
            LoginCodeButton!.Text = AppTranslator.ClipboardLoginCodeNotDetected[CurrentLanguageInt];
            StartNewInstanceButton.Text = AppTranslator.UnknownClientVersion[CurrentLanguageInt];
            DisplayLauncherVersionOnFooter();
        }
        return false;
    }

    public async Task CleanDeprecatedClients()
    {
        // AGREGAR OPCION PARA HABILITAR/DESHABILITAR LA LIMPIEZA AUTOMATICA DE CLIENTES OBSOLETOS?
        try
        {
            if (UpdateSource == "AIR_Official")
            {
                StartNewInstanceButton!.Text = "Cleaning deprecated clients";
                JsonElement jsonRoot = JsonDocument.Parse(await GetRemoteJsonAsync("https://images.habbo.com/habbo-native-clients/launcher/clientversions.json")).RootElement;
                string?[] validClientVersions = jsonRoot.GetProperty("win").GetProperty("air").EnumerateArray().Select(x => x.GetString()).ToArray();
                foreach (var installedClientVersion in Directory.GetDirectories(GetPossibleClientPath("")).Select(x => Path.GetFileName(x)))
                {
                    if (IsNumeric(installedClientVersion) && validClientVersions.Contains(installedClientVersion) == false)
                    {
                        await Task.Run(() => Directory.Delete(GetPossibleClientPath(installedClientVersion), true));
                    }
                }
            }
        }
        catch
        {
            // We ignore the error
        }
    }

    public async Task<string> GetRemoteLastModifiedHeaderEpoch(string url)
    {
        HttpClient.DefaultRequestHeaders.Clear();
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36");
        var request = new HttpRequestMessage(HttpMethod.Head, url);
        HttpResponseMessage response = await HttpClient.SendAsync(request);
        if (response.Headers.Contains("x-ms-creation-time"))
        {
            var lastmodified = response.Headers.GetValues("x-ms-creation-time").FirstOrDefault();
            DateTime dateTimeUtc = DateTime.ParseExact(lastmodified!, "ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            long epochTime = (long)(dateTimeUtc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            return epochTime.ToString();
        }
        else
        {
            throw new Exception("Last modified header not found");
        }
    }

    public bool IsClientVersionExists(string clientVersion = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(clientVersion))
            {
                clientVersion = CurrentClientUrls!.FlashWindowsVersion;
            }
            string clientPath = GetPossibleClientPath(clientVersion);
            if (UpdateSource == "AIR_HFA")
            {
                // No vale el registro de instalaciones "air": la carpeta hfa debe tener su
                // propio runtime extraído (VERSION.txt) y el SWF generado por HfaPlus.
                return Directory.Exists(clientPath)
                    && File.Exists(Path.Combine(clientPath, "VERSION.txt"))
                    && File.Exists(Path.Combine(clientPath, "HabboAir.swf"));
            }
            return Directory.Exists(clientPath) && (File.Exists(Path.Combine(clientPath, "VERSION.txt")) || InstallationExists(clientVersion, "air"));
        }
        catch
        {
            return false;
        }
    }

    public async Task UpdateClientButtonStatus()
    {
        StartNewInstanceButton!.IsButtonDisabled = true;
        StartNewInstanceButton2!.IsButtonDisabled = true;
        ChangeUpdateSourceButton!.IsButtonDisabled = true;
        ChangeUpdateSourceButton2!.IsButtonDisabled = true;
        try
        {
            bool isClientUpdated = false;
            StartNewInstanceButton.Text = AppTranslator.ClientUpdatesCheck[CurrentLanguageInt];
            if (UpdateSource == "AIR_Official")
            {
                CurrentClientUrls = new JsonClientUrls(await GetRemoteJsonAsync("https://" + CurrentLoginCode!.ServerUrl + "/gamedata/clienturls"));
            }
            if (UpdateSource == "AIR_HFA")
            {
                // La versión sigue siendo la oficial (para que los identificadores cuadren con
                // AIR Official), pero el SWF ya parcheado se DESCARGA del repo de distribución
                // HfaPlusSwf por versión, replicando el mecanismo de AirPlus.
                var officialUrls = new JsonClientUrls(await GetRemoteJsonAsync("https://" + CurrentLoginCode!.ServerUrl + "/gamedata/clienturls"));
                string hfaSwfUrl = HfaPlusSwfBaseUrl + "/versions/" + officialUrls.FlashWindowsVersion + "/HabboAir.swf";
                CurrentClientUrls = new JsonClientUrls(("{'flash-windows-version':'" + officialUrls.FlashWindowsVersion + "','flash-windows':'" + hfaSwfUrl + "'}").Replace("'", ((char)34).ToString()));
            }
            if (UpdateSource == "AIR_Plus")
            {
                string airPlusClientLatestVersion = await GetRemoteLastModifiedHeaderEpoch(AirPlusClientURL);
                CurrentClientUrls = new JsonClientUrls(("{'flash-windows-version':'" + airPlusClientLatestVersion + "','flash-windows':'" + AirPlusClientURL + "'}").Replace("'", ((char)34).ToString()));
            }

            isClientUpdated = IsClientVersionExists();
            // Await CleanDeprecatedClients() 'No se si lo ideal seria ponerlo aca o solo en UpdateClient, lo malo seria que de esa forma si un cliente se actualiza a un server actualiza a una version de cliente ya existe entonces no se eliminaria la version anterior a menos que se vuelva a actualizar.

            if (isClientUpdated) // Abria que verificar swf o mejor aun que exista un archivo READY para asegurarse que se completo todo el proceso de modificacion
            {
                StartNewInstanceButton.Text = AppTranslator.LaunchClientVersion[CurrentLanguageInt] + " " + CurrentClientUrls!.FlashWindowsVersion;
            }
            else
            {
                StartNewInstanceButton.Text = AppTranslator.UpdateClientVersion[CurrentLanguageInt] + " " + CurrentClientUrls!.FlashWindowsVersion;
            }
        }
        catch (Exception ex)
        {
            // StartNewInstanceButton.BackColor = Media.Color.FromRgb(200, 0, 0)
            StartNewInstanceButton.Text = AppTranslator.RetryClientUpdatesCheck[CurrentLanguageInt];
            _ = MsgBox(AppTranslator.ErrorDebugClipboardHint[CurrentLanguageInt], AppTranslator.ClientUpdatesCheckError[CurrentLanguageInt], ex.Message);
        }
        StartNewInstanceButton.IsButtonDisabled = false;
        StartNewInstanceButton2.IsButtonDisabled = false;
        ChangeUpdateSourceButton.IsButtonDisabled = false;
        ChangeUpdateSourceButton2.IsButtonDisabled = false;
    }

    public string GetAppDataPath()
    {
        string appDataFolderPath = ""; // Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        if (string.IsNullOrWhiteSpace(appDataFolderPath))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                appDataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                appDataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Roaming");
            }
            else
            {
                appDataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
            }
        }
        Directory.CreateDirectory(appDataFolderPath);
        return appDataFolderPath;
    }

    public string GetPossibleClientPath(string clientVersion)
    {
        string clientType = "air";
        if (UpdateSource == "AIR_Plus")
        {
            clientType = "airplus";
        }
        if (UpdateSource == "AIR_HFA")
        {
            clientType = "hfa";
        }
        return Path.Combine(GetAppDataPath(), "Habbo Launcher", "downloads", clientType, clientVersion);
    }

    public void SaveCurrentUpdateSource()
    {
        string destinationFolder = Path.Combine(GetAppDataPath(), "Habbo Launcher", "downloads");
        Directory.CreateDirectory(destinationFolder);
        File.WriteAllText(Path.Combine(destinationFolder, "UpdateSource.txt"), UpdateSource);
    }

    public void LoadSavedUpdateSource()
    {
        string destinationFile = Path.Combine(GetAppDataPath(), "Habbo Launcher", "downloads", "UpdateSource.txt");
        if (File.Exists(destinationFile))
        {
            string savedSource = File.ReadAllText(destinationFile);
            string[] allowedSources = { "AIR_Plus", "AIR_Official", "AIR_HFA" };
            if (allowedSources.Contains(savedSource))
            {
                UpdateSource = savedSource;
                RefreshUpdateSourceText();
            }
        }
    }

    public async Task<string> GetRemoteJsonAsync(string jsonUrl)
    {
        HttpClient.DefaultRequestHeaders.Clear();
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(LauncherUserAgent);
        HttpResponseMessage response = await HttpClient.GetAsync(jsonUrl);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            return "";
        }
    }

    private void LoginCodeButton_Click(object? sender, EventArgs e)
    {
        if ((LoginCodeButton!.Text == AppTranslator.ClipboardLoginCodeNotDetected[CurrentLanguageInt]) == false)
        {
            CurrentLoginCode = null;
            return;
        }
        string habboAvatarSettingsUrl = "https://www.habbo.com/settings/avatars";
        if (CultureInfo.CurrentCulture.Name.ToLower().StartsWith("pt"))
        {
            habboAvatarSettingsUrl = "https://www.habbo.com.br/settings/avatars";
        }
        if (CultureInfo.CurrentCulture.Name.ToLower().StartsWith("es"))
        {
            habboAvatarSettingsUrl = "https://www.habbo.es/settings/avatars";
        }
        if (CultureInfo.CurrentCulture.Name.ToLower().StartsWith("de"))
        {
            habboAvatarSettingsUrl = "https://www.habbo.de/settings/avatars";
        }
        if (CultureInfo.CurrentCulture.Name.ToLower().StartsWith("fr"))
        {
            habboAvatarSettingsUrl = "https://www.habbo.fr/settings/avatars";
        }
        if (CultureInfo.CurrentCulture.Name.ToLower().StartsWith("it"))
        {
            habboAvatarSettingsUrl = "https://www.habbo.it/settings/avatars";
        }
        if (CultureInfo.CurrentCulture.Name.ToLower() == "tr")
        {
            habboAvatarSettingsUrl = "https://www.habbo.com.tr/settings/avatars";
        }
        if (CultureInfo.CurrentCulture.Name.ToLower().StartsWith("nl"))
        {
            habboAvatarSettingsUrl = "https://www.habbo.nl/settings/avatars";
        }
        if (CultureInfo.CurrentCulture.Name.ToLower() == "fi")
        {
            habboAvatarSettingsUrl = "https://www.habbo.fi/settings/avatars";
        }
        try
        {
            Process.Start(new ProcessStartInfo(habboAvatarSettingsUrl) { UseShellExecute = true });
        }
        catch
        {
            // Error while launching habbo avatar settings url
        }
    }

    private void RefreshUpdateSourceText()
    {
        string currentUpdateSourceLabel = AppTranslator.CurrentUpdateSource[CurrentLanguageInt];
        switch (UpdateSource)
        {
            case "AIR_Official":
                ChangeUpdateSourceButton!.Text = currentUpdateSourceLabel + ": AIR Classic";
                break;
            case "AIR_Plus":
                ChangeUpdateSourceButton!.Text = currentUpdateSourceLabel + ": AIR Plus";
                break;
            case "AIR_HFA":
                ChangeUpdateSourceButton!.Text = currentUpdateSourceLabel + ": AIR HFA";
                break;
            default:
                ChangeUpdateSourceButton!.Text = currentUpdateSourceLabel + ": Unknown";
                break;
        }
    }

    public string GetCurrentUpdateSourceName()
    {
        switch (UpdateSource)
        {
            case "AIR_Official":
                return "AIR Classic";
            case "AIR_Plus":
                return "AIR Plus";
            case "AIR_HFA":
                return "AIR HFA";
            default:
                return "Unknown";
        }
    }

    private void ChangeUpdateSourceButton_Click(object? sender, EventArgs e)
    {
        switch (UpdateSource)
        {
            case "AIR_Official":
                UpdateSource = "AIR_Plus";
                break;
            case "AIR_Plus":
                UpdateSource = "AIR_HFA";
                break;
            default:
                UpdateSource = "AIR_Official";
                break;
        }
        SaveCurrentUpdateSource();
        RefreshUpdateSourceText();
        _ = UpdateClientButtonStatus();
    }

    private void StartNewInstanceButton2_Click(object? sender, EventArgs e)
    {
        // Temporalmente elimina la instalacion actual, en un futuro deberia abrirse una ventana con varias opciones
        // (Por ejemplo usar una version especifica ya descargada del cliente, borrar instalacion existente, borrar todas instalaciones, etc.)
        try
        {
            Directory.Delete(GetPossibleClientPath(CurrentClientUrls!.FlashWindowsVersion), true);
            RemoveInstallation(CurrentClientUrls.FlashWindowsVersion, "air");
            StartNewInstanceButton!.IsButtonDisabled = true;
            StartNewInstanceButton2!.IsButtonDisabled = true;
            ChangeUpdateSourceButton!.IsButtonDisabled = true;
            ChangeUpdateSourceButton2!.IsButtonDisabled = true;
            FocusManager?.ClearFocus();
            _ = UpdateClientButtonStatus();
        }
        catch (Exception ex)
        {
            _ = MsgBox(AppTranslator.ErrorDebugClipboardHint[CurrentLanguageInt], AppTranslator.ClientDeleteError[CurrentLanguageInt], ex.Message);
        }
    }

    public bool RegisterHabboProtocol()
    {
        try
        {
            string uriScheme = "habbo";
            string friendlyName = "HFA Launcher";
            string applicationLocation = Process.GetCurrentProcess().MainModule!.FileName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + uriScheme))
                {
                    key.SetValue("", "URL:" + friendlyName);
                    key.SetValue("URL Protocol", "");

                    using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
                    {
                        defaultIcon.SetValue("", applicationLocation + ",1");
                    }

                    using (var commandKey = key.CreateSubKey("shell\\open\\command"))
                    {
                        commandKey.SetValue("", "\"" + applicationLocation + "\" \"%1\"");
                    }
                }
                return true;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            {
                AddStartMenuShortcut(); // xdg protocol association requires an start menu shortcut
                var processInfo = new ProcessStartInfo("xdg-mime", "default HabboCustomLauncher.desktop x-scheme-handler/habbo")
                {
                    UseShellExecute = false,
                    CreateNoWindow = false
                };
                Process.Start(processInfo)?.WaitForExit();
                return true;
            }
            throw new Exception("Could not register protocol");
        }
        catch
        {
            // MsgBox(AppTranslator.ProtocolRegError(CurrentLanguageInt), MsgBoxStyle.Critical, "Error")
            return false;
        }
    }

    public void FixWindowsTLS()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (var key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings"))
                {
                    if (Convert.ToInt32(key.GetValue("SecureProtocols")) < 2048) // johnou implementation
                    {
                        key.SetValue("SecureProtocols", Convert.ToInt32(key.GetValue("SecureProtocols")) + 2048);
                    }
                    if (string.IsNullOrEmpty(Convert.ToString(key.GetValue("DefaultSecureProtocols"))) == false)
                    {
                        if (Convert.ToInt32(key.GetValue("DefaultSecureProtocols")) < 2048)
                        {
                            key.SetValue("DefaultSecureProtocols", Convert.ToInt32(key.GetValue("DefaultSecureProtocols")) + 2048);
                        }
                    }
                }
                bool needExtraSteps = false;
                using (var key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\SecurityProviders\\SCHANNEL\\Protocols\\TLS 1.2\\Client"))
                {
                    if ((key == null) == false)
                    {
                        if ((Convert.ToString(key!.GetValue("DisabledByDefault")) == "1") || (Convert.ToString(key.GetValue("Enabled")) == "0"))
                        {
                            needExtraSteps = true;
                        }
                    }
                }
                if (needExtraSteps == true)
                {
                    if (WindowsUserIsAdmin())
                    {
                        using (var key = Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\SecurityProviders\\SCHANNEL\\Protocols\\TLS 1.2\\Client"))
                        {
                            key.SetValue("DisabledByDefault", 0);
                            key.SetValue("Enabled", 1);
                        }
                    }
                    else
                    {
                        // MsgBox(AppTranslator.TLSFixAdminRightsError(CurrentLanguageInt), MsgBoxStyle.Critical, "Error")
                        Environment.Exit(0);
                    }
                }
            }
        }
        catch
        {
            Console.WriteLine("Could not fix Windows TLS.");
        }
    }

    private bool WindowsUserIsAdmin()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        else
        {
            return false;
        }
    }

    private void FooterButton_Click(object? sender, EventArgs e)
    {
        if (FooterButton!.Text.StartsWith(AppTranslator.PlayingAs[CurrentLanguageInt]))
        {
            string profileUrl = "https://" + CurrentLoginCode!.ServerUrl + "/profile/" + CurrentLoginCode.Username;
            try
            {
                Process.Start(new ProcessStartInfo(profileUrl) { UseShellExecute = true });
            }
            catch
            {
                // Error while launching habbo profile url
            }
        }
    }

    private void GithubButton_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/LilithRainbows/HabboCustomLauncher") { UseShellExecute = true });
        }
        catch
        {
            // Error while launching github url
        }
    }

    // ---- Créditos / agradecimientos a LilithRainbows -------------------------------------
    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* error al abrir la url */ }
    }

    private void CreditsLink_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (CreditsOverlay != null) CreditsOverlay.IsVisible = true;
    }

    private void CreditsCloseButton_Click(object? sender, EventArgs e)
    {
        if (CreditsOverlay != null) CreditsOverlay.IsVisible = false;
    }

    // Clic en el fondo del overlay (fuera de la tarjeta) → cerrar.
    private void CreditsOverlay_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (CreditsOverlay != null) CreditsOverlay.IsVisible = false;
    }

    // Clic dentro de la tarjeta: no propagar al fondo (no cerrar).
    private void CreditsCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    private void CreditsRepoLink_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        OpenUrl("https://github.com/LilithRainbows/HabboCustomLauncher");
    }

    private void CreditsAccountLink_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        OpenUrl("https://github.com/LilithRainbows");
    }

    private void SulakeButton_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://www.sulake.com/habbo/") { UseShellExecute = true });
        }
        catch
        {
            // Error while launching sulake url
        }
    }

    private void HabboLogoButton_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (HabboLogoButton!.ContextMenu != null && HabboLogoButton.ContextMenu.IsOpen)
        {
            return;
        }
        HabboLogoButton.Source = new Bitmap(AssetLoader.Open(new Uri("avares://" + Assembly.GetExecutingAssembly().GetName().Name + "/Assets/hfa-logo.png")));
    }

    private void HabboLogoButton_PointerExited(object? sender, PointerEventArgs e)
    {
        if (HabboLogoButton!.ContextMenu != null && HabboLogoButton.ContextMenu.IsOpen)
        {
            return;
        }
        HabboLogoButton.Source = new Bitmap(AssetLoader.Open(new Uri("avares://" + Assembly.GetExecutingAssembly().GetName().Name + "/Assets/hfa-logo.png")));
    }

    private void GithubButton_PointerEntered(object? sender, PointerEventArgs e)
    {
        GithubButton!.Source = new Bitmap(AssetLoader.Open(new Uri("avares://" + Assembly.GetExecutingAssembly().GetName().Name + "/Assets/github-icon-2.png")));
    }

    private void GithubButton_PointerExited(object? sender, PointerEventArgs e)
    {
        GithubButton!.Source = new Bitmap(AssetLoader.Open(new Uri("avares://" + Assembly.GetExecutingAssembly().GetName().Name + "/Assets/github-icon.png")));
    }

    private void SulakeButtonButton_PointerEntered(object? sender, PointerEventArgs e)
    {
        SulakeButton!.Source = new Bitmap(AssetLoader.Open(new Uri("avares://" + Assembly.GetExecutingAssembly().GetName().Name + "/Assets/habbo-footer-2.png")));
    }

    private void SulakeButtonButton_PointerExited(object? sender, PointerEventArgs e)
    {
        SulakeButton!.Source = new Bitmap(AssetLoader.Open(new Uri("avares://" + Assembly.GetExecutingAssembly().GetName().Name + "/Assets/habbo-footer.png")));
    }

    private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        Process.GetCurrentProcess().Kill();
    }

    private void HabboLogoButton_ContextMenuClosed(object? sender, RoutedEventArgs e)
    {
        HabboLogoButton_PointerExited(null, null!);
    }

    private void HabboLogoButton_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var addDesktopShortcutMenuItem = new MenuItem { Header = AppTranslator.AddDesktopShortcut[CurrentLanguageInt] };
        addDesktopShortcutMenuItem.Click += (s, ev) => AddDesktopShortcut();

        var addStartMenuShortcutMenuItem = new MenuItem { Header = AppTranslator.AddStartMenuShortcut[CurrentLanguageInt] };
        addStartMenuShortcutMenuItem.Click += (s, ev) => AddStartMenuShortcut();

        // Dim ToggleAutomaticHabboProtocolMenuItem As New MenuItem With {.Header = AppTranslator.AutomaticHabboProtocol(CurrentLanguageInt) & " (" & AppTranslator.Enabled(CurrentLanguageInt).ToLower & ")"}
        // AddHandler ToggleAutomaticHabboProtocolMenuItem.Click, AddressOf ToggleAutomaticHabboProtocol

        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(addDesktopShortcutMenuItem);
        contextMenu.Items.Add(addStartMenuShortcutMenuItem);
        // contextMenu.Items.Add(ToggleAutomaticHabboProtocolMenuItem)
        if (HabboLogoButton!.ContextMenu != null)
        {
            HabboLogoButton.ContextMenu.Close();
            HabboLogoButton.ContextMenu = null;
        }
        HabboLogoButton.ContextMenu = contextMenu;
        HabboLogoButton.ContextMenu.Closed += HabboLogoButton_ContextMenuClosed;
        HabboLogoButton.ContextMenu.Open();
    }

    private void AddDesktopShortcut()
    {
        CreateShortcut(Environment.ProcessPath!, "HabboCustomLauncher", true);
    }

    private void AddStartMenuShortcut()
    {
        CreateShortcut(Environment.ProcessPath!, "HabboCustomLauncher", false);
    }

    private void ToggleAutomaticHabboProtocol()
    {
        // TODO
    }

    private void CreateShortcut(string appPath, string appName, bool isDesktop)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using (var shortcut = new WindowsShortcut { Path = appPath })
            {
                if (isDesktop)
                {
                    shortcut.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), appName + ".lnk"));
                }
                else
                {
                    string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                    Directory.CreateDirectory(startMenuPath);
                    shortcut.Save(Path.Combine(startMenuPath, appName + ".lnk"));
                }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            string osxDownloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            CopyEmbeddedAsset(LauncherShortcutOSXPatchName, osxDownloadFolder);

            ZipFile.ExtractToDirectory(Path.Combine(osxDownloadFolder, LauncherShortcutOSXPatchName), osxDownloadFolder, true);
            File.Delete(Path.Combine(osxDownloadFolder, LauncherShortcutOSXPatchName));

            string scriptPath = Path.Combine(osxDownloadFolder, "HabboCustomLauncherShortcut.sh");
            var originalScriptContent = File.ReadAllText(scriptPath, new UTF8Encoding(false));
            originalScriptContent = originalScriptContent.Replace("%HabboCustomLauncherAppPath%", appPath);
            if (isDesktop)
            {
                originalScriptContent = originalScriptContent.Replace("/Applications/", "$HOME/Desktop/");
            }
            File.WriteAllText(scriptPath, originalScriptContent, new UTF8Encoding(false));

            MakeUnixExecutable(scriptPath);
            var process = new Process();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = "-c \"\"" + scriptPath + "\"\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            File.Delete(Path.Combine(osxDownloadFolder, "HabboCustomLauncherShortcut.sh"));
        }
        else // Linux
        {
            string shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrWhiteSpace(shortcutPath))
            {
                shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop");
            }
            string iconsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".icons");
            if (isDesktop == false)
            {
                shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "applications");
            }
            string shortcutContent =
                ($"[Desktop Entry]\n" +
                 $"                Type=Application\n" +
                 $"                Name={appName}\n" +
                 $"                Exec=\"{appPath}\" %U\n" +
                 $"                Terminal=false\n" +
                 $"                Icon=HabboCustomLauncherIcon.png\n" +
                 $"                Categories=Game;\n" +
                 $"                MimeType=x-scheme-handler/habbo;").Replace("                ", "");
            Directory.CreateDirectory(iconsPath);
            Directory.CreateDirectory(shortcutPath);
            CopyEmbeddedAsset("HabboCustomLauncherIcon.png", iconsPath);
            File.WriteAllText(Path.Combine(shortcutPath, appName + ".desktop"), shortcutContent);
            MakeUnixExecutable(Path.Combine(shortcutPath, appName + ".desktop"));
        }
    }

    private void ChangeUpdateSourceButton2_Click(object? sender, EventArgs e)
    {
        var contextMenu = new ContextMenu();
        string clientHint = AppTranslator.ClassicAirClientHint[CurrentLanguageInt];
        if (UpdateSource == "AIR_Plus")
        {
            clientHint = AppTranslator.AirPlusClientHint[CurrentLanguageInt];
        }
        if (UpdateSource == "AIR_HFA")
        {
            clientHint = AppTranslator.HfaClientHint[CurrentLanguageInt];
        }
        contextMenu.Items.Add(new MenuItem { Header = clientHint });
        if (ChangeUpdateSourceButton2!.ContextMenu != null)
        {
            ChangeUpdateSourceButton2.ContextMenu.Close();
        }
        ChangeUpdateSourceButton2.ContextMenu = contextMenu;
        ChangeUpdateSourceButton2.ContextMenu.Open();
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

    private void CloseButton_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private async Task<bool> EnsureWindowFocus(Window requestedWindow)
    {
        try
        {
            requestedWindow.Show(); // Quizas convendria hacer un ShowDialog(Window) especificamente para el LoadingWindow pero luego queda abierto en el background, reformar codigo! Quizas usando luego Window.Owner desde el LoadingWindow
            if (requestedWindow.IsActive == false)
            {
                requestedWindow.WindowState = WindowState.Minimized;
                await Task.Delay(100);
                requestedWindow.WindowState = WindowState.Normal;
                requestedWindow.Activate();
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void StartNewInstanceButton_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == "Text")
        {
            if (LoadingWindowChild != null)
            {
                if (StartNewInstanceButton!.Text.StartsWith(AppTranslator.RetryClientUpdatesCheck[CurrentLanguageInt])) // Update fail
                {
                    LoadingWindowChild.MainMenuRequested = true;
                    LoadingWindowChild.Close();
                    return;
                }
                if (StartNewInstanceButton.Text.StartsWith(AppTranslator.LaunchClientVersion[CurrentLanguageInt])) // Launch client
                {
                    LoadingWindowClientLaunchRequested = true;
                    // StartNewInstanceButton_Click(Nothing, Nothing)
                    LaunchClientFromLoadingWindowWithDelay(3);
                    return;
                }
                if (StartNewInstanceButton.Text.StartsWith(AppTranslator.UnknownClientVersion[CurrentLanguageInt]) && LoadingWindowClientLaunchRequested == true) // Client launched
                {
                    Process.GetCurrentProcess().Kill();
                    return;
                }
                if (StartNewInstanceButton.Text.StartsWith(AppTranslator.UpdateClientVersion[CurrentLanguageInt])) // Update client
                {
                    StartNewInstanceButton_Click(null, EventArgs.Empty);
                    return;
                }
                if (StartNewInstanceButton.Text.StartsWith(AppTranslator.ClientUpdatesCheck[CurrentLanguageInt]) || StartNewInstanceButton.Text.StartsWith(AppTranslator.DownloadingClient[CurrentLanguageInt]) || StartNewInstanceButton.Text.StartsWith(AppTranslator.ExtractingClient[CurrentLanguageInt])) // Client update check or downloading or extracting
                {
                    LoadingWindowChild.StatusLabel!.Text = e.NewValue?.ToString();
                }
                else
                {
                    LoadingWindowChild.StatusLabel!.Text = AppTranslator.GenericLoading[CurrentLanguageInt] + " ..."; // Generic loading
                }
            }
        }
    }

    private async void LaunchClientFromLoadingWindowWithDelay(int delaySeconds)
    {
        while (!(delaySeconds == 0 || LoadingWindowChild == null))
        {
            LoadingWindowChild!.StatusLabel!.Text = AppTranslator.GenericLoading[CurrentLanguageInt] + " " + GetCurrentUpdateSourceName() + " (" + delaySeconds + "s)"; // Generic loading
            await Task.Delay(1000);
            delaySeconds -= 1;
        }
        if (LoadingWindowChild != null)
        {
            LoadingWindowChild.StatusLabel!.Text = AppTranslator.GenericLoading[CurrentLanguageInt] + " " + GetCurrentUpdateSourceName();
            StartNewInstanceButton_Click(null, EventArgs.Empty);
        }
    }

    private void LoadingWindowChild_Closed(object? sender, EventArgs e)
    {
        // If LoadingWindowCloseRequested = True Then
        //     LoadingWindowChild = Nothing
        //     EnsureWindowFocus(Me)
        // Else
        //     Window.Close()
        // End If
    }

    private void MainWindow_Activated(object? sender, EventArgs e)
    {
        if (LoadingWindowChild != null)
        {
            _ = EnsureWindowFocus(LoadingWindowChild);
        }
    }

    public void AddOrUpdateInstallation(string version, string path, string client, long lastModified)
    {
        string jsonPath = Path.Combine(GetAppDataPath(), "Habbo Launcher", "versions.json");
        JsonObject root;

        try
        {
            if (File.Exists(jsonPath))
            {
                var txt = File.ReadAllText(jsonPath);
                if (!string.IsNullOrWhiteSpace(txt))
                {
                    root = JsonNode.Parse(txt)!.AsObject();
                }
                else
                {
                    root = new JsonObject();
                }
            }
            else
            {
                root = new JsonObject();
            }
        }
        catch
        {
            root = new JsonObject();
        }

        if (root["installations"] == null)
        {
            root["installations"] = new JsonArray();
        }

        JsonArray installations = root["installations"]!.AsArray();

        JsonObject? existing = null;

        foreach (JsonObject? item in installations)
        {
            if (item!["version"]?.ToString() == version && item["client"]?.ToString() == client)
            {
                existing = item;
                break;
            }
        }

        if (existing != null)
        {
            existing["path"] = path;
            existing["lastModified"] = lastModified;
        }
        else
        {
            installations.Add(new JsonObject
            {
                { "version", version },
                { "path", path },
                { "client", client },
                { "lastModified", lastModified }
            });
        }

        string? dir = Path.GetDirectoryName(jsonPath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(jsonPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    public void RemoveInstallation(string version, string client)
    {
        string jsonPath = Path.Combine(GetAppDataPath(), "Habbo Launcher", "versions.json");
        if (!File.Exists(jsonPath)) return;

        JsonObject root;

        try
        {
            var txt = File.ReadAllText(jsonPath);
            if (string.IsNullOrWhiteSpace(txt)) return;
            root = JsonNode.Parse(txt)!.AsObject();
        }
        catch
        {
            return;
        }

        JsonArray? installations = root["installations"]?.AsArray();
        if (installations == null) return;

        for (int i = installations.Count - 1; i >= 0; i--)
        {
            JsonObject item = installations[i]!.AsObject();
            if (item["version"]?.ToString() == version && item["client"]?.ToString() == client)
            {
                installations.RemoveAt(i);
            }
        }

        File.WriteAllText(jsonPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    public bool InstallationExists(string version, string client)
    {
        string jsonPath = Path.Combine(GetAppDataPath(), "Habbo Launcher", "versions.json");
        if (!File.Exists(jsonPath)) return false;

        try
        {
            var txt = File.ReadAllText(jsonPath);
            if (string.IsNullOrWhiteSpace(txt)) return false;

            var root = JsonNode.Parse(txt)!.AsObject();
            JsonArray? installations = root["installations"]?.AsArray();

            if (installations == null) return false;

            foreach (JsonObject? item in installations)
            {
                if (item!["version"]?.ToString() == version && item["client"]?.ToString() == client)
                {
                    return true;
                }
            }
        }
        catch
        {
        }

        return false;
    }

    /// <summary>
    /// Equivalente al IsNumeric de VB (usado en CleanDeprecatedClients): true si la cadena se
    /// puede interpretar como número.
    /// </summary>
    private static bool IsNumeric(string? value) =>
        double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out _);
}
