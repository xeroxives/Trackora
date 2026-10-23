using Trackora.Core.Interfaces;
using Trackora.Android.Utils;

namespace Trackora.Android.Services;

public class MauiFileHasherService : I_FileHasher
{
    private readonly MauiFileHasher _hasher = new();

    public string ComputePartialHash(string filePath)
    {
        return _hasher.ComputePartialHash(filePath);
    }
}
