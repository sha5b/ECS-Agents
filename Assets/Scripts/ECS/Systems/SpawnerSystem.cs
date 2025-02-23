using UnityEngine;
using UnityEngine.Animations;
using ECS.Core;
using ECS.Components;
using System.Collections.Generic;

namespace ECS.Systems
{
    public class SpawnerSystem : ISystem
    {
        private World world;
        private List<Entity> entities;
        private GameObject npcPrefab;

        public SpawnerSystem(World world)
        {
            this.world = world;
            this.entities = world.GetEntities();
            CreateNPCPrefab();
        }

        private void CreateNPCPrefab()
        {
            // Create a basic NPC prefab
            npcPrefab = new GameObject("NPC_Prefab");
            
            // Add a cube for the body
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(npcPrefab.transform);
            cube.transform.localPosition = Vector3.up; // Center the cube
            cube.transform.localScale = new Vector3(0.8f, 1.6f, 0.8f); // Make it more humanoid proportions
            
            // Add material with color
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = Color.yellow;
            cube.GetComponent<MeshRenderer>().material = material;
            
            // Add animator
            var animator = npcPrefab.AddComponent<Animator>();
            
            // For now, we'll just use the cube without animations
            // In a real implementation, you would create an Animator Controller in Unity
            // and assign it to the prefab, or load it from Resources

            // Don't destroy the prefab when loading new scenes
            GameObject.DontDestroyOnLoad(npcPrefab);
        }

        public void SpawnNPC(Vector3 position)
        {
            var npc = world.CreateEntity();

            // Add core components
            npc.AddComponent(new Position3DComponent(position));
            npc.AddComponent(new PhysicalComponent(
                size: Random.Range(0.8f, 1.2f),
                mass: Random.Range(60f, 90f),
                maxSpeed: Random.Range(4f, 6f),
                strength: Random.Range(0.7f, 1.3f),
                stamina: Random.Range(80f, 120f)
            ));

            // Add behavior and navigation
            npc.AddComponent(new BehaviorComponent(
                sociability: Random.Range(0.3f, 1f),
                productivity: Random.Range(0.3f, 1f),
                curiosity: Random.Range(0.3f, 1f),
                resilience: Random.Range(0.3f, 1f)
            ));
            npc.AddComponent(new NavMeshComponent(
                agentRadius: 0.5f,
                agentHeight: 2f,
                maxSlope: 45f,
                stepHeight: 0.4f
            ));

            // Add visual representation
            npc.AddComponent(new BodyComponent(npcPrefab));

            // Add other components
            npc.AddComponent(new NeedComponent());
            npc.AddComponent(new MemoryComponent());
            npc.AddComponent(new TaskComponent());
            npc.AddComponent(new SocialComponent());
        }

        public void Update(float deltaTime)
        {
            // Maintain desired population
            const int DESIRED_POPULATION = 10;
            const float RESPAWN_RANGE = 50f; // Wider range for respawns
            int currentPopulation = entities.Count;
            
            if (currentPopulation < DESIRED_POPULATION)
            {
                // Respawn at a random position in the wider area
                float angle = Random.Range(0f, 360f);
                float radius = Random.Range(10f, RESPAWN_RANGE); // Minimum distance from center
                Vector3 spawnPos = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                    0f,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius
                );
                SpawnNPC(spawnPos);
                Debug.Log($"Respawned NPC at {spawnPos}, Current population: {currentPopulation + 1}");
            }

            // Update NPC colors based on their state
            UpdateNPCColors();
        }

        private void UpdateNPCColors()
        {
            foreach (var entity in entities)
            {
                var body = entity.GetComponent<BodyComponent>();
                var behavior = entity.GetComponent<BehaviorComponent>();
                
                if (body?.ModelInstance == null || behavior == null) continue;

                var renderer = body.ModelInstance.GetComponentInChildren<MeshRenderer>();
                if (renderer == null) continue;

                Color color = Color.yellow; // Default color
                switch (behavior.CurrentState)
                {
                    case BehaviorState.SeekingFood:
                        color = Color.red;
                        break;
                    case BehaviorState.SeekingWater:
                        color = Color.blue;
                        break;
                    case BehaviorState.Socializing:
                        color = Color.green;
                        break;
                    case BehaviorState.Resting:
                        color = Color.gray;
                        break;
                    case BehaviorState.Working:
                        color = Color.cyan;
                        break;
                    case BehaviorState.Exploring:
                        color = Color.magenta;
                        break;
                }

                renderer.material.color = color;
            }
        }

        public void Cleanup()
        {
            if (npcPrefab != null)
            {
                GameObject.Destroy(npcPrefab);
            }
        }
    }
}
