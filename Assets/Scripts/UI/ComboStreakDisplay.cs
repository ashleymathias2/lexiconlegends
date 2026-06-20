using TMPro;
using UnityEngine;

namespace LexiconLegends.UI
{
    /// <summary>
    /// HUD combo streak text. Mirrors ComboTracker's reset window (GDD Section 6: streak
    /// resets if more than streakResetSeconds passes without a confirmed word) so the
    /// display matches the actual streak state even between casts.
    /// </summary>
    public class ComboStreakDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _label;
        private float _resetSeconds;
        private float _lastCastTime = float.NegativeInfinity;
        private int _lastStreak;

        public void Init(TextMeshProUGUI label, float resetSeconds)
        {
            _label = label;
            _resetSeconds = resetSeconds;
            UpdateText(0);
        }

        public void RegisterStreak(int streak)
        {
            _lastStreak = streak;
            _lastCastTime = Time.time;
            UpdateText(streak);
        }

        private void Update()
        {
            if (_lastStreak == 0) return;
            if (Time.time - _lastCastTime > _resetSeconds)
            {
                _lastStreak = 0;
                UpdateText(0);
            }
        }

        private void UpdateText(int streak)
        {
            if (_label != null) _label.text = $"Streak: {streak}";
        }
    }
}
