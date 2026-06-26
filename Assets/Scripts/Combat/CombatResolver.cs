using LexiconLegends.Config;

namespace LexiconLegends.Combat
{
    /// <summary>Combines damage calculation, spell categorization, and combo tracking for one cast.</summary>
    public class CombatResolver
    {
        private readonly DamageConfig _config;
        private readonly ComboTracker _comboTracker;

        public CombatResolver(DamageConfig config)
        {
            _config = config;
            _comboTracker = new ComboTracker(config);
        }

        public int CurrentStreak => _comboTracker.CurrentStreak;

        public SpellCastResult ResolveCast(string word)
        {
            var (streak, comboMultiplier) = _comboTracker.RegisterWord();
            float damage = DamageCalculator.ComputeDamage(word, comboMultiplier, _config,
                out float lengthMultiplier, out float averageRarityWeight);
            var spellType = SpellResolver.DetermineSpellType(word, _config);

            float burnTickDamage = spellType == SpellType.Burn ? damage * _config.burnTickDamageRatio : 0f;
            int burnDuration = spellType == SpellType.Burn ? _config.burnDurationTurns : 0;

            return new SpellCastResult(word, lengthMultiplier, averageRarityWeight, comboMultiplier,
                streak, damage, spellType, burnTickDamage, burnDuration);
        }
    }
}
