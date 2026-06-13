using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace HabboCustomLauncher;

public partial class CustomButton : UserControl
{
    private Button? InnerButton;

    public CustomButton()
    {
        InitializeComponent();
    }

    // Auto-wiring does not work for VB, so do it manually (se conserva ese patrón en C#)
    // Wires up the controls and optionally loads XAML markup and
    // attaches dev tools (if Avalonia.Diagnostics package is referenced)
    private void InitializeComponent(bool loadXaml = true)
    {
        if (loadXaml)
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Control = FindNameScope().Find("Control_Name")
        InnerButton = this.FindNameScope()?.Find<Button>("InnerButton");
        if (InnerButton != null)
        {
            InnerButton.Click += InnerButton_Click; // Handles InnerButton.Click
        }
        UpdateButtonCorner();
    }

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<CustomButton, string>(nameof(Text));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<Color> BackColorProperty =
        AvaloniaProperty.Register<CustomButton, Color>(nameof(BackColor));

    public Color BackColor
    {
        get => GetValue(BackColorProperty);
        set => SetValue(BackColorProperty, value);
    }

    public static readonly StyledProperty<bool> IsButtonDisabledProperty =
        AvaloniaProperty.Register<CustomButton, bool>(nameof(IsButtonDisabled));

    public bool IsButtonDisabled
    {
        get => GetValue(IsButtonDisabledProperty);
        set => SetValue(IsButtonDisabledProperty, value);
    }

    public static readonly StyledProperty<bool> IsFocusableProperty =
        AvaloniaProperty.Register<CustomButton, bool>(nameof(IsFocusable));

    public bool IsFocusable
    {
        get => GetValue(IsFocusableProperty);
        set
        {
            SetValue(IsFocusableProperty, value);
            if (value == false)
            {
                InnerButton!.Focusable = false;
            }
        }
    }

    public static readonly StyledProperty<bool> IsButtonCorneredProperty =
        AvaloniaProperty.Register<CustomButton, bool>(nameof(IsButtonCornered));

    public bool IsButtonCornered
    {
        get => GetValue(IsButtonCorneredProperty);
        set
        {
            SetValue(IsButtonCorneredProperty, value);
            UpdateButtonCorner();
        }
    }

    public void UpdateButtonCorner()
    {
        try
        {
            if (InnerButton != null)
            {
                if (GetValue(IsButtonCorneredProperty))
                {
                    InnerButton.CornerRadius = new CornerRadius(3);
                }
                else
                {
                    InnerButton.CornerRadius = new CornerRadius(0);
                }
            }
        }
        catch
        {
            // Error while changing corner radius
        }
    }

    public event EventHandler? Click;

    private void InnerButton_Click(object? sender, RoutedEventArgs e)
    {
        if (IsButtonDisabled == false)
        {
            Click?.Invoke(null, EventArgs.Empty);
        }
    }
}
