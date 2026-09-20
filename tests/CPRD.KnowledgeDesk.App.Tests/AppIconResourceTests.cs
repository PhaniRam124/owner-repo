using System.Windows;
using Xunit;

namespace CPRD.KnowledgeDesk.App.Tests;

public sealed class AppIconResourceTests
{
    [Fact]
    public void Icon_resource_dictionary_exposes_AppIcon()
    {
        var resources = new ResourceDictionary
        {
            Source = new Uri(
                "/CPRD.KnowledgeDesk.App;component/Resources/Icons.xaml",
                UriKind.RelativeOrAbsolute)
        };

        Assert.True(resources.Contains("AppIcon"));
        Assert.NotNull(resources["AppIcon"]);
    }
}
