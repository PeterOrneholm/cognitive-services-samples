using System;

namespace Orneholm.NewsSearch.Models
{
    public class Playlist
    {
        public int Duration { get; set; }
        public DateTime Publishdateutc { get; set; }
        public int Id { get; set; }
        public string Url { get; set; }
    }
}