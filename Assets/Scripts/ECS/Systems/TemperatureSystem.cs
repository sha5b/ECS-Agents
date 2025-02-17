using UnityEngine;
using ECS.Core;
using ECS.Components;

namespace ECS.Systems
{
    public class TemperatureSystem : ISystem
    {
        private World world;
        private float environmentalTemperature = 20f;
        private float damageThreshold = 0.8f;
        private float damageRate = 10f; // Damage per second when at max stress

        public TemperatureSystem(World world)
        {
            this.world = world;
        }

        public void Update(float deltaTime)
        {
            foreach (var entity in world.GetEntities())
            {
                if (!entity.HasComponent<TemperatureComponent>())
                    continue;

                var tempComponent = entity.GetComponent<TemperatureComponent>();
                
                // Update temperature based on environment
                tempComponent.UpdateTemperature(environmentalTemperature, deltaTime);

                // Apply damage if entity has health and is under temperature stress
                if (entity.HasComponent<HealthComponent>())
                {
                    var healthComponent = entity.GetComponent<HealthComponent>();
                    float stress = tempComponent.GetTemperatureStress();
                    
                    if (stress > damageThreshold)
                    {
                        float damage = damageRate * (stress - damageThreshold) * deltaTime;
                        healthComponent.TakeDamage(damage);
                    }
                }
            }
        }

        public void SetEnvironmentalTemperature(float temperature)
        {
            environmentalTemperature = temperature;
        }

        public float GetEnvironmentalTemperature()
        {
            return environmentalTemperature;
        }
    }
}
