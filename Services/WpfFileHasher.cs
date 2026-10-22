using Trackora.Core.Interfaces;
using WpfApp14.Utils;

namespace WpfApp14.Services
{
    public class WpfFileHasher : I_FileHasher
    {
        public string ComputePartialHash(string filePath) => FileHasher.ComputePartialHash(filePath);
    }
}