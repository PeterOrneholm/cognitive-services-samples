using System;

namespace Orneholm.NewsSearch.Models
{
    public class Downloadpodfile
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int FileSizeInBytes { get; set; }
        public DateTime AvailableFromUtc { get; set; }
        public int Duration { get; set; }
        public DateTime PublishDateUtc { get; set; }
        public int Id { get; set; }
        public string Url { get; set; }
    }
}