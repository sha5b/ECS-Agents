using UnityEngine;
using ECS.Core;
using ECS.Systems;

namespace ECS.Components
{
    public class EnvironmentVisualizerComponent : MonoBehaviour, IComponent
    {
        private GameObject[] visualizationLayers;
        private Material[] layerMaterials;
        private bool isInitialized = false;
        private const int GRID_RESOLUTION = 32;
        private const float LAYER_HEIGHT_OFFSET = 5f;
        private Texture2D arrowTexture;

        public void Initialize(Vector3 size)
        {
            if (isInitialized) return;

            // Create arrow texture
            CreateArrowTexture();

            // Create layers for different visualization types
            visualizationLayers = new GameObject[4]; // Temperature, Moisture, Wind, Biomes
            layerMaterials = new Material[4];

            string[] layerNames = { "Temperature", "Moisture", "Wind", "Biomes" };
            for (int i = 0; i < 4; i++)
            {
                // Create layer
                visualizationLayers[i] = CreateVisualizationLayer(
                    layerNames[i],
                    size,
                    i * LAYER_HEIGHT_OFFSET
                );
                visualizationLayers[i].transform.parent = transform;

                // Create material
                layerMaterials[i] = new Material(Shader.Find("Custom/EnvironmentalVisualization"));
                layerMaterials[i].mainTexture = new Texture2D(GRID_RESOLUTION, GRID_RESOLUTION);
                if (i == 2) // Wind layer
                {
                    layerMaterials[i].SetTexture("_ArrowTex", arrowTexture);
                    layerMaterials[i].SetFloat("_ArrowScale", 0.2f);
                    layerMaterials[i].SetFloat("_ArrowSpacing", 0.2f);
                    layerMaterials[i].SetFloat("_ArrowSpeed", 1f);
                }
                visualizationLayers[i].GetComponent<MeshRenderer>().material = layerMaterials[i];
            }

            isInitialized = true;
            Debug.Log("EnvironmentVisualizer initialized with 2D layers");
        }

        private void CreateArrowTexture()
        {
            const int ARROW_SIZE = 32;
            arrowTexture = new Texture2D(ARROW_SIZE, ARROW_SIZE);
            Color[] pixels = new Color[ARROW_SIZE * ARROW_SIZE];

            // Create arrow shape
            for (int y = 0; y < ARROW_SIZE; y++)
            {
                for (int x = 0; x < ARROW_SIZE; x++)
                {
                    float normalizedX = x / (float)ARROW_SIZE;
                    float normalizedY = y / (float)ARROW_SIZE;
                    
                    // Arrow shaft
                    bool isShaft = normalizedX >= 0.4f && normalizedX <= 0.6f && 
                                 normalizedY >= 0.2f && normalizedY <= 0.8f;
                    
                    // Arrow head
                    bool isHead = normalizedX >= 0.3f && normalizedX <= 0.7f && 
                                normalizedY >= 0.6f && normalizedY <= 0.8f &&
                                (normalizedY >= -2f * normalizedX + 1.9f) &&
                                (normalizedY >= 2f * normalizedX - 0.5f);

                    pixels[y * ARROW_SIZE + x] = (isShaft || isHead) ? 
                        new Color(1, 1, 1, 1) : new Color(0, 0, 0, 0);
                }
            }

            arrowTexture.SetPixels(pixels);
            arrowTexture.Apply();
            arrowTexture.filterMode = FilterMode.Bilinear;
            arrowTexture.wrapMode = TextureWrapMode.Clamp;
        }

