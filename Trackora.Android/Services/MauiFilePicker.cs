using Microsoft.Maui.Devices;

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Trackora.Core.Interfaces;

namespace Trackora.Android.Services;

public class MauiFilePicker : I_FilePicker
{
    public string? PickAudioFile()
    {
        return Task.Run(async () => await PickAudioFileAsync()).GetAwaiter().GetResult();
    }

    private async Task<string?> PickAudioFileAsync()
    {
        try
        {
            var audioTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "audio/*" } }
            });

            var options = new PickOptions
            {
                PickerTitle = "Select Music File",
                FileTypes = audioTypes
            };

            var result = await FilePicker.Default.PickAsync(options);
            if (result == null) return null;
            return result.FullPath;
        }
        catch (Exception){ return null; }
    }
}
