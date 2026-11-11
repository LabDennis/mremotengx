using FluentAssertions;
using mRemoteNG.Core.Config.Import;
using Xunit;

namespace mRemoteNG.Tests.CrossPlatform.Core.Import;

public sealed class LegacyConnectionFilesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "legacy-files-" + Guid.NewGuid().ToString("N"));
    private readonly string _roaming;
    private readonly string _local;

    public LegacyConnectionFilesTests()
    {
        _roaming = Directory.CreateDirectory(Path.Combine(_root, "Roaming", "mRemoteNG")).FullName;
        _local = Directory.CreateDirectory(Path.Combine(_root, "Local", "mRemoteNG")).FullName;
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Find_ListsTheConfiguredFileFirst_ThenOtherConnectionFilesNewestFirst()
    {
        var custom = ConnectionFile(Path.Combine(_root, "Desktop", "work.xml"), DateTime.UtcNow.AddDays(-30));
        var old = ConnectionFile(Path.Combine(_roaming, "WPW2025.xml"), DateTime.UtcNow.AddDays(-20));
        var recent = ConnectionFile(Path.Combine(_roaming, "confCons.xml"), DateTime.UtcNow.AddDays(-1));
        File.WriteAllText(Path.Combine(_roaming, "extApps.xml"), "<Apps />");
        File.WriteAllText(Path.Combine(_roaming, "broken.xml"), "<Connections");
        UserConfig("1.78.2.1", DateTime.UtcNow, ("ConnectionFilePath", custom));

        LegacyConnectionFiles.Find(_roaming, _local).Should().Equal(custom, recent, old);
    }

    [Theory]
    [InlineData("CustomConsPath")] // 1.76
    [InlineData("BackupLocation")] // 1.77
    public void ConfiguredFile_ReadsTheCustomLocationOfOlderVersions(string setting)
    {
        UserConfig("1.77.3.1784", DateTime.UtcNow, ("LoadConsFromCustomLocation", "True"), (setting, @"D:\cons.xml"));

        LegacyConnectionFiles.ConfiguredFile(_local).Should().Be(@"D:\cons.xml");
    }

    [Fact]
    public void ConfiguredFile_FollowsTheNewestConfig()
    {
        UserConfig("1.77.3.1784", DateTime.UtcNow.AddDays(-100), ("LoadConsFromCustomLocation", "True"), ("BackupLocation", @"D:\old.xml"));
        UserConfig("1.76.20.24615", DateTime.UtcNow, ("LoadConsFromCustomLocation", "False"));

        LegacyConnectionFiles.ConfiguredFile(_local).Should().BeNull("the newest config uses the default file");
    }

    [Fact]
    public void Find_ReturnsNothing_WhenTheClassicAppWasNeverInstalled() =>
        LegacyConnectionFiles.Find(Path.Combine(_root, "none"), Path.Combine(_root, "none2")).Should().BeEmpty();

    private static string ConnectionFile(string path, DateTime lastWrite)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, """<?xml version="1.0" encoding="utf-8"?><Connections Name="Connections" ConfVersion="2.8" />""");
        File.SetLastWriteTimeUtc(path, lastWrite);
        return Path.GetFullPath(path);
    }

    private void UserConfig(string version, DateTime lastWrite, params (string Name, string Value)[] settings)
    {
        var dir = Directory.CreateDirectory(Path.Combine(_local, "mRemoteNG.exe_Url_abc", version)).FullName;
        var path = Path.Combine(dir, "user.config");
        var values = string.Concat(settings.Select(s =>
            $"""<setting name="{s.Name}" serializeAs="String"><value>{s.Value}</value></setting>"""));
        File.WriteAllText(path, $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration><userSettings><mRemoteNG.Settings>{values}</mRemoteNG.Settings></userSettings></configuration>
            """);
        File.SetLastWriteTimeUtc(path, lastWrite);
    }
}
