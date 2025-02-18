using UnityEngine;
using ECS.Core;
using ECS.Components;

namespace ECS.Systems
{
    public class VoxelSystem : ISystem
    {
        private World world;
        private WeatherSystem weatherSystem;
        private TimeSystem timeSystem;
        private TerrainGeneratorSystem terrainSystem;

        // Visualization settings
        private bool showVisualization = true;
        private VisualizationType currentVisualization = VisualizationType.Temperature;
        private GameObject visualizerPrefab;

        public enum VisualizationType
        {
            Temperature,
            Moisture,
            WindSpeed,
            Biomes
        }

        public VoxelSystem(World world, WeatherSystem weatherSystem, TimeSystem timeSystem, TerrainGeneratorSystem terrainSystem)
        {
            this.world = world;
            this.weatherSystem = weatherSystem;
            this.timeSystem = timeSystem;
            this.terrainSystem = terrainSystem;

            InitializeVisualization();
        }

        private void InitializeVisualization()
        {
            // Create visualizer prefab
            visualizerPrefab = new GameObject("EnvironmentVisualizer");
            var visualizer = visualizerPrefab.AddComponent<EnvironmentVisualizerComponent>();
            Object.DontDestroyOnLoad(visualizerPrefab);
            visualizerPrefab.SetActive(false);

            Debug.Log("Visualization initialized successfully");
        }

        public void Update(float deltaTime)
        {
            foreach (var entity in world.GetEntities())
            {
                if (!entity.HasComponent<VoxelComponent>())
                    continue;

                var voxelComponent = entity.GetComponent<VoxelComponent>();
                UpdateEnvironmentalData(voxelComponent, deltaTime);
                
                if (showVisualization && entity.HasComponent<EnvironmentVisualizerComponent>())
                {
                    var visualizer = entity.GetComponent<EnvironmentVisualizerComponent>();
                    visualizer.UpdateVisualization(voxelComponent);
                }
            }
        }

        private void UpdateEnvironmentalData(VoxelComponent voxels, float deltaTime)
        {
            var weatherState = weatherSystem.GetCurrentWeather();
            var weatherIntensity = weatherSystem.GetWeatherIntensity();
            var currentHour = timeSystem.GetCurrentHour();

            // Debug log to verify data updates
            bool debugLog = Time.frameCount % 60 == 0; // Log every 60 frames
            if (debugLog) Debug.Log($"Updating environmental data - Weather: {weatherState}, Intensity: {weatherIntensity}, Hour: {currentHour}");

            // Update each voxel's environmental data
            for (int z = 0; z < voxels.GridSize.z; z++)
            {
                for (int y = 0; y < voxels.GridSize.y; y++)
                {
                    for (int x = 0; x < voxels.GridSize.x; x++)
                    {
                        Vector3Int pos = new Vector3Int(x, y, z);
                        Vector3 worldPos = voxels.GridToWorld(pos);

                        // Get current voxel data
                        voxels.GetVoxelData(pos, out float temperature, out float moisture, out float windSpeed, out BiomeType biome);

                        // Update temperature based on height and time
                        float heightFactor = 1.0f - (float)y / voxels.GridSize.y;
                        float timeFactor = Mathf.Sin(currentHour * Mathf.PI / 12f); // Day/night cycle
                        temperature = Mathf.Lerp(5f, 30f, heightFactor) + timeFactor * 10f;

                        // Apply weather effects
                        switch (weatherState)
                        {
                            case WeatherSystem.WeatherState.Rain:
                                moisture += weatherIntensity * deltaTime;
                                temperature -= weatherIntensity * 2f * deltaTime;
                                windSpeed = weatherIntensity * 5f;
                                break;
                            case WeatherSystem.WeatherState.Snow:
                                moisture += weatherIntensity * 0.5f * deltaTime;
                                temperature -= weatherIntensity * 5f * deltaTime;
                                windSpeed = weatherIntensity * 3f;
                                break;
                            case WeatherSystem.WeatherState.Clear:
                                moisture -= deltaTime * 0.1f;
                                windSpeed = weatherIntensity * 2f;
                                break;
                            case WeatherSystem.WeatherState.Sandstorm:
                                moisture -= weatherIntensity * deltaTime;
                                temperature += weatherIntensity * 3f * deltaTime;
                                windSpeed = weatherIntensity * 10f;
                                break;
                        }

                        // Clamp values
                        moisture = Mathf.Clamp01(moisture);
                        temperature = Mathf.Clamp(temperature, -20f, 40f);
                        windSpeed = Mathf.Clamp(windSpeed, 0f, 20f);

                        // Update biome based on conditions
                        biome = DetermineBiome(temperature, moisture, heightFactor);

                        // Store updated data
                        voxels.SetVoxelData(pos, temperature, moisture, windSpeed, biome);

                        // Debug log center voxel data
                        if (debugLog && x == voxels.GridSize.x/2 && y == voxels.GridSize.y/2 && z == voxels.GridSize.z/2)
                        {
                            Debug.Log($"Center voxel data - Temp: {temperature:F1}, Moisture: {moisture:F2}, Wind: {windSpeed:F1}, Biome: {biome}");
                        }
                    }
                }
            }
        }

