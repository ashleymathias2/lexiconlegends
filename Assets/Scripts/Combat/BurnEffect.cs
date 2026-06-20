namespace LexiconLegends.Combat
{
    /// <summary>
    /// An active Burn DoT. A "turn" is one confirmed word — combat here is entirely
    /// word-paced, there's no real-time clock to hang a turn definition on otherwise.
    /// </summary>
    public class BurnEffect
    {
        public float TickDamage;
        public int RemainingTurns;

        public BurnEffect(float tickDamage, int turns)
        {
            TickDamage = tickDamage;
            RemainingTurns = turns;
        }
    }
}
