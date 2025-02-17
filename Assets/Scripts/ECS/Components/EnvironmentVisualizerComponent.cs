using UnityEngine;
using ECS.Core;

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

            // Normals (pointing outward for each vertex)
            Vector3[] normals = new Vector3[]
            {
                new Vector3(-1, -1, -1).normalized,
                new Vector3(1, -1, -1).normalized,
                new Vector3(1, 1, -1).normalized,
                new Vector3(-1, 1, -1).normalized,
                new Vector3(-1, -1, 1).normalized,
                new Vector3(1, -1, 1).normalized,
                new Vector3(1, 1, 1).normalized,
                new Vector3(-1, 1, 1).normalized
            };

            // Assign mesh data
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.RecalculateBounds();

            // Assign mesh to MeshFilter
            meshFilter.mesh = mesh;

            // Load and assign the visualization material
            visualizationMaterial = new Material(Shader.Find("Custom/EnvironmentalVisualization"));
            meshRenderer.material = visualizationMaterial;

            // Set default visualization parameters
            visualizationMaterial.SetFloat("_StepSize", 0.05f);
            visualizationMaterial.SetFloat("_Density", 1.0f);
            visualizationMaterial.SetFloat("_AlphaThreshold", 0.02f);

            isInitialized = true;
        }

        public void UpdateVisualization(RenderTexture volumeTexture)
        {
            if (!isInitialized || visualizationMaterial == null) return;
            visualizationMaterial.SetTexture("_VolumeTexture", volumeTexture);
        }

        public void SetVisualizationParameters(float stepSize, float density, float alphaThreshold)
        {
            if (!isInitialized || visualizationMaterial == null) return;
            visualizationMaterial.SetFloat("_StepSize", stepSize);
            visualizationMaterial.SetFloat("_Density", density);
            visualizationMaterial.SetFloat("_AlphaThreshold", alphaThreshold);
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
