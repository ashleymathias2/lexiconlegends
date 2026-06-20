using UnityEngine;

namespace LexiconLegends.Config
{
    public enum DifficultyStage { Early, Mid, Late }

    /// <summary>
    /// All tunables for the GDD Section 5 letter spawn system, exposed for playtesting.
    /// </summary>
    [CreateAssetMenu(fileName = "LetterSpawnConfig", menuName = "Lexicon Legends/Letter Spawn Config")]
    public class LetterSpawnConfig : ScriptableObject
    {
        [Header("Letter Tiers")]
        public string vowelLetters = "AEIOU";
        public string commonLetters = "RTLNS";
        public string mediumLetters = "DGHMP";
        public string rareLetters = "BCFWY";
        public string legendaryLetters = "JQXZ";

        [Header("Base Spawn Chances (sum to 1.0)")]
        [Range(0f, 1f)] public float baseVowelChance = 0.35f;
        [Range(0f, 1f)] public float baseCommonChance = 0.30f;
        [Range(0f, 1f)] public float baseMediumChance = 0.20f;
        [Range(0f, 1f)] public float baseRareChance = 0.10f;
        [Range(0f, 1f)] public float baseLegendaryChance = 0.05f;

        [Header("Vowel Balance Rule")]
        [Min(0)] public int minVowelsOnBoard = 6;
        [Min(0)] public int maxVowelsOnBoard = 9;

        [Header("Rare Letter (Legendary) Cooldown")]
        [Min(0)] public int legendaryCooldownSpawns = 6;

        [Header("Large Word Reward")]
        [Min(1)] public int largeWordRewardLength = 6;
        [Min(0)] public int largeWordRewardLetterCount = 1;

        [Header("Difficulty Scaling")]
        public DifficultyStage currentStage = DifficultyStage.Early;

        [Range(0f, 1f)] public float earlyVowelChance = 0.40f;
        [Range(0f, 1f)] public float earlyLegendaryChance = 0.03f;

        [Range(0f, 1f)] public float midVowelChance = 0.35f;
        [Range(0f, 1f)] public float midLegendaryChance = 0.05f;

        [Range(0f, 1f)] public float lateVowelChance = 0.30f;
        [Range(0f, 1f)] public float lateLegendaryChance = 0.07f;

        [Header("Seed Word Selection (Section 5, Step 1)")]
        [Min(0f)] public float seedWeight4Letters = 0.50f;
        [Min(0f)] public float seedWeight5Letters = 0.30f;
        [Min(0f)] public float seedWeight6Letters = 0.15f;
        [Min(0f)] public float seedWeight7PlusLetters = 0.05f;

        [Header("Board Playability")]
        [Min(1)] public int minValidWordsAfterRefill = 3;
        [Min(1)] public int maxRefillRetries = 5;

        public void GetStageChances(out float vowelChance, out float legendaryChance)
        {
            switch (currentStage)
            {
                case DifficultyStage.Mid:
                    vowelChance = midVowelChance;
                    legendaryChance = midLegendaryChance;
                    return;
                case DifficultyStage.Late:
                    vowelChance = lateVowelChance;
                    legendaryChance = lateLegendaryChance;
                    return;
                default:
                    vowelChance = earlyVowelChance;
                    legendaryChance = earlyLegendaryChance;
                    return;
            }
        }
    }
}
