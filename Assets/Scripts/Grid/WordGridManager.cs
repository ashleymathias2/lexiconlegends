using System;
using System.Collections.Generic;
using System.Text;
using LexiconLegends.Combat;
using LexiconLegends.Config;
using LexiconLegends.Dictionary;
using LexiconLegends.Spawn;
using TMPro;
using UnityEngine;

namespace LexiconLegends.Grid
{
    /// <summary>
    /// Word grid: free-tile-selection input, word preview, the destroy + gravity + refill
    /// loop (Section 4), the full Section 5 letter spawn system, and (Stage 3) the Section 6
    /// damage/spell resolution per confirmed word. Combat application (HP, enemy) arrives in
    /// Stage 4 — for now SpellCast fires the computed breakdown for display/testing.
    /// </summary>
    public class WordGridManager : MonoBehaviour
    {
        private GridConfig _config;
        private LetterSpawnConfig _spawnConfig;
        private DamageConfig _damageConfig;
        private WordDictionary _dictionary;
        private System.Random _rng;
        private LetterSpawner _spawner;
        private SeedWordSelector _seedSelector;
        private CombatResolver _combatResolver;

        private TileView[,] _tiles;
        private readonly List<TileView> _selectionOrder = new List<TileView>();
        private int _pendingRewardLetters;

        private TextMeshProUGUI _previewLabel;
        private TextMeshProUGUI _feedbackLabel;

        /// <summary>Fired after a word is confirmed and destroyed, with its full damage/spell breakdown.</summary>
        public event Action<SpellCastResult> SpellCast;

        public void Init(GridConfig config, LetterSpawnConfig spawnConfig, DamageConfig damageConfig,
            WordDictionary dictionary, TileView[,] tiles, TextMeshProUGUI previewLabel, TextMeshProUGUI feedbackLabel)
        {
            _config = config;
            _spawnConfig = spawnConfig;
            _damageConfig = damageConfig;
            _dictionary = dictionary;
            _tiles = tiles;
            _previewLabel = previewLabel;
            _feedbackLabel = feedbackLabel;
            _rng = new System.Random();
            _spawner = new LetterSpawner(_spawnConfig, _rng);
            _seedSelector = new SeedWordSelector(_spawnConfig, _dictionary, _rng);
            _combatResolver = new CombatResolver(_damageConfig);

            for (int r = 0; r < _config.rows; r++)
            for (int c = 0; c < _config.columns; c++)
                _tiles[r, c].Clicked += OnTileClicked;

            GenerateInitialBoard();
            UpdatePreview();
        }

        // ---------------------------------------------------------------
        // Initial board generation (Section 5, Steps 1-4)
        // ---------------------------------------------------------------

        private void GenerateInitialBoard()
        {
            int attempts = 0;
            bool valid;
            do
            {
                PlaceSeedAndFillBoard();
                valid = IsBoardValid();
                attempts++;
            } while (!valid && attempts < _spawnConfig.maxRefillRetries);

            if (!valid)
                Debug.LogWarning("Lexicon Legends: could not generate an initial board meeting the minimum valid-word count after retries; using last attempt.");
        }

        private void PlaceSeedAndFillBoard()
        {
            int totalCells = _config.rows * _config.columns;
            var allCells = new List<(int row, int col)>(totalCells);
            for (int r = 0; r < _config.rows; r++)
            for (int c = 0; c < _config.columns; c++)
                allCells.Add((r, c));
            Shuffle(allCells);

            string seedWord = _seedSelector.SelectSeedWord(totalCells, _config.maxWordLength);

            var remainingCells = new List<(int row, int col)>(allCells);
            if (!string.IsNullOrEmpty(seedWord))
            {
                for (int i = 0; i < seedWord.Length; i++)
                {
                    var cell = remainingCells[0];
                    remainingCells.RemoveAt(0);
                    _tiles[cell.row, cell.col].SetLetter(seedWord[i]);
                }
            }

            foreach (var cell in remainingCells)
            {
                char letter = _spawner.SpawnLetter(CountVowelsOnBoard());
                _tiles[cell.row, cell.col].SetLetter(letter);
            }
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ---------------------------------------------------------------
        // Selection / preview
        // ---------------------------------------------------------------

        private bool _inputEnabled = true;

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
        }

        private void OnTileClicked(TileView tile)
        {
            if (!_inputEnabled) return;

            if (tile.IsSelected)
            {
                tile.SetSelected(false);
                _selectionOrder.Remove(tile);
            }
            else
            {
                if (_selectionOrder.Count >= _config.maxWordLength)
                {
                    SetFeedback($"Max word length is {_config.maxWordLength}.");
                    return;
                }
                tile.SetSelected(true);
                _selectionOrder.Add(tile);
            }
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var sb = new StringBuilder();
            foreach (var tile in _selectionOrder) sb.Append(tile.Letter);
            _previewLabel.text = sb.Length == 0 ? "—" : sb.ToString();
        }

        private void SetFeedback(string message)
        {
            if (_feedbackLabel != null) _feedbackLabel.text = message;
        }

        public void ClearSelection()
        {
            foreach (var tile in _selectionOrder) tile.SetSelected(false);
            _selectionOrder.Clear();
            UpdatePreview();
            SetFeedback(string.Empty);
        }

