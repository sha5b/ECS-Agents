using UnityEngine;
using ECS.Core;
using ECS.Components;
using ECS.Types;
using System.Collections.Generic;

namespace ECS.Systems
{
    public class TerrainGeneratorSystem : ISystem
    {
        private World world;
        private Dictionary<Vector2Int, Entity> chunks;
        private HashSet<Vector2Int> loadedChunkCoords;
        private const int VIEW_DISTANCE = 3;
        private Vector3 lastPlayerPosition;
        private float updateThreshold = 16f; // Only update chunks when moved this far

        private FastNoiseLite noiseGenerator;
        private FastNoiseLite biomeNoiseGenerator;
        private FastNoiseLite erosionNoiseGenerator;
        private FastNoiseLite caveNoiseGenerator;

        private GameObject chunksParent;

        public TerrainGeneratorSystem(World world)
        {
            this.world = world;
            chunks = new Dictionary<Vector2Int, Entity>();
            loadedChunkCoords = new HashSet<Vector2Int>();

            InitializeNoiseGenerators();
            
            // Create parent object for chunks
            chunksParent = new GameObject("TerrainChunks");
            Object.DontDestroyOnLoad(chunksParent);
        }

        private void InitializeNoiseGenerators()
        {
            // Main terrain noise
            noiseGenerator = new FastNoiseLite(42);
            noiseGenerator.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            noiseGenerator.SetFrequency(0.015f); // Reduced frequency for larger features
            noiseGenerator.SetFractalType(FastNoiseLite.FractalType.FBm);
            noiseGenerator.SetFractalOctaves(8); // Increased octaves for more detail blending

            // Biome noise
            biomeNoiseGenerator = new FastNoiseLite(43);
            biomeNoiseGenerator.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            biomeNoiseGenerator.SetFrequency(0.0005f); // Even slower biome changes for smoother transitions

            // Erosion noise
            erosionNoiseGenerator = new FastNoiseLite(44);
            erosionNoiseGenerator.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            erosionNoiseGenerator.SetFrequency(0.02f); // Reduced frequency for gentler erosion
            erosionNoiseGenerator.SetFractalType(FastNoiseLite.FractalType.FBm);
            erosionNoiseGenerator.SetFractalOctaves(4); // Increased octaves for smoother erosion

            // Cave noise
            caveNoiseGenerator = new FastNoiseLite(45);
            caveNoiseGenerator.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            caveNoiseGenerator.SetFrequency(0.03f);
            caveNoiseGenerator.SetFractalType(FastNoiseLite.FractalType.FBm);
            caveNoiseGenerator.SetFractalOctaves(4);
        }

        public void Update(float deltaTime)
        {
            if (Camera.main == null) return;

            Vector3 playerPos = Camera.main.transform.position;
            if (Vector3.Distance(playerPos, lastPlayerPosition) < updateThreshold)
                return;

            lastPlayerPosition = playerPos;
            UpdateLoadedChunks(playerPos);
            GenerateChunks();
        }

        private void UpdateLoadedChunks(Vector3 playerPos)
        {
            // Calculate visible chunks
            Vector2Int centerChunk = new Vector2Int(
                Mathf.RoundToInt(playerPos.x / (TerrainChunkComponent.CHUNK_SIZE * TerrainChunkComponent.VOXEL_SIZE)),
                Mathf.RoundToInt(playerPos.z / (TerrainChunkComponent.CHUNK_SIZE * TerrainChunkComponent.VOXEL_SIZE))
            );

            loadedChunkCoords.Clear();
            for (int x = -VIEW_DISTANCE; x <= VIEW_DISTANCE; x++)
            {
                for (int z = -VIEW_DISTANCE; z <= VIEW_DISTANCE; z++)
                {
                    if (Vector2.Distance(Vector2.zero, new Vector2(x, z)) <= VIEW_DISTANCE)
                    {
                        loadedChunkCoords.Add(centerChunk + new Vector2Int(x, z));
                    }
                }
            }
        }

        private void GenerateChunks()
        {
            // Create new chunks
            foreach (Vector2Int coord in loadedChunkCoords)
            {
                if (!chunks.ContainsKey(coord))
                {
                    CreateChunk(coord);
                }
            }

            // Remove far chunks
            List<Vector2Int> chunksToRemove = new List<Vector2Int>();
            foreach (var chunk in chunks)
            {
                if (!loadedChunkCoords.Contains(chunk.Key))
                {
                    chunksToRemove.Add(chunk.Key);
                }
            }

            foreach (var coord in chunksToRemove)
            {
                RemoveChunk(coord);
            }
        }

