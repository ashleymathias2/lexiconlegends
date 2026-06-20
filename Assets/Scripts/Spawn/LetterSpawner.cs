using System;
using LexiconLegends.Config;

namespace LexiconLegends.Spawn
{
    public enum LetterTier { Vowel, Common, Medium, Rare, Legendary }

    /// <summary>
    /// Implements GDD Section 5: weighted letter distribution, the vowel balance rule,
    /// and the rare (legendary) letter cooldown. One instance tracks cooldown state across
    /// an entire board's lifetime; vowel-count state is tracked by the caller and passed in
    /// per spawn since the caller (the grid) is the source of truth for current board state.
    /// </summary>
    public class LetterSpawner
    {
        private readonly LetterSpawnConfig _config;
        private readonly Random _rng;
        private int _legendaryCooldownRemaining;

        public LetterSpawner(LetterSpawnConfig config, Random rng)
        {
            _config = config;
            _rng = rng;
        }

        public LetterTier LastSpawnedTier { get; private set; }

        /// <summary>
        /// Spawns the next letter given the board's current vowel count. Enforces the
        /// vowel balance rule (force vowel below minimum, suppress vowel at/above maximum)
        /// before falling back to the difficulty-scaled weighted distribution.
        /// </summary>
        public char SpawnLetter(int currentVowelCount)
        {
            if (currentVowelCount < _config.minVowelsOnBoard)
            {
                LastSpawnedTier = LetterTier.Vowel;
                TickCooldown(spawnedLegendary: false);
                return RandomFrom(_config.vowelLetters);
            }

            bool suppressVowel = currentVowelCount >= _config.maxVowelsOnBoard;
            bool suppressLegendary = _legendaryCooldownRemaining > 0;

            var tier = PickWeightedTier(suppressVowel, suppressLegendary);
            LastSpawnedTier = tier;
            TickCooldown(spawnedLegendary: tier == LetterTier.Legendary);
            return RandomFrom(LettersForTier(tier));
        }

        /// <summary>Forces a letter from the Rare tier (used by the Section 5 large-word reward).</summary>
        public char SpawnGuaranteedRareLetter()
        {
            LastSpawnedTier = LetterTier.Rare;
            TickCooldown(spawnedLegendary: false);
            return RandomFrom(_config.rareLetters);
        }

        public char SpawnGuaranteedVowel()
        {
            LastSpawnedTier = LetterTier.Vowel;
            TickCooldown(spawnedLegendary: false);
            return RandomFrom(_config.vowelLetters);
        }

        private void TickCooldown(bool spawnedLegendary)
        {
            if (spawnedLegendary)
            {
                _legendaryCooldownRemaining = _config.legendaryCooldownSpawns;
            }
            else if (_legendaryCooldownRemaining > 0)
            {
                _legendaryCooldownRemaining--;
            }
        }

        private LetterTier PickWeightedTier(bool suppressVowel, bool suppressLegendary)
        {
            _config.GetStageChances(out float vowelChance, out float legendaryChance);
            if (suppressVowel) vowelChance = 0f;
            if (suppressLegendary) legendaryChance = 0f;

            // Remaining probability mass is split across common/medium/rare, preserving
            // their relative ratios from the base distribution table (Section 5).
            float remaining = Math.Max(0f, 1f - vowelChance - legendaryChance);
            float baseOthersSum = _config.baseCommonChance + _config.baseMediumChance + _config.baseRareChance;
            float commonChance, mediumChance, rareChance;
            if (baseOthersSum <= 0f)
            {
                commonChance = remaining;
                mediumChance = 0f;
                rareChance = 0f;
            }
            else
            {
                commonChance = remaining * (_config.baseCommonChance / baseOthersSum);
                mediumChance = remaining * (_config.baseMediumChance / baseOthersSum);
                rareChance = remaining * (_config.baseRareChance / baseOthersSum);
            }

            float roll = (float)_rng.NextDouble();
            float cumulative = vowelChance;
            if (roll < cumulative) return LetterTier.Vowel;
            cumulative += commonChance;
            if (roll < cumulative) return LetterTier.Common;
            cumulative += mediumChance;
            if (roll < cumulative) return LetterTier.Medium;
            cumulative += rareChance;
            if (roll < cumulative) return LetterTier.Rare;
            return suppressLegendary ? LetterTier.Common : LetterTier.Legendary;
        }

        private string LettersForTier(LetterTier tier)
        {
            switch (tier)
            {
                case LetterTier.Vowel: return _config.vowelLetters;
                case LetterTier.Common: return _config.commonLetters;
                case LetterTier.Medium: return _config.mediumLetters;
                case LetterTier.Rare: return _config.rareLetters;
                default: return _config.legendaryLetters;
            }
        }

        private char RandomFrom(string letters) => letters[_rng.Next(letters.Length)];
    }
}
