using System.Collections.Generic;

namespace Orneholm.BirdOrNot.Models
{
    public class BirdAnalysisResult
    {
        public bool IsBird { get; set; }
        public double? IsBirdConfidence { get; set; }

        public List<BirdAnalysisBird> Birds { get; set; }

        public BirdAnalysisMetadata Metadata { get; set; }
    }
}