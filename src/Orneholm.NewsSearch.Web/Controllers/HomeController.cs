﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using Orneholm.NewsSearch.Web.Models;

namespace Orneholm.NewsSearch.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly CloudBlobClient _cloudBlobClient;

        public HomeController(CloudBlobClient cloudBlobClient)
        {
            _cloudBlobClient = cloudBlobClient;
        }

        public async Task<IActionResult> Index(string entityName = null, string entityType = null)
        {
            var blobContainer = _cloudBlobClient.GetContainerReference("newsmediaepisodes");

            var srAnalyzedEpisodes = new List<SrAnalyzedEpisode>();
            foreach (var item in blobContainer.ListBlobs(null, true).OfType<CloudBlockBlob>())
            {
                var json = await item.DownloadTextAsync();
                var srAnalyzedEpisode = JsonConvert.DeserializeObject<SrAnalyzedEpisode>(json);
                srAnalyzedEpisodes.Add(srAnalyzedEpisode);
            }

            var filtered = srAnalyzedEpisodes;
            if (!string.IsNullOrWhiteSpace(entityName))
            {
                filtered = filtered
                    .Where(x => x.TranscriptionEntities.Any(y => y.Name == entityName && y.Type == entityType))
                    .ToList();
            }

            var ordered = filtered.Take(100).OrderByDescending(x => x.PublishDateUtc);

            return View(ordered.ToList());
        }
    }
}
