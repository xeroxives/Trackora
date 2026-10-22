//using System.Diagnostics;
//using System.IO;
//using System.Text.Json;
//using System.Windows;
//using System.Windows.Media;
//using WpfApp14.Models;
//using WpfApp14.Utils;

//namespace WpfApp14.Services
//{
//    public class FontService
//    {
//        private static FontService? _instance;
//        public static FontService Instance => _instance ??= new FontService();

//        private readonly Dictionary<string, FontConfig> _configs = new();
//        private readonly Dictionary<string, FontFamily> _cache = new();
//        private readonly string _fontsPath;

//        private FontService()
//        {
//            _fontsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts");
//            LoadConfigs();
//        }

//        private void LoadConfigs()
//        {
//            var jsonPath = Path.Combine(_fontsPath, "fonts.json");
//            if (!File.Exists(jsonPath)) return;

//            var json = File.ReadAllText(jsonPath);
//            var configs = JsonSerializer.Deserialize<List<FontConfig>>(json,
//                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

//            if (configs == null) return;

//            foreach (var cfg in configs)
//            {
//                Debug.WriteLine($"[FONT-CFG]: {cfg.Context}|{cfg.FontFile}");
//                _configs[cfg.Context] = cfg;
//            }
//        }

//        /// <summary>
//        /// Получить FontFamily для контекста.
//        /// Если файл не найден — вернёт системный шрифт по умолчанию.
//        /// </summary>
//        public FontFamily GetFontFamily(string context)
//        {
//            if (!_configs.TryGetValue(context, out var cfg))
//                return SystemFonts.MessageFontFamily;

//            if (_cache.TryGetValue(context, out var cached))
//                return cached;

//            var fontPath = Path.Combine(_fontsPath, cfg.FontFile);
//            if (!File.Exists(fontPath))
//            {
//                if(cfg.FontFile == "")
//                {
//                    Debug.WriteLine($"[FontService] {cfg.Context} пуст");
//                    return SystemFonts.MessageFontFamily;
//                }

//                Debug.WriteLine($"[FontService] Файл не найден: {fontPath}\\{{{cfg.FontFile}}}");
//                return SystemFonts.MessageFontFamily;
//            }

//            try
//            {
//                // WPF поддерживает загрузку шрифта по URI файла
//                var uri = new Uri(fontPath, UriKind.Absolute);
//                var family = new FontFamily(uri, $"./#{GetFontName(fontPath)}");
//                _cache[context] = family;
//                return family;
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"[FontService] Ошибка загрузки {cfg.FontFile}: {ex.Message}");
//                return SystemFonts.MessageFontFamily;
//            }
//        }

//        /// <summary>
//        /// Получить множитель размера шрифта для контекста.
//        /// Возвращает 1.0 если не задан.
//        /// </summary>
//        public double GetFontSizeMultiplier(string context)
//        {
//            if (!_configs.TryGetValue(context, out var cfg) || cfg.FontSize <= 0)
//                return 1.0;
//            return cfg.FontSize;
//        }

//        /// <summary>
//        /// Извлечь имя шрифта из файла (упрощённо — берёт имя файла без расширения).
//        /// Для точного имени нужен парсинг таблицы name — здесь упрощение.
//        /// </summary>
//        private static string GetFontName(string fontPath)
//        {
//            return Path.GetFileNameWithoutExtension(fontPath);
//        }

//        /// <summary>
//        /// Перезагрузить конфиги (для горячей перезагрузки).
//        /// </summary>
//        public void Reload()
//        {
//            _configs.Clear();
//            _cache.Clear();
//            LoadConfigs();
//        }
//    }
//}
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Text.Json;
using Trackora.Core.Models;
using Trackora.Core.Utils;
using GdiFontFamily = System.Drawing.FontFamily;
using WpfFontFamily = System.Windows.Media.FontFamily;

namespace WpfApp14.Services;

public class FontService
{
    private static FontService? _instance;
    public static FontService Instance => _instance ??= new FontService();

    private readonly Dictionary<string, FontConfig> _configs = new();
    private readonly Dictionary<string, WpfFontFamily> _cache = new();
    private readonly PrivateFontCollection _fontCollection = new();
    private readonly string _fontsPath;

    private FontService()
    {

        _fontsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts");
        LoadConfigs();
    }

    private void LoadConfigs()
    {
        var jsonPath = Path.Combine(_fontsPath, "fonts.json");
        if (!File.Exists(jsonPath)) return;

        var json = File.ReadAllText(jsonPath);
        var configs = JsonSerializer.Deserialize<List<FontConfig>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (configs == null) return;

        foreach (var cfg in configs)
        {
            Logger.Log($"[FONT-CFG]: {cfg.Context}|{cfg.FontFile}");
            _configs[cfg.Context] = cfg;
        }
    }

    public WpfFontFamily GetFontFamily(string context)
    {
        if (!_configs.TryGetValue(context, out var cfg))
            return System.Windows.SystemFonts.MessageFontFamily;

        if (_cache.TryGetValue(context, out var cached))
            return cached;
        if (cfg.FontFile == "")
        {
            Logger.Error($"[FontService] {cfg.Context} пуст");
            return System.Windows.SystemFonts.MessageFontFamily;
        }
        var fontPath = Path.Combine(_fontsPath, cfg.FontFile);
        if (!File.Exists(fontPath))
        {
            Logger.Error($"[FontService] Файл не найден: {fontPath}");
            return System.Windows.SystemFonts.MessageFontFamily;
        }

        try
        {
            _fontCollection.AddFontFile(fontPath);
            var familyName = _fontCollection.Families[^1].Name;

            var family = new WpfFontFamily(familyName);
            _cache[context] = family;

            System.Diagnostics.Debug.WriteLine($"[FontService] Загружен: {cfg.FontFile} → {familyName}");
            return family;
        }
        catch (Exception ex)
        {
            Logger.Error($"[FontService] Ошибка загрузки {cfg.FontFile}: {ex.Message}");
            return System.Windows.SystemFonts.MessageFontFamily;
        }
    }

    public double GetFontSizeMultiplier(string context)
    {
        if (!_configs.TryGetValue(context, out var cfg) || cfg.FontSize <= 0)
            return 1.0;
        return cfg.FontSize;
    }

    public void Reload()
    {
        _configs.Clear();
        _cache.Clear();
        LoadConfigs();
    }
}