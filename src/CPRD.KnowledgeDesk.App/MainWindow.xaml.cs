using System.Diagnostics;
using System.IO;
using System.Text;
using CPRD.KnowledgeDesk.Core.Services;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using CPRD.KnowledgeDesk.App.Services;
using CPRD.KnowledgeDesk.App.ViewModels;
using CPRD.KnowledgeDesk.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CPRD.KnowledgeDesk.App;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly MainWindowViewModel _viewModel;
    private bool _allowClose;

    public MainWindow(
        MainWindowViewModel viewModel,
        IServiceProvider services,
        KeyboardShortcutService shortcuts)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _services = services;
        DataContext = viewModel;

        shortcuts.Register(
            this,
            FocusSearch,
            ShowQuickCapture,
            viewModel.CreateNewNoteCommand,
            viewModel.Editor.SaveNowCommand,
            viewModel.CloseActiveWorkspaceCommand,
            viewModel.Editor.ToggleFavoriteCommand);

        SourceInitialized += (_, _) => ApplyInitialBounds();
        Closing += OnClosing;
    }

    private void ApplyInitialBounds()
    {
        var area = SystemParameters.WorkArea;
        var bounds = WindowLayoutPolicy.Fit(
            preferredWidth: 1440,
            preferredHeight: 880,
            minimumWidth: MinWidth,
            minimumHeight: MinHeight,
            workLeft: area.Left,
            workTop: area.Top,
            workWidth: area.Width,
            workHeight: area.Height);

        Width = bounds.Width;
        Height = bounds.Height;
        Left = bounds.Left;
        Top = bounds.Top;
    }

    private void FocusSearch()
    {
        GlobalSearchBox.Focus();
        GlobalSearchBox.SelectAll();
    }

    private async void GlobalSearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        await _viewModel.SearchCommand.ExecuteAsync(null);
    }

    private async void RecentSearches_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (sender is not System.Windows.Controls.ComboBox combo ||
            combo.SelectedItem is not string query ||
            string.IsNullOrWhiteSpace(query))
            return;

        _viewModel.GlobalSearchText = query;
        combo.SelectedItem = null;
        await _viewModel.SearchCommand.ExecuteAsync(null);
    }

    private void ShowQuickCapture()
    {
        var window = _services.GetRequiredService<QuickCaptureWindow>();
        window.Owner = this;
        var saved = window.ShowDialog() == true;

        if (saved && _viewModel.SelectedFolderId is Guid folderId)
            _viewModel.SelectFolderCommand.Execute(folderId);
    }

    private void QuickCapture_Click(object sender, RoutedEventArgs e) => ShowQuickCapture();

    private void Find_Click(object sender, RoutedEventArgs e) => FocusSearch();

    private async void CreateFolder_Click(object sender, RoutedEventArgs e)
    {
        var name = PromptForText("Create Folder", "Folder name:", string.Empty);
        if (string.IsNullOrWhiteSpace(name)) return;

        await RunMaintenanceAsync("Create Folder", async () =>
        {
            await _viewModel.CreateFolderAsync(null, name);
        });
    }

    private async void CreateSubfolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.MenuItem menu ||
            menu.Tag is not CPRD.KnowledgeDesk.Core.Models.Folder parent)
            return;

        var name = PromptForText(
            "Create Subfolder",
            $"New subfolder under '{parent.Name}':",
            string.Empty);
        if (string.IsNullOrWhiteSpace(name)) return;

        await RunMaintenanceAsync("Create Subfolder", async () =>
        {
            await _viewModel.CreateFolderAsync(parent.Id, name);
        });
    }

    private async void RenameFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.MenuItem menu ||
            menu.Tag is not CPRD.KnowledgeDesk.Core.Models.Folder folder)
            return;

        var name = PromptForText("Rename Folder", "New folder name:", folder.Name);
        if (string.IsNullOrWhiteSpace(name) ||
            string.Equals(name.Trim(), folder.Name, StringComparison.Ordinal))
            return;

        await RunMaintenanceAsync("Rename Folder", async () =>
        {
            await _viewModel.RenameFolderAsync(folder.Id, name);
        });
    }

    private async void ArchiveFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.MenuItem menu ||
            menu.Tag is not CPRD.KnowledgeDesk.Core.Models.Folder folder)
            return;

        var result = MessageBox.Show(
            $"Archive folder '{folder.Name}'?\n\nNotes are preserved in the database and are not deleted.",
            "Archive Folder",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        await RunMaintenanceAsync("Archive Folder", async () =>
        {
            await _viewModel.ArchiveFolderAsync(folder.Id);
        });
    }

    private string? PromptForText(string title, string prompt, string initialValue)
    {
        var owner = this;
        var dialog = new Window
        {
            Title = title,
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight,
            ShowInTaskbar = false,
            Background = (System.Windows.Media.Brush)FindResource("CanvasBrush")
        };

        var panel = new System.Windows.Controls.StackPanel
        {
            Margin = new Thickness(18),
            MinWidth = 380
        };

        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = prompt,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var textBox = new System.Windows.Controls.TextBox
        {
            Text = initialValue,
            MinWidth = 350,
            Margin = new Thickness(0, 0, 0, 14)
        };
        panel.Children.Add(textBox);

        var buttons = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var cancel = new System.Windows.Controls.Button
        {
            Content = "Cancel",
            MinWidth = 80,
            Margin = new Thickness(0, 0, 8, 0)
        };
        var ok = new System.Windows.Controls.Button
        {
            Content = "OK",
            MinWidth = 80,
            IsDefault = true
        };

        cancel.Click += (_, _) => dialog.DialogResult = false;
        ok.Click += (_, _) => dialog.DialogResult = true;
        buttons.Children.Add(cancel);
        buttons.Children.Add(ok);
        panel.Children.Add(buttons);

        dialog.Content = panel;
        dialog.Loaded += (_, _) =>
        {
            textBox.Focus();
            textBox.SelectAll();
        };

        return dialog.ShowDialog() == true ? textBox.Text.Trim() : null;
    }

    private async void DuplicateFinder_Click(object sender, RoutedEventArgs e)
    {
        var note = _viewModel.Editor.CurrentNote;
        if (note is null)
        {
            MessageBox.Show(
                "Select a note first, then run Duplicate Finder.",
                "Duplicate Finder",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            await FlushPendingChangesAsync();

            var service = _services.GetRequiredService<IDuplicateDetectionService>();
            var candidates = (await service.FindCandidatesAsync(
                    new CPRD.KnowledgeDesk.Core.Models.DuplicateProbe(
                        _viewModel.Editor.Title,
                        _viewModel.Editor.PlainText),
                    CancellationToken.None))
                .Where(candidate => candidate.NoteId != note.Id)
                .Take(12)
                .ToArray();

            if (candidates.Length == 0)
            {
                MessageBox.Show(
                    "No exact or highly similar active notes were found.",
                    "Duplicate Finder",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("Possible duplicate notes:");
            builder.AppendLine();

            foreach (var candidate in candidates)
            {
                builder.Append("• ")
                    .Append(candidate.Title)
                    .Append(" — ")
                    .Append(candidate.Reason)
                    .Append(" (")
                    .Append(candidate.Similarity.ToString("P0"))
                    .AppendLine(")");
            }

            builder.AppendLine();
            builder.Append("No notes were changed or merged.");

            MessageBox.Show(
                builder.ToString(),
                "Duplicate Finder",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            await _services.GetRequiredService<IAppLogService>()
                .LogAsync(
                    AppLogLevel.Error,
                    "Duplicate Finder failed.",
                    ex,
                    CancellationToken.None);

            MessageBox.Show(
                $"Duplicate Finder failed.\n\n{ex.Message}",
                "Duplicate Finder",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void KeyboardShortcuts_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Ctrl+N        New Note\n" +
            "Ctrl+Shift+N  Quick Note\n" +
            "Ctrl+K        Global Search\n" +
            "Ctrl+F        Global Search\n" +
            "Ctrl+S        Save Current Note\n" +
            "Ctrl+W        Close Workspace Note\n" +
            "Ctrl+Shift+F  Toggle Favorite",
            "Keyboard Shortcuts",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var paths = _services.GetRequiredService<IAppPaths>();
        var version = typeof(MainWindow).Assembly.GetName().Version?.ToString() ?? "Unknown";

        MessageBox.Show(
            $"CPRD Knowledge Desk\nVersion {version}\n\n" +
            "Local-first Windows notes and knowledge management.\n\n" +
            $"Database:\n{paths.DatabasePath}",
            "About CPRD Knowledge Desk",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void CompactMode_Click(object sender, RoutedEventArgs e)
    {
        if (CompactModeMenuItem.IsChecked)
        {
            ReadingModeMenuItem.IsChecked = false;
            ApplyWorkspaceLayout(WorkspaceLayoutMode.Compact);
        }
        else
        {
            ApplyWorkspaceLayout(WorkspaceLayoutMode.Normal);
        }
    }

    private void ReadingMode_Click(object sender, RoutedEventArgs e)
    {
        if (ReadingModeMenuItem.IsChecked)
        {
            CompactModeMenuItem.IsChecked = false;
            ApplyWorkspaceLayout(WorkspaceLayoutMode.Reading);
        }
        else
        {
            ApplyWorkspaceLayout(WorkspaceLayoutMode.Normal);
        }
    }

    private void ApplyWorkspaceLayout(WorkspaceLayoutMode mode)
    {
        var layout = WorkspaceLayoutPolicy.For(mode);

        NavigationColumn.MinWidth = layout.ShowNavigation ? 180 : 0;
        NavigationColumn.Width = new GridLength(layout.NavigationWidth);
        NavigationSplitterColumn.Width = new GridLength(layout.ShowNavigation ? 5 : 0);

        NotesColumn.MinWidth = layout.ShowNotes ? 230 : 0;
        NotesColumn.Width = new GridLength(layout.NotesWidth);
        NotesSplitterColumn.Width = new GridLength(layout.ShowNotes ? 5 : 0);
    }

    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        await RunMaintenanceAsync("Backup", async () =>
        {
            await FlushPendingChangesAsync();
            var backup = await _services.GetRequiredService<IBackupService>()
                .BackupNowAsync(CancellationToken.None);

            MessageBox.Show(
                $"Backup completed successfully.\n\n{backup.Path}",
                "Backup Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        });
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        var paths = _services.GetRequiredService<IAppPaths>();
        var dialog = new OpenFileDialog
        {
            Title = "Restore CPRD Knowledge Desk Backup",
            Filter = "Knowledge Desk Database (*.db)|*.db|All Files (*.*)|*.*",
            InitialDirectory = Directory.Exists(paths.BackupsDirectory)
                ? paths.BackupsDirectory
                : paths.DocumentsDirectory
        };

        if (dialog.ShowDialog(this) != true)
            return;

        var confirm = MessageBox.Show(
            "Restore the selected backup?\n\nA safety copy of the current database will be created first. The application will restart after restore.",
            "Confirm Restore",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
            return;

        await RunMaintenanceAsync("Restore", async () =>
        {
            await FlushPendingChangesAsync();
            await _services.GetRequiredService<IBackupService>()
                .RestoreAsync(dialog.FileName, CancellationToken.None);

            MessageBox.Show(
                "Backup restored successfully. CPRD Knowledge Desk will now restart.",
                "Restore Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            RestartApplication();
        });
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var paths = _services.GetRequiredService<IAppPaths>();
        Directory.CreateDirectory(paths.ExportsDirectory);

        var dialog = new SaveFileDialog
        {
            Title = "Export All CPRD Knowledge Desk Notes",
            Filter = "CPRD Notes Export (*.cprdnotes)|*.cprdnotes",
            DefaultExt = ".cprdnotes",
            AddExtension = true,
            InitialDirectory = paths.ExportsDirectory,
            FileName = $"CPRD-Knowledge-Desk-{DateTime.Now:yyyyMMdd-HHmmss}.cprdnotes"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        await RunMaintenanceAsync("Export", async () =>
        {
            await FlushPendingChangesAsync();
            var result = await _services.GetRequiredService<IExportImportService>()
                .ExportAllAsync(dialog.FileName, CancellationToken.None);

            MessageBox.Show(
                $"Export completed successfully.\n\nNotes exported: {result.NoteCount}\n{result.Path}",
                "Export Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        });
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import CPRD Knowledge Desk Notes",
            Filter = "CPRD Notes Export (*.cprdnotes)|*.cprdnotes|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        var fallbackFolderId = _viewModel.SelectedFolderId
            ?? _viewModel.Folders.FirstOrDefault(folder => !folder.IsArchived)?.Id;

        if (fallbackFolderId is null)
        {
            MessageBox.Show(
                "No active folder is available for imported notes.",
                "Import Notes",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        await RunMaintenanceAsync("Import", async () =>
        {
            await FlushPendingChangesAsync();
            var result = await _services.GetRequiredService<IExportImportService>()
                .ImportAsync(dialog.FileName, fallbackFolderId.Value, CancellationToken.None);

            await _viewModel.ShowAllNotesCommand.ExecuteAsync(null);

            MessageBox.Show(
                $"Import completed.\n\nImported: {result.ImportedCount}\nDuplicates skipped: {result.SkippedDuplicateCount}",
                "Import Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        });
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        var paths = _services.GetRequiredService<IAppPaths>();
        OpenFolder(paths.DataDirectory);
    }

    private void OpenBackupsFolder_Click(object sender, RoutedEventArgs e)
    {
        var paths = _services.GetRequiredService<IAppPaths>();
        OpenFolder(paths.BackupsDirectory);
    }

    private void OpenLogsFolder_Click(object sender, RoutedEventArgs e)
    {
        var paths = _services.GetRequiredService<IAppPaths>();
        OpenFolder(Path.Combine(paths.DataDirectory, "Logs"));
    }

    private async Task FlushPendingChangesAsync()
    {
        await EditorPane.FlushEditorContentAsync();
        await _viewModel.FlushEditorAsync();
    }

    private async Task RunMaintenanceAsync(string operation, Func<Task> action)
    {
        IsEnabled = false;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            await _services.GetRequiredService<IAppLogService>()
                .LogAsync(
                    AppLogLevel.Error,
                    $"{operation} operation failed.",
                    ex,
                    CancellationToken.None);

            MessageBox.Show(
                $"{operation} failed.\n\n{ex.Message}\n\nThe error was written to the diagnostic log.",
                $"{operation} Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private void RestartApplication()
    {
        var executable = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(executable))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = true
            });
        }

        _allowClose = true;
        Application.Current.Shutdown();
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose) return;

        e.Cancel = true;
        IsEnabled = false;
        try
        {
            await FlushPendingChangesAsync();
        }
        finally
        {
            _allowClose = true;
            IsEnabled = true;
            Close();
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
