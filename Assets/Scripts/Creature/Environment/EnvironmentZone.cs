using UnityEngine;

namespace Neuro.Creature.Environment
{
    [RequireComponent(typeof(Collider2D))]
    public class EnvironmentZone : MonoBehaviour
    {
        public string environmentTag = "environment_zone";
        public float enterReward;
        public float stayRewardPerSecond;
        public float enterPenalty;
        public float stayPenaltyPerSecond;

        protected virtual void Reset()
        {
            Collider2D zoneCollider = GetComponent<Collider2D>();
            if (zoneCollider != null)
                zoneCollider.isTrigger = true;
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            CreatureEnvironmentTracker tracker = GetTracker(other);
            if (tracker == null)
                return;

            tracker.EnterTag(environmentTag);
            tracker.AddReward(enterReward);
            tracker.AddPenalty(enterPenalty);
            OnCreatureEntered(tracker);
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            CreatureEnvironmentTracker tracker = GetTracker(other);
            if (tracker == null)
                return;

            tracker.AddReward(stayRewardPerSecond * Time.deltaTime);
            tracker.AddPenalty(stayPenaltyPerSecond * Time.deltaTime);
            OnCreatureStayed(tracker);
        }

        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            CreatureEnvironmentTracker tracker = GetTracker(other);
            if (tracker == null)
                return;

            tracker.ExitTag(environmentTag);
            OnCreatureExited(tracker);
        }

        protected CreatureEnvironmentTracker GetTracker(Collider2D other)
        {
            CreatureAgent agent = other.GetComponentInParent<CreatureAgent>();
            return agent != null ? agent.GetComponent<CreatureEnvironmentTracker>() : null;
        }

        protected virtual void OnCreatureEntered(CreatureEnvironmentTracker tracker) { }
        protected virtual void OnCreatureStayed(CreatureEnvironmentTracker tracker) { }
        protected virtual void OnCreatureExited(CreatureEnvironmentTracker tracker) { }
    }
}
