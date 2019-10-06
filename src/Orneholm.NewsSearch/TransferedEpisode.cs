using System;
using Orneholm.NewsSearch.Models;

namespace Orneholm.NewsSearch
{
    public class TransferedEpisode
    {
        public Episode Episode { get; set; }
        public string EpisodeBlobIdentifier { get; set; }
        public Uri OriginalAudioUri { get; set; }
        public string OriginalAudioExtension { get; set; }
        public Uri BlobAudioAuthenticatedUri { get; set; }
        public Uri BlobAudioUri { get; set; }
    }
}