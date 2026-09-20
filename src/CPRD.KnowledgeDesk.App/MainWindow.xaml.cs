using System.ComponentModel;
using System.Windows;
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
            viewModel.Editor.SaveNowCommand,
            viewModel.CloseActiveWorkspaceCommand,
            viewModel.Editor.ToggleFavoriteCommand);

        Closing += OnClosing;
    }

    private void FocusSearch()
    {
        GlobalSearchBox.Focus();
        GlobalSearchBox.SelectAll();
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

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose) return;

        e.Cancel = true;
        IsEnabled = false;
        try
        {
            await _viewModel.FlushEditorAsync();
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
