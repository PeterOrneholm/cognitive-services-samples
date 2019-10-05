using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Shared.Protocol;
using Newtonsoft.Json;
using Orneholm.NewsSearch.Models;

namespace Orneholm.NewsSearch
{
    public class Program
    {
        private static HttpClient _httpClient;
        private static HttpClient _httpClientNoRedirect;

        // 4540
        public static async Task Main(string[] args)
        {
            _httpClient = new HttpClient();
            _httpClientNoRedirect = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false
            });

            var programId = 4540;
            await TransferSrEpisodes(programId, 100);

            Console.WriteLine("Done");
            Console.ReadLine();

        }

        private static async Task TransferSrEpisodes(int programId, int count)
        {
            var srEpisodes = await GetSrEpisodes(programId, count);

            var blobs = new Dictionary<string, string>();
            foreach (var episode in srEpisodes)
            {
                var file = episode.Broadcast.Broadcastfiles.FirstOrDefault();
                if (file != null)
                {
                    var finalUri = await GetUriAfterOneRedirect(file.Url);
                    var finalUrl = finalUri.ToString();
                    var extension = finalUrl.Substring(finalUrl.LastIndexOf('.') + 1);

                    var name = $"SR/{programId}/SR_{programId}__{episode.PublishDateUtc:yyyy-MM-dd}_{episode.PublishDateUtc:HH-mm}__{episode.Id}.{extension}";
                    blobs.Add(name, finalUrl);
                }
            }

            await TransferBlockBlobs("newsmedia", blobs);
        }

        private static async Task TransferBlockBlobs(string cloudBlobContainerName, Dictionary<string, string> blobs)
        {
            if (CloudStorageAccount.TryParse(StorageConnectionString, out var storageAccount))
            {
                var cloudBlobClient = storageAccount.CreateCloudBlobClient();

                var cloudBlobContainer = cloudBlobClient.GetContainerReference(cloudBlobContainerName);
                await cloudBlobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, new BlobRequestOptions(), new OperationContext());

                var tasks = new List<Task>();
                foreach (var blob in blobs)
                {
                    tasks.Add(TransferBlockBlobIfNotExists(cloudBlobContainer, blob.Key, blob.Value));
                }
                await Task.WhenAll(tasks);
            }
        }

        private static async Task TransferBlockBlobIfNotExists(CloudBlobContainer cloudBlobContainer, string targetBlobName, string sourceUrl)
        {
            var cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(targetBlobName);

            if (await cloudBlockBlob.ExistsAsync())
            {
                return;
            }

            var blockId = GetBase64Encoded("1");
            var sourceUri = new Uri(sourceUrl);

            cloudBlockBlob.PutBlock(blockId, sourceUri, 0, null, Checksum.None);
            await cloudBlockBlob.PutBlockListAsync(new List<string> { blockId });
        }

        private static async Task<List<Episode>> GetSrEpisodes(int programId, int count)
        {
            var httpResult = await _httpClient.GetAsync($"http://api.sr.se/api/v2/episodes/index?programid={programId}&urltemplateid=3&audioquality=hi&format=json&size={count}");
            var content = await httpResult.Content.ReadAsStringAsync();
            var episodesResult = JsonConvert.DeserializeObject<SrEpisodesResult>(content);

            return episodesResult.Episodes.ToList();
        }

        private static async Task<Uri> GetUriAfterOneRedirect(string url)
        {
            var httpResult = await _httpClientNoRedirect.GetAsync(url);
            if (httpResult.StatusCode == HttpStatusCode.Redirect
                || httpResult.StatusCode == HttpStatusCode.PermanentRedirect)
            {
                return httpResult.Headers.Location;
            }

            return new Uri(url);
        }

        private static string GetBase64Encoded(string text)
        {
            var encodedBytes = System.Text.Encoding.Unicode.GetBytes(text);
            return Convert.ToBase64String(encodedBytes);
        }
    }
}
