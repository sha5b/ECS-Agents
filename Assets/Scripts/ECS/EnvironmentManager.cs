using UnityEngine;
using ECS.Core;
using ECS.Systems;
using ECS.Components;

namespace ECS
{
    [RequireComponent(typeof(Terrain))]
    public class EnvironmentManager : MonoBehaviour
    {
        private World world;
        private TimeSystem timeSystem;
        private WeatherSystem weatherSystem;
        private TerrainGeneratorSystem terrainSystem;
        private VoxelSystem voxelSystem;

        [Header("Environment Settings")]
        [SerializeField] private Vector3Int environmentGridSize = new Vector3Int(32, 16, 32);
        [SerializeField] private float voxelSize = 30f;
        [SerializeField] private float baseTemperature = 20f;

        [Header("Terrain Generation")]
        [SerializeField] private int terrainResolution = 256;
        [SerializeField] private float terrainHeight = 200f;
        [SerializeField] private float baseHeight = 0f;
        [SerializeField] private int terrainSeed = 0;
        [SerializeField] private float erosionFactor = 0.1f;
        [SerializeField] private float weatheringRate = 0.05f;

        [Header("Visualization")]
        [SerializeField] private bool showEnvironmentVisuals = true;
        [SerializeField] private VoxelSystem.VisualizationType defaultVisualization = VoxelSystem.VisualizationType.Temperature;

        private Terrain terrain;
        private TerrainData terrainData;
        private Entity terrainEntity;
        private Entity environmentEntity;

        private void OnEnable()
        {
            terrain = GetComponent<Terrain>();
            if (terrain == null)
            {
                Debug.LogError("EnvironmentManager requires a Terrain component!");
                enabled = false;
                return;
            }

            InitializeTerrainData();
            InitializeEnvironment();
        }

        private void InitializeTerrainData()
        {
            // Create new terrain data if none exists
            if (terrain.terrainData == null)
            {
                terrainData = new TerrainData();
                terrain.terrainData = terrainData;
            }
            else
            {
                terrainData = terrain.terrainData;
            }

            // Configure terrain data
            terrainData.heightmapResolution = terrainResolution;
            terrainData.size = new Vector3(terrainResolution, terrainHeight, terrainResolution);
        }

        private void InitializeEnvironment()
        {
            try
            {
                // Initialize ECS World if not already initialized
                if (world == null)
                {
                    world = gameObject.AddComponent<World>();
                }

                // Create systems
                timeSystem = new TimeSystem(world, 12f); // Start at noon
                weatherSystem = new WeatherSystem(world, timeSystem);
                terrainSystem = new TerrainGeneratorSystem(world, weatherSystem, timeSystem);
                voxelSystem = new VoxelSystem(world, weatherSystem, timeSystem, terrainSystem);

                // Add systems in correct order
                world.AddSystem(timeSystem);
                world.AddSystem(weatherSystem);
                world.AddSystem(terrainSystem);
                world.AddSystem(voxelSystem);

                // Set up terrain entity
                SetupTerrain();

                // Set up environmental grid
                SetupEnvironmentalGrid();

                // Configure visualization
                if (showEnvironmentVisuals)
                {
                    voxelSystem.SetVisualizationType(defaultVisualization);
                }
                else
                {
                    voxelSystem.ToggleVisualization();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to initialize environment: {e.Message}\n{e.StackTrace}");
                enabled = false;
            }
        }

        private void SetupTerrain()
        {
            if (terrainSystem == null || terrain == null || terrainData == null)
            {
                throw new System.InvalidOperationException("Required components not initialized!");
            }

            terrainEntity = terrainSystem.CreateTerrainEntity(
                terrain,
                baseHeight,
                terrainHeight
            );

            if (terrainEntity == null)
            {
                throw new System.InvalidOperationException("Failed to create terrain entity!");
            }

            var terrainComponent = terrainEntity.GetComponent<TerrainComponent>();
            if (terrainComponent != null)
            {
                terrainComponent.GenerateNewTerrain(terrainSeed);
            }
        }

        private void SetupEnvironmentalGrid()
        {
            if (voxelSystem == null || terrain == null || terrainData == null)
            {
                throw new System.InvalidOperationException("Required components not initialized!");
            }

            // Calculate grid position based on terrain
            Vector3 gridOrigin = transform.position + new Vector3(
                terrainData.size.x * 0.5f - (environmentGridSize.x * voxelSize * 0.5f),
                0,
                terrainData.size.z * 0.5f - (environmentGridSize.z * voxelSize * 0.5f)
            );

            environmentEntity = voxelSystem.CreateVoxelGrid(gridOrigin, environmentGridSize, voxelSize);
            
            if (environmentEntity == null)
            {
                throw new System.InvalidOperationException("Failed to create environmental grid!");
            }
        }

        private void OnDisable()
        {
            CleanupEnvironment();
        }

        private void OnDestroy()
        {
            CleanupEnvironment();
        }

        private void CleanupEnvironment()
        {
            if (world != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(world);
                }
                else
                {
                    DestroyImmediate(world);
                }
                world = null;
            }

            timeSystem = null;
            weatherSystem = null;
            terrainSystem = null;
            voxelSystem = null;
            terrainEntity = null;
            environmentEntity = null;
        }

