using System;
using System.Collections.Generic;
using LexiconLegends.Config;
using LexiconLegends.Dictionary;

namespace LexiconLegends.Spawn
{
    /// <summary>
    /// GDD Section 5, initial board generation Steps 1-3: pick a seed word length by the
    /// weighted table, pick a dictionary word of that length, and hand back which letters
    /// to place. Grid-position selection stays with the caller (it owns the grid shape).
    /// </summary>
    public class SeedWordSelector
    {
        private readonly LetterSpawnConfig _config;
        private readonly WordDictionary _dictionary;
        private readonly Random _rng;

        public SeedWordSelector(LetterSpawnConfig config, WordDictionary dictionary, Random rng)
        {
            _config = config;
            _dictionary = dictionary;
            _rng = rng;
        }

        /// <summary>Picks a seed word that fits within maxGridSize and the dictionary's max word length.</summary>
        public string SelectSeedWord(int maxGridSize, int maxWordLength)
        {
            var lengthWeights = new List<(int length, float weight)>
            {
                (4, _config.seedWeight4Letters),
                (5, _config.seedWeight5Letters),
                (6, _config.seedWeight6Letters),
            };
            lengthWeights.RemoveAll(lw => lw.length > maxGridSize || !_dictionary.HasWordsOfLength(lw.length));

            // "7+" bucket: the table's single weight is split evenly across whichever
            // 7+ lengths actually fit the grid and have dictionary entries.
            var sevenPlusLengths = new List<int>();
            for (int len = 7; len <= Math.Min(maxGridSize, maxWordLength); len++)
                if (_dictionary.HasWordsOfLength(len)) sevenPlusLengths.Add(len);

            if (sevenPlusLengths.Count > 0)
            {
                float perLengthWeight = _config.seedWeight7PlusLetters / sevenPlusLengths.Count;
                foreach (var len in sevenPlusLengths)
                    lengthWeights.Add((len, perLengthWeight));
            }

            if (lengthWeights.Count == 0) return null;

            float totalWeight = 0f;
            foreach (var lw in lengthWeights) totalWeight += lw.weight;

            float roll = (float)_rng.NextDouble() * totalWeight;
            float cumulative = 0f;
            int chosenLength = lengthWeights[lengthWeights.Count - 1].length;
            foreach (var lw in lengthWeights)
            {
                cumulative += lw.weight;
                if (roll <= cumulative)
                {
                    chosenLength = lw.length;
                    break;
                }
            }

            var candidates = _dictionary.GetWordsOfLength(chosenLength);
            if (candidates.Count == 0) return null;
            return candidates[_rng.Next(candidates.Count)].Word;
        }
    }
}
