using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Orneholm.BirdOrNot.Models;
using Orneholm.BirdOrNot.Services;

namespace Orneholm.BirdOrNot.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBirdAnalyzer _birdAnalyzer;
        private readonly TelemetryClient _telemetryClient;

        public HomeController(IBirdAnalyzer birdAnalyzer, TelemetryClient telemetryClient)
        {
            _birdAnalyzer = birdAnalyzer;
            _telemetryClient = telemetryClient;
        }

        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        public async Task<ActionResult<BirdAnalysisResult>> Index(string imageUrl)
        {
            var viewModel = new HomeIndexViewModel
            {
                ImageUrl = imageUrl
            };

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                try
                {
                    var result = await _birdAnalyzer.AnalyzeImageFromUrlAsync(imageUrl);
                    viewModel.Result = result;
                    _telemetryClient.TrackEvent("BON_ImageAnalyzed", new Dictionary<string, string>
                    {
                        { "BON_ImageUrl", imageUrl },
                        { "BON_IsBird", result.IsBird.ToString() },
                        { "BON_BirdCount", result.Birds.Count.ToString() },
                        { "BON_IsBirdConfidence", result.IsBirdConfidence.ToString() },
                        { "BON_ImageDescription", result.Metadata.ImageDescription },
                    });
                }
                catch (Exception ex)
                {
                    viewModel.Result = null;
                    _telemetryClient.TrackException(ex, new Dictionary<string, string>
                    {
                        { "BON_ImageUrl", imageUrl }
                    });
                }
            }

            return View(viewModel);
        }
    }
}
