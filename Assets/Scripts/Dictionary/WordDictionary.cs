using System;
using System.Collections.Generic;

namespace LexiconLegends.Dictionary
{
    /// <summary>
    /// Stage 1 placeholder dictionary: plain HashSet lookup against a small word list.
    /// Stage 2/6 will replace the backing data with the full letter-frequency-signature
    /// indexed dictionary described in GDD Section 9, but this public API (IsValidWord)
    /// is intended to remain stable.
    /// </summary>
    public class WordDictionary
    {
        private readonly HashSet<string> _words = new HashSet<string>(StringComparer.Ordinal);

        public int Count => _words.Count;

        public void LoadFromText(string rawText)
        {
            _words.Clear();
            if (string.IsNullOrEmpty(rawText)) return;

            var lines = rawText.Split('\n');
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                _words.Add(line.ToUpperInvariant());
            }
        }

        public bool IsValidWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            return _words.Contains(word.ToUpperInvariant());
        }
    }
}
