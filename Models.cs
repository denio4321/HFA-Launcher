using System.Text.Json;

namespace HabboCustomLauncher;

/// <summary>
/// Modelos y tablas de traducción extraídos de los code-behind del proyecto VB
/// (estaban embebidos al final de MainWindow.axaml.vb y LoadingWindow.axaml.vb).
/// </summary>
public class JsonClientUrls
{
    public readonly string FlashWindowsVersion;
    public readonly string FlashWindowsUrl;

    public JsonClientUrls(string jsonString)
    {
        JsonElement jsonRoot = JsonDocument.Parse(jsonString).RootElement;
        FlashWindowsVersion = jsonRoot.GetProperty("flash-windows-version").GetString()!;
        FlashWindowsUrl = jsonRoot.GetProperty("flash-windows").GetString()!;
    }
}

public class LoginCode
{
    public readonly string SSOTicket = "";
    public readonly string ServerId = "";
    public readonly string ServerUrl = "";
    public readonly string Username = "";

    public LoginCode(string loginCode)
    {
        // Example: habbo://hab?server=hhes&token=11111111-1111-1111-1111-111111111111-11111111.V4.LilithRainbows
        if (loginCode.StartsWith("habbo://") && loginCode.Contains("server="))
        {
            loginCode = loginCode.Remove(0, loginCode.IndexOf("?server=") + 8);
            loginCode = loginCode.Replace("&token=", ".");
        }
        if (CheckLoginCode(loginCode))
        {
            string loginServerId = loginCode.Split(".")[0]; // Example: hhes
            string loginTicket = loginCode.Split(".")[1] + "." + loginCode.Split(".")[2]; // Example: 11111111-1111-1111-1111-111111111111-11111111.V4
            if (GetCharCount(loginCode, '.') > 2)
            {
                Username = loginCode.Split(".")[3]; // Example: LilithRainbows
            }
            SSOTicket = loginTicket;
            ServerId = loginServerId;
            ServerUrl = GetHabboServerUrl(ServerId);
        }
    }

