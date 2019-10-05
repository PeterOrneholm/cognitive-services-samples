using System;

namespace Orneholm.NewsSearch.Models
{
    public class Broadcast
    {
        public DateTime Availablestoputc { get; set; }
        public Playlist Playlist { get; set; }
        public Broadcastfile[] Broadcastfiles { get; set; }
    }
}