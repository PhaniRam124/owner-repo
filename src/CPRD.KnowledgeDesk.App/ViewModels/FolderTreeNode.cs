using System.Collections.ObjectModel;
using CPRD.KnowledgeDesk.Core.Models;

namespace CPRD.KnowledgeDesk.App.ViewModels;

public sealed class FolderTreeNode
{
    public FolderTreeNode(Folder folder) => Folder = folder;

    public Folder Folder { get; }
    public Guid Id => Folder.Id;
    public string Name => Folder.Name;
    public ObservableCollection<FolderTreeNode> Children { get; } = new();
}
