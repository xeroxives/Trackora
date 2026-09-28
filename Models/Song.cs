using System;

namespace WpfApp14.Models
{
	public class Song
	{
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
		// <s/> BPM, возможен null
		public int? Bpm { get; set; }
		public string BackgroundImagePath { get; set; } = "";
		public string Info { get; set; } = "";
		public string FilePathOrUri { get; set; } = "";
	}
}