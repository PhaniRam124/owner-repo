using System.Windows;
using CPRD.KnowledgeDesk.App.ViewModels;

namespace CPRD.KnowledgeDesk.App.Views;

public partial class QuickCaptureWindow : Window
{
    private readonly QuickCaptureViewModel _viewModel;

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
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Saved -= OnSaved;
        base.OnClosed(e);
    }
}