        // Public API

        public void SetVisualizationType(VoxelSystem.VisualizationType type)
        {
            if (voxelSystem != null)
            {
                voxelSystem.SetVisualizationType(type);
            }
        }

        public void ToggleVisualization()
        {
            if (voxelSystem != null)
            {
                showEnvironmentVisuals = !showEnvironmentVisuals;
                voxelSystem.ToggleVisualization();
            }
        }

        public float GetCurrentTemperature(Vector3 worldPosition)
        {
            if (environmentEntity == null || !environmentEntity.HasComponent<VoxelComponent>())
                return baseTemperature;

            var voxels = environmentEntity.GetComponent<VoxelComponent>();
            voxels.InterpolateEnvironmentalData(worldPosition, out float temperature, out _, out _);
            return temperature;
        }

        public float GetCurrentMoisture(Vector3 worldPosition)
        {
            if (environmentEntity == null || !environmentEntity.HasComponent<VoxelComponent>())
                return 0f;

            var voxels = environmentEntity.GetComponent<VoxelComponent>();
            voxels.InterpolateEnvironmentalData(worldPosition, out _, out float moisture, out _);
            return moisture;
        }

        public float GetCurrentWindSpeed(Vector3 worldPosition)
        {
            if (environmentEntity == null || !environmentEntity.HasComponent<VoxelComponent>())
                return 0f;

            var voxels = environmentEntity.GetComponent<VoxelComponent>();
            voxels.InterpolateEnvironmentalData(worldPosition, out _, out _, out float windSpeed);
            return windSpeed;
        }

        public WeatherSystem.WeatherState GetCurrentWeather()
        {
            return weatherSystem != null ? weatherSystem.GetCurrentWeather() : WeatherSystem.WeatherState.Clear;
        }

        public float GetCurrentHour()
        {
            return timeSystem != null ? timeSystem.GetCurrentHour() : 12f;
        }

        public TimeSystem.DayPhase GetCurrentDayPhase()
        {
            return timeSystem != null ? timeSystem.GetCurrentPhase() : TimeSystem.DayPhase.Day;
        }

        public BiomeType GetBiomeAt(Vector3 worldPosition)
        {
            if (environmentEntity == null || !environmentEntity.HasComponent<VoxelComponent>())
                return BiomeType.Plains;

            var voxels = environmentEntity.GetComponent<VoxelComponent>();
            Vector3Int gridPos = voxels.WorldToGrid(worldPosition);
            voxels.GetVoxelData(gridPos, out _, out _, out _, out BiomeType biome);
            return biome;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !showEnvironmentVisuals || environmentEntity == null)
                return;

            Gizmos.color = Color.yellow;
            if (environmentEntity.HasComponent<VoxelComponent>())
            {
                var voxels = environmentEntity.GetComponent<VoxelComponent>();
                Vector3 size = new Vector3(
                    environmentGridSize.x * voxelSize,
                    environmentGridSize.y * voxelSize,
                    environmentGridSize.z * voxelSize
                );
                Gizmos.DrawWireCube(
                    voxels.WorldOrigin + size * 0.5f,
                    size
                );
            }
        }
    }
}
