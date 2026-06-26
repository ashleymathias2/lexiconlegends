using System;
using System.Collections.Generic;
using LexiconLegends.Config;
using UnityEngine;

namespace LexiconLegends.Combat
{
    /// <summary>
    /// Enemy HP and the enemy approach timer. Per project direction, enemy proximity is
    /// driven by a real-time timer (not the GDD Section 7 Aggression Meter): the enemy
    /// continuously closes in regardless of word pace, and reaching the player is an
    /// instant loss — the only way the player loses. There is no player HP and no other
    /// source of player damage (simplification per project direction). Subscribes to
    /// WordGridManager.SpellCast (Stage 3) and applies damage; Burn/Stagger layer their
    /// extra effect on top, per Section 6's "Effect" column (Restoration's heal was removed
    /// along with player HP, so Restoration words now just deal their base damage).
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        private CombatConfig _config;
        private EnemyApproachTimer _timer;
        private readonly List<BurnEffect> _activeBurns = new List<BurnEffect>();

        public float EnemyHP { get; private set; }
        public bool IsGameOver { get; private set; }
        public EnemyStage CurrentEnemyStage { get; private set; } = EnemyStage.Neutral;

        public event Action<float, float> EnemyHPChanged;    // (current, max)
        public event Action<float> EnemyDamaged;             // amount of a single hit/tick, for floating damage numbers
        public event Action<EnemyStage> EnemyStageChanged;
        public event Action<float> EnemyApproachProgressChanged; // 0 (far) -> 1 (reached player), fired every frame
        public event Action PlayerDefeated; // enemy reached the player (timer expired) — the only loss condition
        public event Action EnemyDefeated;

        public void Init(CombatConfig config)
        {
            _config = config;
            _timer = new EnemyApproachTimer(config);

            EnemyHP = config.enemyMaxHP;
            IsGameOver = false;
            _activeBurns.Clear();
            CurrentEnemyStage = EnemyStage.Neutral;

            EnemyHPChanged?.Invoke(EnemyHP, config.enemyMaxHP);
            EnemyStageChanged?.Invoke(CurrentEnemyStage);
            EnemyApproachProgressChanged?.Invoke(0f);
        }

        private void Update()
        {
            if (_timer == null || IsGameOver) return;

            _timer.Tick(Time.deltaTime);
            SetEnemyStage(_timer.GetStage());
            EnemyApproachProgressChanged?.Invoke(GetApproachProgress());

            if (_timer.HasReachedPlayer)
            {
                IsGameOver = true;
                PlayerDefeated?.Invoke();
            }
        }

        /// <summary>Call once per confirmed word (a "turn"): ticks burns, applies the cast's effect.</summary>
        public void OnSpellCast(SpellCastResult result)
        {
            if (IsGameOver) return;

            TickBurns();
            if (IsGameOver) return; // a burn tick could have finished the enemy.

            ApplyCastEffect(result);
        }

        private void TickBurns()
        {
            for (int i = _activeBurns.Count - 1; i >= 0; i--)
            {
                var burn = _activeBurns[i];
                DamageEnemy(burn.TickDamage);
                burn.RemainingTurns--;
                if (burn.RemainingTurns <= 0) _activeBurns.RemoveAt(i);
                if (IsGameOver) return;
            }
        }

        private void ApplyCastEffect(SpellCastResult result)
        {
            DamageEnemy(result.Damage);
            if (IsGameOver) return;

            switch (result.SpellType)
            {
                case SpellType.Burn:
                    _activeBurns.Add(new BurnEffect(result.BurnTickDamage, result.BurnDurationTurns));
                    break;
                case SpellType.Stagger:
                    _timer.ApplyStaggerBonus();
                    SetEnemyStage(_timer.GetStage());
                    EnemyApproachProgressChanged?.Invoke(GetApproachProgress());
                    break;
            }
        }

        private float GetApproachProgress()
        {
            return _config.timeToReachPlayerSeconds <= 0f
                ? 1f
                : Mathf.Clamp01(_timer.Elapsed / _config.timeToReachPlayerSeconds);
        }

        private void SetEnemyStage(EnemyStage stage)
        {
            if (stage == CurrentEnemyStage) return;
            CurrentEnemyStage = stage;
            EnemyStageChanged?.Invoke(stage);
        }

        private void DamageEnemy(float amount)
        {
            if (amount <= 0f || IsGameOver) return;
            EnemyHP = Mathf.Max(0f, EnemyHP - amount);
            EnemyHPChanged?.Invoke(EnemyHP, _config.enemyMaxHP);
            EnemyDamaged?.Invoke(amount);

            if (EnemyHP <= 0f)
            {
                IsGameOver = true;
                EnemyDefeated?.Invoke();
            }
        }
    }
}
