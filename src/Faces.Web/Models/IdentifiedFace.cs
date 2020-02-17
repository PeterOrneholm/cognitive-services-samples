using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace Faces.Web.Models
{
    public class IdentifiedFace
    {
        public DetectedFace DetectedFace { get; set; }
        public Person Person { get; set; }
        public double Confidence { get; set; }
    }
}