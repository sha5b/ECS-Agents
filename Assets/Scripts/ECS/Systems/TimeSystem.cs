using UnityEngine;
using ECS.Core;
using ECS.Components;

namespace ECS.Systems
{
    public class TimeSystem : ISystem
    {
        private World world;
        private float currentHour;
        private float timeScale = 1f; // 1 real second = 1 minute in game
        
        public enum DayPhase
        {
            Dawn,   // 5-7
            Day,    // 7-17
            Dusk,   // 17-19
            Night   // 19-5
        }

        public TimeSystem(World world, float startHour = 12f)
        {
            this.world = world;
            this.currentHour = Mathf.Clamp(startHour, 0f, 24f);
        }

        public void Update(float deltaTime)
        {
            // Update time (1 real second = 1 minute in game by default)
            float minutesPassedThisFrame = deltaTime * timeScale * 60f;
            float hoursPassedThisFrame = minutesPassedThisFrame / 60f;
            
            currentHour += hoursPassedThisFrame;
            if (currentHour >= 24f)
            {
                currentHour -= 24f;
            }

            // Update all entities with time components
            foreach (var entity in world.GetEntities())
            {
                if (!entity.HasComponent<TimeComponent>())
                    continue;

                var timeComponent = entity.GetComponent<TimeComponent>();
                
                // If entity has other components, apply time-based effects
                if (entity.HasComponent<TemperatureComponent>())
                {
                    var tempComponent = entity.GetComponent<TemperatureComponent>();
                    // Temperature varies by time of day
                    float multiplier = timeComponent.GetCurrentMultiplier(currentHour);
                    // This would be called from TemperatureSystem normally, but showing interaction
                    tempComponent.UpdateTemperature(GetEnvironmentTemperature(), deltaTime * multiplier);
                }
            }
        }

        public float GetCurrentHour()
        {
            return currentHour;
        }

        public DayPhase GetCurrentPhase()
        {
            if (currentHour >= 5 && currentHour < 7)
                return DayPhase.Dawn;
            else if (currentHour >= 7 && currentHour < 17)
                return DayPhase.Day;
            else if (currentHour >= 17 && currentHour < 19)
                return DayPhase.Dusk;
            else
                return DayPhase.Night;
        }

        public void SetTimeScale(float scale)
        {
            timeScale = Mathf.Max(0f, scale);
        }

        private float GetEnvironmentTemperature()
        {
            // Temperature varies by time of day
            // Coldest at 3AM (15°C), hottest at 3PM (30°C)
            float baseTemp = 22.5f; // Average temperature
            float amplitude = 7.5f;  // Temperature variation
            
            // Convert current hour to radians (24 hours = 2π)
            float hourInRadians = (currentHour - 3f) * Mathf.PI / 12f;
            
            return baseTemp + amplitude * Mathf.Cos(hourInRadians);
        }
    }
}
