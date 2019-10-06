using System.Collections.Generic;

namespace Orneholm.NewsSearch.BatchClient
{
    public class SegmentResult
    {
        public string RecognitionStatus { get; set; }
        public string Offset { get; set; }
        public string Duration { get; set; }
        public List<NBest> NBest { get; set; }
    }
}