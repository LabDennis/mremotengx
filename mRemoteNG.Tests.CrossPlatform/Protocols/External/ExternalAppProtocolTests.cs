using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using mRemoteNG.Protocols.Abstractions;
using mRemoteNG.Protocols.External;
using Xunit;

namespace mRemoteNG.Tests.CrossPlatform.Protocols.External;

public sealed class ExternalAppProtocolTests
{
    private static ConnectionParameters Params(string hostname, string? username = null, string? password = null) => new()
    {
        Hostname = hostname,
        Port = 7070,
        Protocol = ProtocolType.ExternalApp,
        Username = username,
        Password = password,
    };

    [Fact]
    public void BuildStartInfo_AnyDesk_PassesTheIdAsOneArgument()
    {
        var psi = ExternalAppProtocol.BuildStartInfo(ConnectionParametersFactory.AnyDeskCommand, Params("123456789"));

        psi.FileName.Should().Be("anydesk");
        psi.Arguments.Should().BeEmpty();
        psi.ArgumentList.Should().Equal("123456789");
    }

    [Theory]
    [InlineData("1 --x")]
    [InlineData("123456789\t--plain")]
    [InlineData("--with-password")]
    [InlineData("-x")]
    [InlineData("1\n2")]
    [InlineData("")]
    public void BuildStartInfo_RejectsHostnamesThatWouldAddOptions(string hostname)
    {
        var act = () => ExternalAppProtocol.BuildStartInfo(ConnectionParametersFactory.AnyDeskCommand, Params(hostname));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BuildStartInfo_KeepsValuesWithSpacesInsideTheirArgument()
    {
        var psi = ExternalAppProtocol.BuildStartInfo(
            "\"/opt/My Tool/tool\" --user {username} --pw {password} rdp://{hostname}:{port}",
            Params("srv01", "bob smith", "p w\"d --evil"));

        psi.FileName.Should().Be("/opt/My Tool/tool");
        psi.ArgumentList.Should().Equal("--user", "bob smith", "--pw", "p w\"d --evil", "rdp://srv01:7070");
    }

    [Fact]
    public void BuildStartInfo_RejectsValuesThatWouldBecomeAnOption()
    {
        var act = () => ExternalAppProtocol.BuildStartInfo("tool {username} {hostname}", Params("srv01", "--evil"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BuildStartInfo_SkipsEmptyValues()
    {
        var psi = ExternalAppProtocol.BuildStartInfo("tool {username} {hostname}", Params("srv01"));

        psi.ArgumentList.Should().Equal("srv01");
    }

    [Fact]
    public async Task ConnectAsync_InjectedHostname_FailsWithoutLaunching()
    {
        using var protocol = new ExternalAppProtocol(NullLogger<ExternalAppProtocol>.Instance);
        var parameters = Params("123456789 --some-option value") with
        {
            Extras = new Dictionary<string, string> { [ConnectionParametersFactory.Keys.ExternalCommand] = "anydesk {hostname}" },
        };

        await protocol.ConnectAsync(parameters);

        protocol.State.Should().Be(ConnectionState.Error);
    }

    [Fact]
    public async Task ConnectAsync_LaunchesWithSeparateArguments()
    {
        if (OperatingSystem.IsWindows())
            return;

        using var protocol = new ExternalAppProtocol(NullLogger<ExternalAppProtocol>.Instance);
        var parameters = Params("srv01") with
        {
            Extras = new Dictionary<string, string> { [ConnectionParametersFactory.Keys.ExternalCommand] = "true {hostname}" },
        };

        await protocol.ConnectAsync(parameters);

        protocol.State.Should().NotBe(ConnectionState.Error);
        await protocol.DisconnectAsync();
    }
}
