namespace LexiconLegends.Combat
{
    /// <summary>GDD Section 7 proximity/emoji escalation stages, driven by the Aggression Meter fill.</summary>
    public enum EnemyStage
    {
        Neutral,  // starting distance, neutral expression
        Stage1,   // ~33%: steps closer, Angry (😠)
        Stage2,   // ~66%: steps closer again, Shaking angry (😡)
        Stage3    // 100%: reaches the player, Steaming angry (🤬) — attacks immediately
    }
}
