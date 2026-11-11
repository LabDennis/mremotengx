using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using mRemoteNG.Protocols.Web;

namespace mRemoteNG.Platform.Windows.Web;

/// <summary>
/// Microsoft Edge WebView2, the browser the WinForms app embeds, drawn into the session tab's native child
/// window. Created by <see cref="EmbeddedBrowserProtocol"/> through reflection.
/// </summary>
internal sealed class WindowsWebView2Host : IEmbeddedBrowser
{
    private readonly nint _parent;
    private readonly CoreWebView2Controller _controller;

    private WindowsWebView2Host(nint parent, CoreWebView2Controller controller)
    {
        _parent = parent;
        _controller = controller;
    }

    /// <summary>Throws when no WebView2 runtime is installed.</summary>
    public static async Task<IEmbeddedBrowser?> CreateAsync(nint parent, string url)
    {
        // The default profile folder sits next to the executable, which is read-only under Program Files.
        var profile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "mRemoteNG", "WebView2");
        var environment = await CoreWebView2Environment.CreateAsync(null, profile);
        var controller = await environment.CreateCoreWebView2ControllerAsync(parent);
        var host = new WindowsWebView2Host(parent, controller);
        host.Resize();
        controller.CoreWebView2.Navigate(url);
        return host;
    }

    public void Resize()
    {
        if (GetClientRect(_parent, out var r))
            _controller.Bounds = new Rectangle(0, 0, r.Right - r.Left, r.Bottom - r.Top);
    }

    public void Dispose() => _controller.Close();

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(nint hWnd, out Rect rect);
}
