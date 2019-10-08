using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Rest;
using Newtonsoft.Json;
using Orneholm.NewsSearch.Models;

namespace Orneholm.NewsSearch
{
    public class TranscriptionEnricher
    {
        private readonly string _sourceContainerName;
        private readonly string _targetContainerName;
        private readonly CloudBlobClient _cloudBlobClient;
        private readonly TextAnalyticsClient _textAnalyticsClient;

        public TranscriptionEnricher(string storageConnectionString, string sourceContainerName, string targetContainerName, string textAnalyticsKey, string textAnalyticsEndpoint)
        {
            _sourceContainerName = sourceContainerName;
            _targetContainerName = targetContainerName;
            if (CloudStorageAccount.TryParse(storageConnectionString, out var storageAccount))
            {
                _cloudBlobClient = storageAccount.CreateCloudBlobClient();
            }

            var credentials = new ApiKeyServiceClientCredentials(textAnalyticsKey);
            _textAnalyticsClient = new TextAnalyticsClient(credentials)
            {
                Endpoint = textAnalyticsEndpoint
            };
        }

        public async Task Enrich()
        {
            var sourceBlobContainer = _cloudBlobClient.GetContainerReference(_sourceContainerName);
            var targetBlobContainer = _cloudBlobClient.GetContainerReference(_targetContainerName);
            await targetBlobContainer.CreateIfNotExistsAsync();


            var srAnalyzedEpisodes = new List<SrAnalyzedEpisode>();
            var blobs = sourceBlobContainer.ListBlobs(null, true).OfType<CloudBlockBlob>().ToList();
            var index = 0;
            foreach (var item in blobs)
            {
                index++;
                await item.FetchAttributesAsync();

                if (!item.Metadata.ContainsKey("NS_Channel") || item.Metadata["NS_Channel"] != "0")
                {
                    continue;
                }

                if (item.Metadata.ContainsKey("NS_Enriched") && item.Metadata["NS_Enriched"] != "1")
                {
                    Console.WriteLine($"[{index}/{blobs.Count}] {item.Name} already enriched!");
                    continue;
                }

                Console.WriteLine($"[{index}/{blobs.Count}] Encriching {item.Name}...");

                var itemContent = await item.DownloadTextAsync();
                var parsedFile = JsonConvert.DeserializeObject<TranscribtionResultFile>(itemContent);
                var parsedItem = parsedFile.AudioFileResults.FirstOrDefault();
                var combinedResult = parsedItem?.CombinedResults.FirstOrDefault();

                var srAnalyzedEpisode = GetSrAnalyzedEpisode(item, parsedItem, combinedResult);

                await EncirchWithAnalytics(srAnalyzedEpisode);

                srAnalyzedEpisodes.Add(srAnalyzedEpisode);

                var episodeJson = JsonConvert.SerializeObject(srAnalyzedEpisode, Formatting.Indented);

                var targetBlob = targetBlobContainer.GetBlockBlobReference(item.Name);
                await targetBlob.UploadTextAsync(episodeJson);

                item.Metadata["NS_Enriched"] = "1";
                await item.SetMetadataAsync();

                Console.WriteLine($"[{index}/{blobs.Count}] Encriched {item.Name}!");
            }
        }

        private async Task EncirchWithAnalytics(SrAnalyzedEpisode srAnalyzedEpisode)
        {
            var limitedText = srAnalyzedEpisode.OriginalDisplayTranscription.Substring(0,
                Math.Min(5120, srAnalyzedEpisode.OriginalDisplayTranscription.Length));

            var keyPhrases = await _textAnalyticsClient.KeyPhrasesAsync(limitedText);
            srAnalyzedEpisode.TranscriptionKeyPhrases = keyPhrases.KeyPhrases?.ToList() ?? new List<string>();

            var sentimentAsync = await _textAnalyticsClient.SentimentAsync(limitedText);
            srAnalyzedEpisode.TranscriptionSentiment = sentimentAsync.Score;

            var entities = await _textAnalyticsClient.EntitiesAsync(limitedText);
            srAnalyzedEpisode.TranscriptionEntities = entities.Entities?.ToList() ?? new List<EntityRecord>();
        }

        private SrAnalyzedEpisode GetSrAnalyzedEpisode(CloudBlockBlob item, AudioFileResult parsedItem,
            Combinedresult combinedResult)
        {
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
            return srAnalyzedEpisode;
        }

        private string DecodeBase64(string encoded)
        {
            var data = Convert.FromBase64String(encoded);
            return System.Text.Encoding.Unicode.GetString(data);
        }

        private class ApiKeyServiceClientCredentials : ServiceClientCredentials
        {
            private readonly string apiKey;

            public ApiKeyServiceClientCredentials(string apiKey)
            {
                this.apiKey = apiKey;
            }

            public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request == null)
                {
                    throw new ArgumentNullException("request");
                }
                request.Headers.Add("Ocp-Apim-Subscription-Key", this.apiKey);
                return base.ProcessHttpRequestAsync(request, cancellationToken);
            }
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