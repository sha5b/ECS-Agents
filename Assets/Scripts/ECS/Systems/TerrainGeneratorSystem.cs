using UnityEngine;
using ECS.Core;
using ECS.Components;

namespace ECS.Systems
{
    public class TerrainGeneratorSystem : ISystem
    {
        private World world;
        private WeatherSystem weatherSystem;
        private TimeSystem timeSystem;
        private float updateInterval = 1f; // How often to update terrain (in seconds)
        private float timeSinceLastUpdate = 0f;

        // Material properties
        private Material terrainMaterial;
        private float tiling = 50f;

        public TerrainGeneratorSystem(World world, WeatherSystem weatherSystem, TimeSystem timeSystem)
        {
            this.world = world;
            this.weatherSystem = weatherSystem;
            this.timeSystem = timeSystem;
            CreateTerrainMaterial();
        }

        private void CreateTerrainMaterial()
        {
            // Create a basic material for the terrain
            terrainMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            terrainMaterial.name = "Terrain Material";
            
            // Set base color and properties
            terrainMaterial.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.5f));
            terrainMaterial.SetFloat("_Metallic", 0.0f);
            terrainMaterial.SetFloat("_Smoothness", 0.2f);
        }

        public void Update(float deltaTime)
        {
            timeSinceLastUpdate += deltaTime;
            
            if (timeSinceLastUpdate >= updateInterval)
            {
                UpdateTerrainStates();
                timeSinceLastUpdate = 0f;
            }

            // Apply continuous effects
            foreach (var entity in world.GetEntities())
            {
                if (!entity.HasComponent<TerrainComponent>())
                    continue;

                var terrainComponent = entity.GetComponent<TerrainComponent>();
                ApplyWeatherEffects(terrainComponent, deltaTime);
                terrainComponent.ApplyErosion(deltaTime);
                UpdateTerrainMaterial(terrainComponent);
            }
        }

        private void UpdateTerrainStates()
        {
            foreach (var entity in world.GetEntities())
            {
                if (!entity.HasComponent<TerrainComponent>())
                    continue;

                var terrainComponent = entity.GetComponent<TerrainComponent>();
                UpdateTerrainBasedOnTimeOfDay(terrainComponent);
            }
        }

        private void ApplyWeatherEffects(TerrainComponent terrain, float deltaTime)
        {
            // Get current weather state
            var weatherState = weatherSystem.GetCurrentWeather();
            var weatherIntensity = weatherSystem.GetWeatherIntensity();

            // Update terrain moisture based on weather
            switch (weatherState)
            {
                case WeatherSystem.WeatherState.Rain:
                    terrain.UpdateMoisture(0.1f * weatherIntensity * deltaTime);
                    break;
                case WeatherSystem.WeatherState.Snow:
                    // Snow adds moisture more slowly
                    terrain.UpdateMoisture(0.05f * weatherIntensity * deltaTime);
                    break;
                case WeatherSystem.WeatherState.Clear:
                    // Moisture evaporates in clear weather
                    terrain.UpdateMoisture(-0.05f * deltaTime);
                    break;
                case WeatherSystem.WeatherState.Sandstorm:
                    // Sandstorms cause rapid erosion
                    terrain.UpdateMoisture(-0.1f * weatherIntensity * deltaTime);
                    break;
            }
        }

        private void UpdateTerrainBasedOnTimeOfDay(TerrainComponent terrain)
        {
            float currentHour = timeSystem.GetCurrentHour();
            var dayPhase = timeSystem.GetCurrentPhase();

            // Adjust temperature based on time of day
            switch (dayPhase)
            {
                case TimeSystem.DayPhase.Dawn:
                    terrain.UpdateTemperature(15f); // Morning temperature
                    break;
                case TimeSystem.DayPhase.Day:
                    terrain.UpdateTemperature(25f); // Day temperature
                    break;
                case TimeSystem.DayPhase.Dusk:
                    terrain.UpdateTemperature(18f); // Evening temperature
                    break;
                case TimeSystem.DayPhase.Night:
                    terrain.UpdateTemperature(10f); // Night temperature
                    break;
            }
        }

        private void UpdateTerrainMaterial(TerrainComponent terrain)
        {
            if (terrainMaterial == null || terrain == null) return;

            // Adjust material properties based on terrain state
            float moisture = terrain.Moisture;
            float temperature = terrain.Temperature;

            // Darken color when wet
            Color baseColor = new Color(0.5f, 0.5f, 0.5f);
            baseColor *= Mathf.Lerp(1f, 0.7f, moisture);

            // Add snow effect when cold
            if (temperature < 0)
            {
                baseColor = Color.Lerp(baseColor, Color.white, Mathf.Abs(temperature) / 10f);
            }

            terrainMaterial.SetColor("_BaseColor", baseColor);
            
            // Adjust smoothness based on moisture
            terrainMaterial.SetFloat("_Smoothness", Mathf.Lerp(0.2f, 0.5f, moisture));
        }

        public void GenerateNewTerrain(TerrainComponent terrain)
        {
            terrain.GenerateNewTerrain();
        }

        // Helper method to create a new terrain entity
        public Entity CreateTerrainEntity(Terrain unityTerrain, float baseHeight = 100f, float heightVariation = 50f)
        {
            var entity = world.CreateEntity();
            var terrainComponent = new TerrainComponent(
                unityTerrain,
                baseHeight,
                heightVariation
            );
            entity.AddComponent(terrainComponent);
            
            // Apply material to terrain
            if (unityTerrain != null && terrainMaterial != null)
            {
                unityTerrain.materialTemplate = terrainMaterial;
            }
            
            // Generate initial terrain
            GenerateNewTerrain(terrainComponent);
            
            return entity;
        }
    }
}
