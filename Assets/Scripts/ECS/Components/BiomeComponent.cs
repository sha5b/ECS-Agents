using UnityEngine;
using ECS.Core;
using ECS.Types;

namespace ECS.Components
{
    public class BiomeComponent : IComponent
    {
        public BiomeType Type { get; private set; }
        public float Temperature { get; private set; }
        public float Humidity { get; private set; }
        public float Height { get; private set; }
        public Color BaseColor { get; private set; }

        public BiomeComponent(BiomeType type, float temperature, float humidity, float height)
        {
            Type = type;
            Temperature = temperature;
            Humidity = humidity;
            Height = height;
            BaseColor = GetBiomeColor(type);
        }

        private Color GetBiomeColor(BiomeType type)
        {
            switch (type)
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
