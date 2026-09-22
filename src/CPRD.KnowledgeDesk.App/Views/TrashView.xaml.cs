using System.Windows;
using System.Windows.Controls;
using CPRD.KnowledgeDesk.App.ViewModels;

namespace CPRD.KnowledgeDesk.App.Views;

public partial class TrashView : UserControl
{
    public TrashView() => InitializeComponent();

    private async void DeletePermanently_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not TrashViewModel vm ||
            sender is not Button button ||
            button.Tag is not Guid noteId)
            return;

        var result = MessageBox.Show(
            "Permanently delete this note?\n\nThis action cannot be undone.",
            "Delete Note Permanently",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        await vm.DeletePermanentlyCommand.ExecuteAsync(noteId);
    }

    private async void EmptyTrash_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not TrashViewModel vm || vm.Items.Count == 0)
            return;

        var result = MessageBox.Show(
            $"Permanently delete all {vm.Items.Count} note(s) in Trash?\n\nThis action cannot be undone.",
            "Empty Trash",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        await vm.EmptyTrashCommand.ExecuteAsync(null);
    }
}
