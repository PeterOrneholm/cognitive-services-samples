using System;

namespace Orneholm.NewsSearch.Models
{
    public class TranscribtionResultFile
    {
        public AudioFileResult[] AudioFileResults { get; set; }
    }

    public class AudioFileResult
    {
        public string AudioFileName { get; set; }
        public object AudioFileUrl { get; set; }
        public float AudioLengthInSeconds { get; set; }
        public Combinedresult[] CombinedResults { get; set; }
        public Segmentresult[] SegmentResults { get; set; }
    }

    public class Combinedresult
    {
        public object ChannelNumber { get; set; }
        public string Lexical { get; set; }
        public string ITN { get; set; }
        public string MaskedITN { get; set; }
        public string Display { get; set; }
    }

    public class Segmentresult
    {
        public string RecognitionStatus { get; set; }
        public object ChannelNumber { get; set; }
        public object SpeakerId { get; set; }
        public Int64 Offset { get; set; }
        public int Duration { get; set; }
        public float OffsetInSeconds { get; set; }
        public float DurationInSeconds { get; set; }
        public Nbest[] NBest { get; set; }
    }

    public class Nbest
    {
        public float Confidence { get; set; }
        public string Lexical { get; set; }
        public string ITN { get; set; }
        public string MaskedITN { get; set; }
        public string Display { get; set; }
        public Sentiment Sentiment { get; set; }
        public WordObject[] Words { get; set; }
    }

    public class Sentiment
    {
        public float Negative { get; set; }
        public float Neutral { get; set; }
        public float Positive { get; set; }
    }

    public class WordObject
    {
        public string Word { get; set; }
        public Int64 Offset { get; set; }
        public int Duration { get; set; }
        public float OffsetInSeconds { get; set; }
        public float DurationInSeconds { get; set; }
    }

}
