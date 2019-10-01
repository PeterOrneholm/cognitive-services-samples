using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Orneholm.BirdOrNot.Models;
using Orneholm.BirdOrNot.Services;

namespace Orneholm.BirdOrNot.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBirdAnalyzer _birdAnalyzer;

        public HomeController(IBirdAnalyzer birdAnalyzer)
        {
            _birdAnalyzer = birdAnalyzer;
        }

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
                    viewModel.Result = await _birdAnalyzer.AnalyzeImageFromUrlAsync(imageUrl);
                }
                catch (Exception)
                {
                    viewModel.Result = null;
                }
            }

            return View(viewModel);
        }
    }
}
