using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp14.Models;

namespace WpfApp14.Interfaces
{
    public interface I_DbService
    {
        void AddListeningTime(double seconds, Song song);
        int GetVol(Song song);
        int GetVol(string fileHash);
        double GetListeningTime(Song song);
        Task UpdateVolAsync(Song song, CancellationToken token);
    }
}