    private bool CheckLoginCode(string loginCode)
    {
        if (GetCharCount(loginCode, '.') >= 2)
        {
            foreach (var habboServer in GetHabboServers())
            {
                if (loginCode.StartsWith(habboServer.Id + "."))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private string GetHabboServerUrl(string serverId)
    {
        foreach (var habboServer in GetHabboServers())
        {
            if (habboServer.Id == serverId)
            {
                return habboServer.Url;
            }
        }
        return "";
    }

    private List<HabboServer> GetHabboServers()
    {
        return new List<HabboServer>
        {
            new HabboServer("hhus", "www.habbo.com"),
            new HabboServer("hhfr", "www.habbo.fr"),
            new HabboServer("hhes", "www.habbo.es"),
            new HabboServer("hhbr", "www.habbo.com.br"),
            new HabboServer("hhfi", "www.habbo.fi"),
            new HabboServer("hhtr", "www.habbo.com.tr"),
            new HabboServer("hhde", "www.habbo.de"),
            new HabboServer("hhnl", "www.habbo.nl"),
            new HabboServer("hhit", "www.habbo.it"),
            new HabboServer("local", "localhost:s3dcom:3000"),
            new HabboServer("hhs1", "s1.varoke.net"),
            new HabboServer("hhs2", "sandbox.habbo.com"),
            new HabboServer("duke", "duke.varoke.net"),
            new HabboServer("d63", "d63.varoke.net"),
            new HabboServer("dev", "dev.varoke.net"),
            new HabboServer("hhxd", "habbox.varoke.net"),
            new HabboServer("hhxp", "www.habbox.game")
        };
    }

    private int GetCharCount(string input, char requestedChar)
    {
        return input.Count(x => x == requestedChar);
    }
}

public class HabboServer
{
    public readonly string Id = "";
    public readonly string Url = "";

    public HabboServer(string serverId, string serverUrl)
    {
        Id = serverId;
        Url = serverUrl;
    }
}

public class AppTranslator
{
    // 0=English 1=Spanish
    public static string[] CreditsLink = { "♥ Credits", "♥ Créditos" };
    public static string[] CreditsTitle = { "Acknowledgements", "Agradecimientos" };
    public static string[] CreditsBody =
    {
        "This launcher is a project based on the work of LilithRainbows. Heartfelt thanks for everything you've given to the Habbo community. ♥",
        "Este launcher es un proyecto basado en el trabajo de LilithRainbows. Mil gracias por todo lo que has aportado a la comunidad de Habbo. ♥"
    };
    public static string[] CreditsRepoLabel = { "LilithRainbows' launcher (GitHub repo)", "Launcher de LilithRainbows (repo en GitHub)" };
    public static string[] CreditsClose = { "Close", "Cerrar" };
    public static string[] GenericLoading = { "Loading", "Cargando" };
    public static string[] DownloadingClient = { "Downloading client", "Descargando cliente" };
    public static string[] ExtractingClient = { "Extracting client", "Extrayendo cliente" };
    public static string[] PlayingAs = { "Playing as", "Jugando como" };
    public static string[] ClipboardLoginCodeDetected = { "Clipboard login code detected", "Codigo de inicio de sesion del portapapeles detectado" };
    public static string[] ClipboardLoginCodeNotDetected = { "Clipboard login code not detected", "Codigo de inicio de sesion del portapapeles no detectado" };
    public static string[] UnknownClientVersion = { "Unknown client version", "Version del cliente desconocida" };
    public static string[] CurrentUpdateSource = { "Current update source", "Fuente de actualizaciones" };
    public static string[] RetryClientUpdatesCheck = { "Retry to check for client updates", "Reintentar verificar actualizaciones del cliente" };
    public static string[] ClientUpdatesCheck = { "Checking for client updates", "Verificando actualizaciones del cliente" };
    public static string[] UpdateClientVersion = { "Update client to version", "Actualizar cliente a la version" };
    public static string[] LaunchClientVersion = { "Launch client version", "Ejecutar cliente version" };
    public static string[] Enabled = { "Enabled", "Habilitado" };
    public static string[] Disabled = { "Disabled", "Deshabilitado" };
    public static string[] AddDesktopShortcut = { "Add shortcut to desktop", "Añadir acceso directo al escritorio" };
    public static string[] AddStartMenuShortcut = { "Add shortcut to start menu", "Añadir acceso directo al menu de inicio" };
    public static string[] AutomaticHabboProtocol = { "Automatic habbo protocol", "Habbo protocol automatico" };
    public static string[] ClientLaunchError = { "Client could not be launched!", "No se pudo ejecutar el cliente!" };
    public static string[] ClientUpdateError = { "Client could not be updated!", "No se pudo actualizar el cliente!" };
    public static string[] ClientDeleteError = { "Client version could not be deleted!", "No se pudo eliminar la version del cliente!" };
    public static string[] ClientUpdatesCheckError = { "Client updates could not be checked!", "No se pudo comprobar las actualizaciones del cliente!" };
    public static string[] ErrorDebugClipboardHint = { "Error (CTRL + C to copy technical details)", "Error (CTRL + C para copiar detalles tecnicos)" };
    public static string[] ClassicAirClientHint = { "The official classic Habbo client without modifications.", "El cliente clasico oficial de Habbo sin modificaciones." };
    public static string[] AirPlusClientHint = { "The classic Habbo client with unofficial modifications.", "El cliente clasico de Habbo con modificaciones no oficiales." };
    public static string[] HfaClientHint = { "The official Habbo client patched locally with HFA commands (generated with HfaPlus build).", "El cliente oficial de Habbo parcheado localmente con comandos HFA (generado con HfaPlus build)." };
    public static string[] HfaClientMissing = { "HFA client not generated for version", "Cliente HFA no generado para la version" };
}

public class LauncherUpdaterTranslator
{
    // 0=English 1=Spanish
    public static string[] ReturnToMainMenu = { "Return to main menu", "Volver al menu principal" };
    public static string[] VerifyingUpdate = { "Verifying update ...", "Verificando actualizacion ..." };
    public static string[] DeletingPreviousVersion = { "Deleting previous version ...", "Borrando version anterior ..." };
    public static string[] ApplyingUpdate = { "Applying update ...", "Aplicando actualizacion ..." };
    public static string[] UpdateReady = { "Update ready!", "Actualizacion lista!" };
    public static string[] UpdateError = { "Update error!", "Error de actualizacion!" };
}
