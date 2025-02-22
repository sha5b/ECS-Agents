using UnityEngine;
using ECS.Core;
using System.Collections.Generic;

namespace ECS.Systems
{
    public class TerrainGeneratorSystem : ISystem
    {
        private World world;
        private GameObject terrainObject;
        private const float TERRAIN_SIZE = 200f;

        public TerrainGeneratorSystem(World world)
        {
            this.world = world;
            CreateBaseTerrain();
        }

        public void Update(float deltaTime)
        {
            // No update needed for static terrain
        }

        private void CreateBaseTerrain()
        {
            // Create a simple plane for NPCs to move on
            terrainObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            terrainObject.name = "Terrain";
            
            // Scale the plane to desired size (plane is 10x10 units by default)
            float scale = TERRAIN_SIZE / 10f;
            terrainObject.transform.localScale = new Vector3(scale, 1f, scale);
            
            // Position at origin
            terrainObject.transform.position = Vector3.zero;
            
            // Add physics
            if (!terrainObject.GetComponent<MeshCollider>())
            {
                terrainObject.AddComponent<MeshCollider>();
            }

            // Create a simple material
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(0.3f, 0.5f, 0.3f); // Grass-like green
            terrainObject.GetComponent<MeshRenderer>().material = material;

            Debug.Log("Base terrain created");
        }

        public void OnDestroy()
        {
            if (terrainObject != null)
            {
                Object.Destroy(terrainObject);
            }
        }

        public float GetTerrainSize()
        {
            return TERRAIN_SIZE;
        }
    }
}
