using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orneholm.NewsSearch.BatchClient;

namespace Orneholm.NewsSearch
{
    public class SrEpisodeTranscriber
    {
        private readonly string _transcriptionsContainerName;
        private readonly string _mediaContainerName;
        private readonly StorageTransfer _storageTransfer;
        private readonly SpeechBatchClient _speechBatchClient;

        public SrEpisodeTranscriber(string transcriptionsContainerName, string mediaContainerName, string searchSpeechKey, string searchHostName, StorageTransfer storageTransfer)
        {
            _transcriptionsContainerName = transcriptionsContainerName;
            _mediaContainerName = mediaContainerName;
            _storageTransfer = storageTransfer;
            _speechBatchClient = SpeechBatchClient.CreateApiV2Client(searchSpeechKey, searchHostName, 443);
        }

        public async Task TranscribeAndPersist(List<TransferedEpisode> srEpisodes, string audioLocale)
        {
            await CleanExistingTranscribtions();

            var episodeTranscriptions = await TranscribeEpisodes(srEpisodes, audioLocale);
            await TransferTranscribedEpisodes(episodeTranscriptions);
        }

        private async Task TransferTranscribedEpisodes(Dictionary<Guid, TransferedEpisode> episodeTranscriptions)
        {
            var completed = 0;
            while (completed < episodeTranscriptions.Count)
            {
                var transcriptions = (await _speechBatchClient.GetTranscriptionsAsync()).ToList();

                Console.WriteLine("Checking transcription status...");

                foreach (var transcription in transcriptions.Where(x => x.Status == "Failed" || x.Status == "Succeeded"))
                {
                    if (!episodeTranscriptions.ContainsKey(transcription.Id))
                    {
                        continue;
                    }

                    completed++;

                    var transferedEpisode = episodeTranscriptions[transcription.Id];

                    if (transcription.Status == "Succeeded")
                    {
                        Console.WriteLine($"[{completed} / {episodeTranscriptions.Count}] Transcribed {transferedEpisode.BlobAudioUri}!");

                        await TransferResultForChannel(transferedEpisode, transcription, "0");
                        await TransferResultForChannel(transferedEpisode, transcription, "1");

                        await _storageTransfer.SetMetadata(_mediaContainerName, transferedEpisode.EpisodeBlobIdentifier,
                            new Dictionary<string, string>
                            {
                                { "NS_IsTranscribed", "True" }
                            });
                    }
                    else
                    {
                        Console.WriteLine($"Error transcribing {transferedEpisode.BlobAudioUri}!");
                    }

                    await _speechBatchClient.DeleteTranscriptionAsync(transcription.Id);
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        private async Task<Dictionary<Guid, TransferedEpisode>> TranscribeEpisodes(List<TransferedEpisode> srEpisodes, string audioLocale)
        {
            var episodeTranscriptions = new Dictionary<Guid, TransferedEpisode>();
            foreach (var episode in srEpisodes)
            {
                Console.WriteLine($"Transcribing {episode.BlobAudioUri}...");

                var transcriptionLocation = await _speechBatchClient.PostTranscriptionAsync(
                    $"NewsSearch - {episode.EpisodeBlobIdentifier}",
                    "NewsSearch",
                    audioLocale,
                    episode.BlobAudioAuthenticatedUri
                );
                var transcriptionGuid = new Guid(transcriptionLocation.ToString().Split('/').LastOrDefault());

                episodeTranscriptions.Add(transcriptionGuid, episode);
            }

            return episodeTranscriptions;
        }

        private async Task TransferResultForChannel(TransferedEpisode transferedEpisode, Transcription transcription, string channel)
        {
            if (transcription.ResultsUrls.ContainsKey($"channel_{channel}"))
            {
                var targetBlobPrefix = transferedEpisode.EpisodeBlobIdentifier + "__Transcription_";
                var metadata = SrEpisodeTransfer.GetEpisodeMetadata(transferedEpisode.Episode);
                metadata.Add("NS_Channel", channel);

                var resultsUri = transcription.ResultsUrls[$"channel_{channel}"];
                var targetBlobUrl = $"{targetBlobPrefix}{channel}.json";

                await _storageTransfer.TransferBlockBlobIfNotExists(
                    _transcriptionsContainerName,
                    targetBlobUrl,
                    resultsUri,
                    metadata
                );
            }
        }

        private async Task CleanExistingTranscribtions()
        {
            var transcriptions = await _speechBatchClient.GetTranscriptionsAsync();
            foreach (var transcription in transcriptions)
            {
                await _speechBatchClient.DeleteTranscriptionAsync(transcription.Id);
            }
        }
    }
}