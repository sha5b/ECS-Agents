using UnityEngine;
using ECS.Core;
using ECS.Systems;

namespace ECS
{
    public class SimulationManager : MonoBehaviour
    {
        private World world;
        private SpawnerSystem spawnerSystem;
        private MovementSystem movementSystem;
        private NeedSystem needSystem;
        private BehaviorSystem behaviorSystem;
        private ResourceSystem resourceSystem;
        private TerrainGeneratorSystem terrainSystem;

        private void Start()
        {
            InitializeWorld();
            InitializeSystems();
        }

        private void InitializeWorld()
        {
            world = gameObject.AddComponent<World>();
            Debug.Log("ECS World initialized");
        }

        private void InitializeSystems()
        {
            // Create systems in dependency order
            terrainSystem = new TerrainGeneratorSystem(world);
            needSystem = new NeedSystem(world);
            resourceSystem = new ResourceSystem(world, needSystem);
            behaviorSystem = new BehaviorSystem(world, needSystem);
            movementSystem = new MovementSystem(world);
            spawnerSystem = new SpawnerSystem(world);

            // Add systems to world in update order
            world.AddSystem(terrainSystem);    // First: Create and manage terrain
            world.AddSystem(needSystem);       // Second: Update NPC needs
            world.AddSystem(behaviorSystem);   // Third: Make decisions based on needs
            world.AddSystem(movementSystem);   // Fourth: Move entities based on decisions
            world.AddSystem(resourceSystem);   // Fifth: Handle resource interactions
            world.AddSystem(spawnerSystem);    // Last: Spawn new entities if needed

            Debug.Log("ECS Systems initialized");
        }

        private void Update()
        {
            // World.Update() is called in World's own Update method
            // since it inherits from MonoBehaviour
        }

        private void OnDestroy()
        {
            // Cleanup if needed
            Debug.Log("SimulationManager destroyed");
        }

        // Helper methods for external control
        public void PauseSimulation()
        {
            Time.timeScale = 0;
            Debug.Log("Simulation paused");
        }

        public void ResumeSimulation()
        {
            Time.timeScale = 1;
            Debug.Log("Simulation resumed");
        }

        public void SetSimulationSpeed(float speed)
        {
            Time.timeScale = Mathf.Clamp(speed, 0, 10);
            Debug.Log($"Simulation speed set to {Time.timeScale}x");
        }

        public void RestartSimulation()
        {
            // Destroy all entities
            foreach (var entity in world.GetEntities())
            {
                world.DestroyEntity(entity);
            }

            // Reset systems if they have reset functionality
            Debug.Log("Simulation restarted");
        }
    }
}
