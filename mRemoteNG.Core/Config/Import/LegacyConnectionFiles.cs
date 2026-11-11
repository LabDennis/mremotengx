using System.Xml;
using System.Xml.Linq;

namespace mRemoteNG.Core.Config.Import;

/// <summary>
/// Finds the connection files of the classic WinForms mRemoteNG on this machine, for the migration assistant:
/// the file its settings (user.config) open, its default confCons.xml and the other connection files in its data
/// folders. The files are only read.
/// </summary>
public static class LegacyConnectionFiles
{
    /// <param name="roamingDir">%APPDATA%\mRemoteNG: the default confCons.xml and other connection files.</param>
    /// <param name="localDir">%LOCALAPPDATA%\mRemoteNG: one user.config per installed version (and older confCons.xml).</param>
    /// <returns>Existing connection files: the one the classic app opens first, then the others newest first.</returns>
    public static IReadOnlyList<string> Find(string roamingDir, string localDir)
    {
        var candidates = new List<string>();
        if (ConfiguredFile(localDir) is { } configured)
            candidates.Add(Environment.ExpandEnvironmentVariables(configured));
        candidates.Add(Path.Combine(localDir, "confCons.xml"));
        candidates.Add(Path.Combine(roamingDir, "confCons.xml"));
        candidates.AddRange(new[] { roamingDir, localDir }
            .Where(Directory.Exists)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*.xml"))
            .OrderByDescending(File.GetLastWriteTimeUtc));

        return candidates
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(IsConnectionFile)
            .ToList();
    }

    /// <summary>
    /// The connection file named by the newest user.config, or null when it uses the default file. The setting moved
    /// between versions: 1.76 <c>LoadConsFromCustomLocation</c> + <c>CustomConsPath</c>, 1.77
    /// <c>LoadConsFromCustomLocation</c> + <c>BackupLocation</c>, 1.78 <c>ConnectionFilePath</c>.
    /// </summary>
    public static string? ConfiguredFile(string localDir)
    {
        if (!Directory.Exists(localDir))
            return null;

        var configs = Directory.EnumerateFiles(localDir, "user.config", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc);
        foreach (var config in configs)
        {
            Dictionary<string, string> settings;
            try
            {
                settings = XDocument.Load(config).Descendants("setting")
                    .GroupBy(s => (string?)s.Attribute("name") ?? string.Empty)
                    .ToDictionary(g => g.Key, g => g.First().Element("value")?.Value ?? string.Empty);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
            {
                continue; // an unreadable config: try the next older one
            }

            string Get(string name) => settings.TryGetValue(name, out var value) ? value.Trim() : string.Empty;
            if (Get("ConnectionFilePath") is { Length: > 0 } path)
                return path;
            if (string.Equals(Get("LoadConsFromCustomLocation"), "True", StringComparison.OrdinalIgnoreCase))
            {
                var custom = Get("CustomConsPath") is { Length: > 0 } c ? c : Get("BackupLocation");
                if (custom.Length > 0)
                    return custom;
            }
            return null; // the newest version uses the default file
        }
        return null;
    }

    /// <summary>True for an existing file whose root element is mRemoteNG's &lt;Connections&gt;.</summary>
    public static bool IsConnectionFile(string path)
    {
        try
        {
            if (!File.Exists(path))
                return false;
            using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            return reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == "Connections";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
        {
            return false;
        }
    }
}
