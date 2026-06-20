using LexiconLegends.Config;

namespace LexiconLegends.Combat
{
    /// <summary>
    /// GDD Section 7: meter fills per confirmed word (more for short words, less for long),
    /// Stagger words subtract extra fill, and reaching the Attack Threshold triggers an
    /// immediate enemy attack and resets the meter to 0. Never exposed numerically to the
    /// player — only consumed by EnemyStage for proximity/emoji.
    /// </summary>
    public class AggressionMeter
    {
        private readonly CombatConfig _config;
        private float _fill;

        public AggressionMeter(CombatConfig config)
        {
            _config = config;
        }

        public float Fill => _fill;

        /// <summary>Adds fill for a confirmed word and returns true if the Attack Threshold was reached (caller should then call Reset).</summary>
        public bool AddFill(int wordLength, bool isStagger)
        {
            float fillAmount = _config.GetMeterFill(wordLength);
            if (isStagger) fillAmount -= _config.staggerMeterReductionBonus;
            _fill += System.Math.Max(fillAmount, 0f);

            return _fill >= _config.attackThreshold;
        }

        public void Reset()
        {
            _fill = 0f;
        }

        public EnemyStage GetStage()
        {
            float ratio = _config.attackThreshold <= 0f ? 0f : _fill / _config.attackThreshold;
            if (ratio >= 1f) return EnemyStage.Stage3;
            if (ratio >= _config.stage2Fraction) return EnemyStage.Stage2;
            if (ratio >= _config.stage1Fraction) return EnemyStage.Stage1;
            return EnemyStage.Neutral;
        }
    }
}