        private BiomeType DetermineBiome(float temperature, float moisture, float heightFactor)
        {
            if (heightFactor < 0.2f) return BiomeType.Ocean;
            if (heightFactor < 0.25f) return BiomeType.Beach;
            if (heightFactor > 0.8f) return temperature < 0 ? BiomeType.SnowPeak : BiomeType.Mountain;
            
            if (temperature < 0) return BiomeType.Tundra;
            if (temperature > 30) return moisture < 0.3f ? BiomeType.Desert : BiomeType.Jungle;
            
            return moisture > 0.6f ? BiomeType.Forest : BiomeType.Plains;
        }

        public void SetVisualizationType(VisualizationType type)
        {
            currentVisualization = type;
            Debug.Log($"Switching visualization to: {type}");
            
            // Update visualizer components with new type
            foreach (var entity in world.GetEntities())
            {
                if (entity.HasComponent<EnvironmentVisualizerComponent>())
                {
                    var visualizer = entity.GetComponent<EnvironmentVisualizerComponent>();
                    visualizer.SetVisualizationType(type);
                }
            }
        }

        public void ToggleVisualization()
        {
            showVisualization = !showVisualization;
            Debug.Log($"Visualization {(showVisualization ? "enabled" : "disabled")}");

            // Update visualizer objects
            foreach (var entity in world.GetEntities())
            {
                if (entity.HasComponent<EnvironmentVisualizerComponent>())
                {
                    var visualizer = entity.GetComponent<EnvironmentVisualizerComponent>();
                    visualizer.gameObject.SetActive(showVisualization);
                }
            }
        }

        public Entity CreateVoxelGrid(Vector3 origin, Vector3Int gridSize, float voxelSize)
        {
            var entity = world.CreateEntity();
            
            // Add voxel component
            var voxelComponent = new VoxelComponent(gridSize, voxelSize, origin);
            entity.AddComponent(voxelComponent);

            // Create and initialize visualizer
            var visualizerObj = Object.Instantiate(visualizerPrefab);
            visualizerObj.SetActive(true);
            var visualizer = visualizerObj.GetComponent<EnvironmentVisualizerComponent>();
            visualizer.Initialize(new Vector3(gridSize.x * voxelSize, gridSize.y * voxelSize, gridSize.z * voxelSize));
            
            // Position the visualizer at the center of the grid
            Vector3 center = origin + new Vector3(
                gridSize.x * voxelSize * 0.5f,
                0, // Place at ground level
                gridSize.z * voxelSize * 0.5f
            );
            visualizerObj.transform.position = center;
            
            // Add visualizer component to entity
            entity.AddComponent(visualizer);
            visualizer.SetVisualizationType(currentVisualization);

            Debug.Log($"Created voxel grid at {origin} with size {gridSize} and voxel size {voxelSize}");
            Debug.Log($"Visualizer positioned at {center}");
            return entity;
        }
    }
}
