﻿using System;
using System.Collections.Generic;

namespace Orneholm.NewsSearch.BatchClient
{
    public sealed class TranscriptionDefinition
    {
        private TranscriptionDefinition(string name, string description, string locale, Uri recordingsUrl, IEnumerable<ModelIdentity> models)
        {
            this.Name = name;
            this.Description = description;
            this.RecordingsUrl = recordingsUrl;
            this.Locale = locale;
            this.Models = models;
            this.properties = new Dictionary<string, string>
            {
                {"PunctuationMode", "DictatedAndAutomatic"},
                {"ProfanityFilterMode", "None"},
                {"AddWordLevelTimestamps", "True"},
                {"AddSentiment", "True"}
            };
        }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public string Description { get; set; }

        /// <inheritdoc />
        public Uri RecordingsUrl { get; set; }

        public string Locale { get; set; }

        public IEnumerable<ModelIdentity> Models { get; set; }

        public IDictionary<string, string> properties { get; set; }

        public static TranscriptionDefinition Create(
            string name,
            string description,
            string locale,
            Uri recordingsUrl)
        {
            return TranscriptionDefinition.Create(name, description, locale, recordingsUrl, new ModelIdentity[0]);
        }

        public static TranscriptionDefinition Create(
            string name,
            string description,
            string locale,
            Uri recordingsUrl,
            IEnumerable<ModelIdentity> models)
        {
            return new TranscriptionDefinition(name, description, locale, recordingsUrl, models);
        }
    }
}