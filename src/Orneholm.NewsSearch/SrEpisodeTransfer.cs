using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Orneholm.NewsSearch.Models;

namespace Orneholm.NewsSearch
{
    public class SrEpisodeTransfer
    {
        private readonly string _cloudBlobContainerName;
        private readonly HttpClient _httpClient;
        private readonly HttpClient _httpClientNoRedirect;
        private readonly StorageTransfer _storageTransfer;

        public SrEpisodeTransfer(string cloudBlobContainerName, StorageTransfer storageTransfer)
        {
            _cloudBlobContainerName = cloudBlobContainerName;
            _httpClient = new HttpClient();
            _httpClientNoRedirect = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false
            });

            _storageTransfer = storageTransfer;
        }

        public async Task<List<TransferedEpisode>> TransferSrEpisodes(int programId, int count)
        {
            var srEpisodes = await GetSrEpisodes(programId, count);

            var transferedEpisodes = new List<TransferedEpisode>();

            foreach (var episode in srEpisodes)
            {
                var fileUrl = episode?.Broadcast?.Broadcastfiles.FirstOrDefault()?.Url;
                fileUrl ??= episode?.Downloadpodfile.Url;

                if (fileUrl != null)
                {
                    var finalUri = await GetUriAfterOneRedirect(fileUrl);
                    var finalUrl = finalUri.ToString();
                    var extension = finalUrl.Substring(finalUrl.LastIndexOf('.') + 1);

                    var name = $"SR/{programId}/SR_{programId}__{episode.PublishDateUtc:yyyy-MM-dd}_{episode.PublishDateUtc:HH-mm}__{episode.Id}.{extension}";
                    transferedEpisodes.Add(new TransferedEpisode
                    {
                        Episode = episode,
                        EpisodeBlobIdentifier = name,
                        OriginalAudioUri = finalUri,
                        OriginalAudioExtension = extension
                    });
                }
            }

            var blobs = transferedEpisodes.Select(x => new TransferBlob
            {
                TargetBlobIdentifier = x.EpisodeBlobIdentifier,
                SourceUrl = x.OriginalAudioUri.ToString(),
                TargetBlobMetadata = GetEpisodeMetadata(x.Episode)
            }).ToList();
            var transferBlockBlobs = await _storageTransfer.TransferBlockBlobs(_cloudBlobContainerName, blobs);

            foreach (var transferedEpisode in transferedEpisodes)
            {
                var transferBlockBlobUri = transferBlockBlobs[transferedEpisode.EpisodeBlobIdentifier];
                transferedEpisode.BlobAudioAuthenticatedUri = transferBlockBlobUri;
                transferedEpisode.BlobAudioUri = new Uri(transferBlockBlobUri.ToString().Split('?')[0]);
            }

            return transferedEpisodes;
        }

        private async Task<List<Episode>> GetSrEpisodes(int programId, int count)
        {
            var httpResult = await _httpClient.GetAsync($"http://api.sr.se/api/v2/episodes/index?programid={programId}&urltemplateid=3&audioquality=hi&format=json&size={count}");
            var content = await httpResult.Content.ReadAsStringAsync();
            var episodesResult = JsonConvert.DeserializeObject<SrEpisodesResult>(content);

            return episodesResult.Episodes.ToList();
        }

        private async Task<Uri> GetUriAfterOneRedirect(string url)
        {
            var httpResult = await _httpClientNoRedirect.GetAsync(url);
            if (httpResult.StatusCode == HttpStatusCode.Redirect
                || httpResult.StatusCode == HttpStatusCode.PermanentRedirect)
            {
                return httpResult.Headers.Location;
            }

            return new Uri(url);
        }

        public static Dictionary<string, string> GetEpisodeMetadata(Episode episode)
        {
            return new Dictionary<string, string>
            {
                { "NS_Episode_Program_Id", episode.Program.Id.ToString() },
                { "NS_Episode_Program_Name", episode.Program.Name },
                { "NS_Episode_Id", episode.Id.ToString() },
                { "NS_Episode_WebUrl", episode.Url },
                { "NS_Episode_ImageUrl", episode.ImageUrl },
                { "NS_Episode_AudioUrl", episode.Downloadpodfile?.Url },
                { "NS_Episode_Title_B64", GetBase64Encoded(episode.Title) },
                { "NS_Episode_Description_B64", GetBase64Encoded(episode.Description) },
                { "NS_Episode_PublishDateUtc", episode.PublishDateUtc.ToString("yyyy-MM-dd HH:mm") },
            };
        }

        private static string GetBase64Encoded(string text)
        {
            var encodedBytes = System.Text.Encoding.Unicode.GetBytes(text);
            return Convert.ToBase64String(encodedBytes);
        }
    }
}
