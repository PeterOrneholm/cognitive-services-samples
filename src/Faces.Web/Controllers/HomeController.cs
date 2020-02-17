using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faces.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face;

namespace Faces.Web.Controllers
{
    public class HomeController : Controller
    {
        private const string PersonGroupId = "riksdag";
        private readonly FaceClient _faceClient;

        public HomeController(FaceClient faceClient)
        {
            _faceClient = faceClient;
        }

        public async Task<IActionResult> Index(string imageUrl)
        {
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                return View(new HomeIndexViewModel
                {
                    IsAnalyzed = true,
                    ImageUrl = imageUrl,
                    IdentifiedFaces = await IdentifyFaces(imageUrl)
                });
            }

            return View(new HomeIndexViewModel
            {
                IsAnalyzed = false
            });
        }

        private async Task<List<IdentifiedFace>> IdentifyFaces(string imageUrl)
        {
            var result = new List<IdentifiedFace>();

            var faces = await _faceClient.Face.DetectWithUrlAsync(imageUrl);
            var faceIds = faces.Where(face => face.FaceId.HasValue).Select(face => face.FaceId.Value).ToArray();
            var identifiedFaces = await _faceClient.Face.IdentifyAsync(faceIds, PersonGroupId);

            foreach (var identifyResult in identifiedFaces)
            {
                var identifiedFace = new IdentifiedFace
                {
                    DetectedFace = faces.FirstOrDefault(x => x.FaceId == identifyResult.FaceId)
                };

                if (identifyResult.Candidates.Any())
                {
                    var candidate = identifyResult.Candidates.First();
                    var candidateId = candidate.PersonId;
                    var person = await _faceClient.PersonGroupPerson.GetAsync(PersonGroupId, candidateId);

                    identifiedFace.Person = person;
                    identifiedFace.Confidence = candidate.Confidence;
                }

                result.Add(identifiedFace);
            }

            return result;
        }
    }
}
