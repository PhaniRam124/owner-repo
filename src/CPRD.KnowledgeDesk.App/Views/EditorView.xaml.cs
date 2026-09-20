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
        var serialized = _serializer.Serialize(EditorBox.Document);
        vm.ContentPackage = serialized.ContentPackage;
        vm.PlainText = serialized.PlainText;
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
