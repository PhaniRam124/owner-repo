using CPRD.KnowledgeDesk.App.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.App.Tests;

public sealed class WindowLayoutPolicyTests
{
    [Fact]
    public void Oversized_window_is_reduced_and_centered_inside_work_area()
    {
        var bounds = WindowLayoutPolicy.Fit(
            preferredWidth: 1440,
            preferredHeight: 880,
            minimumWidth: 900,
            minimumHeight: 560,
            workLeft: 0,
            workTop: 0,
            workWidth: 1024,
            workHeight: 720);

        Assert.True(bounds.Width <= 992);
        Assert.True(bounds.Height <= 688);
        Assert.True(bounds.Left >= 16);
        Assert.True(bounds.Top >= 16);
        Assert.True(bounds.Left + bounds.Width <= 1008);
        Assert.True(bounds.Top + bounds.Height <= 704);
    }

    [Fact]
    public void Large_work_area_keeps_preferred_window_size()
    {
        var bounds = WindowLayoutPolicy.Fit(
            preferredWidth: 1440,
            preferredHeight: 880,
            minimumWidth: 900,
            minimumHeight: 560,
            workLeft: 0,
            workTop: 0,
            workWidth: 1920,
            workHeight: 1040);

        Assert.Equal(1440, bounds.Width);
        Assert.Equal(880, bounds.Height);
    }
}
