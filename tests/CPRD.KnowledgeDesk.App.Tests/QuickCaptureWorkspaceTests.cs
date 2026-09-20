using CPRD.KnowledgeDesk.App.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.App.Tests;

public sealed class QuickCaptureWorkspaceTests
{
    [Fact]
    public void Workspace_keeps_at_most_six_notes_and_never_deletes_notes()
    {
        var service = new WorkspaceService();
        var ids = Enumerable.Range(0, 7).Select(_ => Guid.NewGuid()).ToArray();

        foreach (var id in ids)
            service.Open(id);

        Assert.Equal(6, service.OpenNoteIds.Count);
        Assert.DoesNotContain(ids[0], service.OpenNoteIds);
        Assert.Equal(ids.Skip(1), service.OpenNoteIds);
    }

    [Fact]
    public void Reopening_note_moves_it_to_most_recent_position()
    {
        var service = new WorkspaceService();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        service.Open(a);
        service.Open(b);
        service.Open(a);

        Assert.Equal(new[] { b, a }, service.OpenNoteIds);
    }
}
