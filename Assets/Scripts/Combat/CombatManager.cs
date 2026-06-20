using System;
using System.Collections.Generic;
using LexiconLegends.Config;
using UnityEngine;

namespace LexiconLegends.Combat
{
    /// <summary>
    /// GDD Section 7: player/enemy HP, the Aggression Meter, and enemy attacks. Subscribes
    /// to WordGridManager.SpellCast (Stage 3) and applies every effect — base damage always
    /// applies; Restoration/Burn/Stagger layer their extra effect on top, per Section 6's
    /// "Effect" column.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        private CombatConfig _config;
        private System.Random _rng;
        private AggressionMeter _meter;
        private readonly List<BurnEffect> _activeBurns = new List<BurnEffect>();

        public float PlayerHP { get; private set; }
        public float EnemyHP { get; private set; }
        public bool IsGameOver { get; private set; }
        public EnemyStage CurrentEnemyStage { get; private set; } = EnemyStage.Neutral;

        public event Action<float, float> PlayerHPChanged;   // (current, max)
        public event Action<float, float> EnemyHPChanged;    // (current, max)
        public event Action<EnemyStage> EnemyStageChanged;
        public event Action<float> EnemyAttacked;            // damage dealt to player
        public event Action PlayerDefeated;
        public event Action EnemyDefeated;

        public void Init(CombatConfig config)
        {
            _config = config;
            _rng = new System.Random();
            _meter = new AggressionMeter(config);

            PlayerHP = config.playerMaxHP;
            EnemyHP = config.enemyMaxHP;
            IsGameOver = false;
            _activeBurns.Clear();
            CurrentEnemyStage = EnemyStage.Neutral;

            PlayerHPChanged?.Invoke(PlayerHP, config.playerMaxHP);
            EnemyHPChanged?.Invoke(EnemyHP, config.enemyMaxHP);
            EnemyStageChanged?.Invoke(CurrentEnemyStage);
        }

        /// <summary>Call once per confirmed word (a "turn"): ticks burns, applies the cast's effect, advances the Aggression Meter.</summary>
        public void OnSpellCast(SpellCastResult result)
        {
            if (IsGameOver) return;

            TickBurns();
            if (IsGameOver) return; // a burn tick could have finished the enemy.

            ApplyCastEffect(result);
            if (IsGameOver) return; // the cast's own damage could have finished the enemy.

            AdvanceAggressionMeter(result);
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
                case SpellType.Restoration:
                    HealPlayer(result.HealAmount);
                    break;
                case SpellType.Burn:
                    _activeBurns.Add(new BurnEffect(result.BurnTickDamage, result.BurnDurationTurns));
                    break;
                // Stagger's extra effect (meter pushback) is applied in AdvanceAggressionMeter.
            }
        }

        private void AdvanceAggressionMeter(SpellCastResult result)
        {
            bool isStagger = result.SpellType == SpellType.Stagger;
            bool thresholdReached = _meter.AddFill(result.Word.Length, isStagger);

            if (thresholdReached)
            {
                float damage = _config.RollEnemyHitDamage(_rng);
                PlayerHP = Mathf.Max(0f, PlayerHP - damage);
                PlayerHPChanged?.Invoke(PlayerHP, _config.playerMaxHP);
                EnemyAttacked?.Invoke(damage);

                _meter.Reset();
                SetEnemyStage(EnemyStage.Neutral);

                if (PlayerHP <= 0f)
                {
                    IsGameOver = true;
                    PlayerDefeated?.Invoke();
                    return;
                }
            }
            else
            {
                SetEnemyStage(_meter.GetStage());
            }
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

            if (EnemyHP <= 0f)
            {
                IsGameOver = true;
                EnemyDefeated?.Invoke();
            }
        }

        private void HealPlayer(float amount)
        {
            if (amount <= 0f) return;
            PlayerHP = Mathf.Min(_config.playerMaxHP, PlayerHP + amount);
            PlayerHPChanged?.Invoke(PlayerHP, _config.playerMaxHP);
        }
    }
}
