namespace CPRD.KnowledgeDesk.App.Services;

public enum WorkspaceLayoutMode
{
    Normal,
    Compact,
    Reading
}

public sealed record WorkspaceLayout(
    double NavigationWidth,
    double NotesWidth,
    bool ShowNavigation,
    bool ShowNotes);

public static class WorkspaceLayoutPolicy
{
    public static WorkspaceLayout For(WorkspaceLayoutMode mode) =>
        mode switch
        {
            WorkspaceLayoutMode.Compact => new WorkspaceLayout(210, 280, true, true),
            WorkspaceLayoutMode.Reading => new WorkspaceLayout(0, 0, false, false),
            _ => new WorkspaceLayout(260, 340, true, true)
        };
}
