using UnityEngine;
using ECS.Core;

namespace ECS.Components
{
    public class TerrainComponent : IComponent
    {
        // Reference to Unity's terrain
        public Terrain UnityTerrain { get; private set; }
        
        // Terrain properties
        public float BaseHeight { get; private set; }
        public float HeightVariation { get; private set; }
        public float Moisture { get; private set; }
        public float Temperature { get; private set; }
        
        // Erosion and modification factors
        public float ErosionFactor { get; private set; }
        public float WeatheringRate { get; private set; }

        public TerrainComponent(
            Terrain unityTerrain,
            float baseHeight = -100f,
            float heightVariation = 50f,
            float initialMoisture = 0.5f,
            float initialTemperature = 20f,
            float erosionFactor = 0.1f,
            float weatheringRate = 0.05f)
        {
            UnityTerrain = unityTerrain;
            BaseHeight = baseHeight;
            HeightVariation = heightVariation;
            Moisture = initialMoisture;
            Temperature = initialTemperature;
            ErosionFactor = erosionFactor;
            WeatheringRate = weatheringRate;
        }

        public void UpdateMoisture(float amount)
        {
            Moisture = Mathf.Clamp01(Moisture + amount);
        }

        public void UpdateTemperature(float newTemp)
        {
            Temperature = newTemp;
        }

        public void ApplyErosion(float deltaTime)
        {
            // Get terrain data
            TerrainData terrainData = UnityTerrain.terrainData;
            float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
            
            // Apply erosion based on moisture and temperature
            float erosionAmount = ErosionFactor * deltaTime * Moisture;
            if (Temperature < 0)
            {
                // Frost weathering increases erosion
                erosionAmount *= 1.5f;
            }

            // Simple erosion: reduce heights slightly where moisture is present
            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    if (Random.value < erosionAmount)
                    {
                        heights[y, x] = Mathf.Max(0, heights[y, x] - (erosionAmount * WeatheringRate));
                    }
                }
            }

            // Apply modified heights back to terrain
            terrainData.SetHeights(0, 0, heights);
        }

        public void GenerateNewTerrain()
        {
            TerrainData terrainData = UnityTerrain.terrainData;
            int resolution = terrainData.heightmapResolution;
            float[,] heights = new float[resolution, resolution];

            // Calculate center of terrain
            float centerX = resolution / 2f;
            float centerY = resolution / 2f;
            float maxDistance = resolution / 2f;

            // Generate bowl-shaped terrain
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    // Calculate distance from center (normalized)
                    float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY)) / maxDistance;
                    
                    // Create steeper bowl shape with raised edges
                    float bowlHeight;
                    if (distanceFromCenter < 0.8f)
                    {
                        // Inner bowl area - create a steeper curve
                        bowlHeight = 0.2f + (1f - Mathf.Pow(distanceFromCenter / 0.8f, 3f)) * 0.3f;
                    }
                    else
                    {
                        // Outer rim - create raised edges
                        float rimFactor = (distanceFromCenter - 0.8f) / 0.2f;
                        bowlHeight = 0.5f + rimFactor * 0.5f;
                    }

                    // Add some noise to make it more natural
                    float noise = Mathf.PerlinNoise(
                        x * HeightVariation / resolution,
                        y * HeightVariation / resolution
                    ) * 0.05f; // Reduced noise amount

                    // Combine bowl shape with noise
                    heights[y, x] = Mathf.Clamp01(bowlHeight + noise);
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }
    }
}
