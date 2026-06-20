using System;
using System.Collections.Generic;

namespace LexiconLegends.Dictionary
{
    /// <summary>
    /// Dictionary backed by precomputed per-word letter-frequency signatures, indexed by
    /// length (GDD Section 9). A word is formable from a board if, for every letter, the
    /// board's running letter count is >= the word's required count — O(1) per candidate,
    /// and the length index keeps each scan to only plausible candidates rather than the
    /// whole dictionary.
    /// </summary>
    public class WordDictionary
    {
        public readonly struct WordEntry
        {
            public readonly string Word;
            public readonly int[] Signature; // count per letter A-Z

            public WordEntry(string word, int[] signature)
            {
                Word = word;
                Signature = signature;
            }
        }

        private readonly HashSet<string> _words = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<int, List<WordEntry>> _byLength = new Dictionary<int, List<WordEntry>>();

        public int Count => _words.Count;

        public void LoadFromText(string rawText)
        {
            _words.Clear();
            _byLength.Clear();
            if (string.IsNullOrEmpty(rawText)) return;

            var lines = rawText.Split('\n');
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                AddWord(line.ToUpperInvariant());
            }
        }

        private void AddWord(string word)
        {
            if (!_words.Add(word)) return;

            var signature = BuildSignature(word);
            if (!_byLength.TryGetValue(word.Length, out var bucket))
            {
                bucket = new List<WordEntry>();
                _byLength[word.Length] = bucket;
            }
            bucket.Add(new WordEntry(word, signature));
        }

        public static int[] BuildSignature(string word)
        {
            var sig = new int[26];
            foreach (var ch in word)
            {
                int idx = char.ToUpperInvariant(ch) - 'A';
                if (idx >= 0 && idx < 26) sig[idx]++;
            }
            return sig;
        }

        public bool IsValidWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            return _words.Contains(word.ToUpperInvariant());
        }

        public IReadOnlyList<WordEntry> GetWordsOfLength(int length)
        {
            return _byLength.TryGetValue(length, out var bucket) ? bucket : Array.Empty<WordEntry>();
        }

        public bool HasWordsOfLength(int length) => _byLength.ContainsKey(length);

        /// <summary>Returns whether the board signature can form this word's signature.</summary>
        public static bool IsFormable(int[] boardSignature, int[] wordSignature)
        {
            for (int i = 0; i < 26; i++)
                if (boardSignature[i] < wordSignature[i]) return false;
            return true;
        }

        /// <summary>
        /// Counts how many distinct dictionary words (across the given length range) are
        /// formable from the board signature, stopping early once atLeast is reached.
        /// </summary>
        public int CountFormableWords(int[] boardSignature, int minLength, int maxLength, int atLeast)
        {
            int found = 0;
            for (int length = minLength; length <= maxLength; length++)
            {
                if (!_byLength.TryGetValue(length, out var bucket)) continue;
                foreach (var entry in bucket)
                {
                    if (IsFormable(boardSignature, entry.Signature))
                    {
                        found++;
                        if (found >= atLeast) return found;
                    }
                }
            }
            return found;
        }
    }
}
