using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CPRD.KnowledgeDesk.App.Services;
using CPRD.KnowledgeDesk.App.ViewModels;

namespace CPRD.KnowledgeDesk.App.Views;

public partial class EditorView : UserControl
{
    private readonly FlowDocumentSerializer _serializer = new();
    private readonly AutoSaveCoordinator _contentSync =
        new(new SystemDelayScheduler(), TimeSpan.FromMilliseconds(300));
    private EditorViewModel? _subscribedViewModel;
    private bool _loading;

    public EditorView()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadDocument();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_subscribedViewModel is not null)
            _subscribedViewModel.PropertyChanged -= ViewModel_PropertyChanged;

        _subscribedViewModel = DataContext as EditorViewModel;

        if (_subscribedViewModel is not null)
            _subscribedViewModel.PropertyChanged += ViewModel_PropertyChanged;

        LoadDocument();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorViewModel.DocumentVersion))
            LoadDocument();
    }

    private void LoadDocument()
    {
        if (!IsLoaded || DataContext is not EditorViewModel vm) return;

        _loading = true;
        try
        {
            EditorBox.Document = _serializer.Deserialize(vm.ContentPackage, vm.PlainText);
        }
        finally
        {
            _loading = false;
        }
    }

    private void EditorBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || DataContext is not EditorViewModel vm) return;

        var range = new TextRange(EditorBox.Document.ContentStart, EditorBox.Document.ContentEnd);
        vm.PlainText = range.Text.TrimEnd();
        vm.ContentPackage = string.Empty;

        var version = vm.DocumentVersion;
        _ = _contentSync.ScheduleAsync(async cancellationToken =>
        {
            await EditorBox.Dispatcher.InvokeAsync(() =>
            {
                if (DataContext is not EditorViewModel current ||
                    !ReferenceEquals(current, vm) ||
                    current.DocumentVersion != version)
                    return;

                var serialized = _serializer.Serialize(EditorBox.Document);
                current.ContentPackage = serialized.ContentPackage;
                current.PlainText = serialized.PlainText;
            });
        });
    }

    public Task FlushEditorContentAsync(CancellationToken cancellationToken = default)
    {
        if (DataContext is not EditorViewModel vm)
            return Task.CompletedTask;

        var version = vm.DocumentVersion;
        return _contentSync.FlushAsync(async _ =>
        {
            await EditorBox.Dispatcher.InvokeAsync(() =>
            {
                if (DataContext is not EditorViewModel current ||
                    !ReferenceEquals(current, vm) ||
                    current.DocumentVersion != version)
                    return;

                var serialized = _serializer.Serialize(EditorBox.Document);
                current.ContentPackage = serialized.ContentPackage;
                current.PlainText = serialized.PlainText;
            });
        }, cancellationToken);
    }

    private async void EditorBox_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        try
        {
            await FlushEditorContentAsync();
        }
        catch
        {
            // The view-model save path reports persistence failures.
        }
    }

    private void Bold_Click(object sender, RoutedEventArgs e) =>
        EditingCommands.ToggleBold.Execute(null, EditorBox);

    private void Italic_Click(object sender, RoutedEventArgs e) =>
        EditingCommands.ToggleItalic.Execute(null, EditorBox);

    private void Underline_Click(object sender, RoutedEventArgs e) =>
        EditingCommands.ToggleUnderline.Execute(null, EditorBox);

    private void Bullets_Click(object sender, RoutedEventArgs e) =>
        EditingCommands.ToggleBullets.Execute(null, EditorBox);

    private void Numbering_Click(object sender, RoutedEventArgs e) =>
        EditingCommands.ToggleNumbering.Execute(null, EditorBox);
}
