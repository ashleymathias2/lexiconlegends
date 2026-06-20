using UnityEngine;
using LexiconLegends.Config;

namespace LexiconLegends.Combat
{
    /// <summary>
    /// Real-time replacement for the GDD Section 7 Aggression Meter (project direction):
    /// the enemy continuously approaches over a configurable duration regardless of word
    /// pace. Stagger words buy time by reducing elapsed approach time.
    /// </summary>
    public class EnemyApproachTimer
    {
        private readonly CombatConfig _config;
        private float _elapsed;

        public EnemyApproachTimer(CombatConfig config)
        {
            _config = config;
        }

        public float Elapsed => _elapsed;
        public bool HasReachedPlayer => _elapsed >= _config.timeToReachPlayerSeconds;

        public void Tick(float deltaTime)
        {
            _elapsed = Mathf.Min(_elapsed + deltaTime, _config.timeToReachPlayerSeconds);
        }

        public void ApplyStaggerBonus()
        {
            _elapsed = Mathf.Max(0f, _elapsed - _config.staggerTimeBonusSeconds);
        }

        public void Reset()
        {
            _elapsed = 0f;
        }

        public EnemyStage GetStage()
        {
            float progress = _config.timeToReachPlayerSeconds <= 0f ? 1f : _elapsed / _config.timeToReachPlayerSeconds;
            if (progress >= 1f) return EnemyStage.Stage3;
            if (progress >= _config.stage2Fraction) return EnemyStage.Stage2;
            if (progress >= _config.stage1Fraction) return EnemyStage.Stage1;
            return EnemyStage.Neutral;
        }
    }
}
