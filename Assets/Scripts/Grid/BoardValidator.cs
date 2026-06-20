using LexiconLegends.Dictionary;

namespace LexiconLegends.Grid
{
    /// <summary>
    /// GDD Section 8/9 board playability check using the letter-frequency-signature
    /// approach: O(1) formability per dictionary word against a running 26-letter board
    /// count, scanning only the length-indexed buckets rather than every word.
    /// </summary>
    public static class BoardValidator
    {
        public static int[] BuildSignature(TileView[,] tiles, int rows, int columns)
        {
            var signature = new int[26];
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
            {
                int idx = tiles[r, c].Letter - 'A';
                if (idx >= 0 && idx < 26) signature[idx]++;
            }
            return signature;
        }

        public static bool HasMinimumValidWords(int[] boardSignature, WordDictionary dictionary,
            int minWordLength, int maxWordLength, int minValidWords)
        {
            return dictionary.CountFormableWords(boardSignature, minWordLength, maxWordLength, minValidWords) >= minValidWords;
        }
    }
}
