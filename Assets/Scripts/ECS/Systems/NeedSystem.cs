using UnityEngine;
using ECS.Core;
using ECS.Components;
using System.Collections.Generic;

namespace ECS.Systems
{
    public class NeedSystem : ISystem
    {
        private World world;
        private List<Entity> entities;
        private float nextDebugTime;
        private const float DEBUG_INTERVAL = 5f; // Log needs every 5 seconds

        // Need change rates per second
        private const float HUNGER_RATE = 2f;
        private const float THIRST_RATE = 3f;
        private const float ENERGY_RATE = 1f;
        private const float SOCIAL_RATE = 1.5f;

        // Critical thresholds
        private const float CRITICAL_THRESHOLD = 20f;
        private const float URGENT_THRESHOLD = 40f;
        private const float SATISFIED_THRESHOLD = 80f;

        public NeedSystem(World world)
        {
            this.world = world;
            this.entities = world.GetEntities();
            this.nextDebugTime = Time.time + DEBUG_INTERVAL;
        }

        public void Update(float deltaTime)
        {
            foreach (var entity in entities)
            {
                var needs = entity.GetComponent<NeedComponent>();
                var physical = entity.GetComponent<PhysicalComponent>();
                var behavior = entity.GetComponent<BehaviorComponent>();

                if (needs == null) continue;

                // Update needs based on current state and activities
                UpdateNeeds(entity, needs, physical, behavior, deltaTime);

                // Log needs status if it's time
                if (Time.time >= nextDebugTime)
                {
                    LogNeedsStatus(entity, needs);
                }
            }

            // Update next debug time
            if (Time.time >= nextDebugTime)
            {
                nextDebugTime = Time.time + DEBUG_INTERVAL;
            }
        }

        private void UpdateNeeds(
            Entity entity,
            NeedComponent needs,
            PhysicalComponent physical,
            BehaviorComponent behavior,
            float deltaTime)
        {
            float hungerModifier = 1f;
            float thirstModifier = 1f;
            float energyModifier = 1f;
            float socialModifier = 1f;

            // Modify rates based on physical state
            if (physical != null)
            {
                float activityLevel = physical.MovementSpeed / physical.MaxSpeed;
                hungerModifier *= (1f + activityLevel * 0.5f);
                thirstModifier *= (1f + activityLevel * 0.5f);
                energyModifier *= (1f + activityLevel);
            }

            // Modify rates based on behavior state
            if (behavior != null)
            {
                switch (behavior.CurrentState)
                {
                    case BehaviorState.Working:
                        energyModifier *= 1.5f;
                        break;
                    case BehaviorState.Exploring:
                        energyModifier *= 1.2f;
                        hungerModifier *= 1.2f;
                        break;
                    case BehaviorState.Resting:
                        energyModifier *= 0.5f;
                        break;
                    case BehaviorState.Socializing:
                        socialModifier *= 0.5f;
                        break;
                }
            }

            // Apply need changes
            needs.ModifyNeed(NeedType.Hunger, -HUNGER_RATE * hungerModifier * deltaTime);
            needs.ModifyNeed(NeedType.Thirst, -THIRST_RATE * thirstModifier * deltaTime);
            needs.ModifyNeed(NeedType.Energy, -ENERGY_RATE * energyModifier * deltaTime);
            needs.ModifyNeed(NeedType.Social, -SOCIAL_RATE * socialModifier * deltaTime);
        }

        private void LogNeedsStatus(Entity entity, NeedComponent needs)
        {
            string status = $"Entity {entity.Id} Needs:\n" +
                          $"  Hunger: {needs.GetNeedValue(NeedType.Hunger):F1} " +
                          $"({GetNeedStatus(needs, NeedType.Hunger)})\n" +
                          $"  Thirst: {needs.GetNeedValue(NeedType.Thirst):F1} " +
                          $"({GetNeedStatus(needs, NeedType.Thirst)})\n" +
                          $"  Energy: {needs.GetNeedValue(NeedType.Energy):F1} " +
                          $"({GetNeedStatus(needs, NeedType.Energy)})\n" +
                          $"  Social: {needs.GetNeedValue(NeedType.Social):F1} " +
                          $"({GetNeedStatus(needs, NeedType.Social)})";

            Debug.Log(status);
        }

        private string GetNeedStatus(NeedComponent needs, NeedType type)
        {
            float value = needs.GetNeedValue(type);
            if (value <= CRITICAL_THRESHOLD) return "CRITICAL";
            if (value <= URGENT_THRESHOLD) return "Urgent";
            if (value >= SATISFIED_THRESHOLD) return "Satisfied";
            return "Normal";
        }

        public NeedType GetMostUrgentNeed(Entity entity)
        {
            var needs = entity.GetComponent<NeedComponent>();
            if (needs == null) return NeedType.None;

            NeedType mostUrgent = NeedType.None;
            float lowestValue = float.MaxValue;

            foreach (NeedType type in System.Enum.GetValues(typeof(NeedType)))
            {
                if (type == NeedType.None) continue;

                float value = needs.GetNeedValue(type);
                if (value < lowestValue)
                {
                    lowestValue = value;
                    mostUrgent = type;
                }
            }

            return mostUrgent;
        }

        public bool IsNeedCritical(Entity entity, NeedType type)
        {
            var needs = entity.GetComponent<NeedComponent>();
            return needs != null && needs.GetNeedValue(type) <= CRITICAL_THRESHOLD;
        }

        public bool IsNeedUrgent(Entity entity, NeedType type)
        {
            var needs = entity.GetComponent<NeedComponent>();
            return needs != null && needs.GetNeedValue(type) <= URGENT_THRESHOLD;
        }

        public bool IsNeedSatisfied(Entity entity, NeedType type)
        {
            var needs = entity.GetComponent<NeedComponent>();
            return needs != null && needs.GetNeedValue(type) >= SATISFIED_THRESHOLD;
        }
    }
}
