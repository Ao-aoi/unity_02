namespace Neuro.Creature.Environment
{
    public class BridgeTraversalReward : EnvironmentZone
    {
        protected override void Reset()
        {
            base.Reset();
            environmentTag = "bridge";
            enterReward = 1f;
            stayRewardPerSecond = 0.2f;
        }
    }
}
