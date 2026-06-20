using LexiconLegends.Config;

namespace LexiconLegends.Combat
{
    /// <summary>
    /// GDD Section 6 / Section 9 reference implementation:
    /// damage = lengthMult * avgRarity * comboMult.
    /// </summary>
    public static class DamageCalculator
    {
        public static float GetAverageRarityWeight(string word, DamageConfig config)
        {
            float sum = 0f;
            foreach (var letter in word)
                sum += config.GetLetterRarityWeight(letter);
            return word.Length == 0 ? 0f : sum / word.Length;
        }

        public static float ComputeDamage(string word, float comboMultiplier, DamageConfig config,
            out float lengthMultiplier, out float averageRarityWeight)
        {
            lengthMultiplier = config.GetLengthMultiplier(word.Length);
            averageRarityWeight = GetAverageRarityWeight(word, config);
            return lengthMultiplier * averageRarityWeight * comboMultiplier;
        }
    }
}
