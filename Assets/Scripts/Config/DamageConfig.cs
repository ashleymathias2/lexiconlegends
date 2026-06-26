using UnityEngine;

namespace LexiconLegends.Config
{
    /// <summary>
    /// All tunables for the GDD Section 6 damage formula and spell-type categorization.
    /// Letter rarity tiers here are intentionally distinct from the Section 5 spawn tiers —
    /// the GDD defines them as separate tables with different letter groupings.
    /// </summary>
    [CreateAssetMenu(fileName = "DamageConfig", menuName = "Lexicon Legends/Damage Config")]
    public class DamageConfig : ScriptableObject
    {
        [Header("Length Multiplier (Section 6)")]
        [Tooltip("Index 0 = length 3, index 1 = length 4, ... index 5 = length 8+.")]
        public float[] lengthMultipliers = { 1.0f, 1.3f, 1.7f, 2.2f, 3.0f, 4.0f };

        [Header("Letter Rarity Weight Tiers (Section 6)")]
        public string commonLetters = "EART";
        public float commonWeight = 1.0f;
        public string uncommonLetters = "DLNS";
        public float uncommonWeight = 1.2f;
        public string rareLetters = "CMPW";
        public float rareWeight = 1.5f;
        public string epicLetters = "BFVY";
        public float epicWeight = 2.0f;
        public string legendaryLetters = "JQXZ";
        public float legendaryWeight = 3.0f;
        [Tooltip("The GDD's rarity table doesn't list every letter (e.g. G,H,I,K,O,U). Unlisted letters fall back to this weight.")]
        public float defaultWeightForUnlistedLetters = 1.0f;

        [Header("Combo Multiplier (Section 6)")]
        [Tooltip("Streak 1-2.")] public float comboMultiplierLow = 1.0f;
        [Tooltip("Streak 3-4.")] public float comboMultiplierMid = 1.15f;
        [Tooltip("Streak 5+.")] public float comboMultiplierHigh = 1.3f;
        [Min(0f)] public float streakResetSeconds = 4f;

        [Header("Spell Categorization (Section 6)")]
        [Range(0f, 1f)] public float vowelCategoryThreshold = 0.6f;
        [Range(0f, 1f)] public float consonantCategoryThreshold = 0.6f;
        public string vowels = "AEIOU";

        [Header("Burn")]
        [Min(1)] public int burnDurationTurns = 3;
        [Tooltip("Portion of the word's damage value applied per burn tick.")]
        [Range(0f, 1f)] public float burnTickDamageRatio = 0.15f;

        public float GetLengthMultiplier(int wordLength)
        {
            int clampedLength = Mathf.Clamp(wordLength, 3, 8);
            int index = Mathf.Clamp(clampedLength - 3, 0, lengthMultipliers.Length - 1);
            return lengthMultipliers[index];
        }

        public float GetLetterRarityWeight(char letter)
        {
            char upper = char.ToUpperInvariant(letter);
            if (commonLetters.IndexOf(upper) >= 0) return commonWeight;
            if (uncommonLetters.IndexOf(upper) >= 0) return uncommonWeight;
            if (rareLetters.IndexOf(upper) >= 0) return rareWeight;
            if (epicLetters.IndexOf(upper) >= 0) return epicWeight;
            if (legendaryLetters.IndexOf(upper) >= 0) return legendaryWeight;
            return defaultWeightForUnlistedLetters;
        }

        public bool IsRareOrEpic(char letter)
        {
            char upper = char.ToUpperInvariant(letter);
            return rareLetters.IndexOf(upper) >= 0 || epicLetters.IndexOf(upper) >= 0;
        }

        public bool IsLegendary(char letter)
        {
            return legendaryLetters.IndexOf(char.ToUpperInvariant(letter)) >= 0;
        }

        public float GetComboMultiplier(int streak)
        {
            if (streak >= 5) return comboMultiplierHigh;
            if (streak >= 3) return comboMultiplierMid;
            return comboMultiplierLow;
        }
    }
}
