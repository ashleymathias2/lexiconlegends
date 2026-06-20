using System.Collections.Generic;
using System.Text;
using LexiconLegends.Config;
using LexiconLegends.Dictionary;
using TMPro;
using UnityEngine;

namespace LexiconLegends.Grid
{
    /// <summary>
    /// Stage 1 word grid: free-tile-selection input, word preview, and the
    /// destroy + gravity + refill loop (GDD Sections 4 and 5).
    /// Refill in this stage uses a uniform-random placeholder pool; the weighted
    /// spawn distribution, vowel balance rule, and rare-letter cooldown arrive in Stage 2.
    /// </summary>
    public class WordGridManager : MonoBehaviour
    {
        private GridConfig _config;
        private WordDictionary _dictionary;
        private System.Random _rng;

        private TileView[,] _tiles;
        private readonly List<TileView> _selectionOrder = new List<TileView>();

        private TextMeshProUGUI _previewLabel;
        private TextMeshProUGUI _feedbackLabel;

        public void Init(GridConfig config, WordDictionary dictionary, TileView[,] tiles,
            TextMeshProUGUI previewLabel, TextMeshProUGUI feedbackLabel)
        {
            _config = config;
            _dictionary = dictionary;
            _tiles = tiles;
            _previewLabel = previewLabel;
            _feedbackLabel = feedbackLabel;
            _rng = new System.Random();

            for (int r = 0; r < _config.rows; r++)
            for (int c = 0; c < _config.columns; c++)
                _tiles[r, c].Clicked += OnTileClicked;

            GenerateInitialBoard();
            UpdatePreview();
        }

        private void GenerateInitialBoard()
        {
            for (int r = 0; r < _config.rows; r++)
            for (int c = 0; c < _config.columns; c++)
                _tiles[r, c].SetLetter(RandomPlaceholderLetter());
        }

        private char RandomPlaceholderLetter()
        {
            var pool = _config.placeholderRefillPool;
            return pool[_rng.Next(pool.Length)];
        }

        private void OnTileClicked(TileView tile)
        {
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

        public void Reshuffle()
        {
            ClearSelection();

            var letters = new List<char>();
            for (int r = 0; r < _config.rows; r++)
            for (int c = 0; c < _config.columns; c++)
                letters.Add(_tiles[r, c].Letter);

            // Fisher-Yates shuffle.
            for (int i = letters.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (letters[i], letters[j]) = (letters[j], letters[i]);
            }

            int idx = 0;
            for (int r = 0; r < _config.rows; r++)
            for (int c = 0; c < _config.columns; c++)
                _tiles[r, c].SetLetter(letters[idx++]);

            SetFeedback("Board reshuffled.");
        }

        public void Confirm()
        {
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

            SetFeedback($"\"{word}\" confirmed!");
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

            for (int c = 0; c < _config.columns; c++)
            {
                var surviving = new List<char>();
                for (int r = 0; r < _config.rows; r++)
                {
                    var tile = _tiles[r, c];
                    if (!destroyedTiles.Contains(tile))
                        surviving.Add(tile.Letter);
                }

                int destroyedInColumn = _config.rows - surviving.Count;

                // Tiles fall downward: surviving letters settle to the bottom rows,
                // new letters refill the gap left at the top of the column.
                for (int r = 0; r < destroyedInColumn; r++)
                    _tiles[r, c].SetLetter(RandomPlaceholderLetter());

                for (int r = 0; r < surviving.Count; r++)
                    _tiles[destroyedInColumn + r, c].SetLetter(surviving[r]);

                foreach (var tile in destroyedTiles)
                    if (tile.Col == c) tile.SetSelected(false);
            }

            UpdatePreview();
        }
    }
}
