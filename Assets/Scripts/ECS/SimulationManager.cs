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
        private TimeSystem timeSystem;
        private EventSystem eventSystem;
        private NavigationSystem navigationSystem;
        private SocialSystem socialSystem;
        private TaskSystem taskSystem;

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
            timeSystem = new TimeSystem(world);                    // Manages day/night cycle
            eventSystem = new EventSystem(world);                  // Handles world events
            terrainSystem = new TerrainGeneratorSystem(world);    // Creates the world
            navigationSystem = new NavigationSystem(world);        // Handles pathfinding
            needSystem = new NeedSystem(world);                   // Manages NPC needs
            socialSystem = new SocialSystem(world);               // Handles NPC interactions
            resourceSystem = new ResourceSystem(world, needSystem); // Manages resources
            behaviorSystem = new BehaviorSystem(world, needSystem); // Makes decisions
            taskSystem = new TaskSystem(world);                   // Manages NPC tasks
            movementSystem = new MovementSystem(world);           // Handles movement
            spawnerSystem = new SpawnerSystem(world);            // Creates entities

            // Add systems to world in update order
            world.AddSystem(timeSystem);       // First: Update time of day
            world.AddSystem(eventSystem);      // Second: Process world events
            world.AddSystem(terrainSystem);    // Third: Update terrain
            world.AddSystem(needSystem);       // Fourth: Update NPC needs
            world.AddSystem(socialSystem);     // Fifth: Process social interactions
            world.AddSystem(behaviorSystem);   // Sixth: Make decisions
            world.AddSystem(taskSystem);       // Seventh: Update tasks
            world.AddSystem(navigationSystem); // Eighth: Plan paths
            world.AddSystem(movementSystem);   // Ninth: Move entities
            world.AddSystem(resourceSystem);   // Tenth: Handle resources
            world.AddSystem(spawnerSystem);    // Last: Spawn new entities

            // Spawn initial NPCs
            SpawnInitialNPCs();

            Debug.Log("ECS Systems initialized");
        }

        private void SpawnInitialNPCs()
        {
            const int INITIAL_NPC_COUNT = 10;
            const float INITIAL_SPAWN_RANGE = 5f; // Much smaller range for initial spawn

            for (int i = 0; i < INITIAL_NPC_COUNT; i++)
            {
                // Spawn in a small circle around center
                float angle = (i / (float)INITIAL_NPC_COUNT) * 360f;
                float radius = Random.Range(1f, INITIAL_SPAWN_RANGE);
                Vector3 position = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                    0f,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius
                );
                spawnerSystem.SpawnNPC(position);
                Debug.Log($"Spawned NPC {i + 1} at {position}");
            }
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
