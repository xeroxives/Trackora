using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Trackora.Core.Interfaces;

namespace Trackora.Android.Services;

public class MauiOsActions : I_OsActions
{
    public void ShowInExplorer(string filePath)
    {
        // Если файл не существует, то и шарить нечего
        if (!File.Exists(filePath))
            return;

        // На Android выполняем операцию асинхронно через безопасную обертку
        Task.Run(async () =>
        {
            // Открывает системное диалоговое окно Android "Поделиться файлом", 
            // откуда пользователь сможет открыть его в любом плеере или проводнике
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Open File",
                File = new ShareFile(filePath)
            });
        }).GetAwaiter().GetResult();
    }
}
