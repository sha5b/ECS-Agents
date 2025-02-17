using UnityEngine;
using System.Collections.Generic;
using ECS.Core;
using ECS.Components;
using ECS.Systems;

namespace Demo
{
    public class DemoManager : MonoBehaviour
    {
        [Header("Time Settings")]
        [SerializeField] private float timeScale = 60f; // 1 second real time = 1 minute game time

        [Header("Camera Settings")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private CameraController cameraController;

        [Header("Terrain Settings")]
        [SerializeField] private float terrainSize = 200f;  // Smaller overall size
        [SerializeField] private float terrainHeight = 100f; // Lower height
        [SerializeField] private float baseHeight = 50f;    // Lower base height
        [SerializeField] private float heightVariation = 20f; // Less height variation
        [SerializeField] private int resolution = 129; // Must be 2^n + 1

        [Header("Terrain Texturing")]
        [SerializeField] private Texture2D grassTexture;
        [SerializeField] private Texture2D rockTexture;
        [SerializeField] private Texture2D snowTexture;
        [SerializeField] private float textureScale = 50f;

        private World world;
        private TemperatureSystem temperatureSystem;
        private TimeSystem timeSystem;
        private WeatherSystem weatherSystem;
        private TerrainGeneratorSystem terrainSystem;
        private Entity terrainEntity;
        private Terrain terrain;

        void Start()
        {
            SetupCamera();
            
            // Create terrain
            CreateUnityTerrain();

            // Add MaterialManager
            var materialManager = gameObject.AddComponent<MaterialManager>();

            // Get or create World component
            world = FindObjectOfType<World>();
            if (world == null)
            {
                world = gameObject.AddComponent<World>();
            }

            // Create and add systems
            timeSystem = new TimeSystem(world, 12f); // Start at noon
            temperatureSystem = new TemperatureSystem(world);
            weatherSystem = new WeatherSystem(world, timeSystem);
            terrainSystem = new TerrainGeneratorSystem(world, weatherSystem, timeSystem);

            world.AddSystem(timeSystem);
            world.AddSystem(temperatureSystem);
            world.AddSystem(weatherSystem);
            world.AddSystem(terrainSystem);

            // Set time scale
            timeSystem.SetTimeScale(timeScale);

            // Create test entities
            CreateTestEntities();
            CreateTerrainEntity();

            // Log initial state
            LogWorldState();
        }

        private void SetupCamera()
        {
            // Find or create main camera
            if (mainCamera == null)
            {
                var cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
                if (cameraObject == null)
                {
                    cameraObject = new GameObject("Main Camera");
                    cameraObject.tag = "MainCamera";
                    mainCamera = cameraObject.AddComponent<Camera>();
                }
                else
                {
                    mainCamera = cameraObject.GetComponent<Camera>();
                }
            }

            // Add camera controller if not present
            if (cameraController == null)
            {
                cameraController = mainCamera.gameObject.GetComponent<CameraController>();
                if (cameraController == null)
                {
                    cameraController = mainCamera.gameObject.AddComponent<CameraController>();
                }
            }

            // Position camera for better view of the bowl
            mainCamera.transform.position = new Vector3(0, 150, -150);
            mainCamera.transform.rotation = Quaternion.Euler(45, 0, 0);
        }

        private void CreateUnityTerrain()
        {
            // Create GameObject and add Terrain component
            GameObject terrainObject = new GameObject("Generated Terrain");
            terrainObject.transform.position = Vector3.zero;
            terrain = terrainObject.AddComponent<Terrain>();
            TerrainCollider terrainCollider = terrainObject.AddComponent<TerrainCollider>();

            // Create and configure TerrainData
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = resolution;
            terrainData.size = new Vector3(terrainSize, terrainHeight, terrainSize);
            
            // Set up terrain textures
            SetupTerrainTextures(terrainData);

            // Assign TerrainData
            terrain.terrainData = terrainData;
            terrainCollider.terrainData = terrainData;

            // Position the terrain centered at origin
            terrainObject.transform.position = new Vector3(-terrainSize/2, 0, -terrainSize/2);
        }

        private void SetupTerrainTextures(TerrainData terrainData)
        {
            // Skip if textures aren't assigned
            if (grassTexture == null || rockTexture == null || snowTexture == null)
            {
                Debug.LogWarning("Terrain textures not assigned. Using default textures.");
                return;
            }

            // Create terrain layers
            TerrainLayer[] terrainLayers = new TerrainLayer[3];

            // Grass layer (base)
            terrainLayers[0] = new TerrainLayer();
            terrainLayers[0].diffuseTexture = grassTexture;
            terrainLayers[0].tileSize = new Vector2(textureScale, textureScale);

            // Rock layer (slopes)
            terrainLayers[1] = new TerrainLayer();
            terrainLayers[1].diffuseTexture = rockTexture;
            terrainLayers[1].tileSize = new Vector2(textureScale, textureScale);

            // Snow layer (high altitude)
            terrainLayers[2] = new TerrainLayer();
            terrainLayers[2].diffuseTexture = snowTexture;
            terrainLayers[2].tileSize = new Vector2(textureScale, textureScale);

            // Assign layers to terrain
            terrainData.terrainLayers = terrainLayers;
        }

        private void CreateTerrainEntity()
        {
            if (terrain == null)
            {
                Debug.LogError("Failed to create terrain!");
                return;
            }

            terrainEntity = terrainSystem.CreateTerrainEntity(terrain, baseHeight, heightVariation);
        }

        private void Update()
        {
            // Log world state every game hour
            float currentHour = timeSystem.GetCurrentHour();
            float previousHour = currentHour - (Time.deltaTime * timeScale / 60f);
            
            if (Mathf.Floor(currentHour) != Mathf.Floor(previousHour))
            {
                LogWorldState();
            }
        }

        [Header("Entity Settings")]
        [SerializeField] private int numberOfEntities = 10;
        [SerializeField] private float spawnHeight = 80f;    // Spawn above the bowl rim
        [SerializeField] private float spawnAreaSize = 100f; // Spawn within bowl area

        private void CreateTestEntities()
        {
            if (terrain == null) return;

            for (int i = 0; i < numberOfEntities; i++)
            {
                // Get random position on terrain
                float x = Random.Range(-spawnAreaSize/2, spawnAreaSize/2);
                float z = Random.Range(-spawnAreaSize/2, spawnAreaSize/2);
                Vector3 spawnPosition = new Vector3(x, spawnHeight, z);

                // Create entity with components
                var entity = world.CreateEntity();
                
                // Add standard components
                entity.AddComponent(new HealthComponent(100f));
                entity.AddComponent(new TemperatureComponent(
                    Random.Range(10f, 30f), // random optimal temperature
                    Random.Range(5f, 15f),  // random tolerance
                    Random.Range(0.5f, 1.5f) // random adaptation rate
                ));
                
                // Randomly assign day/night activity
                bool activeDay = Random.value > 0.5f;
                bool activeNight = !activeDay;
                float startHour = activeDay ? Random.Range(6f, 12f) : Random.Range(18f, 22f);
                float endHour = activeDay ? Random.Range(16f, 20f) : Random.Range(4f, 8f);
                entity.AddComponent(new TimeComponent(activeNight, activeDay, startHour, endHour));

                // Add weather component with random resistances
                entity.AddComponent(new WeatherComponent(
                    (WeatherComponent.WeatherResistance)Random.Range(0, 4),
                    (WeatherComponent.WeatherResistance)Random.Range(0, 4),
                    (WeatherComponent.WeatherResistance)Random.Range(0, 4),
                    (WeatherComponent.WeatherResistance)Random.Range(0, 4)
                ));

                // Add physical representation
                entity.AddComponent(new PhysicalEntityComponent(spawnPosition));
            }
        }

        private void LogWorldState()
        {
            var time = timeSystem.GetCurrentHour();
            var phase = timeSystem.GetCurrentPhase();
            var weather = weatherSystem.GetCurrentWeather();
            var intensity = weatherSystem.GetWeatherIntensity();

            Debug.Log($"\nWorld State Update:");
            Debug.Log($"Time: {time:F1}:00 ({phase})");
            Debug.Log($"Weather: {weather} (Intensity: {intensity:F2})");

            // Log terrain state if it exists
            if (terrainEntity != null)
            {
                var terrainComp = terrainEntity.GetComponent<TerrainComponent>();
                Debug.Log($"\nTerrain State:");
                Debug.Log($"- Moisture: {terrainComp.Moisture:F2}");
                Debug.Log($"- Temperature: {terrainComp.Temperature:F1}°C");
            }

            foreach (var entity in world.GetEntities())
            {
                var health = entity.GetComponent<HealthComponent>();
                var temp = entity.GetComponent<TemperatureComponent>();
                var timeComp = entity.GetComponent<TimeComponent>();
                var weatherComp = entity.GetComponent<WeatherComponent>();
                var physicalComp = entity.GetComponent<PhysicalEntityComponent>();

                if (health != null && temp != null && timeComp != null && weatherComp != null)
                {
                    Debug.Log($"\nEntity {entity.Id}:");
                    Debug.Log($"- Health: {health.CurrentHealth:F1}");
                    Debug.Log($"- Temperature: {temp.CurrentTemperature:F1}°C (Stress: {temp.GetTemperatureStress():F2})");
                    Debug.Log($"- Active: {timeComp.IsActiveAtTime(time)}");
                    Debug.Log($"- Weather Effect: {weatherComp.CurrentEffect} (Intensity: {weatherComp.EffectIntensity:F2})");
                    
                    if (physicalComp != null)
                    {
                        physicalComp.UpdateMaterial(temp.CurrentTemperature, weatherComp.EffectIntensity);
                    }
                }
            }
        }

        // Debug method to force terrain regeneration
        [ContextMenu("Regenerate Terrain")]
        private void RegenerateTerrain()
        {
            if (terrainEntity != null)
            {
                var terrainComponent = terrainEntity.GetComponent<TerrainComponent>();
                terrainSystem.GenerateNewTerrain(terrainComponent);
                Debug.Log("Terrain regenerated!");
            }
        }
    }
}
