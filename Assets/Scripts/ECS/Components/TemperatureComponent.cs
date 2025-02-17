using UnityEngine;
using ECS.Core;

namespace ECS.Components
{
    public class TemperatureComponent : IComponent
    {
        public float CurrentTemperature { get; private set; }
        public float OptimalTemperature { get; private set; }
        public float TemperatureTolerance { get; private set; }
        public float AdaptationRate { get; private set; }

        public TemperatureComponent(
            float optimalTemperature = 20f,
            float tolerance = 10f,
            float adaptRate = 1f)
        {
            OptimalTemperature = optimalTemperature;
            TemperatureTolerance = tolerance;
            AdaptationRate = adaptRate;
            CurrentTemperature = optimalTemperature;
        }

        public void UpdateTemperature(float environmentalTemperature, float deltaTime)
        {
            CurrentTemperature = Mathf.Lerp(
                CurrentTemperature,
                environmentalTemperature,
                AdaptationRate * deltaTime
            );
        }

        public float GetTemperatureStress()
        {
            float temperatureDifference = Mathf.Abs(CurrentTemperature - OptimalTemperature);
            return Mathf.Clamp01(temperatureDifference / TemperatureTolerance);
        }
    }
}
