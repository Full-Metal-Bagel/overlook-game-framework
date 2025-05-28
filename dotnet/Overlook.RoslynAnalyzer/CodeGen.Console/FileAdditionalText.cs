using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

public class FileAdditionalText : AdditionalText
{
    private readonly string _path;
    private readonly Lazy<SourceText> _content;

    public FileAdditionalText(string path)
    {
        _path = path;
        _content = new Lazy<SourceText>(() => SourceText.From(File.ReadAllText(_path)));
    }

    public override string Path => _path;

    public override SourceText GetText(CancellationToken cancellationToken = default)
    {
        return _content.Value;
    }
}
