using LexiconLegends.Config;
using UnityEngine;

namespace LexiconLegends.Combat
{
    /// <summary>
    /// GDD Section 6 combo streak: increments per confirmed word, resets if more than
    /// streakResetSeconds passes without a confirmed word.
    /// </summary>
    public class ComboTracker
    {
        private readonly DamageConfig _config;
        private int _streak;
        private float _lastWordTime = float.NegativeInfinity;

        public ComboTracker(DamageConfig config)
        {
            _config = config;
        }

        public int CurrentStreak => _streak;

        /// <summary>Call once per confirmed word. Returns the streak count and multiplier to use for that word.</summary>
        public (int streak, float multiplier) RegisterWord()
        {
            float now = Time.time;
            if (now - _lastWordTime > _config.streakResetSeconds)
                _streak = 0;

            _streak++;
            _lastWordTime = now;

            return (_streak, _config.GetComboMultiplier(_streak));
        }

        public void Reset()
        {
            _streak = 0;
            _lastWordTime = float.NegativeInfinity;
        }
    }
}
