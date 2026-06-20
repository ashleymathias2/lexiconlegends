using UnityEngine;

namespace LexiconLegends.Config
{
    /// <summary>All tunables for the GDD Section 7 combat system and Aggression Meter.</summary>
    [CreateAssetMenu(fileName = "CombatConfig", menuName = "Lexicon Legends/Combat Config")]
    public class CombatConfig : ScriptableObject
    {
        [Header("Combatant Stats")]
        [Min(1f)] public float playerMaxHP = 100f;
        [Min(1f)] public float enemyMaxHP = 50f; // Per-level/enemy-type values arrive with the level system (Section 8, out of scope for now).

        [Header("Enemy Damage Scaling (Section 7)")]
        [Min(0f)] public float enemyBaseHitMin = 12f;
        [Min(0f)] public float enemyBaseHitMax = 15f;
        [Min(0f)] public float hitDamageGrowthPerLevel = 0.08f;
        [Min(1)] public int level = 1;

        [Header("Aggression Meter Fill by Word Length")]
        [Tooltip("Index 0 = length 3, index 1 = length 4, ... index 5 = length 8+.")]
        public float[] meterFillByLength = { 1.0f, 0.85f, 0.70f, 0.55f, 0.45f, 0.40f };

        [Min(0f)] public float attackThreshold = 2.5f;
        [Min(0f)] public float staggerMeterReductionBonus = 0.5f;

        [Header("Emoji/Proximity Stage Breakpoints (fractions of Attack Threshold)")]
        [Range(0f, 1f)] public float stage1Fraction = 0.33f;
        [Range(0f, 1f)] public float stage2Fraction = 0.66f;

        public float GetMeterFill(int wordLength)
        {
            int clampedLength = Mathf.Clamp(wordLength, 3, 8);
            int index = Mathf.Clamp(clampedLength - 3, 0, meterFillByLength.Length - 1);
            return meterFillByLength[index];
        }

        public float RollEnemyHitDamage(System.Random rng)
        {
            float baseHit = enemyBaseHitMin + (float)rng.NextDouble() * (enemyBaseHitMax - enemyBaseHitMin);
            return baseHit * (1f + hitDamageGrowthPerLevel * (level - 1));
        }
    }
}
