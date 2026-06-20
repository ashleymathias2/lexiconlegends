using LexiconLegends.Config;

namespace LexiconLegends.Combat
{
    /// <summary>
    /// GDD Section 6 spell-type categorization. The table lists four category rules that
    /// aren't mutually exclusive (a word can be vowel-heavy AND contain a legendary
    /// letter), so this resolver applies an explicit priority order, most specific first:
    /// Legendary letter (Stagger) > Rare/Epic letter (Burn) > vowel-heavy (Restoration) >
    /// consonant-heavy (Strike) > default Strike for anything in between.
    /// </summary>
    public static class SpellResolver
    {
        public static SpellType DetermineSpellType(string word, DamageConfig config)
        {
            bool hasLegendary = false;
            bool hasRareOrEpic = false;
            int vowelCount = 0;

            foreach (var letter in word)
            {
                if (config.IsLegendary(letter)) hasLegendary = true;
                if (config.IsRareOrEpic(letter)) hasRareOrEpic = true;
                if (config.vowels.IndexOf(char.ToUpperInvariant(letter)) >= 0) vowelCount++;
            }

            if (hasLegendary) return SpellType.Stagger;
            if (hasRareOrEpic) return SpellType.Burn;

            float vowelRatio = word.Length == 0 ? 0f : (float)vowelCount / word.Length;
            float consonantRatio = 1f - vowelRatio;

            if (vowelRatio >= config.vowelCategoryThreshold) return SpellType.Restoration;
            if (consonantRatio >= config.consonantCategoryThreshold) return SpellType.Strike;

            return SpellType.Strike;
        }
    }
}
