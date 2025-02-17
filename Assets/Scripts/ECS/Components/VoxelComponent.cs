using UnityEngine;
using ECS.Core;

namespace ECS.Components
{
    public class VoxelComponent : IComponent
    {
        // Voxel grid properties
        public Vector3Int GridSize { get; private set; }
        public float VoxelSize { get; private set; }
        public Vector3 WorldOrigin { get; private set; }

        // Voxel data arrays
        public float[] Temperature { get; private set; }
        public float[] Moisture { get; private set; }
        public float[] WindSpeed { get; private set; }
        public BiomeType[] Biomes { get; private set; }

        public VoxelComponent(Vector3Int gridSize, float voxelSize, Vector3 worldOrigin)
        {
            GridSize = gridSize;
            VoxelSize = voxelSize;
            WorldOrigin = worldOrigin;

            int totalVoxels = gridSize.x * gridSize.y * gridSize.z;
            Temperature = new float[totalVoxels];
            Moisture = new float[totalVoxels];
            WindSpeed = new float[totalVoxels];
            Biomes = new BiomeType[totalVoxels];
        }

        public int GetVoxelIndex(Vector3Int gridPosition)
        {
            if (!IsValidGridPosition(gridPosition)) return -1;
            return gridPosition.x + GridSize.x * (gridPosition.y + GridSize.y * gridPosition.z);
        }

        public Vector3Int WorldToGrid(Vector3 worldPosition)
        {
            Vector3 localPosition = worldPosition - WorldOrigin;
            return new Vector3Int(
                Mathf.FloorToInt(localPosition.x / VoxelSize),
                Mathf.FloorToInt(localPosition.y / VoxelSize),
                Mathf.FloorToInt(localPosition.z / VoxelSize)
            );
        }

        public Vector3 GridToWorld(Vector3Int gridPosition)
        {
            return new Vector3(
                gridPosition.x * VoxelSize + WorldOrigin.x,
                gridPosition.y * VoxelSize + WorldOrigin.y,
                gridPosition.z * VoxelSize + WorldOrigin.z
            );
        }

        public bool IsValidGridPosition(Vector3Int gridPosition)
        {
            return gridPosition.x >= 0 && gridPosition.x < GridSize.x &&
                   gridPosition.y >= 0 && gridPosition.y < GridSize.y &&
                   gridPosition.z >= 0 && gridPosition.z < GridSize.z;
        }

        public void SetVoxelData(Vector3Int gridPosition, float temperature, float moisture, float windSpeed, BiomeType biome)
        {
            int index = GetVoxelIndex(gridPosition);
            if (index == -1) return;

            Temperature[index] = temperature;
            Moisture[index] = moisture;
            WindSpeed[index] = windSpeed;
            Biomes[index] = biome;
        }

        public void GetVoxelData(Vector3Int gridPosition, out float temperature, out float moisture, out float windSpeed, out BiomeType biome)
        {
            int index = GetVoxelIndex(gridPosition);
            if (index == -1)
            {
                temperature = 0f;
                moisture = 0f;
                windSpeed = 0f;
                biome = BiomeType.Plains;
                return;
            }

            temperature = Temperature[index];
            moisture = Moisture[index];
            windSpeed = WindSpeed[index];
            biome = Biomes[index];
        }

        public void InterpolateEnvironmentalData(Vector3 worldPosition, out float temperature, out float moisture, out float windSpeed)
        {
            Vector3Int gridPos = WorldToGrid(worldPosition);
            Vector3 localPos = (worldPosition - WorldOrigin) / VoxelSize - gridPos;

            temperature = 0f;
            moisture = 0f;
            windSpeed = 0f;
            float totalWeight = 0f;

            // Trilinear interpolation
            for (int z = 0; z <= 1; z++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int x = 0; x <= 1; x++)
                    {
                        Vector3Int samplePos = gridPos + new Vector3Int(x, y, z);
                        if (!IsValidGridPosition(samplePos)) continue;

                        float weight = (1 - Mathf.Abs(x - localPos.x)) *
                                     (1 - Mathf.Abs(y - localPos.y)) *
                                     (1 - Mathf.Abs(z - localPos.z));

                        int index = GetVoxelIndex(samplePos);
                        temperature += Temperature[index] * weight;
                        moisture += Moisture[index] * weight;
                        windSpeed += WindSpeed[index] * weight;
                        totalWeight += weight;
                    }
                }
            }

            if (totalWeight > 0)
            {
                temperature /= totalWeight;
                moisture /= totalWeight;
                windSpeed /= totalWeight;
            }
        }
    }
}
