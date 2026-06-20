using UnityEngine;

namespace LexiconLegends.Config
{
    [CreateAssetMenu(fileName = "GridConfig", menuName = "Lexicon Legends/Grid Config")]
    public class GridConfig : ScriptableObject
    {
        [Header("Grid Dimensions (Section 4)")]
        [Min(1)] public int rows = 4;
        [Min(1)] public int columns = 5;

        [Header("Word Rules (Section 4)")]
        [Min(1)] public int minWordLength = 3;
        [Min(1)] public int maxWordLength = 10;

        [Header("Visuals")]
        [Min(0f)] public float tileSpacing = 10f;
        [Min(0f)] public float newTilePopDuration = 0.12f;
    }
}
