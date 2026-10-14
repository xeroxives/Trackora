using System.Windows.Markup;
using WpfApp14.Services;

namespace WpfApp14.Extensions;
public class FontSizeExtension : MarkupExtension
{
    public string Context { get; set; } = string.Empty;
    public double BaseSize { get; set; } = 14;

    public FontSizeExtension() { }

    public FontSizeExtension(string context, double baseSize)
    {
        Context = context;
        BaseSize = baseSize;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var multiplier = FontService.Instance.GetFontSizeMultiplier(Context);
        return BaseSize * multiplier;
    }
}