using System.Threading;
using System.Threading.Tasks;
using WpfApp14.Interfaces;
using WpfApp14.Models;

namespace WpfApp14.Services
{
    public class DbServiceWrapper : I_DbService
    {
        private readonly DBService _inner = new();

        public void AddListeningTime(double seconds, Song song) => _inner.AddListeningTime(seconds, song);
        public int GetVol(Song song) => _inner.GetVol(song);
        public int GetVol(string fileHash) => _inner.GetVol(fileHash);
        public double GetListeningTime(Song song) => _inner.GetListeningTime(song);
        public Task UpdateVolAsync(Song song, CancellationToken token) => _inner.UpdateVolAsync(song, token);
    }
}