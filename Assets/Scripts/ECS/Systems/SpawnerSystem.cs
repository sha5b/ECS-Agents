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
        private bool initialSpawnDone = false;
        private const int INITIAL_NPC_COUNT = 10;
        private const float SPAWN_AREA_SIZE = 50f;

        public SpawnerSystem(World world)
        {
            this.world = world;
            this.entities = world.GetEntities();
        }

        public void Update(float deltaTime)
        {
            if (!initialSpawnDone)
            {
                SpawnInitialEntities();
                initialSpawnDone = true;
            }
        }

        private void SpawnInitialEntities()
        {
            Debug.Log("Spawning initial NPCs...");

            // Spawn NPCs
            for (int i = 0; i < INITIAL_NPC_COUNT; i++)
            {
                SpawnNPC();
            }

            // Spawn some resources
            SpawnResources();
        }

        private void SpawnNPC()
        {
            Vector3 position = new Vector3(
                Random.Range(-SPAWN_AREA_SIZE, SPAWN_AREA_SIZE),
                0f,
                Random.Range(-SPAWN_AREA_SIZE, SPAWN_AREA_SIZE)
            );

            var npc = world.CreateEntity();

            // Add components with randomized attributes
            npc.AddComponent(new Position3DComponent(position));
            
            var physical = new PhysicalComponent(
                size: Random.Range(0.8f, 1.2f),
                mass: Random.Range(60f, 90f),
                maxSpeed: Random.Range(4f, 6f),
                strength: Random.Range(0.7f, 1.3f),
                stamina: Random.Range(80f, 120f)
            );
            npc.AddComponent(physical);

            var social = new SocialComponent(
                extroversion: Random.Range(0.3f, 1f),
                agreeableness: Random.Range(0.3f, 1f),
                trustworthiness: Random.Range(0.3f, 1f)
            );
            npc.AddComponent(social);

            var behavior = new BehaviorComponent(
                sociability: Random.Range(0.3f, 1f),
                productivity: Random.Range(0.3f, 1f),
                curiosity: Random.Range(0.3f, 1f),
                resilience: Random.Range(0.3f, 1f)
            );
            npc.AddComponent(behavior);

            npc.AddComponent(new NeedComponent());
            npc.AddComponent(new MemoryComponent());
            npc.AddComponent(new TaskComponent());

            Debug.Log($"Spawned NPC {npc.Id} at {position}");
            Debug.Log($"Physical Attributes - Size: {physical.Size:F2}, Speed: {physical.MaxSpeed:F2}, Strength: {physical.Strength:F2}");
            Debug.Log($"Social Attributes - Extroversion: {social.Extroversion:F2}, Agreeableness: {social.Agreeableness:F2}");
            Debug.Log($"Behavior Traits - Sociability: {behavior.GetSocialPreference():F2}, Curiosity: {behavior.GetExplorationPreference():F2}");
        }

        private void SpawnResources()
        {
            // Spawn food sources
            for (int i = 0; i < 5; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-SPAWN_AREA_SIZE, SPAWN_AREA_SIZE),
                    0f,
                    Random.Range(-SPAWN_AREA_SIZE, SPAWN_AREA_SIZE)
                );

                var resource = world.CreateEntity();
                resource.AddComponent(new Position3DComponent(position));
                resource.AddComponent(ResourceComponent.CreateFoodSource(
                    quantity: Random.Range(80f, 120f),
                    quality: Random.Range(0.6f, 1f)
                ));

                Debug.Log($"Spawned Food Source at {position}");
            }

            // Spawn water sources
            for (int i = 0; i < 3; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-SPAWN_AREA_SIZE, SPAWN_AREA_SIZE),
                    0f,
                    Random.Range(-SPAWN_AREA_SIZE, SPAWN_AREA_SIZE)
                );

                var resource = world.CreateEntity();
                resource.AddComponent(new Position3DComponent(position));
                resource.AddComponent(ResourceComponent.CreateWaterSource(infinite: true));

                Debug.Log($"Spawned Water Source at {position}");
            }

            // Spawn rest spots
            for (int i = 0; i < 4; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-SPAWN_AREA_SIZE, SPAWN_AREA_SIZE),
                    0f,
                    Random.Range(-SPAWN_AREA_SIZE, SPAWN_AREA_SIZE)
                );

                var resource = world.CreateEntity();
                resource.AddComponent(new Position3DComponent(position));
                resource.AddComponent(ResourceComponent.CreateRestSpot());

                Debug.Log($"Spawned Rest Spot at {position}");
            }
        }
    }
}
