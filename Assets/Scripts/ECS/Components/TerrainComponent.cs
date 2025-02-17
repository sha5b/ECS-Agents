using UnityEngine;
using ECS.Core;

namespace ECS.Components
{
    public enum BiomeType
    {
        Ocean,
        Beach,
        Plains,
        Forest,
        Jungle,
        Desert,
        Tundra,
        Mountain,
        SnowPeak
    }

    public class TerrainComponent : IComponent
    {
        // Reference to Unity's terrain
        public Terrain UnityTerrain { get; private set; }
        
        // Terrain properties
        public float BaseHeight { get; private set; }
        public float HeightVariation { get; private set; }
        public float Moisture { get; private set; }
        public float Temperature { get; private set; }
        public BiomeType CurrentBiome { get; private set; }
        
        // Noise layers for natural generation
        public float[] NoiseFrequencies { get; private set; }
        public float[] NoiseAmplitudes { get; private set; }
        public int Octaves { get; private set; }
        public float Persistence { get; private set; }
        public float Lacunarity { get; private set; }
        
        // Erosion and modification factors
        public float ErosionFactor { get; private set; }
        public float WeatheringRate { get; private set; }
        public float RiverFactor { get; private set; }
        public float SeaLevel { get; private set; }

        public TerrainComponent(
            Terrain unityTerrain,
            float baseHeight = 0f,
            float heightVariation = 100f,
            float initialMoisture = 0.5f,
            float initialTemperature = 20f,
            float erosionFactor = 0.1f,
            float weatheringRate = 0.05f,
            float riverFactor = 0.1f,
            float seaLevel = 0.3f,
            int octaves = 6)
        {
            UnityTerrain = unityTerrain;
            BaseHeight = baseHeight;
            HeightVariation = heightVariation;
            Moisture = initialMoisture;
            Temperature = initialTemperature;
            ErosionFactor = erosionFactor;
            WeatheringRate = weatheringRate;
            RiverFactor = riverFactor;
            SeaLevel = seaLevel;
            
            // Initialize noise parameters
            Octaves = octaves;
            Persistence = 0.5f;
            Lacunarity = 2.0f;
            
            NoiseFrequencies = new float[Octaves];
            NoiseAmplitudes = new float[Octaves];
            for (int i = 0; i < Octaves; i++)
            {
                NoiseFrequencies[i] = Mathf.Pow(2, i);
                NoiseAmplitudes[i] = Mathf.Pow(Persistence, i);
            }
        }

        public void UpdateMoisture(float amount)
        {
            Moisture = Mathf.Clamp01(Moisture + amount);
            UpdateBiome();
        }

        public void UpdateTemperature(float newTemp)
        {
            Temperature = newTemp;
            UpdateBiome();
        }

        private void UpdateBiome()
        {
            TerrainData terrainData = UnityTerrain.terrainData;
            float height = terrainData.GetHeight(
                terrainData.heightmapResolution / 2,
                terrainData.heightmapResolution / 2
            ) / terrainData.size.y;

            // Determine biome based on height, temperature, and moisture
            if (height < SeaLevel)
            {
                CurrentBiome = BiomeType.Ocean;
            }
            else if (height < SeaLevel + 0.05f)
            {
                CurrentBiome = BiomeType.Beach;
            }
            else if (height > 0.8f)
            {
                CurrentBiome = Temperature < 0 ? BiomeType.SnowPeak : BiomeType.Mountain;
            }
            else if (Temperature < 0)
            {
                CurrentBiome = BiomeType.Tundra;
            }
            else if (Temperature > 30)
            {
                CurrentBiome = Moisture < 0.3f ? BiomeType.Desert : BiomeType.Jungle;
            }
            else
            {
                CurrentBiome = Moisture > 0.6f ? BiomeType.Forest : BiomeType.Plains;
            }
        }

