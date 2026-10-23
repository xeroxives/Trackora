using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using System.Diagnostics;

namespace Trackora.Android
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            Debug.WriteLine("[TEST-START] ====================================");
            builder.ConfigureLifecycleEvents(static events =>
            {
#if ANDROID
                events.AddAndroid(android => android.OnCreate((activity, bundle) =>
                {
                    // Запускаем тесты сразу после того, как нативное Android-активити создано
                    Task.Run(async () =>
                    {
                        // Небольшая задержка, чтобы интерфейс успел отрисоваться
                        await Task.Delay(10000);

                        // Самый надежный способ получить корневой провайдер запущенного MAUI приложения:
                        var handlerProvider = Application.Current?.Handler?.MauiContext?.Services;

                        if (handlerProvider != null)
                        {
                            var tester = new MobileServicesTester(handlerProvider);
                            await tester.RunAllTestsAsync();
                        }
                        else
                        {
                            Debug.WriteLine("[TEST-FAIL] ❌ Критическая ошибка: Не удалось получить MauiContext.Services из живого приложения!");
                        }
                    });
                }));
#endif
            });

            return builder.Build();
        }
    }
}
