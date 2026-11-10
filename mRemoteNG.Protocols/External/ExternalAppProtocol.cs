using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Media;
using Microsoft.Extensions.Logging;
using mRemoteNG.Core.Tools;
using mRemoteNG.Protocols.Abstractions;

namespace mRemoteNG.Protocols.External;

/// <summary>
/// Generic external application protocol handler.
/// Launches any configured external tool (AnyDesk, ARD, RealVNC Viewer,
/// custom scripts, etc.) with token-substituted command-line arguments.
///
/// Supported tokens in the command template:
///   {hostname}   — resolved hostname
///   {port}       — port number
///   {username}   — username
///   {password}   — password (use with caution)
///   {domain}     — Windows domain
///
/// The template is split into arguments (<see cref="CommandLineTokenizer"/>) before the tokens are
/// substituted, so a connection value always stays inside the argument it appears in and is never
/// re-split or parsed as an option.
///
/// Example configurations in <see cref="ConnectionParameters.Extras"/>:
///   "external.command"   = "anydesk {hostname}"
///   "external.command"   = "open rdp://{username}:{password}@{hostname}"
///   "external.command"   = "mstsc.exe /v:{hostname}:{port}"
/// </summary>
public sealed class ExternalAppProtocol : ProtocolBase, IVisualProtocol
{
    private readonly ILogger<ExternalAppProtocol> _logger;
    private Process? _process;

    public ExternalAppProtocol(ILogger<ExternalAppProtocol> logger) => _logger = logger;

    public Control CreateView() => new ExternalAppView();

    public override async Task ConnectAsync(ConnectionParameters parameters, CancellationToken ct = default)
    {
        State = ConnectionState.Connecting;

        if (!parameters.Extras.TryGetValue("external.command", out string? commandTemplate)
            || string.IsNullOrWhiteSpace(commandTemplate))
        {
            State = ConnectionState.Error;
            RaiseStatus("No external command configured. Set 'external.command' in connection extras.");
            return;
        }

        ProcessStartInfo startInfo;
        try
        {
            startInfo = BuildStartInfo(commandTemplate, parameters);
        }
        catch (ArgumentException ex)
        {
            State = ConnectionState.Error;
            RaiseStatus($"Launch failed: {ex.Message}");
            _logger.LogWarning("External app not launched: {Reason}", ex.Message);
            return;
        }

        string exe = startInfo.FileName;
        string command = string.Join(' ', startInfo.ArgumentList.Prepend(exe));
        _logger.LogInformation("External app: {Command}", command);
        RaiseStatus($"Launching: {command}");

        try
        {
            _process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };

            _process.Exited += (_, _) =>
            {
                State = ConnectionState.Disconnected;
                RaiseStatus($"External app exited (code {_process.ExitCode}).");
            };

            _process.Start();
            State = ConnectionState.Connected;
            RaiseStatus($"External app running: {exe}");
        }
        catch (Exception ex)
        {
            State = ConnectionState.Error;
            RaiseStatus($"Launch failed: {ex.Message}");
            _logger.LogError(ex, "External app launch failed");
        }

        await Task.CompletedTask;
    }

    public override async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_process is { HasExited: false })
        {
            try { _process.Kill(); }
            catch { /* already exited */ }
        }
        State = ConnectionState.Disconnected;
        await Task.CompletedTask;
    }

    /// <summary>
    /// Builds the process to launch for <paramref name="template"/>: the first argument is the program, each
    /// further argument is passed separately through <see cref="ProcessStartInfo.ArgumentList"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The template is empty, the hostname is unsafe, or a substituted value would turn an argument into an option.
    /// </exception>
    internal static ProcessStartInfo BuildStartInfo(string template, ConnectionParameters parameters)
    {
        if (template.Contains("{hostname}", StringComparison.OrdinalIgnoreCase))
            ValidateHostname(parameters.Hostname);

        var arguments = new List<string>();
        foreach (string token in CommandLineTokenizer.Split(template))
        {
            string value = SubstituteTokens(token, parameters);
            if (value.Length == 0)
                continue;
            if (value.StartsWith('-') && !token.StartsWith('-'))
                throw new ArgumentException($"A connection value would turn the argument '{token}' into an option.");
            arguments.Add(value);
        }

        if (arguments.Count == 0)
            throw new ArgumentException("The external command is empty.");

        var startInfo = new ProcessStartInfo(arguments[0])
        {
            UseShellExecute = true, // Allows macOS `open`, Windows `start`, etc.
        };
        foreach (string argument in arguments.Skip(1))
            startInfo.ArgumentList.Add(argument);
        return startInfo;
    }

    private static void ValidateHostname(string hostname)
    {
        if (string.IsNullOrEmpty(hostname))
            throw new ArgumentException("The hostname is empty.");
        if (hostname.StartsWith('-'))
            throw new ArgumentException("The hostname must not start with '-'.");
        if (hostname.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
            throw new ArgumentException("The hostname must not contain whitespace or control characters.");
    }

    private static string SubstituteTokens(string template, ConnectionParameters p)
    {
        return template
            .Replace("{hostname}", p.Hostname, StringComparison.OrdinalIgnoreCase)
            .Replace("{port}", p.Port.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{username}", p.Username ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{password}", p.Password ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{domain}", p.Domain ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _process?.Dispose();
    }
}

/// <summary>
/// Placeholder view shown while the external app runs.
/// </summary>
internal sealed class ExternalAppView : UserControl
{
    public ExternalAppView()
    {
        Background = new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x1e));
        Content = new TextBlock
        {
            Text = "External application is running.\n" +
                   "Return to mRemoteNG to manage other sessions.",
            Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FontSize = 14,
        };
    }
}
