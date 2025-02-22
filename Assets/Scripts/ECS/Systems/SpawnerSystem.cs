using UnityEngine;
using ECS.Core;
using ECS.Components;
using System.Collections.Generic;

namespace ECS.Systems
{
    public class SpawnerSystem : ISystem
    {
        private World world;
        private List<Entity> entities;
        private float nextSpawnTime;
        private const float SPAWN_INTERVAL = 10f;
        private const int MAX_NPCS = 20;
        private const float WORLD_BOUNDS = 100f;

        public SpawnerSystem(World world)
        {
            this.world = world;
            this.entities = world.GetEntities();
            this.nextSpawnTime = Time.time + SPAWN_INTERVAL;
        }

        public void Update(float deltaTime)
        {
            if (Time.time >= nextSpawnTime)
            {
                SpawnEntities();
                nextSpawnTime = Time.time + SPAWN_INTERVAL;
            }
        }

        private void SpawnEntities()
        {
            int npcCount = CountNPCs();
            if (npcCount < MAX_NPCS)
            {
                SpawnNPC();
            }

            // Spawn resources if needed
            EnsureResourcesExist();
        }

        private void SpawnNPC()
        {
            Vector3 position = GetRandomPosition();
            Entity npc = world.CreateEntity();

            // Add core components
            npc.AddComponent(new Position3DComponent(position));
            npc.AddComponent(new NeedComponent());
            npc.AddComponent(new BehaviorComponent(
                sociability: Random.Range(0.3f, 1f),
                productivity: Random.Range(0.3f, 1f),
                curiosity: Random.Range(0.3f, 1f),
                resilience: Random.Range(0.3f, 1f)
            ));

            Debug.Log($"Spawned NPC at position {position}");
        }

        private Vector3 GetRandomPosition()
        {
            float x = Random.Range(-WORLD_BOUNDS, WORLD_BOUNDS);
            float z = Random.Range(-WORLD_BOUNDS, WORLD_BOUNDS);
            return new Vector3(x, 0, z);
        }

        private void EnsureResourcesExist()
        {
            // Count existing resources
            int foodSources = CountResourcesOfType(ResourceType.Food);
            int waterSources = CountResourcesOfType(ResourceType.Water);
            int restSpots = CountResourcesOfType(ResourceType.RestSpot);

            // Spawn resources if needed
            if (foodSources < 5) SpawnResource(ResourceType.Food);
            if (waterSources < 3) SpawnResource(ResourceType.Water);
            if (restSpots < 3) SpawnResource(ResourceType.RestSpot);
        }

        private void SpawnResource(ResourceType type)
        {
            Vector3 position = GetRandomPosition();
            Entity resource = world.CreateEntity();

            // Add core components
            resource.AddComponent(new Position3DComponent(position));

            // Add resource component based on type
            ResourceComponent resourceComponent = type switch
            {
                ResourceType.Food => ResourceComponent.CreateFoodSource(
                    quantity: Random.Range(50f, 150f),
                    quality: Random.Range(0.6f, 1f)
                ),
                ResourceType.Water => ResourceComponent.CreateWaterSource(
                    infinite: Random.value > 0.7f // 30% chance of infinite water source
                ),
                ResourceType.RestSpot => ResourceComponent.CreateRestSpot(),
                _ => new ResourceComponent(type)
            };

            resource.AddComponent(resourceComponent);
            Debug.Log($"Spawned {type} resource at position {position}");
        }

        private int CountNPCs()
        {
            int count = 0;
            foreach (var entity in entities)
            {
                if (entity.HasComponent<NeedComponent>() && 
                    entity.HasComponent<BehaviorComponent>())
                {
                    count++;
                }
            }
            return count;
        }

        private int CountResourcesOfType(ResourceType type)
        {
            int count = 0;
            foreach (var entity in entities)
            {
                var resource = entity.GetComponent<ResourceComponent>();
                if (resource != null && resource.Type == type)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
