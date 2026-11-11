using Avalonia.Controls;

namespace mRemoteNG.Avalonia.Views.Dialogs;

/// <summary>What the migration assistant does with the chosen file: import its connections, or open it.</summary>
public sealed record LegacyImportChoice(string Path, bool Open);

/// <summary>A connection file of the classic mRemoteNG, as listed by the migration assistant.</summary>
public sealed record LegacyFileItem(string Path)
{
    public string Name => System.IO.Path.GetFileName(Path);
    public string Details => $"{System.IO.Path.GetDirectoryName(Path)} · {File.GetLastWriteTime(Path):g}";
}

/// <summary>
/// Migration assistant: lists the classic mRemoteNG's connection files (first: the one it opens).
/// <see cref="Window.ShowDialog{TResult}(Window)"/> returns a <see cref="LegacyImportChoice"/>, or null for "Not now".
/// </summary>
public partial class LegacyImportDialog : Window
{
    public LegacyImportDialog() : this([])
    {
    }

    public LegacyImportDialog(IReadOnlyList<string> files)
    {
        InitializeComponent();
        FileList.ItemsSource = files.Select(f => new LegacyFileItem(f)).ToList();
        FileList.SelectedIndex = files.Count > 0 ? 0 : -1;
        NotNowButton.Click += (_, _) => Close(null);
        OpenButton.Click += (_, _) => CloseWith(open: true);
        ImportButton.Click += (_, _) => CloseWith(open: false);
        FileList.DoubleTapped += (_, _) => CloseWith(open: false);
    }

    private void CloseWith(bool open)
    {
        if (FileList.SelectedItem is LegacyFileItem item)
            Close(new LegacyImportChoice(item.Path, open));
    }
}