        /// <summary>In-place restart for a new run: fresh board (new seed word, new letters), cleared selection/state, input re-enabled.</summary>
        public void RestartBoard()
        {
            foreach (var tile in _selectionOrder) tile.SetSelected(false);
            _selectionOrder.Clear();
            _pendingRewardLetters = 0;
            SetInputEnabled(true);

            GenerateInitialBoard();
            UpdatePreview();
            SetFeedback(string.Empty);
        }

        public void Reshuffle()
        {
            if (!_inputEnabled) return;
            ClearSelection();

            var letters = new List<char>();
            for (int r = 0; r < _config.rows; r++)
            for (int c = 0; c < _config.columns; c++)
                letters.Add(_tiles[r, c].Letter);

            Shuffle(letters);

            int idx = 0;
            for (int r = 0; r < _config.rows; r++)
            for (int c = 0; c < _config.columns; c++)
                _tiles[r, c].SetLetter(letters[idx++]);

            SetFeedback("Board reshuffled.");
        }

        // ---------------------------------------------------------------
        // Confirm / destroy / gravity / refill
        // ---------------------------------------------------------------

        public void Confirm()
        {
            if (!_inputEnabled) return;

            if (_selectionOrder.Count < _config.minWordLength)
            {
                SetFeedback($"Words need at least {_config.minWordLength} letters.");
                FlashSelection();
                return;
            }

            if (_selectionOrder.Count > _config.maxWordLength)
            {
                SetFeedback($"Words can be at most {_config.maxWordLength} letters.");
                FlashSelection();
                return;
            }

            var sb = new StringBuilder();
            foreach (var tile in _selectionOrder) sb.Append(tile.Letter);
            string word = sb.ToString();

            if (!_dictionary.IsValidWord(word))
            {
                SetFeedback($"\"{word}\" is not in the dictionary.");
                FlashSelection();
                return;
            }

            if (word.Length >= _spawnConfig.largeWordRewardLength)
                _pendingRewardLetters += _spawnConfig.largeWordRewardLetterCount;

            var result = _combatResolver.ResolveCast(word);
            SetFeedback(string.Empty); // Damage breakdown intentionally not shown on screen; see the floating number beside the enemy instead.
            SpellCast?.Invoke(result);

            DestroySelectedAndRefill();
        }

        private void FlashSelection()
        {
            foreach (var tile in _selectionOrder) tile.FlashInvalid();
        }

        private void DestroySelectedAndRefill()
        {
            var destroyedTiles = new HashSet<TileView>(_selectionOrder);
            _selectionOrder.Clear();

            // Phase 1: gravity. Surviving letters per column settle to the bottom rows;
            // collect the (now-empty) top slots per column to be refilled in Phase 2.
            var emptySlots = new List<(int row, int col)>();

            for (int c = 0; c < _config.columns; c++)
            {
                var surviving = new List<char>();
                for (int r = 0; r < _config.rows; r++)
                {
                    var tile = _tiles[r, c];
                    if (!destroyedTiles.Contains(tile))
                        surviving.Add(tile.Letter);
                    else
                        tile.SetSelected(false);
                }

                int destroyedInColumn = _config.rows - surviving.Count;
                for (int r = 0; r < surviving.Count; r++)
                    _tiles[destroyedInColumn + r, c].SetLetter(surviving[r]);

                for (int r = 0; r < destroyedInColumn; r++)
                    emptySlots.Add((r, c));
            }

            // Phase 2: refill empty slots with retries against the board playability check.
            int attempts = 0;
            bool valid;
            do
            {
                FillEmptySlots(emptySlots);
                valid = IsBoardValid();
                attempts++;
            } while (!valid && attempts < _spawnConfig.maxRefillRetries);

            if (!valid)
                Debug.LogWarning("Lexicon Legends: refill could not reach the minimum valid-word count after retries; using last attempt.");

            UpdatePreview();
        }

        private void FillEmptySlots(List<(int row, int col)> emptySlots)
        {
            int rewardLettersToUse = _pendingRewardLetters;
            _pendingRewardLetters = 0;

            foreach (var cell in emptySlots)
            {
                char letter;
                if (rewardLettersToUse > 0)
                {
                    letter = _spawner.SpawnGuaranteedRareLetter();
                    rewardLettersToUse--;
                }
                else
                {
                    letter = _spawner.SpawnLetter(CountVowelsOnBoard());
                }
                _tiles[cell.row, cell.col].SetLetter(letter);
            }
        }

        // ---------------------------------------------------------------
        // Board state helpers
        // ---------------------------------------------------------------

        private int CountVowelsOnBoard()
        {
            int count = 0;
            string vowels = _spawnConfig.vowelLetters;
            for (int r = 0; r < _config.rows; r++)
            for (int c = 0; c < _config.columns; c++)
                if (vowels.IndexOf(_tiles[r, c].Letter) >= 0) count++;
            return count;
        }

        private bool IsBoardValid()
        {
            var signature = BoardValidator.BuildSignature(_tiles, _config.rows, _config.columns);
            return BoardValidator.HasMinimumValidWords(signature, _dictionary, _config.minWordLength,
                _config.maxWordLength, _spawnConfig.minValidWordsAfterRefill);
        }
    }
}
