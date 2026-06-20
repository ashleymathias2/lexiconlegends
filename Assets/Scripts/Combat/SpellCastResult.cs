namespace LexiconLegends.Combat
{
    /// <summary>Full breakdown of a single word's cast, for display and (in Stage 4) application to combat.</summary>
    public readonly struct SpellCastResult
    {
        public readonly string Word;
        public readonly float LengthMultiplier;
        public readonly float AverageRarityWeight;
        public readonly float ComboMultiplier;
        public readonly int StreakAtCast;
        public readonly float Damage;
        public readonly SpellType SpellType;
        public readonly float HealAmount;
        public readonly float BurnTickDamage;
        public readonly int BurnDurationTurns;

        public SpellCastResult(string word, float lengthMultiplier, float averageRarityWeight,
            float comboMultiplier, int streakAtCast, float damage, SpellType spellType,
            float healAmount, float burnTickDamage, int burnDurationTurns)
        {
            Word = word;
            LengthMultiplier = lengthMultiplier;
            AverageRarityWeight = averageRarityWeight;
            ComboMultiplier = comboMultiplier;
            StreakAtCast = streakAtCast;
            Damage = damage;
            SpellType = spellType;
            HealAmount = healAmount;
            BurnTickDamage = burnTickDamage;
            BurnDurationTurns = burnDurationTurns;
        }
    }
}
