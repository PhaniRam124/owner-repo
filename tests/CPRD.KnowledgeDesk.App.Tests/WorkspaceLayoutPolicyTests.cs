using CPRD.KnowledgeDesk.App.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.App.Tests;

public sealed class WorkspaceLayoutPolicyTests
{
    [Fact]
    public void Compact_mode_reduces_navigation_and_notes_widths()
    {
        var layout = WorkspaceLayoutPolicy.For(WorkspaceLayoutMode.Compact);

        Assert.Equal(210, layout.NavigationWidth);
        Assert.Equal(280, layout.NotesWidth);
        Assert.True(layout.ShowNavigation);
        Assert.True(layout.ShowNotes);
    }

    [Fact]
    public void Reading_mode_hides_navigation_and_notes_columns()
    {
        var layout = WorkspaceLayoutPolicy.For(WorkspaceLayoutMode.Reading);

        Assert.False(layout.ShowNavigation);
        Assert.False(layout.ShowNotes);
        Assert.Equal(0, layout.NavigationWidth);
        Assert.Equal(0, layout.NotesWidth);
    }

    [Fact]
    public void Normal_mode_restores_default_widths()
    {
        var layout = WorkspaceLayoutPolicy.For(WorkspaceLayoutMode.Normal);

        Assert.Equal(260, layout.NavigationWidth);
        Assert.Equal(340, layout.NotesWidth);
        Assert.True(layout.ShowNavigation);
        Assert.True(layout.ShowNotes);
    }
}
