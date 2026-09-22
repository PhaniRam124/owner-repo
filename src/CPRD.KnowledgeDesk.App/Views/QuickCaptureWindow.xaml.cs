using System.Windows;
using CPRD.KnowledgeDesk.App.ViewModels;

namespace CPRD.KnowledgeDesk.App.Views;

public partial class QuickCaptureWindow : Window
{
    private readonly QuickCaptureViewModel _viewModel;

    public Guid? SavedNoteId { get; private set; }
    public bool OpenAfterSave { get; private set; }

    public QuickCaptureWindow(QuickCaptureViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.Saved += OnSaved;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) =>
        await _viewModel.InitializeAsync();

    private void OnSaved(object? sender, Guid noteId)
    {
        SavedNoteId = noteId;
        DialogResult = true;
        Close();
    }

    private async void SaveAndOpen_Click(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.SaveCommand.CanExecute(null))
            return;

        OpenAfterSave = true;
        await _viewModel.SaveCommand.ExecuteAsync(null);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Saved -= OnSaved;
        base.OnClosed(e);
    }
}
