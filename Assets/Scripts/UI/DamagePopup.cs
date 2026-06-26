using System.Collections;
using TMPro;
using UnityEngine;

namespace LexiconLegends.UI
{
    /// <summary>
    /// Floating damage number shown beside the enemy: appears, drifts up slightly, fades
    /// out over its lifetime, then destroys itself. Replaces the on-screen damage-formula
    /// breakdown per project direction (keep the prototype's screen simple).
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        public static DamagePopup Spawn(Transform parent, TextMeshProUGUI prefab, float amount, float lifetimeSeconds, float riseDistance)
        {
            var label = Instantiate(prefab, parent);
            label.text = Mathf.RoundToInt(amount).ToString();
            label.gameObject.SetActive(true);

            var popup = label.gameObject.AddComponent<DamagePopup>();
            popup.StartCoroutine(popup.FadeAndDestroy(label, lifetimeSeconds, riseDistance));
            return popup;
        }

        private IEnumerator FadeAndDestroy(TextMeshProUGUI label, float lifetimeSeconds, float riseDistance)
        {
            var rect = label.rectTransform;
            Vector2 startPos = rect.anchoredPosition;
            Color startColor = label.color;
            float elapsed = 0f;

            while (elapsed < lifetimeSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lifetimeSeconds);

                rect.anchoredPosition = startPos + Vector2.up * (riseDistance * t);
                label.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
