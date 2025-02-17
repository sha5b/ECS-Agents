using UnityEngine;

public class DemoManager : MonoBehaviour
{
    private World world;
    private TemperatureSystem temperatureSystem;
    private float timeSinceLastTemperatureChange = 0f;
    private float temperatureChangeInterval = 5f;
    private float minTemperature = 0f;
    private float maxTemperature = 40f;

    void Start()
    {
        // Get or create World component
        world = FindObjectOfType<World>();
        if (world == null)
        {
            world = gameObject.AddComponent<World>();
        }

        // Create and add temperature system
        temperatureSystem = new TemperatureSystem(world);
        world.AddSystem(temperatureSystem);

        // Create test entities
        CreateTestEntities();
    }

    void Update()
    {
        // Periodically change environmental temperature to demonstrate system
        timeSinceLastTemperatureChange += Time.deltaTime;
        if (timeSinceLastTemperatureChange >= temperatureChangeInterval)
        {
            float newTemperature = Random.Range(minTemperature, maxTemperature);
            temperatureSystem.SetEnvironmentalTemperature(newTemperature);
            Debug.Log($"Environmental temperature changed to: {newTemperature:F1}°C");
            timeSinceLastTemperatureChange = 0f;
        }

        // Log entity states
        foreach (var entity in world.GetEntities())
        {
            var health = entity.GetComponent<HealthComponent>();
            var temp = entity.GetComponent<TemperatureComponent>();
            if (health != null && temp != null)
            {
                Debug.Log($"Entity {entity.Id} - Health: {health.CurrentHealth:F1}, " +
                    $"Temperature: {temp.CurrentTemperature:F1}°C, " +
                    $"Stress: {temp.GetTemperatureStress():F2}");
            }
        }
    }

    private void CreateTestEntities()
    {
        // Create a normal entity (standard temperature tolerance)
        var normalEntity = world.CreateEntity();
        normalEntity.AddComponent(new HealthComponent(100f));
        normalEntity.AddComponent(new TemperatureComponent(20f, 10f, 1f));

        // Create a cold-resistant entity
        var coldEntity = world.CreateEntity();
        coldEntity.AddComponent(new HealthComponent(100f));
        coldEntity.AddComponent(new TemperatureComponent(10f, 15f, 0.5f));

        // Create a heat-resistant entity
        var heatEntity = world.CreateEntity();
        heatEntity.AddComponent(new HealthComponent(100f));
        heatEntity.AddComponent(new TemperatureComponent(30f, 15f, 0.5f));
    }
}