        private void CreateChunk(Vector2Int coord)
        {
            Entity chunk = world.CreateEntity();
            
            // Add components
            var chunkComponent = new TerrainChunkComponent(coord);
            chunk.AddComponent(chunkComponent);

            // Generate biome
            float biomeValue = biomeNoiseGenerator.GetNoise(coord.x * 100f, coord.y * 100f);
            BiomeType biomeType = DetermineBiomeType(biomeValue);
            var biomeComponent = new BiomeComponent(
                biomeType,
                temperature: biomeNoiseGenerator.GetNoise(coord.x * 100f + 1000f, coord.y * 100f),
                humidity: biomeNoiseGenerator.GetNoise(coord.x * 100f, coord.y * 100f + 1000f),
                height: biomeNoiseGenerator.GetNoise(coord.x * 100f - 1000f, coord.y * 100f)
            );
            chunk.AddComponent(biomeComponent);

            // Add materials
            chunk.AddComponent(new TerrainMaterialComponent(biomeType));

            // Generate terrain data
            GenerateTerrainData(chunkComponent, biomeComponent);

            // Create mesh and setup navigation
            var mesh = GenerateChunkMesh(chunk);
            SetupChunkNavigation(chunk, mesh, coord);

            chunks.Add(coord, chunk);
        }

        private BiomeType DetermineBiomeType(float biomeValue)
        {
            // More evenly distributed biome thresholds
            if (biomeValue < -0.6f) return BiomeType.Tundra;
            if (biomeValue < -0.3f) return BiomeType.Mountains;
            if (biomeValue < 0.0f) return BiomeType.Forest;
            if (biomeValue < 0.3f) return BiomeType.Plains;
            if (biomeValue < 0.6f) return BiomeType.Desert;
            return BiomeType.Swamp;
        }

        private void GenerateTerrainData(TerrainChunkComponent chunk, BiomeComponent biome)
        {
            Vector3 worldPos = chunk.GetWorldPosition();
            int size = TerrainChunkComponent.CHUNK_SIZE;
            float voxelSize = TerrainChunkComponent.VOXEL_SIZE;
            
            // Pre-calculate heightmap for efficiency
            float[,] heightMap = new float[size + 1, size + 1];
            for (int x = 0; x <= size; x++)
            {
                for (int z = 0; z <= size; z++)
                {
                    float wx = worldPos.x + x * voxelSize;
                    float wz = worldPos.z + z * voxelSize;

                    // Sample biome values from a larger area for smooth transitions

                    // Get base terrain height
                    float baseHeight = noiseGenerator.GetNoise(wx * 0.5f, wz * 0.5f); // Reduced scale for smoother base terrain
                    baseHeight = (baseHeight + 1f) * 0.4f; // Increased height range
                    float erosion = erosionNoiseGenerator.GetNoise(wx, wz) * 0.05f; // Reduced erosion impact

                    // Sample neighboring biomes for smooth transitions
                    float blendRadius = 16f; // Wider transition area
                    float thisHeight = baseHeight * GetBiomeHeightModifier(biome.Type);
                    
                    // Sample neighboring points
                    Vector2 worldPos2D = new Vector2(wx, wz);
                    float[] neighborHeights = new float[4];
                    Vector2[] offsets = new Vector2[] {
                        new Vector2(blendRadius, 0),
                        new Vector2(-blendRadius, 0),
                        new Vector2(0, blendRadius),
                        new Vector2(0, -blendRadius)
                    };

                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 samplePos = worldPos2D + offsets[i];
                        float biomeValue = biomeNoiseGenerator.GetNoise(samplePos.x * 0.01f, samplePos.y * 0.01f);
                        BiomeType neighborBiome = DetermineBiomeType(biomeValue);
                        neighborHeights[i] = baseHeight * GetBiomeHeightModifier(neighborBiome);
                    }

                    // Blend heights at boundaries
                    float finalHeight = thisHeight;
                    float edgeBlend = 1f;
                    if (x < blendRadius) edgeBlend = Mathf.Min(edgeBlend, x / blendRadius);
                    if (x > size - blendRadius) edgeBlend = Mathf.Min(edgeBlend, (size - x) / blendRadius);
                    if (z < blendRadius) edgeBlend = Mathf.Min(edgeBlend, z / blendRadius);
                    if (z > size - blendRadius) edgeBlend = Mathf.Min(edgeBlend, (size - z) / blendRadius);

                    if (edgeBlend < 1f)
                    {
                        float avgNeighborHeight = 0f;
                        for (int i = 0; i < 4; i++)
                        {
                            avgNeighborHeight += neighborHeights[i];
                        }
                        avgNeighborHeight *= 0.25f;
                        finalHeight = Mathf.Lerp(avgNeighborHeight, thisHeight, edgeBlend);
                    }

                    heightMap[x, z] = (finalHeight + erosion) * size;
                }
            }

