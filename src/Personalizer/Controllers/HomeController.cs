using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Personalizer;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;
using Microsoft.Extensions.Configuration;
using Personalizer.Models;
using UAParser;

namespace Personalizer.Controllers
{
    public class HomeController : Controller
    {
        private readonly PersonalizerClient _personalizerClient;
        public HomeController(IConfiguration configuration)
        {
            var personalizerApiKey = configuration["PersonalizerApiKey"];
            var personalizerServiceEndpoint = configuration["PersonalizerServiceEndpoint"];

            _personalizerClient = new PersonalizerClient(new ApiKeyServiceClientCredentials(personalizerApiKey))
            {
                Endpoint = personalizerServiceEndpoint
            };
        }

        public async Task<IActionResult> Index()
        {
            var actions = GetActions();

            var timeOfDayFeature = GetTimeOfDay();
            var userOsFeature = GetUserOs(Request.Headers["User-Agent"]);

            var currentContext = new List<object> {
                new { time = timeOfDayFeature },
                new { userOs = userOsFeature }
            };

            var request = new RankRequest(actions, currentContext);
            var response = await _personalizerClient.RankAsync(request);

            return View(new PersonalizerModel
            {
                PersonalizerEventId = response.EventId,
                PersonalizerEventStartTime = DateTime.UtcNow,

                Action = response.RewardActionId,
                TimeOfDay = timeOfDayFeature,
                UserOs = userOsFeature,
                Ranking = response.Ranking
            });
        }

        public IActionResult Checkout(PersonalizerModel model)
        {
            if (!string.IsNullOrEmpty(model.PersonalizerEventId))
            {
                var time = DateTime.UtcNow - model.PersonalizerEventStartTime;
                var timePercentage = time.TotalSeconds / 30;

                var reward = Math.Max(0.0, Math.Min(1.0, 1.0 - timePercentage));
                _personalizerClient.RewardAsync(model.PersonalizerEventId, reward);
            }

            return View();
        }


        static string GetTimeOfDay()
        {
            var hour = DateTime.UtcNow.Hour;

            if (hour > 6 && hour < 12)
            {
                return "morning";
            }
            
            if (hour >= 12 && hour < 18)
            {
                return "afternoon";
            }
            
            if (hour >= 18 && hour < 22)
            {
                return "evening";
            }

            return "night";
        }

        static string GetUserOs(string uaString)
        {
            var uaParser = Parser.GetDefault();
            var clientInfo = uaParser.Parse(uaString);
            return clientInfo.OS.Family;
        }

        static IList<RankableAction> GetActions()
        {
            IList<RankableAction> actions = new List<RankableAction>
            {
                new RankableAction
                {
                    Id = "Buy now!",
                    Features =
                    new List<object>
                    {
                        new { mode = "strict", learnMore = false }
                    }
                },

                new RankableAction
                {
                    Id = "Take my money!",
                    Features =
                    new List<object>
                    {
                        new { mode = "informal", learnMore = false }
                    }
                },

                new RankableAction
                {
                    Id = "What's up with all this?",
                    Features =
                        new List<object>
                        {
                            new { mode = "informal", learnMore = true }
                        }
                },

                new RankableAction
                {
                    Id = "I want to learn more",
                    Features =
                    new List<object>
                    {
                        new { mode = "strict", learnMore = true }
                    }
                }
            };

            return actions;
        }
    }
}
