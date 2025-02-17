using UnityEngine;
using System.Collections.Generic;
using ECS.Core;
using ECS.Components;

namespace ECS.Systems
{
    public class WeatherSystem : ISystem
    {
        private World world;
        private TimeSystem timeSystem;

        public enum WeatherState
        {
            Clear,
            Cloudy,
            Rain,
            Thunder,
            Snow,
            Sandstorm
        }

        private WeatherState currentWeather = WeatherState.Clear;
        private float weatherIntensity = 0f;
        private float weatherDuration = 0f;
        private float transitionTime = 0f;
        private WeatherState targetWeather = WeatherState.Clear;

        // Weather probability weights based on time of day
        private Dictionary<TimeSystem.DayPhase, Dictionary<WeatherState, float>> weatherProbabilities;

    public WeatherSystem(World world, TimeSystem timeSystem)
    {
        this.world = world;
        this.timeSystem = timeSystem;
        InitializeWeatherProbabilities();
    }

    private void InitializeWeatherProbabilities()
    {
        weatherProbabilities = new Dictionary<TimeSystem.DayPhase, Dictionary<WeatherState, float>>
        {
            {
                TimeSystem.DayPhase.Dawn, new Dictionary<WeatherState, float>
                {
                    { WeatherState.Clear, 0.4f },
                    { WeatherState.Cloudy, 0.3f },
                    { WeatherState.Rain, 0.2f },
                    { WeatherState.Thunder, 0.0f },
                    { WeatherState.Snow, 0.1f },
                    { WeatherState.Sandstorm, 0.0f }
                }
            },
            {
                TimeSystem.DayPhase.Day, new Dictionary<WeatherState, float>
                {
                    { WeatherState.Clear, 0.5f },
                    { WeatherState.Cloudy, 0.2f },
                    { WeatherState.Rain, 0.15f },
                    { WeatherState.Thunder, 0.05f },
                    { WeatherState.Snow, 0.05f },
                    { WeatherState.Sandstorm, 0.05f }
                }
            },
            {
                TimeSystem.DayPhase.Dusk, new Dictionary<WeatherState, float>
                {
                    { WeatherState.Clear, 0.4f },
                    { WeatherState.Cloudy, 0.3f },
                    { WeatherState.Rain, 0.2f },
                    { WeatherState.Thunder, 0.1f },
                    { WeatherState.Snow, 0.0f },
                    { WeatherState.Sandstorm, 0.0f }
                }
            },
            {
                TimeSystem.DayPhase.Night, new Dictionary<WeatherState, float>
                {
                    { WeatherState.Clear, 0.6f },
                    { WeatherState.Cloudy, 0.2f },
                    { WeatherState.Rain, 0.1f },
                    { WeatherState.Thunder, 0.1f },
                    { WeatherState.Snow, 0.0f },
                    { WeatherState.Sandstorm, 0.0f }
                }
            }
        };
    }

    public void Update(float deltaTime)
    {
        UpdateWeatherState(deltaTime);
        ApplyWeatherEffects(deltaTime);
    }

    private void UpdateWeatherState(float deltaTime)
    {
        // Update weather duration
        if (weatherDuration > 0)
        {
            weatherDuration -= deltaTime;
            if (weatherDuration <= 0)
            {
                // Time to transition to new weather
                DetermineNextWeather();
            }
        }

        // Handle weather transitions
        if (currentWeather != targetWeather)
        {
            transitionTime += deltaTime;
            float transitionDuration = 10f; // 10 seconds to transition
            weatherIntensity = Mathf.Lerp(weatherIntensity, 
                targetWeather == WeatherState.Clear ? 0f : 1f, 
                transitionTime / transitionDuration);

            if (transitionTime >= transitionDuration)
            {
                currentWeather = targetWeather;
                transitionTime = 0f;
            }
        }
    }

    private void DetermineNextWeather()
    {
        var currentPhase = timeSystem.GetCurrentPhase();
        var probabilities = weatherProbabilities[currentPhase];

        float totalWeight = 0f;
        foreach (var prob in probabilities.Values)
        {
            totalWeight += prob;
        }

        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var kvp in probabilities)
        {
            cumulative += kvp.Value;
            if (random <= cumulative)
            {
                targetWeather = kvp.Key;
                weatherDuration = Random.Range(300f, 900f); // 5-15 minutes
                break;
            }
        }
    }

    private void ApplyWeatherEffects(float deltaTime)
    {
        foreach (var entity in world.GetEntities())
        {
            if (!entity.HasComponent<WeatherComponent>())
                continue;

            var weatherComp = entity.GetComponent<WeatherComponent>();
            
            // Update existing effects
            weatherComp.UpdateEffects(deltaTime);

            // Apply new weather effects based on current weather
            switch (currentWeather)
            {
                case WeatherState.Rain:
                    ApplyRainEffects(weatherComp);
                    break;
                case WeatherState.Thunder:
                    ApplyThunderEffects(weatherComp);
                    break;
                case WeatherState.Snow:
                    ApplySnowEffects(weatherComp);
                    break;
                case WeatherState.Sandstorm:
                    ApplySandstormEffects(weatherComp);
                    break;
            }
        }
    }

    private void ApplyRainEffects(WeatherComponent weatherComp)
    {
        float resistance = weatherComp.GetResistanceMultiplier(weatherComp.RainResistance);
        weatherComp.ApplyWeatherEffect(
            WeatherComponent.WeatherEffect.Wet,
            weatherIntensity * resistance,
            5f
        );
    }

    private void ApplyThunderEffects(WeatherComponent weatherComp)
    {
        // First apply rain effects
        ApplyRainEffects(weatherComp);

        // Then maybe apply lightning
        if (Random.value < weatherIntensity * 0.1f) // 10% chance per second at max intensity
        {
            float resistance = weatherComp.GetResistanceMultiplier(weatherComp.ThunderResistance);
            weatherComp.ApplyWeatherEffect(
                WeatherComponent.WeatherEffect.Electrified,
                weatherIntensity * resistance,
                0.5f
            );
        }
    }

    private void ApplySnowEffects(WeatherComponent weatherComp)
    {
        float resistance = weatherComp.GetResistanceMultiplier(weatherComp.SnowResistance);
        weatherComp.ApplyWeatherEffect(
            WeatherComponent.WeatherEffect.Frozen,
            weatherIntensity * resistance,
            3f
        );
    }

        private void ApplySandstormEffects(WeatherComponent weatherComp)
        {
            float resistance = weatherComp.GetResistanceMultiplier(weatherComp.SandstormResistance);
            weatherComp.ApplyWeatherEffect(
                WeatherComponent.WeatherEffect.Burning,
                weatherIntensity * resistance,
                2f
            );
        }

        public WeatherState GetCurrentWeather()
        {
            return currentWeather;
        }

        public float GetWeatherIntensity()
        {
            return weatherIntensity;
        }
    }
}
