using System.Collections.Generic;

namespace Orneholm.NewsSearch.BatchClient
{
    public class AudioFileResult
    {
        public string AudioFileName { get; set; }
        public List<SegmentResult> SegmentResults { get; set; }
    }
}