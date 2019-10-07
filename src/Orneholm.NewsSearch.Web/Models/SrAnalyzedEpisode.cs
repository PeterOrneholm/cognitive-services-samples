using System;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;

namespace Orneholm.NewsSearch.Web.Models
{
    public class SrAnalyzedEpisode
    {
        public int ProgramId { get; set; }
        public string ProgramName { get; set; }

        public int EpisodeId { get; set; }
        public string EpisodeTitle { get; set; }
        public string EpisodeDescription { get; set; }

        public string EpisodeWebUrl { get; set; }
        public string EpisodeImageUrl { get; set; }
        public string EpisodeAudioUrl { get; set; }
        public DateTime PublishDateUtc { get; set; }

        public float AudioLengthInSeconds { get; set; }

        public string OriginalDisplayTranscription { get; set; }
        public Dictionary<string, string> TranslatedDisplayTranscription { get; set; }

        public List<string> TranscriptionKeyPhrases { get; set; }
        public double? TranscriptionSentiment { get; set; }
        public List<EntityRecord> TranscriptionEntities { get; set; }

        public TranscriptionCombinedResult TranscriptionCombined { get; set; }

        public class TranscriptionCombinedResult
        {
            public object ChannelNumber { get; set; }
            public string Lexical { get; set; }
            public string ITN { get; set; }
            public string MaskedITN { get; set; }
            public string Display { get; set; }
        }
    }
}
