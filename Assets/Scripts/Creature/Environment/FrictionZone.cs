using System.Collections.Generic;
using UnityEngine;

namespace Neuro.Creature.Environment
{
    public class FrictionZone : EnvironmentZone
    {
        public float dragMultiplier = 2f;
        private readonly Dictionary<Rigidbody2D, float> originalDrag = new Dictionary<Rigidbody2D, float>();

        protected override void Reset()
        {
            base.Reset();
            environmentTag = "friction_floor";
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            base.OnTriggerEnter2D(other);
            Rigidbody2D body = other.attachedRigidbody;
            if (body != null && !originalDrag.ContainsKey(body))
            {
                originalDrag.Add(body, body.drag);
                body.drag *= dragMultiplier;
            }
        }

        protected override void OnTriggerExit2D(Collider2D other)
        {
            base.OnTriggerExit2D(other);
            Rigidbody2D body = other.attachedRigidbody;
            if (body != null && originalDrag.ContainsKey(body))
            {
                body.drag = originalDrag[body];
                originalDrag.Remove(body);
            }
        }
    }
}
