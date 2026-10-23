using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Trackora.Core.Interfaces;
using Trackora.Core.Models;
using Trackora.Android.Services;
using Trackora.Android.Utils;

namespace Trackora.Android;

public class MobileServicesTester
{
    private readonly IServiceProvider _services;

    public MobileServicesTester(IServiceProvider services)
    {
        _services = services;
    }

    public async Task RunAllTestsAsync()
    {
        Debug.WriteLine("\n==================================================");
        Debug.WriteLine("        🚀 ЗАПУСК ТЕСТИРОВАНИЯ СЕРВИСОВ ANDROID    ");
        Debug.WriteLine("==================================================");

        bool pathOk = false;
        bool dbOk = false;
        bool uiThreadOk = false;
        bool timerOk = false;
        bool mediaOk = false;
        bool fontOk = false;
        string testFolder = "";

        // 1. Тест PathProvider
        var pathProvider = _services.GetService<I_PathProvider>();
        if (pathProvider == null) Debug.WriteLine("[TEST-FAIL] ❌ Сервис I_PathProvider не найден в контейнере!");
        else pathOk = TestPathProvider(pathProvider, out testFolder);

        // 2. Тест DBService
        var dbService = _services.GetService<I_DbService>();
        if (dbService == null) Debug.WriteLine("[TEST-FAIL] ❌ Сервис I_DbService не найден в контейнере!");
        else dbOk = TestDatabase(dbService);

        // 3. Тест UiThread
        var uiThread = _services.GetService<I_UiThread>();
        if (uiThread == null) Debug.WriteLine("[TEST-FAIL] ❌ Сервис I_UiThread не найден в контейнере!");
        else uiThreadOk = await TestUiThreadAsync(uiThread);

        // 4. Тест UiTimer
        var uiTimer = _services.GetService<I_UITimer>();
        if (uiTimer == null) Debug.WriteLine("[TEST-FAIL] ❌ Сервис I_UITimer не найден в контейнере!");
        else timerOk = await TestUiTimerAsync(uiTimer);

        // 5. Тест FileHasher и AudioService
        var fileHasher = _services.GetService<I_FileHasher>();
        var audioService = _services.GetService<I_AudioService>();


        if (fileHasher == null) Debug.WriteLine("[TEST-FAIL] ❌ Сервис I_FileHasher не зарегистрирован в DI или вернул null!");
        if (audioService == null) Debug.WriteLine("[TEST-FAIL] ❌ Сервис I_AudioService не зарегистрирован в DI или вернул null!");

        if (fileHasher != null && audioService != null)
            mediaOk = await TestMediaServicesAsync(testFolder, fileHasher, audioService);

        // 6. Тест FontService
        try { fontOk = TestFontService(); }
        catch (Exception ex) { Debug.WriteLine($"[TEST-FAIL] ❌ Исключение в TestFontService: {ex.Message}"); }

        Debug.WriteLine("\n==================================================");
        Debug.WriteLine("              📊 ИТОГИ ТЕСТИРОВАНИЯ              ");
        Debug.WriteLine("==================================================");
        Debug.WriteLine($"1. Файловая система (PathProvider): {(pathOk ? "✅ ПРОЙДЕНО" : "❌ СБОЙ")}");
        Debug.WriteLine($"2. База данных SQLite (I_DbService):  {(dbOk ? "✅ ПРОЙДЕНО" : "❌ СБОЙ")}");
        Debug.WriteLine($"3. Главный UI поток (I_UiThread):   {(uiThreadOk ? "✅ ПРОЙДЕНО" : "❌ СБОЙ")}");
        Debug.WriteLine($"4. Системный таймер (I_UITimer):    {(timerOk ? "✅ ПРОЙДЕНО" : "❌ СБОЙ")}");
        Debug.WriteLine($"5. Хеширование и Плеер (Audio):     {(mediaOk ? "✅ ПРОЙДЕНО" : "❌ СБОЙ")}");
        Debug.WriteLine($"6. Динамические шрифты (Fonts):     {(fontOk ? "✅ ПРОЙДЕНО" : "❌ СБОЙ")}");
        Debug.WriteLine("==================================================\n");
    }


