using System.Collections.Generic;

namespace Faces.Web.Models
{
    public class HomeIndexViewModel
    {
        public bool IsAnalyzed { get; set; }

        public List<IdentifiedFace> IdentifiedFaces { get; set; }
        public string ImageUrl { get; set; }
    }
}