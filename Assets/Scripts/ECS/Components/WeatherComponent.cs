using UnityEngine;
using ECS.Core;

namespace ECS.Components
{
    public class WeatherComponent : IComponent
    {
        public enum WeatherResistance
        {
            None,
            Low,
            Medium,
            High,
            Immune
        }

        public enum WeatherEffect
        {
            None,
            Wet,
            Frozen,
            Electrified,
            Burning
        }

        // Weather resistance properties
        public WeatherResistance RainResistance { get; private set; }
        public WeatherResistance ThunderResistance { get; private set; }
        public WeatherResistance SnowResistance { get; private set; }
        public WeatherResistance SandstormResistance { get; private set; }

        // Current weather effects
        public WeatherEffect CurrentEffect { get; private set; }
        public float EffectIntensity { get; private set; }
        public float EffectDuration { get; private set; }

        public WeatherComponent(
            WeatherResistance rainResistance = WeatherResistance.None,
            WeatherResistance thunderResistance = WeatherResistance.None,
            WeatherResistance snowResistance = WeatherResistance.None,
            WeatherResistance sandstormResistance = WeatherResistance.None)
        {
            RainResistance = rainResistance;
            ThunderResistance = thunderResistance;
            SnowResistance = snowResistance;
            SandstormResistance = sandstormResistance;
            
            CurrentEffect = WeatherEffect.None;
            EffectIntensity = 0f;
            EffectDuration = 0f;
        }

        public void ApplyWeatherEffect(WeatherEffect effect, float intensity, float duration)
        {
            // Only apply if the new effect is more intense or it's a different effect
            if (intensity > EffectIntensity || effect != CurrentEffect)
            {
                CurrentEffect = effect;
                EffectIntensity = Mathf.Clamp01(intensity);
                EffectDuration = Mathf.Max(0f, duration);
            }
        }

        public void UpdateEffects(float deltaTime)
        {
            if (EffectDuration > 0)
            {
                EffectDuration -= deltaTime;
                if (EffectDuration <= 0)
                {
                    CurrentEffect = WeatherEffect.None;
                    EffectIntensity = 0f;
                }
            }
        }

        public float GetResistanceMultiplier(WeatherResistance resistance)
        {
            switch (resistance)
            {
                case WeatherResistance.None:
                    return 1f;
                case WeatherResistance.Low:
                    return 0.75f;
                case WeatherResistance.Medium:
                    return 0.5f;
                case WeatherResistance.High:
                    return 0.25f;
                case WeatherResistance.Immune:
                    return 0f;
                default:
                    return 1f;
            }
        }
    }
}
