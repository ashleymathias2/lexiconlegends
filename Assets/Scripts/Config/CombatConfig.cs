using UnityEngine;

namespace LexiconLegends.Config
{
    /// <summary>
    /// Tunables for the combat system. NOTE: per project direction (post-GDD revision),
    /// enemy proximity is now driven by a real-time approach timer rather than the GDD
    /// Section 7 Aggression Meter — the enemy creeps continuously toward the player and
    /// reaching them is an instant loss, regardless of remaining player HP. Stagger words
    /// buy time instead of pushing a meter back.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatConfig", menuName = "Lexicon Legends/Combat Config")]
    public class CombatConfig : ScriptableObject
    {
        [Header("Combatant Stats")]
        [Min(1f)] public float playerMaxHP = 100f;
        [Min(1f)] public float enemyMaxHP = 50f; // Per-level/enemy-type values arrive with the level system (Section 8, out of scope for now).

        [Header("Lives (Section 2 HUD; full lives economy is Section 8, out of scope for now)")]
        [Tooltip("Number of times the enemy can reach the player before the run ends for good.")]
        [Min(0)] public int startingLives = 3;

        [Header("Enemy Approach Timer")]
        [Tooltip("Real-time seconds for the enemy to walk from starting distance to the player. Reaching the player is an instant loss.")]
        [Min(0.1f)] public float timeToReachPlayerSeconds = 60f;
        [Tooltip("Seconds subtracted from elapsed approach time when a Stagger word is cast (knocks the enemy back).")]
        [Min(0f)] public float staggerTimeBonusSeconds = 5f;

        [Header("Emoji/Proximity Stage Breakpoints (fractions of the approach timer elapsed)")]
        [Range(0f, 1f)] public float stage1Fraction = 0.33f;
        [Range(0f, 1f)] public float stage2Fraction = 0.66f;

        [Header("Enemy Visual Size")]
        [Tooltip("Width/height of the enemy body box, in canvas reference units.")]
        [Min(10f)] public float enemyBodySize = 220f;

        [Header("Enemy Visual Position (anchor Y within the enemy zone, 0 = bottom, 1 = top)")]
        [Tooltip("Where the enemy starts, at 0% approach progress.")]
        [Range(0f, 1f)] public float enemyFarAnchorY = 0.78f;
        [Tooltip("Anchor Y where the enemy's CENTER sits at 100% approach progress (this is when the game ends). " +
            "Default (0.153) is calculated so the enemy body's bottom edge lands exactly on the HUD strip's top " +
            "edge — i.e. the enemy visually touches the HUD rectangle when it kills the player. If you change " +
            "enemyBodySize or the zone height split in GameBootstrap, recompute: 0.5 * enemyBodySize / enemyZoneHeightInRefUnits.")]
        [Range(0f, 1f)] public float enemyCollisionAnchorY = 0.153f;
    }
}
