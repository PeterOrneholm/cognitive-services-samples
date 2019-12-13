using System;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;

namespace Personalizer.Models
{
    public class PersonalizerModel
    {
        public string PersonalizerEventId { get; set; }
        public DateTime PersonalizerEventStartTime { get; set; }


        public string Action { get; set; }
        public string TimeOfDay { get; set; }
        public string UserOs { get; set; }
        public IList<RankedAction> Ranking { get; set; }
    }
}
