using UnityEngine;
using ECS.Core;
using ECS.Systems;

namespace ECS.Components
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class EnvironmentVisualizerComponent : MonoBehaviour, IComponent
    {
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Material visualizationMaterial;
        private bool isInitialized = false;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        public void Initialize(Vector3 size)
        {
            if (isInitialized) return;

            // Create visualization cube mesh
            Mesh mesh = new Mesh();
            
            // Vertices (8 corners of a cube)
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f)
            };

            // Scale vertices by size
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].x *= size.x;
                vertices[i].y *= size.y;
                vertices[i].z *= size.z;
            }

            // Triangles (6 faces, 2 triangles each = 36 indices)
            int[] triangles = new int[]
            {
                // Front
                0, 2, 1,
                0, 3, 2,
                // Right
                1, 2, 6,
                1, 6, 5,
                // Back
                5, 6, 7,
                5, 7, 4,
                // Left
                4, 7, 3,
                4, 3, 0,
                // Top
                3, 7, 6,
                3, 6, 2,
                // Bottom
                4, 0, 1,
                4, 1, 5
            };

            // Assign mesh data
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Assign mesh to MeshFilter
            meshFilter.mesh = mesh;

            // Create and assign the visualization material
            visualizationMaterial = new Material(Shader.Find("Custom/EnvironmentalVisualization"));
            if (visualizationMaterial == null)
            {
                Debug.LogError("Failed to find EnvironmentalVisualization shader!");
                return;
            }

            meshRenderer.material = visualizationMaterial;

            // Set default visualization parameters
            visualizationMaterial.SetFloat("_StepSize", 0.05f);
            visualizationMaterial.SetFloat("_Density", 1.0f);
            visualizationMaterial.SetFloat("_AlphaThreshold", 0.02f);

            // Enable temperature visualization by default
            SetVisualizationType(VoxelSystem.VisualizationType.Temperature);

            isInitialized = true;
        }

        public void UpdateVisualization(RenderTexture volumeTexture)
        {
            if (!isInitialized || visualizationMaterial == null) return;
            visualizationMaterial.SetTexture("_VolumeTexture", volumeTexture);
        }

        public void SetVisualizationType(VoxelSystem.VisualizationType type)
        {
            if (!isInitialized || visualizationMaterial == null) return;

            // Disable all keywords first
            visualizationMaterial.DisableKeyword("_VISTYPE_TEMPERATURE");
            visualizationMaterial.DisableKeyword("_VISTYPE_MOISTURE");
            visualizationMaterial.DisableKeyword("_VISTYPE_WINDSPEED");
            visualizationMaterial.DisableKeyword("_VISTYPE_BIOMES");

            // Enable the appropriate keyword
            switch (type)
            {
                case VoxelSystem.VisualizationType.Temperature:
                    visualizationMaterial.EnableKeyword("_VISTYPE_TEMPERATURE");
                    break;
                case VoxelSystem.VisualizationType.Moisture:
                    visualizationMaterial.EnableKeyword("_VISTYPE_MOISTURE");
                    break;
                case VoxelSystem.VisualizationType.WindSpeed:
                    visualizationMaterial.EnableKeyword("_VISTYPE_WINDSPEED");
                    break;
                case VoxelSystem.VisualizationType.Biomes:
                    visualizationMaterial.EnableKeyword("_VISTYPE_BIOMES");
                    break;
            }
        }

        private void OnDestroy()
        {
            if (visualizationMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(visualizationMaterial);
                }
                else
                {
                    DestroyImmediate(visualizationMaterial);
                }
            }

            if (meshFilter != null && meshFilter.mesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(meshFilter.mesh);
                }
                else
                {
                    DestroyImmediate(meshFilter.mesh);
                }
            }
        }
    }
}