    private bool TestPathProvider(I_PathProvider provider, out string targetFolder)
    {
        try
        {
            targetFolder = provider.AppDataFolder;
            Debug.WriteLine($"[TEST-PATH] Системная песочница Android: {targetFolder}");

            // Пробуем создать тестовый файл в этой папке
            string testFile = Path.Combine(targetFolder, "test_write.txt");
            File.WriteAllText(testFile, "Trackora Test");
            bool exists = File.Exists(testFile);
            File.Delete(testFile);

            if (exists) Debug.WriteLine("[TEST-PATH] Успешная запись/чтение в AppDataDirectory!");
            return exists;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TEST-PATH] ❌ Ошибка файловой системы: {ex.Message}");
            targetFolder = "";
            return false;
        }
    }

    private bool TestDatabase(I_DbService db)
    {
        try
        {
            var fakeSong = new Song { FileHash = "TEST_HASH_123", Title = "Test Mobile Track", DefaultVolume = 85 };

            // Проверяем запись/обновление громкости трека в SQLite
            db.UpdateVolAsync(fakeSong, CancellationToken.None).GetAwaiter().GetResult();
            int volume = db.GetVol(fakeSong);
            Debug.WriteLine($"[TEST-DB] Записано в базу громкость: 85. Считано обратно: {volume}");

            // Проверяем запись времени прослушивания
            db.AddListeningTime(15.5, fakeSong);

            // Небольшая заурядная пауза, так как запись времени идет в Task.Run внутри сервиса
            Thread.Sleep(300);
            double time = db.GetListeningTime(fakeSong);
            Debug.WriteLine($"[TEST-DB] Записано статистики: 15.5 сек. Считано обратно: {time} сек.");

            return volume == 85 && time > 15.0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TEST-DB] ❌ Ошибка базы данных: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> TestUiThreadAsync(I_UiThread uiThread)
    {
        bool executedInUi = false;
        var tcs = new TaskCompletionSource<bool>();

        // Вызываем из фонового потока
        await Task.Run(() =>
        {
            uiThread.Invoke(() =>
            {
                // Проверяем, действительно ли мы теперь в UI потоке Android
                executedInUi = Microsoft.Maui.ApplicationModel.MainThread.IsMainThread;
                tcs.SetResult(executedInUi);
            });
        });

        return await tcs.Task;
    }

    private async Task<bool> TestUiTimerAsync(I_UITimer timer)
    {
        int tickCount = 0;
        var tcs = new TaskCompletionSource<bool>();

        timer.Interval = TimeSpan.FromMilliseconds(100);
        timer.Tick += (s, e) =>
        {
            tickCount++;
            if (tickCount >= 3)
            {
                timer.Stop();
                tcs.TrySetResult(true);
            }
        };

        timer.Start();

        // Ждем максимум 1 секунду выполнения таймера
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000));
        return completedTask == tcs.Task && tickCount >= 3;
    }

    private async Task<bool> TestMediaServicesAsync(string folder, I_FileHasher hasher, I_AudioService player)
    {
        try
        {
            if (string.IsNullOrEmpty(folder)) return false;

            // Создаем фиктивный аудиофайл (пустой) для проверки логики хеширования
            string dummyAudio = Path.Combine(folder, "dummy_track.mp3");
            byte[] dummyData = new byte[1024 * 200]; // 200 KB
            new Random().NextBytes(dummyData);
            File.WriteAllBytes(dummyAudio, dummyData);

            // Тестируем хешер
            string hash = hasher.ComputePartialHash(dummyAudio);
            Debug.WriteLine($"[TEST-MEDIA] Сгенерирован быстрый хеш файла: {hash}");

            // Тестируем плеер (так как файл не содержит реальных аудиофреймов, 
            // системный плеер выдаст ошибку декодирования при воспроизведении, 
            // но мы проверим, что сам метод Play инициализируется и логирует это)
            Debug.WriteLine("[TEST-MEDIA] Проверка вызова плеера...");
            player.Play(dummyAudio);
            bool stateChecked = !player.IsPlaying; // Ожидаем false для фейкового файла, главное без падения приложения
            player.Stop();

            File.Delete(dummyAudio);
            return !string.IsNullOrEmpty(hash) && stateChecked;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TEST-MEDIA] ❌ Ошибка медиа-сервисов: {ex.Message}");
            return false;
        }
    }

    private bool TestFontService()
    {
        try
        {
            var service = MauiFontService.Instance;
            double size = service.GetFontSizeMultiplier("MainUI");
            object? nativeFont = service.GetNativeFont("MainUI");

            Debug.WriteLine($"[TEST-FONTS] Множитель шрифта: {size}. Нативный объект: {nativeFont}");
            return nativeFont != null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TEST-FONTS] ❌ Ошибка шрифтов: {ex.Message}");
            return false;
        }
    }
}
