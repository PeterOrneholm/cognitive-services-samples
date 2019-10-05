using System;

namespace Orneholm.NewsSearch.Models
{
    public class Episode
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public SrProgram Program { get; set; }
        public string AudioPreference { get; set; }
        public string AudioPriority { get; set; }
        public string AudioPresentation { get; set; }
        public DateTime PublishDateUtc { get; set; }
        public string ImageUrl { get; set; }
        public string ImageUrlTemplate { get; set; }
        public Broadcast Broadcast { get; set; }
        public DateTime AvailableuntilUtc { get; set; }
        public Downloadpodfile Downloadpodfile { get; set; }
    }
}