using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orneholm.NewsSearch.Models;
using Shared;

namespace Orneholm.NewsSearch
{
    public class Program
    {
        private const int SrProgramId = 2054;
        private const int EpisodesCount = 50;
        private const string EpisodesLocale = "en-US";

        private const string StorageConnectionString = SecretKeys.NewsSearchStorageConnectionString;
        private static string StorageMediaContainerName = "newsmedia";
        private const string StorageMediaTranscriptionsContainerName = "newsmediatranscriptions";
        private const string StorageMediaEpisodesContainerName = "newsmediaepisodes";

        private const string SpeechKey = SecretKeys.NewsSearchSpeechKey;
        private static string SpeechHostName = $"{SecretKeys.NewsSearchSpeechRegion}.cris.ai";

        private const string TextAnalyticsKey = SecretKeys.NewsSearchTextAnalyticsKey;
        private static string TextAnalyticsEndpoint = SecretKeys.NewsSearchTextAnalyticsEndpoint;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("News Search!");
            Console.WriteLine("----------------------------");
            Console.WriteLine("");

            var storageTransfer = new StorageTransfer(StorageConnectionString);

            Console.WriteLine($"Transfering episodes from SR (program {SrProgramId})...");
            var srEpisodeTransfer = new SrEpisodeTransfer(StorageMediaContainerName, storageTransfer);
            var transferedSrEpisodes = await srEpisodeTransfer.TransferSrEpisodes(SrProgramId, EpisodesCount);
            Console.WriteLine($"{transferedSrEpisodes.Count} episodes transfered from SR (program {SrProgramId})!");

            Console.WriteLine("");
            Console.WriteLine("");

            var filteredTransferedSrEpisodes = await FilterTransferedSrEpisodes(transferedSrEpisodes, storageTransfer);

            var srEpisodeTranscriber = new SrEpisodeTranscriber(StorageMediaTranscriptionsContainerName, StorageMediaContainerName, SpeechKey, SpeechHostName, storageTransfer);
            await srEpisodeTranscriber.TranscribeAndPersist(filteredTransferedSrEpisodes, EpisodesLocale);

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Enriching transcriptions...");

            var transcriptionEnricher = new TranscriptionEnricher(StorageConnectionString, StorageMediaTranscriptionsContainerName, StorageMediaEpisodesContainerName, TextAnalyticsKey, TextAnalyticsEndpoint);
            await transcriptionEnricher.Enrich();

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        private static async Task<List<TransferedEpisode>> FilterTransferedSrEpisodes(List<TransferedEpisode> transferedSrEpisodes, StorageTransfer storageTransfer)
        {
            var filteredTransferedSrEpisodes = new List<TransferedEpisode>();
            foreach (var transferedSrEpisode in transferedSrEpisodes)
            {
                var metadata = await storageTransfer.GetMetadataValues(StorageMediaContainerName, transferedSrEpisode.EpisodeBlobIdentifier);
                if (!metadata.ContainsKey("NS_IsTranscribed") || metadata["NS_IsTranscribed"] != "True")
                {
                    filteredTransferedSrEpisodes.Add(transferedSrEpisode);
                }
            }

            return filteredTransferedSrEpisodes;
        }
    }
}
