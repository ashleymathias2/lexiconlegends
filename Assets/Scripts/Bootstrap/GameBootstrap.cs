using LexiconLegends.Config;
using LexiconLegends.Dictionary;
using LexiconLegends.Grid;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LexiconLegends.Bootstrap
{
    /// <summary>
    /// Builds the playtest scene entirely at runtime: the three-zone portrait layout from
    /// GDD Section 2 (enemy zone placeholder, HUD strip placeholder, and the fully
    /// functional word grid zone). This means pressing Play in any scene is enough to test
    /// the grid — no manual prefab/scene wiring required.
    /// Enemy zone and HUD strip are inert placeholders here; they become real in Stages 4-5.
    /// </summary>
    public static class GameBootstrap
    {
        private const string DictionaryResourcePath = "Dictionary/placeholder_words";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureEventSystem();

            var config = ScriptableObject.CreateInstance<GridConfig>();
            var spawnConfig = ScriptableObject.CreateInstance<LetterSpawnConfig>();

            var dictionary = new WordDictionary();
            var dictAsset = Resources.Load<TextAsset>(DictionaryResourcePath);
            if (dictAsset != null) dictionary.LoadFromText(dictAsset.text);
            else Debug.LogWarning($"Lexicon Legends: no dictionary TextAsset found at Resources/{DictionaryResourcePath}.");

            var canvas = BuildCanvas();
            BuildEnemyZonePlaceholder(canvas.transform);
            BuildHudStripPlaceholder(canvas.transform);
            BuildGridZone(canvas.transform, config, spawnConfig, dictionary);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static Canvas BuildCanvas()
        {
            var canvasGo = new GameObject("LexiconLegends_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static RectTransform AddZone(Transform parent, string name, float anchorMinY, float anchorMaxY, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, anchorMinY);
            rect.anchorMax = new Vector2(1f, anchorMaxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private static void BuildEnemyZonePlaceholder(Transform canvasTransform)
        {
            // GDD Section 2: Top zone, ~35-40% height. Real enemy sprite/HP/emoji arrive in Stage 4.
            var zone = AddZone(canvasTransform, "EnemyZone_Placeholder", 0.625f, 1f, new Color(0.55f, 0.2f, 0.2f));
            var label = CreateLabel(zone, "Enemy Zone (Stage 4)", 36, new Color(1f, 0.85f, 0.85f));
            StretchFull(label.rectTransform);
        }

        private static void BuildHudStripPlaceholder(Transform canvasTransform)
        {
            // GDD Section 2: Middle zone, ~10% height. Real HP/combo/lives/score arrive in Stage 5.
            var zone = AddZone(canvasTransform, "HudStrip_Placeholder", 0.525f, 0.625f, new Color(0.2f, 0.2f, 0.25f));
            var label = CreateLabel(zone, "HUD Strip (Stage 5)", 28, new Color(0.85f, 0.85f, 1f));
            StretchFull(label.rectTransform);
        }

        private static void BuildGridZone(Transform canvasTransform, GridConfig config, LetterSpawnConfig spawnConfig, WordDictionary dictionary)
        {
            // GDD Section 2: Bottom zone, ~50-55% height. Fully functional in Stage 1.
            var zone = AddZone(canvasTransform, "GridZone", 0f, 0.525f, new Color(0.12f, 0.12f, 0.14f));

            var layout = zone.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 16f;
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            // Preview row.
            var previewLabel = CreateLabel(zone, "—", 48, Color.white);
            previewLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 90;

            // Feedback row (validation messages).
            var feedbackLabel = CreateLabel(zone, string.Empty, 26, new Color(1f, 0.8f, 0.4f));
            feedbackLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;

            // Grid container.
            var gridGo = new GameObject("TileGrid", typeof(RectTransform));
            gridGo.transform.SetParent(zone, false);
            var gridLayoutElement = gridGo.AddComponent<LayoutElement>();
            gridLayoutElement.flexibleHeight = 1f;

            var gridLayout = gridGo.AddComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = config.columns;
            gridLayout.spacing = new Vector2(config.tileSpacing, config.tileSpacing);
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.cellSize = new Vector2(150, 150);

            var tiles = new TileView[config.rows, config.columns];
            for (int r = 0; r < config.rows; r++)
            for (int c = 0; c < config.columns; c++)
                tiles[r, c] = BuildTile(gridGo.transform, r, c);

            // Action buttons row.
            var buttonsRowGo = new GameObject("ActionButtons", typeof(RectTransform));
            buttonsRowGo.transform.SetParent(zone, false);
            buttonsRowGo.AddComponent<LayoutElement>().preferredHeight = 110;
            var buttonsRowLayout = buttonsRowGo.AddComponent<HorizontalLayoutGroup>();
            buttonsRowLayout.spacing = 16f;
            buttonsRowLayout.childForceExpandWidth = true;

            var manager = zone.gameObject.AddComponent<WordGridManager>();
            manager.Init(config, spawnConfig, dictionary, tiles, previewLabel, feedbackLabel);

            BuildButton(buttonsRowGo.transform, "Confirm", new Color(0.25f, 0.6f, 0.3f), manager.Confirm);
            BuildButton(buttonsRowGo.transform, "Clear", new Color(0.5f, 0.5f, 0.5f), manager.ClearSelection);
            BuildButton(buttonsRowGo.transform, "Reshuffle", new Color(0.3f, 0.4f, 0.6f), manager.Reshuffle);
        }

        private static TileView BuildTile(Transform parent, int row, int col)
        {
            var go = new GameObject($"Tile_{row}_{col}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = TileView.UnselectedColor;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 56;
            label.color = Color.black;
            StretchFull(label.rectTransform);

            var tileView = go.AddComponent<TileView>();
            tileView.Init(row, col, label, image);
            return tileView;
        }

        private static Button BuildButton(Transform parent, string text, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Button_{text}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = color;

            var button = go.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            var label = CreateLabel(go.transform, text, 32, Color.white);
            StretchFull(label.rectTransform);

            return button;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            return label;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