        public void ApplyErosion(float deltaTime)
        {
            TerrainData terrainData = UnityTerrain.terrainData;
            float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
            int resolution = terrainData.heightmapResolution;
            
            // Apply thermal erosion
            float thermalErosion = Temperature < 0 ? ErosionFactor * 1.5f : ErosionFactor;
            
            // Apply hydraulic erosion
            float hydraulicErosion = Moisture * ErosionFactor;
            
            for (int y = 1; y < resolution - 1; y++)
            {
                for (int x = 1; x < resolution - 1; x++)
                {
                    float currentHeight = heights[y, x];
                    
                    // Calculate height differences with neighbors
                    float[] heightDiffs = new float[]
                    {
                        heights[y-1, x] - currentHeight,  // North
                        heights[y+1, x] - currentHeight,  // South
                        heights[y, x-1] - currentHeight,  // West
                        heights[y, x+1] - currentHeight   // East
                    };

                    // Apply thermal erosion
                    if (Random.value < thermalErosion * deltaTime)
                    {
                        // Find steepest slope
                        float maxDiff = 0f;
                        int steepestIndex = -1;
                        for (int i = 0; i < heightDiffs.Length; i++)
                        {
                            if (heightDiffs[i] < maxDiff)
                            {
                                maxDiff = heightDiffs[i];
                                steepestIndex = i;
                            }
                        }

                        // Erode towards steepest slope
                        if (steepestIndex != -1)
                        {
                            float erosionAmount = maxDiff * WeatheringRate;
                            heights[y, x] -= erosionAmount;
                            
                            // Deposit material at lower point
                            switch (steepestIndex)
                            {
                                case 0: heights[y-1, x] += erosionAmount; break;
                                case 1: heights[y+1, x] += erosionAmount; break;
                                case 2: heights[y, x-1] += erosionAmount; break;
                                case 3: heights[y, x+1] += erosionAmount; break;
                            }
                        }
                    }

                    // Apply hydraulic erosion
                    if (Moisture > 0.5f && Random.value < hydraulicErosion * deltaTime)
                    {
                        float erosionAmount = WeatheringRate * Moisture;
                        heights[y, x] = Mathf.Max(0, heights[y, x] - erosionAmount);
                    }
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }

        public void GenerateNewTerrain(int seed = 0)
        {
            TerrainData terrainData = UnityTerrain.terrainData;
            int resolution = terrainData.heightmapResolution;
            float[,] heights = new float[resolution, resolution];

            // Initialize noise parameters
            System.Random prng = new System.Random(seed);
            Vector2[] octaveOffsets = new Vector2[Octaves];
            for (int i = 0; i < Octaves; i++)
            {
                float offsetX = prng.Next(-100000, 100000);
                float offsetY = prng.Next(-100000, 100000);
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            float maxHeight = float.MinValue;
            float minHeight = float.MaxValue;

            // Generate heightmap using multiple octaves of noise
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    // Combine multiple layers of noise
                    for (int i = 0; i < Octaves; i++)
                    {
                        float sampleX = (x + octaveOffsets[i].x) * frequency * HeightVariation / resolution;
                        float sampleY = (y + octaveOffsets[i].y) * frequency * HeightVariation / resolution;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= Persistence;
                        frequency *= Lacunarity;
                    }

                    heights[y, x] = noiseHeight;

                    // Track min/max for normalization
                    if (noiseHeight > maxHeight) maxHeight = noiseHeight;
                    if (noiseHeight < minHeight) minHeight = noiseHeight;
                }
            }

            // Normalize and apply height modifications
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    // Normalize to 0-1 range
                    heights[y, x] = Mathf.InverseLerp(minHeight, maxHeight, heights[y, x]);

                    // Apply exponential curve for more dramatic mountains
                    float mountainFactor = Mathf.Pow(heights[y, x], 1.5f);
                    
                    // Create coastal regions
                    if (heights[y, x] < SeaLevel + 0.05f && heights[y, x] > SeaLevel)
                    {
                        heights[y, x] = SeaLevel + (heights[y, x] - SeaLevel) * 0.5f;
                    }

                    // Final height
                    heights[y, x] = Mathf.Lerp(BaseHeight, BaseHeight + mountainFactor, heights[y, x]);
                }
            }

            // Apply the heights to the terrain
            terrainData.SetHeights(0, 0, heights);
            
            // Update terrain size
            terrainData.size = new Vector3(resolution, BaseHeight + HeightVariation, resolution);
            
            // Update biome
            UpdateBiome();
        }
    }
}
