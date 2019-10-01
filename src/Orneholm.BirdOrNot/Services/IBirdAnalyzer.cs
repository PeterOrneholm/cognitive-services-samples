using System.Threading.Tasks;
using Orneholm.BirdOrNot.Models;

namespace Orneholm.BirdOrNot.Services
{
    public interface IBirdAnalyzer
    {
        Task<BirdAnalysisResult> AnalyzeImageFromUrlAsync(string url);
    }
}