        private GameObject CreateVisualizationLayer(string name, Vector3 size, float height)
        {
            GameObject layer = new GameObject(name + "Layer");
            
            // Create mesh
            MeshFilter meshFilter = layer.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = layer.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            
            // Create a flat grid facing upward
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(-size.x/2, height, -size.z/2);
            vertices[1] = new Vector3(size.x/2, height, -size.z/2);
            vertices[2] = new Vector3(-size.x/2, height, size.z/2);
            vertices[3] = new Vector3(size.x/2, height, size.z/2);

            int[] triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            Vector2[] uvs = new Vector2[] {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
            
            return layer;
        }

        public void UpdateVisualization(VoxelComponent voxels)
        {
            if (!isInitialized) return;

            UpdateTemperatureLayer(voxels);
            UpdateMoistureLayer(voxels);
            UpdateWindLayer(voxels);
            UpdateBiomeLayer(voxels);
        }

        private void UpdateTemperatureLayer(VoxelComponent voxels)
        {
            Texture2D tex = (Texture2D)layerMaterials[0].mainTexture;
            Color[] colors = new Color[GRID_RESOLUTION * GRID_RESOLUTION];

            for (int z = 0; z < GRID_RESOLUTION; z++)
            {
                for (int x = 0; x < GRID_RESOLUTION; x++)
                {
                    Vector3Int pos = new Vector3Int(
                        (x * voxels.GridSize.x) / GRID_RESOLUTION,
                        voxels.GridSize.y/2,
                        (z * voxels.GridSize.z) / GRID_RESOLUTION
                    );
                    voxels.GetVoxelData(pos, out float temperature, out _, out _, out _);
                    
                    // Temperature color mapping (-20 to 40 range)
                    float t = (temperature + 20f) / 60f;
                    Color color = Color.Lerp(Color.blue, Color.red, t);
                    colors[z * GRID_RESOLUTION + x] = color;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
        }

        private void UpdateMoistureLayer(VoxelComponent voxels)
        {
            Texture2D tex = (Texture2D)layerMaterials[1].mainTexture;
            Color[] colors = new Color[GRID_RESOLUTION * GRID_RESOLUTION];

            for (int z = 0; z < GRID_RESOLUTION; z++)
            {
                for (int x = 0; x < GRID_RESOLUTION; x++)
                {
                    Vector3Int pos = new Vector3Int(
                        (x * voxels.GridSize.x) / GRID_RESOLUTION,
                        voxels.GridSize.y/2,
                        (z * voxels.GridSize.z) / GRID_RESOLUTION
                    );
                    voxels.GetVoxelData(pos, out _, out float moisture, out _, out _);
                    
                    // Moisture color mapping (0-1 range)
                    Color color = Color.Lerp(Color.white, Color.blue, moisture);
                    colors[z * GRID_RESOLUTION + x] = color;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
        }

        private void UpdateWindLayer(VoxelComponent voxels)
        {
            Texture2D tex = (Texture2D)layerMaterials[2].mainTexture;
            Color[] colors = new Color[GRID_RESOLUTION * GRID_RESOLUTION];

            for (int z = 0; z < GRID_RESOLUTION; z++)
            {
                for (int x = 0; x < GRID_RESOLUTION; x++)
                {
                    Vector3Int pos = new Vector3Int(
                        (x * voxels.GridSize.x) / GRID_RESOLUTION,
                        voxels.GridSize.y/2,
                        (z * voxels.GridSize.z) / GRID_RESOLUTION
                    );
                    voxels.GetVoxelData(pos, out _, out _, out float windSpeed, out _);
                    
                    // Wind color mapping (0-20 range)
                    float t = windSpeed / 20f;
                    Color color = Color.Lerp(Color.white, Color.green, t);
                    colors[z * GRID_RESOLUTION + x] = color;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
        }

        private void UpdateBiomeLayer(VoxelComponent voxels)
        {
            Texture2D tex = (Texture2D)layerMaterials[3].mainTexture;
            Color[] colors = new Color[GRID_RESOLUTION * GRID_RESOLUTION];

            for (int z = 0; z < GRID_RESOLUTION; z++)
            {
                for (int x = 0; x < GRID_RESOLUTION; x++)
                {
                    Vector3Int pos = new Vector3Int(
                        (x * voxels.GridSize.x) / GRID_RESOLUTION,
                        voxels.GridSize.y/2,
                        (z * voxels.GridSize.z) / GRID_RESOLUTION
                    );
                    voxels.GetVoxelData(pos, out _, out _, out _, out BiomeType biome);
                    
                    // Biome color mapping
                    Color color = GetBiomeColor(biome);
                    colors[z * GRID_RESOLUTION + x] = color;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
        }

        private Color GetBiomeColor(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Ocean: return new Color(0f, 0.2f, 0.8f);
                case BiomeType.Beach: return new Color(0.9f, 0.9f, 0.6f);
                case BiomeType.Plains: return new Color(0.5f, 0.8f, 0.3f);
                case BiomeType.Forest: return new Color(0.2f, 0.6f, 0.2f);
                case BiomeType.Jungle: return new Color(0f, 0.4f, 0f);
                case BiomeType.Desert: return new Color(0.9f, 0.8f, 0.2f);
                case BiomeType.Tundra: return new Color(0.9f, 0.9f, 0.9f);
                case BiomeType.Mountain: return new Color(0.5f, 0.5f, 0.5f);
                default: return Color.white;
            }
        }

        public void SetVisualizationType(VoxelSystem.VisualizationType type)
        {
            if (!isInitialized) return;

            // Show only the selected layer
            for (int i = 0; i < visualizationLayers.Length; i++)
            {
                visualizationLayers[i].SetActive(i == (int)type);
            }
        }

        private void OnDestroy()
        {
            if (layerMaterials != null)
            {
                foreach (var material in layerMaterials)
                {
                    if (material != null)
                    {
                        if (material.mainTexture != null)
                        {
                            Destroy(material.mainTexture);
                        }
                        Destroy(material);
                    }
                }
            }

            if (arrowTexture != null)
            {
                Destroy(arrowTexture);
            }

            if (visualizationLayers != null)
            {
                foreach (var layer in visualizationLayers)
                {
                    if (layer != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(layer);
                        }
                        else
                        {
                            DestroyImmediate(layer);
                        }
                    }
                }
            }
        }
    }
}
