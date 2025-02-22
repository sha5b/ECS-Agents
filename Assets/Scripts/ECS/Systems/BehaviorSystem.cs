using UnityEngine;
using ECS.Core;
using ECS.Components;
using System.Collections.Generic;

namespace ECS.Systems
{
    public class BehaviorSystem : ISystem
    {
        private World world;
        private List<Entity> entities;
        private NeedSystem needSystem;

        public BehaviorSystem(World world, NeedSystem needSystem)
        {
            this.world = world;
            this.entities = world.GetEntities();
            this.needSystem = needSystem;
        }

        public void Update(float deltaTime)
        {
            foreach (var entity in entities)
            {
                var behaviorComponent = entity.GetComponent<BehaviorComponent>();
                var needComponent = entity.GetComponent<NeedComponent>();
                
                if (behaviorComponent == null || needComponent == null) continue;

                // Only make new decisions if enough time has passed
                if (!behaviorComponent.CanMakeNewDecision(deltaTime)) continue;

                // Get current state info
                var currentState = behaviorComponent.CurrentState;
                var urgentNeed = needSystem.GetMostUrgentNeed(entity);

                // Decision making process
                DecideNextAction(entity, behaviorComponent, needComponent, urgentNeed);
            }
        }

        private void DecideNextAction(
            Entity entity,
            BehaviorComponent behavior,
            NeedComponent needs,
            NeedType urgentNeed)
        {
            // If we're already handling an urgent need, continue unless it's satisfied
            if (IsHandlingNeed(behavior.CurrentState, urgentNeed) && 
                !needs.IsNeedSatisfied(urgentNeed))
            {
                return;
            }

            // Handle critical needs first
            if (urgentNeed != NeedType.None && needs.IsNeedCritical(urgentNeed))
            {
                SwitchToNeedState(entity, behavior, urgentNeed);
                return;
            }

            // If no critical needs, consider personality-driven behaviors
            if (behavior.CurrentState == BehaviorState.Idle)
            {
                DecideIdleAction(entity, behavior, needs);
            }
        }

        private bool IsHandlingNeed(BehaviorState state, NeedType need)
        {
            return (state, need) switch
            {
                (BehaviorState.SeekingFood, NeedType.Hunger) => true,
                (BehaviorState.SeekingWater, NeedType.Thirst) => true,
                (BehaviorState.Resting, NeedType.Energy) => true,
                (BehaviorState.Socializing, NeedType.Social) => true,
                _ => false
            };
        }

        private void SwitchToNeedState(Entity entity, BehaviorComponent behavior, NeedType need)
        {
            switch (need)
            {
                case NeedType.Hunger:
                    // Try to find remembered food source
                    if (behavior.TryGetRememberedLocation("FoodSource", out Vector3 foodPos))
                    {
                        behavior.UpdateState(BehaviorState.SeekingFood, foodPos);
                    }
                    else
                    {
                        behavior.UpdateState(BehaviorState.Exploring); // Look for food
                    }
                    break;

                case NeedType.Thirst:
                    if (behavior.TryGetRememberedLocation("WaterSource", out Vector3 waterPos))
                    {
                        behavior.UpdateState(BehaviorState.SeekingWater, waterPos);
                    }
                    else
                    {
                        behavior.UpdateState(BehaviorState.Exploring); // Look for water
                    }
                    break;

                case NeedType.Energy:
                    if (behavior.TryGetRememberedLocation("RestPlace", out Vector3 restPos))
                    {
                        behavior.UpdateState(BehaviorState.Resting, restPos);
                    }
                    else
                    {
                        // Find safe place to rest
                        behavior.UpdateState(BehaviorState.Exploring);
                    }
                    break;

                case NeedType.Social:
                    // Find nearby entities to socialize with
                    var friendlyEntities = behavior.GetFriendlyEntities();
                    if (friendlyEntities.Count > 0)
                    {
                        // Pick the closest or most friendly entity
                        behavior.UpdateState(BehaviorState.Socializing, null, friendlyEntities[0]);
                    }
                    else
                    {
                        behavior.UpdateState(BehaviorState.Exploring); // Look for others
                    }
                    break;
            }
        }

        private void DecideIdleAction(Entity entity, BehaviorComponent behavior, NeedComponent needs)
        {
            // List of possible actions with base weights
            var actions = new Dictionary<BehaviorState, float>
            {
                { BehaviorState.Working, behavior.GetWorkPreference() },
                { BehaviorState.Exploring, behavior.GetExplorationPreference() },
                { BehaviorState.Socializing, behavior.GetSocialPreference() }
            };

            // Modify weights based on current needs
            foreach (var need in System.Enum.GetValues(typeof(NeedType)))
            {
                if ((NeedType)need == NeedType.None) continue;

                float needValue = needs.GetNeedValue((NeedType)need);
                float urgency = (100f - needValue) / 100f;

                // Adjust relevant action weights
                switch ((NeedType)need)
                {
                    case NeedType.Social:
                        actions[BehaviorState.Socializing] *= 1f + urgency;
                        break;
                    case NeedType.Energy:
                        actions[BehaviorState.Working] *= 1f - (urgency * 0.5f);
                        break;
                }
            }

            // Choose action based on weighted random selection
            float totalWeight = 0f;
            foreach (var weight in actions.Values)
            {
                totalWeight += weight;
            }

            float random = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var action in actions)
            {
                currentWeight += action.Value;
                if (random <= currentWeight)
                {
                    behavior.UpdateState(action.Key);
                    break;
                }
            }
        }

        // Helper method to find nearest entity with a specific component type
        private Entity FindNearestEntity<T>(Entity source) where T : class, IComponent
        {
            var sourcePos = source.GetComponent<Position3DComponent>();
            if (sourcePos == null) return null;

            Entity nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var entity in entities)
            {
                if (entity == source) continue;
                if (!entity.HasComponent<T>()) continue;

                var targetPos = entity.GetComponent<Position3DComponent>();
                if (targetPos == null) continue;

                float distance = Vector3.Distance(sourcePos.Position, targetPos.Position);
                if (distance < nearestDistance)
                {
                    nearest = entity;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }
    }
}
