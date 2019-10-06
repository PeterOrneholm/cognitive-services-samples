using System;
using System.Threading.Tasks;
using Orneholm.NewsSearch.Models;
using Shared;

namespace Orneholm.NewsSearch
{
    public class Program
    {
        private const int SrProgramId = 2054;
        private const int EpisodesCount = 20;
        private const string EpisodesLocale = "en-US";

        private const string StorageConnectionString = SecretKeys.NewsSearchStorageConnectionString;
        private static string StorageMediaContainerName = "newsmedia";
        private const string StorageMediaTranscriptionsContainerName = "newsmediatranscriptions";

        private const string SpeechKey = SecretKeys.NewsSearchSpeechKey;
        private static string SpeechHostName = $"{SecretKeys.NewsSearchSpeechRegion}.cris.ai";


        public static async Task Main(string[] args)
        {
            var storageTransfer = new StorageTransfer(StorageConnectionString);

            var srEpisodeTransfer = new SrEpisodeTransfer(StorageMediaContainerName, storageTransfer);
            var transferedSrEpisodes = await srEpisodeTransfer.TransferSrEpisodes(SrProgramId, EpisodesCount);

            var srEpisodeTranscriber = new SrEpisodeTranscriber(StorageMediaTranscriptionsContainerName, SpeechKey, SpeechHostName, storageTransfer);
            await srEpisodeTranscriber.TranscribeAndPersist(transferedSrEpisodes, EpisodesLocale);

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
