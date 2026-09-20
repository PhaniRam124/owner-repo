using System.Windows;
using Xunit;

namespace CPRD.KnowledgeDesk.App.Tests;

public sealed class AppIconResourceTests
{
    [Fact]
    public void Icon_resource_dictionary_exposes_AppIcon()
    {
        _ = Application.Current ?? new Application();

        var resources = new ResourceDictionary
        {
            Source = new Uri(
                "pack://application:,,,/CPRD.KnowledgeDesk.App;component/Resources/Icons.xaml",
                UriKind.Absolute)
        };

        Assert.True(resources.Contains("AppIcon"));
        Assert.NotNull(resources["AppIcon"]);
    }
}
