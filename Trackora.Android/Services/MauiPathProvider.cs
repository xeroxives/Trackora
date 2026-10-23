using System.IO;
using Microsoft.Maui.Storage;
using Trackora.Core.Interfaces;

namespace Trackora.Android.Services;

public class MauiPathProvider : I_PathProvider
{
    public string AppDataFolder { get; } = FileSystem.AppDataDirectory;
}
