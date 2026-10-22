using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trackora.Core.Models;

namespace Trackora.Core.Interfaces
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
