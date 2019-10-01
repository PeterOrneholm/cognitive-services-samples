using System.Collections.Generic;

namespace Orneholm.BirdOrNot.Models
{
    public class BirdAnalysisMetadata
    {
        public List<string> ImageTags { get; set; }
        public string ImageDescription { get; set; }
        public string ImageAccentColor { get; set; }
    }
}