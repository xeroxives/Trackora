using System.Collections.Generic;

namespace Trackora.Core.Models
{
	public class Artist
	{
		public int Id { get; set; }
		public string Name { get; set; } = "";
		public string Name_Romanized { get; set; } = "";
		public string ImagePath { get; set; } = "";
		public string BackgroundImagePath { get; set; } = "";
		public string Info { get; set; } = "";
		public List<Song> Songs { get; set; } = new List<Song>();
	}
}