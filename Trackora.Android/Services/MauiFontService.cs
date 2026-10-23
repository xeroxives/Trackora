using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Trackora.Core.Models;
using Trackora.Core.Utils;
using Microsoft.Maui.Storage;

// Нативные пространства имен Android
#if ANDROID
using Android.Graphics;
using Microsoft.Maui.Platform;
#endif

namespace Trackora.Android.Services;

public class MauiFontService
{
    private static MauiFontService? _instance;
    public static MauiFontService Instance => _instance ??= new MauiFontService();

    private readonly Dictionary<string, FontConfig> _configs = new();

    // Кэш для нативных шрифтов Android
#if ANDROID
    private readonly Dictionary<string, Typeface> _nativeCache = new();
#endif

    private readonly string _fontsPath;

    private MauiFontService()
    {
        // Папка "Fonts" во внутреннем изолированном хранилище Android, 
        // куда пользователь или приложение может сохранять файлы .ttf/.otf
        _fontsPath = System.IO.Path.Combine(FileSystem.AppDataDirectory, "Fonts");

        if (!Directory.Exists(_fontsPath))
        {
            Directory.CreateDirectory(_fontsPath);
        }

        LoadConfigs();
    }

    private void LoadConfigs()
    {
        var jsonPath = System.IO.Path.Combine(_fontsPath, "fonts.json");
        if (!File.Exists(jsonPath)) return;

        try
        {
            var json = File.ReadAllText(jsonPath);
            var configs = JsonSerializer.Deserialize<List<FontConfig>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (configs == null) return;

            foreach (var cfg in configs)
            {
                Logger.Log($"[FONT-CFG-ANDROID]: {cfg.Context}|{cfg.FontFile}");
                _configs[cfg.Context] = cfg;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[FontService-Android] Ошибка чтения конфига: {ex.Message}");
        }
    }

    /// <summary>
    /// Метод для получения нативного шрифта Android по контексту из конфига.
    /// Если файл шрифта существует на устройстве, он загружается напрямую в память.
    /// </summary>
    public object? GetNativeFont(string context)
    {
#if ANDROID
        if (!_configs.TryGetValue(context, out var cfg) || string.IsNullOrEmpty(cfg.FontFile))
            return Typeface.Default;

        if (_nativeCache.TryGetValue(context, out var cached))
            return cached;

        var fullPath = System.IO.Path.Combine(_fontsPath, cfg.FontFile);
        if (!File.Exists(fullPath))
        {
            Logger.Error($"[FontService-Android] Файл шрифта не найден по пути: {fullPath}");
            return Typeface.Default;
        }

        try
        {
            // Нативный вызов Android: создаем шрифт напрямую из файла в памяти телефона на лету
            var typeface = Typeface.CreateFromFile(fullPath);
            if (typeface != null)
            {
                _nativeCache[context] = typeface;
                Logger.Log($"[FontService-Android] Успешно загружен кастомный шрифт из файла: {cfg.FontFile}");
                return typeface;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[FontService-Android] Ошибка нативного создания шрифта: {ex.Message}");
        }

        return Typeface.Default;
#else
        return null;
#endif
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
#if ANDROID
        _nativeCache.Clear();
#endif
        LoadConfigs();

        // Оповещаем MAUI о том, что нужно перерисовать интерфейс с новыми шрифтами
        // Реализуем вызов перерисовки окон, если требуется в вашем UI
    }
}
