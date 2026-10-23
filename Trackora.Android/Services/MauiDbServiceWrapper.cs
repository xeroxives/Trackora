using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Trackora.Core.Interfaces;
using Trackora.Core.Models;

namespace Trackora.Android.Services;

public class MauiDbServiceWrapper : I_DbService
{
    private readonly DBService _inner;

    public MauiDbServiceWrapper(I_PathProvider pathProvider)
    {
        string dbPath = Path.Combine(pathProvider.AppDataFolder, "trackora.db");
        _inner = new DBService(dbPath);
    }

    public void AddListeningTime(double seconds, Song song) => _inner.AddListeningTime(seconds, song);
    public int GetVol(Song song) => _inner.GetVol(song);
    public int GetVol(string fileHash) => _inner.GetVol(fileHash);
    public double GetListeningTime(Song song) => _inner.GetListeningTime(song);
    public Task UpdateVolAsync(Song song, CancellationToken token) => _inner.UpdateVolAsync(song, token);
}
