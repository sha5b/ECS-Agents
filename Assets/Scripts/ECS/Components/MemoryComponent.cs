using UnityEngine;
using ECS.Core;
using System.Collections.Generic;

namespace ECS.Components
{
    public class MemoryComponent : IComponent
    {
        // Location memories
        private Dictionary<string, LocationMemory> locationMemories;
        
        // Entity memories
        private Dictionary<int, EntityMemory> entityMemories;
        
        // Memory settings
        private const float MEMORY_DECAY_RATE = 0.1f;
        private const float MEMORY_THRESHOLD = 0.2f;
        private const int MAX_MEMORIES = 50;

        public MemoryComponent()
        {
            locationMemories = new Dictionary<string, LocationMemory>();
            entityMemories = new Dictionary<int, EntityMemory>();
        }

        public void RememberLocation(string key, Vector3 position, float importance = 1f)
        {
            locationMemories[key] = new LocationMemory
            {
                Position = position,
                LastVisitTime = Time.time,
                Importance = importance,
                VisitCount = locationMemories.ContainsKey(key) ? 
                    locationMemories[key].VisitCount + 1 : 1
            };

            // Cleanup old memories if we exceed the limit
            if (locationMemories.Count > MAX_MEMORIES)
            {
                CleanupOldMemories();
            }
        }

        public void RememberEntity(Entity entity, float impression)
        {
            if (!entityMemories.ContainsKey(entity.Id))
            {
                entityMemories[entity.Id] = new EntityMemory
                {
                    LastInteractionTime = Time.time,
                    InteractionCount = 1,
                    Impression = impression
                };
            }
            else
            {
                var memory = entityMemories[entity.Id];
                memory.LastInteractionTime = Time.time;
                memory.InteractionCount++;
                memory.Impression = Mathf.Lerp(memory.Impression, impression, 0.3f);
            }
        }

        public bool TryRecallLocation(string key, out Vector3 position)
        {
            position = Vector3.zero;
            
            if (locationMemories.TryGetValue(key, out LocationMemory memory))
            {
                float timeSinceLastVisit = Time.time - memory.LastVisitTime;
                float memoryStrength = CalculateMemoryStrength(memory.Importance, timeSinceLastVisit, memory.VisitCount);
                
                if (memoryStrength > MEMORY_THRESHOLD)
                {
                    position = memory.Position;
                    return true;
                }
            }
            
            return false;
        }

        public float RecallEntityImpression(Entity entity)
        {
            if (entityMemories.TryGetValue(entity.Id, out EntityMemory memory))
            {
                float timeSinceLastInteraction = Time.time - memory.LastInteractionTime;
                float memoryStrength = CalculateMemoryStrength(
                    Mathf.Abs(memory.Impression),
                    timeSinceLastInteraction,
                    memory.InteractionCount
                );

                if (memoryStrength > MEMORY_THRESHOLD)
                {
                    return memory.Impression;
                }
            }
            
            return 0f;
        }

        private float CalculateMemoryStrength(float importance, float timeSince, int repeatCount)
        {
            float timeDecay = Mathf.Exp(-MEMORY_DECAY_RATE * timeSince);
            float repetitionBonus = Mathf.Log(repeatCount + 1, 2);
            return importance * timeDecay * (1f + repetitionBonus);
        }

        private void CleanupOldMemories()
        {
            var locationsToRemove = new List<string>();
            
            foreach (var kvp in locationMemories)
            {
                float timeSinceLastVisit = Time.time - kvp.Value.LastVisitTime;
                float memoryStrength = CalculateMemoryStrength(
                    kvp.Value.Importance,
                    timeSinceLastVisit,
                    kvp.Value.VisitCount
                );

                if (memoryStrength < MEMORY_THRESHOLD)
                {
                    locationsToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in locationsToRemove)
            {
                locationMemories.Remove(key);
            }
        }

        private struct LocationMemory
        {
            public Vector3 Position;
            public float LastVisitTime;
            public float Importance;
            public int VisitCount;
        }

        private struct EntityMemory
        {
            public float LastInteractionTime;
            public int InteractionCount;
            public float Impression;
        }
    }
}
