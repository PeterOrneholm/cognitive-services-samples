using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Orneholm.NewsSearch.BatchClient
{


    public class SpeechBatchClient
    {
        private const string OneAPIOperationLocationHeaderKey = "Operation-Location";

        private readonly HttpClient client;
        private readonly string speechToTextBasePath;

        private SpeechBatchClient(HttpClient client)
        {
            this.client = client;
            speechToTextBasePath = "api/speechtotext/v2.0/";
        }

        public static SpeechBatchClient CreateApiV2Client(string key, string hostName, int port)
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(25);
            client.BaseAddress = new UriBuilder(Uri.UriSchemeHttps, hostName, port).Uri;

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);

            return new SpeechBatchClient(client);
        }

        public Task<IEnumerable<Transcription>> GetTranscriptionsAsync()
        {
            var path = $"{this.speechToTextBasePath}Transcriptions";
            return this.GetAsync<IEnumerable<Transcription>>(path);
        }

        public Task<Transcription> GetTranscriptionAsync(Guid id)
        {
            var path = $"{this.speechToTextBasePath}Transcriptions/{id}";
            return this.GetAsync<Transcription>(path);
        }

        public Task<Uri> PostTranscriptionAsync(string name, string description, string locale, Uri recordingsUrl)
        {
            var path = $"{this.speechToTextBasePath}Transcriptions/";
            var transcriptionDefinition = TranscriptionDefinition.Create(name, description, locale, recordingsUrl);

            return this.PostAsJsonAsync<TranscriptionDefinition>(path, transcriptionDefinition);
        }

        public Task<Uri> PostTranscriptionAsync(string name, string description, string locale, Uri recordingsUrl, IEnumerable<Guid> modelIds)
        {
            if (!modelIds.Any())
            {
                return this.PostTranscriptionAsync(name, description, locale, recordingsUrl);
            }

            var models = modelIds.Select(m => ModelIdentity.Create(m)).ToList();
            var path = $"{this.speechToTextBasePath}Transcriptions/";

            var transcriptionDefinition = TranscriptionDefinition.Create(name, description, locale, recordingsUrl, models);
            return this.PostAsJsonAsync<TranscriptionDefinition>(path, transcriptionDefinition);
        }

        public Task<Transcription> GetTranscriptionAsync(Uri location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            return this.GetAsync<Transcription>(location.AbsolutePath);
        }

        public Task DeleteTranscriptionAsync(Guid id)
        {
            var path = $"{this.speechToTextBasePath}Transcriptions/{id}";
            return this.client.DeleteAsync(path);
        }

        private static async Task<Uri> GetLocationFromPostResponseAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw await CreateExceptionAsync(response).ConfigureAwait(false);
            }

            IEnumerable<string> headerValues;
            if (response.Headers.TryGetValues(OneAPIOperationLocationHeaderKey, out headerValues))
            {
                if (headerValues.Any())
                {
                    return new Uri(headerValues.First());
                }
            }

            return response.Headers.Location;
        }

        private async Task<Uri> PostAsJsonAsync<TPayload>(string path, TPayload payload)
        {

            string res = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            StringContent sc = new StringContent(res);
            sc.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            using (var response = await client.PostAsync(path, sc))
            {
                return await GetLocationFromPostResponseAsync(response).ConfigureAwait(false);
            }
        }

        private async Task<TResponse> GetAsync<TResponse>(string path)
        {
            using (var response = await this.client.GetAsync(path).ConfigureAwait(false))
            {
                var contentType = response.Content.Headers.ContentType;

                if (response.IsSuccessStatusCode && string.Equals(contentType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase))
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<TResponse>(content);

                    return result;
                }

                throw new NotImplementedException();
            }
        }

        private static async Task<FailedHttpClientRequestException> CreateExceptionAsync(HttpResponseMessage response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.Forbidden:
                    return new FailedHttpClientRequestException(response.StatusCode, "No permission to access this resource.");
                case HttpStatusCode.Unauthorized:
                    return new FailedHttpClientRequestException(response.StatusCode, "Not authorized to see the resource.");
                case HttpStatusCode.NotFound:
                    return new FailedHttpClientRequestException(response.StatusCode, "The resource could not be found.");
                case HttpStatusCode.UnsupportedMediaType:
                    return new FailedHttpClientRequestException(response.StatusCode, "The file type isn't supported.");
                case HttpStatusCode.BadRequest:
                    {
                        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var shape = new { Message = string.Empty };
                        var result = JsonConvert.DeserializeAnonymousType(content, shape);
                        if (result != null && !string.IsNullOrEmpty(result.Message))
                        {
                            return new FailedHttpClientRequestException(response.StatusCode, result.Message);
                        }

                        return new FailedHttpClientRequestException(response.StatusCode, response.ReasonPhrase);
                    }

                default:
                    return new FailedHttpClientRequestException(response.StatusCode, response.ReasonPhrase);
            }
        }
    }
}