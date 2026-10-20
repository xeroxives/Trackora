using System;
using System.IO;
using WpfApp14.Interfaces;

namespace WpfApp14.Services
{
    public class WpfPathProvider : I_PathProvider
    {
        public string AppDataFolder { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Trackora");
    }
}