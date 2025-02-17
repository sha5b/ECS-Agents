using UnityEngine;

public class TemperatureSystem : ISystem
{
    private World world;
    private float environmentalTemperature = 20f;
    private float temperatureDamageThreshold = 0.7f; // When temperature stress is above this, entity takes damage
    private float temperatureDamage = 5f; // Damage per second when in extreme temperatures

    public TemperatureSystem(World world)
    {
        this.world = world;
    }

    public void SetEnvironmentalTemperature(float temperature)
    {
        environmentalTemperature = temperature;
    }

    public void Update(float deltaTime)
    {
        foreach (var entity in world.GetEntities())
        {
            if (!entity.HasComponent<TemperatureComponent>())
                continue;

            var tempComponent = entity.GetComponent<TemperatureComponent>();
            var healthComponent = entity.GetComponent<HealthComponent>();

            // Update entity's temperature based on environment
            tempComponent.UpdateTemperature(environmentalTemperature, deltaTime);

            // If entity has health and is in extreme temperature, apply damage
            if (healthComponent != null)
            {
                float stress = tempComponent.GetTemperatureStress();
                if (stress > temperatureDamageThreshold)
                {
                    float damage = temperatureDamage * (stress - temperatureDamageThreshold) * deltaTime;
                    healthComponent.TakeDamage(damage);
                }
            }
        }
    }
}
