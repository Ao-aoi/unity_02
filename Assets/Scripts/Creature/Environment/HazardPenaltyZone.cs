using UnityEngine;

namespace Neuro.Creature.Environment
{
    public class HazardPenaltyZone : EnvironmentZone
    {
        public float damagePerSecond = 10f;

        protected override void Reset()
        {
            base.Reset();
            environmentTag = "hazard";
            stayPenaltyPerSecond = 2f;
        }

        protected override void OnTriggerStay2D(Collider2D other)
        {
            base.OnTriggerStay2D(other);
            CreatureAgent agent = other.GetComponentInParent<CreatureAgent>();
            if (agent != null && damagePerSecond != 0f)
                agent.ApplyDamage(damagePerSecond * Time.deltaTime);
        }
    }
}