            // Generate density field
            for (int x = 0; x <= size; x++)
            {
                for (int z = 0; z <= size; z++)
                {
                    float surfaceHeight = heightMap[x, z];
                    
                    for (int y = 0; y <= size; y++)
                    {
                        float wy = y * voxelSize;
                        float wx = worldPos.x + x * voxelSize;
                        float wz = worldPos.z + z * voxelSize;

                        // Cave generation
                        float cave = caveNoiseGenerator.GetNoise(wx, wy * 0.5f, wz);
                        bool isCave = cave > 0.6f && y > size * 0.2f && y < size * 0.8f;

                        float density = surfaceHeight - y;
                        if (isCave) density = -1f;

                        chunk.DensityField[x, y, z] = density;

                        if (x < size && y < size && z < size)
                        {
                            chunk.Voxels[x, y, z] = density > 0 ? (byte)1 : (byte)0;
                        }
                    }
                }
            }

            chunk.MarkGenerated();
        }

        private float GetBiomeHeightModifier(BiomeType biomeType)
        {
            switch (biomeType)
            {
                case BiomeType.Mountains:
                    return 0.8f;  // Further reduced for smoother mountains
                case BiomeType.Plains:
                    return 0.5f;  // Closer to other biomes
                case BiomeType.Desert:
                    return 0.55f; // Closer to plains
                case BiomeType.Tundra:
                    return 0.6f;  // Kept same
                case BiomeType.Forest:
                    return 0.65f; // Closer to other biomes
                case BiomeType.Swamp:
                    return 0.45f; // Closer to plains
                default:
                    return 0.5f;
            }
        }

        private float GetDensityValue(float height, float surfaceHeight)
        {
            return surfaceHeight - height;
        }

        private Mesh GenerateChunkMesh(Entity chunk)
        {
            var chunkComponent = chunk.GetComponent<TerrainChunkComponent>();
            var biomeComponent = chunk.GetComponent<BiomeComponent>();
            var materialComponent = chunk.GetComponent<TerrainMaterialComponent>();

            GameObject chunkObject = new GameObject($"Chunk_{chunkComponent.ChunkPosition}");
            chunkObject.transform.SetParent(chunksParent.transform);
            chunkObject.transform.position = chunkComponent.GetWorldPosition();

            MeshFilter meshFilter = chunkObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();
            MeshCollider meshCollider = chunkObject.AddComponent<MeshCollider>();

            Mesh mesh = GenerateMarchingCubesMesh(chunkComponent);
            
            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;
            meshRenderer.material = materialComponent.BaseMaterial;

            chunkComponent.SetChunkObject(chunkObject);
            chunkComponent.MarkMeshGenerated();

            return mesh;
        }

        private Mesh GenerateMarchingCubesMesh(TerrainChunkComponent chunk)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            int resolution = TerrainChunkComponent.CHUNK_SIZE;
            float heightScale = 2.5f; // Reduced height scale for smoother appearance

            // Pre-calculate heights for efficiency
            float[,] heights = new float[resolution + 1, resolution + 1];
            for (int x = 0; x <= resolution; x++)
            {
                for (int z = 0; z <= resolution; z++)
                {
                    for (int y = resolution; y >= 0; y--)
                    {
                        if (chunk.DensityField[x, y, z] > 0)
                        {
                            heights[x, z] = y;
                            break;
                        }
                    }
                }
            }

