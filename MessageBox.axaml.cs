using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace HabboCustomLauncher;

public partial class MessageBox : Window
{
    private Window? Window;
    private Label? TitleBarLabel;
    public TextBlock? MessageLabel;
    private CustomButton? CloseButton;
    private CustomButton? OkButton;
    public int CurrentLanguageInt = 0;
    public bool CopyMessageToClipboardBusy = false;
    public string ClipboardDebugContent = "";
    // Modo confirmación: true si el usuario pulsa el botón de acción (OK), false si cierra con la X.
    public bool Confirmed = false;

    public MessageBox()
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
        MessageLabel = this.FindNameScope()!.Find<TextBlock>("MessageLabel");
        CloseButton = this.FindNameScope()!.Find<CustomButton>("CloseButton");
        OkButton = this.FindNameScope()!.Find<CustomButton>("OkButton");

        CloseButton!.Click += CloseButton_Click;
        OkButton!.Click += OkButton_Click;
        TitleBarLabel!.PointerPressed += TitleBarLabel_PointerPressed;
        KeyDown += MessageBox_KeyDown;

        Singleton.GetCurrentInstance().ScaleMainGrid(Window!);
    }

    public void ConfigureContent(string title, string message, string clipboardDebugContent = "")
    {
        if (clipboardDebugContent == "")
        {
            ClipboardDebugContent = message;
        }
        else
        {
            ClipboardDebugContent = clipboardDebugContent;
        }
        if (title.StartsWith("    "))
        {
            TitleBarLabel!.Content = title;
        }
        else
        {
            TitleBarLabel!.Content = "    " + title;
        }
        MessageLabel!.Text = message;
        AutoAdjustMessageFontSize();
    }

    public void AutoAdjustMessageFontSize()
    {
        switch (MessageLabel!.Text!.Length)
        {
            case > 90:
                MessageLabel.FontSize = 15;
                break;
            case > 40:
                MessageLabel.FontSize = 20;
                break;
            default:
                MessageLabel.FontSize = 30;
                break;
        }
    }

    private void CloseButton_Click(object? sender, EventArgs e)
    {
        Window!.Close();
    }

    private void OkButton_Click(object? sender, EventArgs e)
    {
        Confirmed = true;
        Window!.Close();
    }

    /// <summary>Convierte el diálogo en una confirmación: cambia el texto del botón de acción.
    /// Pulsar ese botón → Confirmed=true; cerrar con la X → false.</summary>
    public void SetConfirmMode(string okButtonText)
    {
        if (OkButton != null) OkButton.Text = okButtonText;
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

    private void MessageBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.C &&
            (e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
             e.KeyModifiers.HasFlag(KeyModifiers.Meta)))
        {
            if (CopyMessageToClipboardBusy == false)
            {
                CopyMessageToClipboard();
            }
        }
    }

    public async void CopyMessageToClipboard()
    {
        CopyMessageToClipboardBusy = true;
        await Clipboard!.SetTextAsync(ClipboardDebugContent);
        var originalMessageLabelText = MessageLabel!.Text;
        MessageLabel.Text = ClipboardDebugContent;
        AutoAdjustMessageFontSize();
        MessageLabel.Background = Brushes.DarkGreen;
        await Task.Delay(500);
        MessageLabel.Text = originalMessageLabelText;
        AutoAdjustMessageFontSize();
        MessageLabel.Background = null;
        CopyMessageToClipboardBusy = false;
    }
}
