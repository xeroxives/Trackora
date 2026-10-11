using System.Windows.Markup;
using System.Windows.Media;
using WpfApp14.Services;

namespace WpfApp14.Extensions;

public class FontFamilyExtension : MarkupExtension
{
    public string Context { get; set; } = string.Empty;

    public FontFamilyExtension() { }

    public FontFamilyExtension(string context)
    {
        Context = context;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return FontService.Instance.GetFontFamily(Context);
    }
}