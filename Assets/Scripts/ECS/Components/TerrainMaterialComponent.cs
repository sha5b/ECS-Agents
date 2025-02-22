using UnityEngine;
using ECS.Core;
using ECS.Types;

namespace ECS.Components
{
    public class TerrainMaterialComponent : IComponent
    {
        public Material BaseMaterial { get; private set; }
        public Material[] LayerMaterials { get; private set; }
        public float[] LayerHeights { get; private set; }
        public float[] LayerStrengths { get; private set; }

        public TerrainMaterialComponent(BiomeType biomeType)
        {
            BaseMaterial = CreateBaseMaterial(biomeType);
            InitializeLayers(biomeType);
        }

        private Material CreateBaseMaterial(BiomeType biomeType)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = GetBiomeBaseColor(biomeType);
            material.SetFloat("_Smoothness", 0.2f); // Add some roughness
            material.enableInstancing = true; // Enable GPU instancing
            return material;
        }

        private void InitializeLayers(BiomeType biomeType)
        {
            switch (biomeType)
            {
                case BiomeType.Mountains:
                    LayerMaterials = new Material[]
                    {
                        CreateMaterial(new Color(0.5f, 0.5f, 0.5f)), // Rock
                        CreateMaterial(new Color(0.8f, 0.8f, 0.8f)), // Snow
                    };
                    LayerHeights = new float[] { 0.6f, 0.8f };
                    LayerStrengths = new float[] { 0.7f, 0.9f };
                    break;

                case BiomeType.Desert:
                    LayerMaterials = new Material[]
                    {
                        CreateMaterial(new Color(0.76f, 0.7f, 0.5f)), // Sand
                    };
                    LayerHeights = new float[] { 0.3f };
                    LayerStrengths = new float[] { 0.8f };
                    break;

                // Add more biome-specific materials...
                default:
                    LayerMaterials = new Material[] { BaseMaterial };
                    LayerHeights = new float[] { 0.5f };
                    LayerStrengths = new float[] { 1.0f };
                    break;
            }
        }

        private Material CreateMaterial(Color color)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            material.SetFloat("_Smoothness", 0.2f);
            material.enableInstancing = true;
            return material;
        }

        private Color GetBiomeBaseColor(BiomeType biomeType)
        {
            switch (biomeType)
            {
                case BiomeType.Plains:
                    return new Color(0.3f, 0.5f, 0.3f);
                case BiomeType.Forest:
                    return new Color(0.2f, 0.4f, 0.2f);
                case BiomeType.Mountains:
                    return new Color(0.5f, 0.5f, 0.5f);
                case BiomeType.Desert:
                    return new Color(0.76f, 0.7f, 0.5f);
                case BiomeType.Tundra:
                    return new Color(0.9f, 0.9f, 0.9f);
                case BiomeType.Swamp:
                    return new Color(0.4f, 0.4f, 0.3f);
                default:
                    return Color.white;
            }
        }
    }
}
