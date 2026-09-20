using System.Windows;
using CPRD.KnowledgeDesk.App.ViewModels;

namespace CPRD.KnowledgeDesk.App;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Exit_Click(object sender, RoutedEventArgs e) =>
        Application.Current.Shutdown();
}