            // Generate mesh
            for (int x = 0; x <= resolution; x++)
            {
                for (int z = 0; z <= resolution; z++)
                {
                    float xPos = x * TerrainChunkComponent.VOXEL_SIZE;
                    float zPos = z * TerrainChunkComponent.VOXEL_SIZE;
                    float height = heights[x, z];

                    vertices.Add(new Vector3(xPos, height * heightScale * TerrainChunkComponent.VOXEL_SIZE, zPos));
                    uvs.Add(new Vector2(x / (float)resolution, z / (float)resolution));

                    if (x < resolution && z < resolution)
                    {
                        int baseIndex = x * (resolution + 1) + z;
                        // Fix face orientation by reversing triangle winding order
                        triangles.Add(baseIndex);
                        triangles.Add(baseIndex + 1);
                        triangles.Add(baseIndex + (resolution + 1));
                        triangles.Add(baseIndex + 1);
                        triangles.Add(baseIndex + (resolution + 1) + 1);
                        triangles.Add(baseIndex + (resolution + 1));
                    }
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private void RemoveChunk(Vector2Int coord)
        {
            if (chunks.TryGetValue(coord, out Entity chunk))
            {
                var chunkComponent = chunk.GetComponent<TerrainChunkComponent>();
                if (chunkComponent.ChunkObject != null)
                {
                    Object.Destroy(chunkComponent.ChunkObject);
                }
                world.DestroyEntity(chunk);
                chunks.Remove(coord);
            }
        }

        private void SetupChunkNavigation(Entity chunk, Mesh mesh, Vector2Int coord)
        {
            var chunkComponent = chunk.GetComponent<TerrainChunkComponent>();
            var navMesh = chunk.GetComponent<NavMeshComponent>();

            if (navMesh == null)
            {
                navMesh = new NavMeshComponent();
                chunk.AddComponent(navMesh);
            }

            // Update NavMesh for this chunk
            navMesh.UpdateNavMesh(mesh, chunkComponent.GetWorldPosition());

            // Create connections to adjacent chunks
            foreach (var adjacentCoord in GetAdjacentChunkCoords(coord))
            {
                if (chunks.TryGetValue(adjacentCoord, out Entity adjacentChunk))
                {
                    CreateChunkConnection(chunk, adjacentChunk);
                }
            }
        }

        private Vector2Int[] GetAdjacentChunkCoords(Vector2Int coord)
        {
            return new Vector2Int[]
            {
                coord + new Vector2Int(1, 0),  // Right
                coord + new Vector2Int(-1, 0), // Left
                coord + new Vector2Int(0, 1),  // Up
                coord + new Vector2Int(0, -1)  // Down
            };
        }

        private void CreateChunkConnection(Entity chunkA, Entity chunkB)
        {
            var chunkAComponent = chunkA.GetComponent<TerrainChunkComponent>();
            var chunkBComponent = chunkB.GetComponent<TerrainChunkComponent>();
            var navMeshA = chunkA.GetComponent<NavMeshComponent>();
            var navMeshB = chunkB.GetComponent<NavMeshComponent>();

            if (chunkAComponent == null || chunkBComponent == null || 
                navMeshA == null || navMeshB == null) return;

            Vector3 posA = chunkAComponent.GetWorldPosition();
            Vector3 posB = chunkBComponent.GetWorldPosition();
            
            // Calculate connection points
            Vector3 midpoint = (posA + posB) * 0.5f;
            float chunkSize = TerrainChunkComponent.CHUNK_SIZE * TerrainChunkComponent.VOXEL_SIZE;

            // Create connection points slightly inset from chunk edges
            Vector3 directionAtoB = (posB - posA).normalized;
            Vector3 connectionA = posA + directionAtoB * (chunkSize * 0.45f);
            Vector3 connectionB = posB - directionAtoB * (chunkSize * 0.45f);

            // Add connection points
            navMeshA.AddConnection(connectionA, midpoint);
            navMeshB.AddConnection(connectionB, midpoint);
        }

        public void OnDestroy()
        {
            foreach (var chunk in chunks.Values)
            {
                var chunkComponent = chunk.GetComponent<TerrainChunkComponent>();
                if (chunkComponent.ChunkObject != null)
                {
                    Object.Destroy(chunkComponent.ChunkObject);
                }
            }
            chunks.Clear();
            
            if (chunksParent != null)
            {
                Object.Destroy(chunksParent);
            }
        }
    }

    public class FastNoiseLite
    {
        public enum NoiseType { OpenSimplex2 }
        public enum FractalType { FBm }

        private System.Random random;
        private float frequency = 1f;
        private int octaves = 1;

        public FastNoiseLite(int seed)
        {
            random = new System.Random(seed);
        }

        public void SetNoiseType(NoiseType type) { }
        public void SetFrequency(float freq) { frequency = freq; }
        public void SetFractalType(FractalType type) { }
        public void SetFractalOctaves(int o) { octaves = o; }

        public float GetNoise(float x, float y)
        {
            return GetNoise(x, 0, y);
        }

        public float GetNoise(float x, float y, float z)
        {
            x *= frequency;
            y *= frequency;
            z *= frequency;

            float sum = 0f;
            float amplitude = 1f;
            float amplitudeSum = 0f;
            float freq = 1f;

            for (int i = 0; i < octaves; i++)
            {
                sum += amplitude * (Mathf.PerlinNoise(x * freq, z * freq) * 2f - 1f);
                amplitudeSum += amplitude;
                amplitude *= 0.5f;
                freq *= 2f;
            }

            return sum / amplitudeSum;
        }
    }
}
