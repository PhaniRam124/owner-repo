using System.IO;
using System.Windows;
using System.Windows.Documents;

namespace CPRD.KnowledgeDesk.App.Services;

public sealed record SerializedDocument(string ContentPackage, string PlainText);

public sealed class FlowDocumentSerializer
{
    public SerializedDocument Serialize(FlowDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var range = new TextRange(document.ContentStart, document.ContentEnd);
        using var stream = new MemoryStream();
        range.Save(stream, DataFormats.XamlPackage);
        return new SerializedDocument(Convert.ToBase64String(stream.ToArray()), range.Text.TrimEnd());
    }

    public FlowDocument Deserialize(string? package, string? fallbackPlainText = null)
    {
        var document = new FlowDocument();
        var range = new TextRange(document.ContentStart, document.ContentEnd);

        if (!string.IsNullOrWhiteSpace(package))
        {
            try
            {
                var bytes = Convert.FromBase64String(package);
                using var stream = new MemoryStream(bytes);
                range.Load(stream, DataFormats.XamlPackage);
                return document;
            }
            catch (FormatException)
            {
            }
            catch (ArgumentException)
            {
            }
        }

        range.Text = fallbackPlainText ?? string.Empty;
        return document;
    }
}
