namespace Orneholm.BirdOrNot.Models
{
    public class HomeIndexViewModel
    {
        public string ImageUrl { get; set; }
        public bool HasResult => Result != null;
        public bool IsInvalid => ImageUrl != null && Result == null;
        public BirdAnalysisResult Result { get; set; }
    }
}