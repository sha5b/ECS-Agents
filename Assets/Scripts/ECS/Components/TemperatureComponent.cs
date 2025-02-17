using UnityEngine;

public class TemperatureComponent : IComponent
{
    public float CurrentTemperature { get; private set; }
    public float OptimalTemperature { get; private set; }
    public float TemperatureTolerance { get; private set; }
    
    // How quickly the entity's temperature changes based on environment
    public float TemperatureAdaptRate { get; private set; }

    public TemperatureComponent(float optimalTemperature = 20f, float tolerance = 10f, float adaptRate = 1f)
    {
        OptimalTemperature = optimalTemperature;
        TemperatureTolerance = tolerance;
        TemperatureAdaptRate = adaptRate;
        CurrentTemperature = optimalTemperature;
    }

    public void UpdateTemperature(float environmentalTemperature, float deltaTime)
    {
        CurrentTemperature = Mathf.Lerp(CurrentTemperature, environmentalTemperature, 
            TemperatureAdaptRate * deltaTime);
    }

    public bool IsInDangerZone()
    {
        return Mathf.Abs(CurrentTemperature - OptimalTemperature) > TemperatureTolerance;
    }

    public float GetTemperatureStress()
    {
        // Returns a value from 0 to 1 indicating how stressed the entity is from temperature
        float temperatureDifference = Mathf.Abs(CurrentTemperature - OptimalTemperature);
        return Mathf.Clamp01(temperatureDifference / TemperatureTolerance);
    }
}
