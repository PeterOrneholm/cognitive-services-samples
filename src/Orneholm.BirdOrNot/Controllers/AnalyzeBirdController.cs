using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Orneholm.BirdOrNot.Models;
using Orneholm.BirdOrNot.Services;

namespace Orneholm.BirdOrNot.Controllers
{
    [ApiController]
    [Route("api/AnalyzeBird")]
    public class AnalyzeBirdController : Controller
    {
        private readonly IBirdAnalyzer _birdAnalyzer;

        public AnalyzeBirdController(IBirdAnalyzer birdAnalyzer)
        {
            _birdAnalyzer = birdAnalyzer;
        }

        [HttpGet("FromUrl")]
        public async Task<ActionResult<BirdAnalysisResult>> ImageFromUrl(string url)
        {
            return await _birdAnalyzer.AnalyzeImageFromUrlAsync(url);
        }
    }
}
