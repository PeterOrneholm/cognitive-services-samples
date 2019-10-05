using System;

namespace Orneholm.NewsSearch.Models
{
    public class Broadcastfile
    {
        public int Duration { get; set; }
        public DateTime PublishDateUtc { get; set; }
        public int Id { get; set; }
        public string Url { get; set; }
    }
}