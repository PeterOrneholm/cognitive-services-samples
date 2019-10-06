using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using Orneholm.NewsSearch.Models;

namespace Orneholm.NewsSearch
{
    public class TranscriptionEnricher
    {
        private readonly string _sourceContainerName;
        private readonly string _targetContainerName;
        private readonly CloudBlobClient _cloudBlobClient;

        public TranscriptionEnricher(string storageConnectionString, string sourceContainerName, string targetContainerName)
        {
            _sourceContainerName = sourceContainerName;
            _targetContainerName = targetContainerName;
            if (CloudStorageAccount.TryParse(storageConnectionString, out var storageAccount))
            {
                _cloudBlobClient = storageAccount.CreateCloudBlobClient();
            }
        }

        public async Task Enrich()
        {
            var cloudBlobContainer = _cloudBlobClient.GetContainerReference(_sourceContainerName);

            var srAnalyzedEpisodes = new List<SrAnalyzedEpisode>();
            foreach (var item in cloudBlobContainer.ListBlobs(null, true).OfType<CloudBlockBlob>())
            {
                await item.FetchAttributesAsync();
                if (!item.Metadata.ContainsKey("NS_Channel") || item.Metadata["NS_Channel"] != "0")
                {
                    continue;
                }

                var itemContent = await item.DownloadTextAsync();
                var parsedFile = JsonConvert.DeserializeObject<TranscribtionResultFile>(itemContent);
                var parsedItem = parsedFile.AudioFileResults.FirstOrDefault();
                var combinedResult = parsedItem?.CombinedResults.FirstOrDefault();

                var srAnalyzedEpisode = new SrAnalyzedEpisode
                {
                    ProgramId = int.Parse(item.Metadata["NS_Episode_Program_Id"]),
                    ProgramName = item.Metadata["NS_Episode_Program_Name"],

                    EpisodeId = int.Parse(item.Metadata["NS_Episode_Id"]),
                    EpisodeTitle = DecodeBase64(item.Metadata["NS_Episode_Title_B64"]),
                    EpisodeDescription = DecodeBase64(item.Metadata["NS_Episode_Description_B64"]),

                    EpisodeWebUrl = item.Metadata["NS_Episode_WebUrl"],
                    EpisodeImageUrl = item.Metadata["NS_Episode_ImageUrl"],
                    EpisodeAudioUrl = item.Metadata["NS_Episode_AudioUrl"],

                    PublishDateUtc = DateTime.Parse(item.Metadata["NS_Episode_PublishDateUtc"]),

                    AudioLengthInSeconds = parsedItem.AudioLengthInSeconds,

                    OriginalDisplayTranscription = combinedResult?.Display,

                    TranscriptionCombined = new SrAnalyzedEpisode.TranscriptionCombinedResult()
                    {
                        ChannelNumber = combinedResult?.ChannelNumber,
                        Display = combinedResult?.Display,
                        Lexical = combinedResult?.Lexical,
                        ITN = combinedResult?.ITN,
                        MaskedITN = combinedResult?.MaskedITN,
                    }
                };

                srAnalyzedEpisodes.Add(srAnalyzedEpisode);
            }

            Console.WriteLine("Enriched!");
        }

        private string DecodeBase64(string encoded)
        {
            var data = Convert.FromBase64String(encoded);
            return System.Text.Encoding.Unicode.GetString(data);
        }
    }

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