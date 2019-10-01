namespace Orneholm.BirdOrNot.Models
{
    public class BirdAnalysisBird
    {
        public string BirdSpiecies { get; set; }
        public double? IsBirdSpieciesConfidence { get; set; }
        public double? IsBirdConfidence { get; set; }
        public double? IsAnimalConfidence { get; set; }

        public BoundingRect Rectangle { get; set; }
    }
}