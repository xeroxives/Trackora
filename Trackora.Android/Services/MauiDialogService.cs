using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Trackora.Core.Interfaces;
using Trackora.Core.Models;
using Trackora.Android.Views;

namespace Trackora.Android.Services;

public class MauiDialogService : I_DialogService
{
    public void ShowEditMetadataDialog(Song song)
    {
        Task.Run(async () =>
        {
            if (Application.Current?.MainPage != null)
            {
                var page = new EditMetadataPage(song);
                await Application.Current.MainPage.Navigation.PushModalAsync(page);
            }
        }).GetAwaiter().GetResult();
    }

    public void ShowUrlDialog()
    {
        Task.Run(async () =>
        {
            if (Application.Current?.MainPage != null)
            {
                var page = new UrlPage();
                await Application.Current.MainPage.Navigation.PushModalAsync(page);
            }
        }).GetAwaiter().GetResult();
    }
}
