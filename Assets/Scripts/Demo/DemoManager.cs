using UnityEngine;
using System.Collections.Generic;
using ECS.Core;
using ECS.Components;
using ECS.Systems;

namespace Demo
{
    public class DemoManager : MonoBehaviour
    {
        private World world;
        private TemperatureSystem temperatureSystem;
        private TimeSystem timeSystem;
        private WeatherSystem weatherSystem;
        private float timeScale = 60f; // 1 second real time = 1 minute game time

        void Start()
        {
            // Get or create World component
            world = FindObjectOfType<World>();
            if (world == null)
            {
                world = gameObject.AddComponent<World>();
            }

            // Create and add systems
            timeSystem = new TimeSystem(world, 12f); // Start at noon
            temperatureSystem = new TemperatureSystem(world);
            weatherSystem = new WeatherSystem(world, timeSystem);

            world.AddSystem(timeSystem);
            world.AddSystem(temperatureSystem);
            world.AddSystem(weatherSystem);

            // Set time scale
            timeSystem.SetTimeScale(timeScale);

            // Create test entities
            CreateTestEntities();

            // Log initial state
            LogWorldState();
        }

        private void Update()
        {
            // Log world state every game hour
            float currentHour = timeSystem.GetCurrentHour();
            float previousHour = currentHour - (Time.deltaTime * timeScale / 60f);
            
            if (Mathf.Floor(currentHour) != Mathf.Floor(previousHour))
            {
                LogWorldState();
            }
        }

        private void CreateTestEntities()
        {
            // Create a normal entity (standard tolerances)
            var normalEntity = world.CreateEntity();
            normalEntity.AddComponent(new HealthComponent(100f));
            normalEntity.AddComponent(new TemperatureComponent(20f, 10f, 1f));
            normalEntity.AddComponent(new TimeComponent(true, true, 6f, 22f)); // Active during day
            normalEntity.AddComponent(new WeatherComponent());

            // Create a nocturnal cold-resistant entity
            var coldEntity = world.CreateEntity();
            coldEntity.AddComponent(new HealthComponent(100f));
            coldEntity.AddComponent(new TemperatureComponent(10f, 15f, 0.5f));
            coldEntity.AddComponent(new TimeComponent(true, false, 20f, 6f)); // Active at night
            coldEntity.AddComponent(new WeatherComponent(
                WeatherComponent.WeatherResistance.High,
                WeatherComponent.WeatherResistance.Medium,
                WeatherComponent.WeatherResistance.Immune,
                WeatherComponent.WeatherResistance.Low
            ));

            // Create a diurnal heat-resistant entity
            var heatEntity = world.CreateEntity();
            heatEntity.AddComponent(new HealthComponent(100f));
            heatEntity.AddComponent(new TemperatureComponent(30f, 15f, 0.5f));
            heatEntity.AddComponent(new TimeComponent(false, true, 8f, 18f)); // Active during day
            heatEntity.AddComponent(new WeatherComponent(
                WeatherComponent.WeatherResistance.Medium,
                WeatherComponent.WeatherResistance.High,
                WeatherComponent.WeatherResistance.Low,
                WeatherComponent.WeatherResistance.Immune
            ));
        }

        private void LogWorldState()
        {
            var time = timeSystem.GetCurrentHour();
            var phase = timeSystem.GetCurrentPhase();
            var weather = weatherSystem.GetCurrentWeather();
            var intensity = weatherSystem.GetWeatherIntensity();

            Debug.Log($"\nWorld State Update:");
            Debug.Log($"Time: {time:F1}:00 ({phase})");
            Debug.Log($"Weather: {weather} (Intensity: {intensity:F2})");

            foreach (var entity in world.GetEntities())
            {
                var health = entity.GetComponent<HealthComponent>();
                var temp = entity.GetComponent<TemperatureComponent>();
                var timeComp = entity.GetComponent<TimeComponent>();
                var weatherComp = entity.GetComponent<WeatherComponent>();

                if (health != null && temp != null && timeComp != null && weatherComp != null)
                {
                    Debug.Log($"\nEntity {entity.Id}:");
                    Debug.Log($"- Health: {health.CurrentHealth:F1}");
                    Debug.Log($"- Temperature: {temp.CurrentTemperature:F1}°C (Stress: {temp.GetTemperatureStress():F2})");
                    Debug.Log($"- Active: {timeComp.IsActiveAtTime(time)}");
                    Debug.Log($"- Weather Effect: {weatherComp.CurrentEffect} (Intensity: {weatherComp.EffectIntensity:F2})");
                }
            }
        }
    }
}
