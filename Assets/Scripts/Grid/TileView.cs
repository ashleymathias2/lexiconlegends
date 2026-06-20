using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LexiconLegends.Grid
{
    /// <summary>
    /// Visual + input representation of a single grid tile. Supports free selection
    /// (tap to select/deselect, any order, no adjacency requirement) per GDD Section 4.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TileView : MonoBehaviour, IPointerClickHandler
    {
        public static readonly Color UnselectedColor = new Color(0.95f, 0.95f, 0.95f);
        public static readonly Color SelectedColor = new Color(1f, 0.85f, 0.35f);

        public event Action<TileView> Clicked;

        public char Letter { get; private set; }
        public bool IsSelected { get; private set; }
        public int Row { get; private set; }
        public int Col { get; private set; }

        private Image _background;
        private TextMeshProUGUI _label;

        public void Init(int row, int col, TextMeshProUGUI label, Image background)
        {
            Row = row;
            Col = col;
            _label = label;
            _background = background;
        }

        public void SetLetter(char letter)
        {
            Letter = char.ToUpperInvariant(letter);
            if (_label != null) _label.text = Letter.ToString();
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (_background != null) _background.color = selected ? SelectedColor : UnselectedColor;
        }

        public void FlashInvalid()
        {
            if (_background == null) return;
            StopAllCoroutines();
            StartCoroutine(FlashRoutine());
        }

        private System.Collections.IEnumerator FlashRoutine()
        {
            var original = _background.color;
            _background.color = new Color(0.9f, 0.3f, 0.3f);
            yield return new WaitForSeconds(0.15f);
            _background.color = original;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(this);
        }
    }
}
