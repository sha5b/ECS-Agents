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

        public NeedSystem(World world)
        {
            this.world = world;
            this.entities = world.GetEntities();
        }

        public void Update(float deltaTime)
        {
            foreach (var entity in entities)
            {
                var needComponent = entity.GetComponent<NeedComponent>();
                if (needComponent == null) continue;

                // Update all needs based on decay rates
                needComponent.UpdateNeeds(deltaTime);

                // Check for critical needs and trigger events/behaviors
                foreach (NeedType need in System.Enum.GetValues(typeof(NeedType)))
                {
                    if (need == NeedType.None) continue;

                    if (needComponent.IsNeedCritical(need))
                    {
                        HandleCriticalNeed(entity, need);
                    }
                }
            }
        }

        private void HandleCriticalNeed(Entity entity, NeedType need)
        {
            // This will be expanded when we implement the behavior system
            // For now, just log the critical need
            Debug.LogWarning($"Entity {entity.Id} has critical {need} need!");

            // Future implementations:
            // 1. Notify behavior system to prioritize this need
            // 2. Look for nearby resources that can satisfy this need
            // 3. Update entity's task queue to address the need
            // 4. Trigger appropriate animations or visual feedback
        }

        public void ModifyNeed(Entity entity, NeedType need, float amount)
        {
            var needComponent = entity.GetComponent<NeedComponent>();
            if (needComponent != null)
            {
                needComponent.ModifyNeed(need, amount);
            }
        }

        public NeedType GetMostUrgentNeed(Entity entity)
        {
            var needComponent = entity.GetComponent<NeedComponent>();
            return needComponent?.GetMostUrgentNeed() ?? NeedType.None;
        }

        public bool HasUrgentNeeds(Entity entity)
        {
            var needComponent = entity.GetComponent<NeedComponent>();
            if (needComponent == null) return false;

            foreach (NeedType need in System.Enum.GetValues(typeof(NeedType)))
            {
                if (need == NeedType.None) continue;
                if (needComponent.IsNeedUrgent(need))
                {
                    return true;
                }
            }
            return false;
        }

        public Dictionary<NeedType, float> GetAllNeedValues(Entity entity)
        {
            var needComponent = entity.GetComponent<NeedComponent>();
            if (needComponent == null) return null;

            var needValues = new Dictionary<NeedType, float>();
            foreach (NeedType need in System.Enum.GetValues(typeof(NeedType)))
            {
                if (need == NeedType.None) continue;
                needValues[need] = needComponent.GetNeedValue(need);
            }
            return needValues;
        }
    }
}
