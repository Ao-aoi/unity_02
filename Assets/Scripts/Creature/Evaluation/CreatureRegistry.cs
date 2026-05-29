using System.Collections.Generic;

namespace Neuro.Creature.Evaluation
{
    public static class CreatureRegistry
    {
        private static readonly List<CreatureAgent> agents = new List<CreatureAgent>();
        public static IReadOnlyList<CreatureAgent> Agents => agents;

        public static void Register(CreatureAgent agent)
        {
            if (agent != null && !agents.Contains(agent))
                agents.Add(agent);
        }

        public static void Unregister(CreatureAgent agent)
        {
            if (agent != null)
                agents.Remove(agent);
        }
    }

    public static class FoodRegistry
    {
        private static readonly List<FoodItem> foods = new List<FoodItem>();
        public static IReadOnlyList<FoodItem> Foods => foods;

        public static void Register(FoodItem food)
        {
            if (food != null && !foods.Contains(food))
                foods.Add(food);
        }

        public static void Unregister(FoodItem food)
        {
            if (food != null)
                foods.Remove(food);
        }
    }
}
