using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace HabboCustomLauncher;

public class Singleton
{
    private static readonly Singleton CurrentInstance = new Singleton();
    public MainWindow? MainWindow;

    // === DPI DETECTION REFERENCES ===
    [DllImport("user32")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    private const int LOGPIXELSX = 88;
    public double CustomWindowScale = 0; // 0=Disabled

    public double GetWindowsDpiScale()
    {
        IntPtr hdc = GetDC(IntPtr.Zero);
        int dpiX = GetDeviceCaps(hdc, LOGPIXELSX);
        ReleaseDC(IntPtr.Zero, hdc);
        return dpiX / 96.0;
    }

    public void ScaleMainGrid(Window requestedWindow)
    {
        var osVersion = Environment.OSVersion.Version;
        // Windows 7/8/8.1 or CustomScale
        if ((RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && osVersion.Major == 6) || CustomWindowScale > 0)
        {
            double escala = CustomWindowScale;
            if (escala == 0)
            {
                escala = GetWindowsDpiScale();
            }
            if (CustomWindowScale > 0 || (requestedWindow.RenderScaling == escala) == false)
            {
                var g = requestedWindow.Content as Grid;
                g!.Margin = new Thickness(requestedWindow.Width * (escala - 1.0) / 2, requestedWindow.Height * (escala - 1.0) / 2);
                if (g != null)
                {
                    var transform = new ScaleTransform(escala, escala);
                    g.RenderTransform = transform;
                    g.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                }
                requestedWindow.Width *= escala;
                requestedWindow.Height *= escala;
                var screen = requestedWindow.Screens.ScreenFromVisual(requestedWindow);
                var wa = screen!.WorkingArea;
                requestedWindow.Position = new PixelPoint((int)(wa.X + (wa.Width - requestedWindow.Bounds.Width) / 2), (int)(wa.Y + (wa.Height - requestedWindow.Bounds.Height) / 2));
                // MsgBox("Hdpi Debug", "Escala aplicada: " & escala.ToString("0.00"))
            }
        }
    }
    // ================================

    // Constructor privado para evitar que se cree desde afuera
    private Singleton()
    {
    }

    public static Singleton GetCurrentInstance() => CurrentInstance;
}
