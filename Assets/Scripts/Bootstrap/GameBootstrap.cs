using LexiconLegends.Combat;
using LexiconLegends.Config;
using LexiconLegends.Dictionary;
using LexiconLegends.Grid;
using LexiconLegends.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LexiconLegends.Bootstrap
{
    /// <summary>
    /// Builds the playtest scene entirely at runtime: the three-zone portrait layout from
    /// GDD Section 2 — a functional enemy zone (combat presentation), a full HUD strip
    /// (player HP, combo streak, lives, pause, score), and the fully functional word grid
    /// zone. This means pressing Play in any scene is enough to test — no manual
    /// prefab/scene wiring required.
    /// </summary>
    public static class GameBootstrap
    {
        private const string DictionaryResourcePath = "Dictionary/placeholder_words";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureEventSystem();

            // Loaded from Assets/Resources/Config/*.asset so every tunable number is visible
            // and editable in the Inspector — select the asset to tweak values, including
            // live edits while in Play mode (it's the same object instance the game is using).
            var config = LoadOrCreateConfig<GridConfig>("Config/GridConfig");
            var spawnConfig = LoadOrCreateConfig<LetterSpawnConfig>("Config/LetterSpawnConfig");
            var damageConfig = LoadOrCreateConfig<DamageConfig>("Config/DamageConfig");
            var combatConfig = LoadOrCreateConfig<CombatConfig>("Config/CombatConfig");

            var dictionary = new WordDictionary();
            var dictAsset = Resources.Load<TextAsset>(DictionaryResourcePath);
            if (dictAsset != null) dictionary.LoadFromText(dictAsset.text);
            else Debug.LogWarning($"Lexicon Legends: no dictionary TextAsset found at Resources/{DictionaryResourcePath}.");

            var rootGo = new GameObject("CombatManager");
            var combatManager = rootGo.AddComponent<CombatManager>();
            combatManager.Init(combatConfig);

            var canvas = BuildCanvas();
            BuildEnemyZone(canvas.transform, combatManager);
            var manager = BuildGridZone(canvas.transform, config, spawnConfig, damageConfig, dictionary);
            var livesLabel = BuildHudStrip(canvas.transform, combatManager, manager, damageConfig);
            BuildEndOfRunFlow(canvas.transform, combatManager, manager, combatConfig, livesLabel);

            manager.SpellCast += combatManager.OnSpellCast;
        }

        private static T LoadOrCreateConfig<T>(string resourcePath) where T : ScriptableObject
        {
            var loaded = Resources.Load<T>(resourcePath);
            if (loaded != null) return loaded;

            Debug.LogWarning($"Lexicon Legends: no config asset found at Resources/{resourcePath}; using uneditable in-memory defaults for {typeof(T).Name}.");
            return ScriptableObject.CreateInstance<T>();
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

        // ---------------------------------------------------------------
        // Enemy zone (Section 2 top zone, Section 7 combat presentation)
        // ---------------------------------------------------------------

        private static void BuildEnemyZone(Transform canvasTransform, CombatManager combatManager)
        {
            // GDD Section 2: Top zone, ~35-40% height.
            var zone = AddZone(canvasTransform, "EnemyZone", 0.625f, 1f, new Color(0.45f, 0.18f, 0.18f));

            // Enemy HP bar, fixed at the top of the zone.
            var hpBarBg = new GameObject("EnemyHpBarBg", typeof(RectTransform));
            hpBarBg.transform.SetParent(zone, false);
            var hpBarBgRect = hpBarBg.GetComponent<RectTransform>();
            hpBarBgRect.anchorMin = new Vector2(0.1f, 0.88f);
            hpBarBgRect.anchorMax = new Vector2(0.9f, 0.95f);
            hpBarBgRect.offsetMin = hpBarBgRect.offsetMax = Vector2.zero;
            hpBarBg.AddComponent<Image>().color = new Color(0.15f, 0.05f, 0.05f);

            var hpBarFillGo = new GameObject("Fill", typeof(RectTransform));
            hpBarFillGo.transform.SetParent(hpBarBg.transform, false);
            var hpBarFillRect = hpBarFillGo.GetComponent<RectTransform>();
            hpBarFillRect.anchorMin = Vector2.zero;
            hpBarFillRect.anchorMax = Vector2.one;
            hpBarFillRect.offsetMin = hpBarFillRect.offsetMax = Vector2.zero;
            hpBarFillRect.pivot = new Vector2(0f, 0.5f);
            var hpBarFillImage = hpBarFillGo.AddComponent<Image>();
            hpBarFillImage.color = new Color(0.8f, 0.2f, 0.2f);

            var enemyHpLabel = CreateLabel(hpBarBg.transform, "Enemy HP", 22, Color.white);
            StretchFull(enemyHpLabel.rectTransform);

            // Enemy body: moves down within the zone as the approach timer escalates.
            var enemyBody = new GameObject("EnemyBody", typeof(RectTransform));
            enemyBody.transform.SetParent(zone, false);
            var enemyBodyRect = enemyBody.GetComponent<RectTransform>();
            enemyBodyRect.sizeDelta = new Vector2(220, 220);
            enemyBodyRect.anchorMin = enemyBodyRect.anchorMax = new Vector2(0.5f, 0.78f);
            enemyBody.AddComponent<Image>().color = new Color(0.6f, 0.1f, 0.6f);

            var emojiLabel = CreateLabel(enemyBody.transform, string.Empty, 30, Color.white);
            var emojiRect = emojiLabel.rectTransform;
            emojiRect.anchorMin = new Vector2(0f, 1f);
            emojiRect.anchorMax = new Vector2(1f, 1.6f);
            emojiRect.offsetMin = emojiRect.offsetMax = Vector2.zero;

            // Continuous approach: anchor Y lerps every frame from far (0.78) to adjacent (0.30)
            // as the real-time approach timer progresses, so movement is visibly gradual rather
            // than only jumping at the 3 discrete emoji-stage breakpoints.
            const float farAnchorY = 0.78f;
            const float adjacentAnchorY = 0.30f;

            combatManager.EnemyHPChanged += (current, max) =>
            {
                float ratio = max <= 0f ? 0f : Mathf.Clamp01(current / max);
                hpBarFillRect.anchorMax = new Vector2(ratio, 1f);
                enemyHpLabel.text = $"Enemy HP: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            };

            combatManager.EnemyApproachProgressChanged += progress =>
            {
                float anchorY = Mathf.Lerp(farAnchorY, adjacentAnchorY, progress);
                var anchor = new Vector2(0.5f, anchorY);
                enemyBodyRect.anchorMin = anchor;
                enemyBodyRect.anchorMax = anchor;
            };

            combatManager.EnemyStageChanged += stage =>
            {
                emojiLabel.text = stage switch
                {
                    EnemyStage.Stage1 => "😠 ANGRY",
                    EnemyStage.Stage2 => "😡 ANGRY (shaking)",
                    EnemyStage.Stage3 => "🤬 ENRAGED!",
                    _ => string.Empty
                };
            };
        }

        // ---------------------------------------------------------------
        // HUD strip (Section 2 middle zone): player HP, combo streak,
        // lives, pause, level/score.
        // ---------------------------------------------------------------

        private static TextMeshProUGUI BuildHudStrip(Transform canvasTransform, CombatManager combatManager,
            WordGridManager gridManager, DamageConfig damageConfig)
        {
            // GDD Section 2: Middle zone, ~10% height.
            var zone = AddZone(canvasTransform, "HudStrip", 0.525f, 0.625f, new Color(0.2f, 0.2f, 0.25f));

            var layout = zone.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;
            layout.padding = new RectOffset(12, 12, 6, 6);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Player HP: compact bar + numeric label, same pattern as the enemy HP bar.
            var hpContainer = new GameObject("PlayerHp", typeof(RectTransform));
            hpContainer.transform.SetParent(zone, false);
            hpContainer.AddComponent<LayoutElement>().flexibleWidth = 1.4f;
            hpContainer.AddComponent<Image>().color = new Color(0.1f, 0.05f, 0.05f);

            var hpFillGo = new GameObject("Fill", typeof(RectTransform));
            hpFillGo.transform.SetParent(hpContainer.transform, false);
            var hpFillRect = hpFillGo.GetComponent<RectTransform>();
            hpFillRect.anchorMin = Vector2.zero;
            hpFillRect.anchorMax = Vector2.one;
            hpFillRect.offsetMin = hpFillRect.offsetMax = Vector2.zero;
            hpFillRect.pivot = new Vector2(0f, 0.5f);
            hpFillGo.AddComponent<Image>().color = new Color(0.2f, 0.75f, 0.3f);

            var hpLabel = CreateLabel(hpContainer.transform, "HP 100/100", 22, Color.white);
            StretchFull(hpLabel.rectTransform);

            combatManager.PlayerHPChanged += (current, max) =>
            {
                float ratio = max <= 0f ? 0f : Mathf.Clamp01(current / max);
                hpFillRect.anchorMax = new Vector2(ratio, 1f);
                hpLabel.text = $"HP {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
            };

            // Combo streak.
            var comboLabel = CreateLabel(zone, "Streak: 0", 22, Color.white);
            comboLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var comboDisplay = comboLabel.gameObject.AddComponent<ComboStreakDisplay>();
            comboDisplay.Init(comboLabel, damageConfig.streakResetSeconds);
            gridManager.SpellCast += result => comboDisplay.RegisterStreak(result.StreakAtCast);

            // Lives.
            var livesLabel = CreateLabel(zone, string.Empty, 22, Color.white);
            livesLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            // Level/score: score tracked as cumulative damage dealt to the current enemy.
            var scoreLabel = CreateLabel(zone, "Score: 0  |  Lvl 1", 22, Color.white);
            scoreLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.2f;
            combatManager.EnemyHPChanged += (current, max) =>
                scoreLabel.text = $"Score: {Mathf.RoundToInt(max - current)}  |  Lvl 1";

            // Pause.
            var pauseButtonGo = new GameObject("PauseButton", typeof(RectTransform));
            pauseButtonGo.transform.SetParent(zone, false);
            pauseButtonGo.AddComponent<LayoutElement>().flexibleWidth = 0.7f;
            pauseButtonGo.AddComponent<Image>().color = new Color(0.35f, 0.35f, 0.4f);
            var pauseButton = pauseButtonGo.AddComponent<Button>();
            var pauseLabel = CreateLabel(pauseButtonGo.transform, "Pause", 22, Color.white);
            StretchFull(pauseLabel.rectTransform);

            bool isPaused = false;
            pauseButton.onClick.AddListener(() =>
            {
                isPaused = !isPaused;
                Time.timeScale = isPaused ? 0f : 1f;
                gridManager.SetInputEnabled(!isPaused);
                pauseLabel.text = isPaused ? "Resume" : "Pause";
            });

            return livesLabel;
        }

        // ---------------------------------------------------------------
        // Grid zone (Section 2 bottom zone) — Stages 1-3
        // ---------------------------------------------------------------

        private static WordGridManager BuildGridZone(Transform canvasTransform, GridConfig config, LetterSpawnConfig spawnConfig,
            DamageConfig damageConfig, WordDictionary dictionary)
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

            // Feedback row (validation messages + cast breakdown).
            var feedbackLabel = CreateLabel(zone, string.Empty, 24, new Color(1f, 0.8f, 0.4f));
            feedbackLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;

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
            manager.Init(config, spawnConfig, damageConfig, dictionary, tiles, previewLabel, feedbackLabel);

            BuildButton(buttonsRowGo.transform, "Confirm", new Color(0.25f, 0.6f, 0.3f), manager.Confirm);
            BuildButton(buttonsRowGo.transform, "Clear", new Color(0.5f, 0.5f, 0.5f), manager.ClearSelection);
            BuildButton(buttonsRowGo.transform, "Reshuffle", new Color(0.3f, 0.4f, 0.6f), manager.Reshuffle);

            return manager;
        }

        // ---------------------------------------------------------------
        // End-of-run flow: win/loss overlay, plus a minimal lives counter
        // (full lives economy — refill timers, IAP — is Section 8, out of scope).
        // Losing to the enemy reaching the player costs a life and continues
        // (fresh enemy HP and approach timer) until lives run out.
        // ---------------------------------------------------------------

        private static void BuildEndOfRunFlow(Transform canvasTransform, CombatManager combatManager,
            WordGridManager gridManager, CombatConfig combatConfig, TextMeshProUGUI livesLabel)
        {
            int livesRemaining = combatConfig.startingLives;
            livesLabel.text = $"Lives: {livesRemaining}";

            var overlayGo = new GameObject("EndOfRunOverlay", typeof(RectTransform));
            overlayGo.transform.SetParent(canvasTransform, false);
            StretchFull(overlayGo.GetComponent<RectTransform>());
            overlayGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            overlayGo.SetActive(false);

            var titleLabel = CreateLabel(overlayGo.transform, string.Empty, 64, Color.white);
            titleLabel.rectTransform.anchorMin = new Vector2(0.1f, 0.55f);
            titleLabel.rectTransform.anchorMax = new Vector2(0.9f, 0.7f);
            titleLabel.rectTransform.offsetMin = titleLabel.rectTransform.offsetMax = Vector2.zero;

            var actionButtonGo = new GameObject("ActionButton", typeof(RectTransform));
            actionButtonGo.transform.SetParent(overlayGo.transform, false);
            var actionRect = actionButtonGo.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(0.3f, 0.42f);
            actionRect.anchorMax = new Vector2(0.7f, 0.5f);
            actionRect.offsetMin = actionRect.offsetMax = Vector2.zero;
            actionButtonGo.AddComponent<Image>().color = new Color(0.3f, 0.5f, 0.3f);
            var actionButton = actionButtonGo.AddComponent<Button>();
            var actionLabel = CreateLabel(actionButtonGo.transform, string.Empty, 36, Color.white);
            StretchFull(actionLabel.rectTransform);

            void ShowFinalOverlay(string title)
            {
                gridManager.SetInputEnabled(false);
                titleLabel.text = title;
                actionLabel.text = "Restart";
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
                overlayGo.SetActive(true);
            }

            void ShowContinueOverlay()
            {
                gridManager.SetInputEnabled(false);
                titleLabel.text = $"The enemy reached you! Lives left: {livesRemaining}";
                actionLabel.text = "Continue";
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(() =>
                {
                    overlayGo.SetActive(false);
                    gridManager.SetInputEnabled(true);
                    combatManager.Init(combatConfig);
                });
                overlayGo.SetActive(true);
            }

            combatManager.EnemyDefeated += () => ShowFinalOverlay("You Win!");
            combatManager.PlayerDefeated += () =>
            {
                livesRemaining--;
                livesLabel.text = $"Lives: {livesRemaining}";

                if (livesRemaining > 0) ShowContinueOverlay();
                else ShowFinalOverlay("You Lose...");
            };
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
