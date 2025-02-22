using UnityEngine;
using ECS.Core;

namespace ECS.Components
{
    public class TerrainChunkComponent : IComponent
    {
        public const int CHUNK_SIZE = 32; // Size in voxels
        public const float VOXEL_SIZE = 1f; // Size in Unity units

        public Vector2Int ChunkPosition { get; private set; } // Position in chunk coordinates
        public byte[,,] Voxels { get; private set; } // 3D array of voxel data
        public float[,,] DensityField { get; private set; } // For smooth terrain generation
        public GameObject ChunkObject { get; private set; }
        public bool IsGenerated { get; private set; }
        public bool IsMeshGenerated { get; private set; }

        public TerrainChunkComponent(Vector2Int chunkPosition)
        {
            ChunkPosition = chunkPosition;
            Voxels = new byte[CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE];
            DensityField = new float[CHUNK_SIZE + 1, CHUNK_SIZE + 1, CHUNK_SIZE + 1]; // +1 for marching cubes
            IsGenerated = false;
            IsMeshGenerated = false;
        }

        public void SetChunkObject(GameObject obj)
        {
            ChunkObject = obj;
        }

        public void MarkGenerated()
        {
            IsGenerated = true;
        }

        public void MarkMeshGenerated()
        {
            IsMeshGenerated = true;
        }

        public Vector3 GetWorldPosition()
        {
            return new Vector3(
                ChunkPosition.x * CHUNK_SIZE * VOXEL_SIZE,
                0,
                ChunkPosition.y * CHUNK_SIZE * VOXEL_SIZE
            );
        }
    }
}
