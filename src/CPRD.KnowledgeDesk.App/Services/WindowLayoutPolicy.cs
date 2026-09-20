namespace CPRD.KnowledgeDesk.App.Services;

public sealed record WindowBounds(double Left, double Top, double Width, double Height);

public static class WindowLayoutPolicy
{
    private const double Margin = 16;

    public static WindowBounds Fit(
        double preferredWidth,
        double preferredHeight,
        double minimumWidth,
        double minimumHeight,
        double workLeft,
        double workTop,
        double workWidth,
        double workHeight)
    {
        var availableWidth = Math.Max(1, workWidth - (Margin * 2));
        var availableHeight = Math.Max(1, workHeight - (Margin * 2));

        var width = Math.Min(preferredWidth, availableWidth);
        var height = Math.Min(preferredHeight, availableHeight);

        if (availableWidth >= minimumWidth)
            width = Math.Max(width, minimumWidth);

        if (availableHeight >= minimumHeight)
            height = Math.Max(height, minimumHeight);

        var left = workLeft + Math.Max(Margin, (workWidth - width) / 2);
        var top = workTop + Math.Max(Margin, (workHeight - height) / 2);

        if (left + width > workLeft + workWidth - Margin)
            left = workLeft + workWidth - Margin - width;

        if (top + height > workTop + workHeight - Margin)
            top = workTop + workHeight - Margin - height;

        return new WindowBounds(left, top, width, height);
    }
}
