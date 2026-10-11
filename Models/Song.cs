using System;
using WpfApp14.ViewModels;

namespace WpfApp14.Models
{
    public class Song : VM
    {
        private bool _isSavedToAppData;
        public bool IsSavedToAppData
        {
            get => _isSavedToAppData;
            set { _isSavedToAppData = value; Notify(nameof(IsSavedToAppData)); }
        }

        private string _appDataFilePath;
        public string AppDataFilePath
        {
            get => _appDataFilePath;
            set { _appDataFilePath = value; Notify(nameof(AppDataFilePath)); }
        }

        public int Id { get; set; }
        public int ArtistId { get; set; }
        public string Title { get; set; } = "";
        public string Title_Romanized { get; set; } = "";
        public string Album { get; set; } = "";
        public string Genre { get; set; } = "";
        public string Language { get; set; } = "";
        public DateTime ReleaseDate { get; set; }
        public double DurationSeconds { get; set; }
        public int BitrateKbps { get; set; }
        public int? Bpm { get; set; }
        public string BackgroundImagePath { get; set; } = "";
        public string Info { get; set; } = "";
        public string FilePathOrUri { get; set; } = "";
        public int ListenedSeconds { get; set; } = 0;
        public string CuttedTitle(int o)
        {
            return Title.Substring(0, Math.Min(Title.Length, o));
        }
        public override string ToString()
        {
            return $"{Id,-3} | {Title.Substring(0, Math.Min(Title.Length, 10))}....";
        }
    }
}