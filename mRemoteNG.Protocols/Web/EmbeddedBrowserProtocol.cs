using Avalonia;
using Avalonia.Controls;
using mRemoteNG.Protocols.Abstractions;
using mRemoteNG.Protocols.Rdp;
using Microsoft.Extensions.Logging;

namespace mRemoteNG.Protocols.Web;

/// <summary>A browser drawn into a native child window; implemented by the Windows platform assembly.</summary>
internal interface IEmbeddedBrowser : IDisposable
{
    /// <summary>Fits the browser to its parent window after the session tab changed size.</summary>
    void Resize();
}

/// <summary>
/// HTTP/HTTPS shown inside the session tab with Microsoft Edge WebView2, as in the WinForms app (Windows only).
/// When WebView2 cannot be created (no runtime installed, no child window), the page opens in the default
/// browser instead, like <see cref="ExternalBrowserProtocol"/> does on the other platforms.
/// </summary>
public sealed class EmbeddedBrowserProtocol : ProtocolBase, IVisualProtocol
{
    private readonly ILogger<EmbeddedBrowserProtocol> _logger;
    // ponytail: reuses the RDP host, it already keeps one HWND alive across tab detach/attach
    private readonly RdpNativeHost _host = new();
    private IEmbeddedBrowser? _browser;

    public EmbeddedBrowserProtocol(ILogger<EmbeddedBrowserProtocol> logger)
    {
        _logger = logger;
        _host.PropertyChanged += (_, e) =>
        {
            if (e.Property == Visual.BoundsProperty)
                _browser?.Resize();
        };
    }

    public Control CreateView() => _host;

    public override async Task ConnectAsync(ConnectionParameters parameters, CancellationToken ct = default)
    {
        var url = WebUrlBuilder.Build(parameters);
        State = ConnectionState.Connecting;
        RaiseStatus($"Opening {url}…");

        var handle = await _host.HandleReady.WaitAsync(ct);
        _browser = _host.EmbeddingFailed ? null : await CreateBrowserAsync(handle.Handle, url);
        if (_browser is null)
        {
            ExternalBrowserProtocol.LaunchWithShell(url);
            RaiseStatus($"WebView2 is not available; opened {url} in the default browser.");
        }
        State = ConnectionState.Connected;
    }

    public override Task DisconnectAsync(CancellationToken ct = default)
    {
        _browser?.Dispose();
        _browser = null;
        State = ConnectionState.Disconnected;
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _browser?.Dispose();
            _browser = null;
            _host.Release();
        }
    }

    /// <summary>Null when the Windows platform assembly or the WebView2 runtime is missing.</summary>
    private async Task<IEmbeddedBrowser?> CreateBrowserAsync(nint parent, string url)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "mRemoteNG.Platform.Windows")
            ?.GetType("mRemoteNG.Platform.Windows.Web.WindowsWebView2Host");
        if (type?.GetMethod("CreateAsync") is not { } create)
            return null;
        try
        {
            return await (Task<IEmbeddedBrowser?>)create.Invoke(null, [parent, url])!;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WebView2 could not be created for {Url}", url);
            return null;
        }
    }
}
