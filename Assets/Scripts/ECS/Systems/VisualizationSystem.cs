using UnityEngine;
using ECS.Core;
using ECS.Components;

namespace ECS.Systems
{
    public class VisualizationSystem : ISystem
    {
        private World world;
        private WeatherSystem weatherSystem;
        private TimeSystem timeSystem;
        private VoxelSystem voxelSystem;

        // Weather visualization
        private ParticleSystem rainParticles;
        private ParticleSystem snowParticles;
        private ParticleSystem sandstormParticles;
        private GameObject lightningEffect;

        // Atmospheric effects
        private Material skyboxMaterial;
        private Light directionalLight;
        private float baseIntensity = 1f;
        private Color dayColor = new Color(1f, 0.95f, 0.8f);
        private Color nightColor = new Color(0.1f, 0.1f, 0.3f);

        public VisualizationSystem(World world, WeatherSystem weatherSystem, TimeSystem timeSystem, VoxelSystem voxelSystem)
        {
            this.world = world;
            this.weatherSystem = weatherSystem;
            this.timeSystem = timeSystem;
            this.voxelSystem = voxelSystem;

            InitializeVisualEffects();
        }

        private void InitializeVisualEffects()
        {
            // Find or create main directional light
            directionalLight = Object.FindFirstObjectByType<Light>();
            if (directionalLight == null)
            {
                var lightObj = new GameObject("Directional Light");
                directionalLight = lightObj.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
                directionalLight.intensity = baseIntensity;
            }

            // Create weather particle systems
            CreateWeatherParticleSystems();

            // Get or create skybox
            if (RenderSettings.skybox != null)
            {
                skyboxMaterial = RenderSettings.skybox;
            }
            else
            {
                skyboxMaterial = new Material(Shader.Find("Skybox/Procedural"));
                RenderSettings.skybox = skyboxMaterial;
            }
        }

        private void CreateWeatherParticleSystems()
        {
            // Rain
            var rainObj = new GameObject("Rain Effect");
            rainParticles = rainObj.AddComponent<ParticleSystem>();
            rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            var rainMain = rainParticles.main;
            rainMain.loop = true;
            rainMain.startSpeed = 20f;
            rainMain.startSize = 0.1f;
            rainMain.maxParticles = 1000;
            rainMain.duration = 1f;
            
            var rainEmission = rainParticles.emission;
            rainEmission.rateOverTime = 500;
            
            var rainShape = rainParticles.shape;
            rainShape.shapeType = ParticleSystemShapeType.Box;
            rainShape.scale = new Vector3(100f, 0f, 100f);

            // Snow
            var snowObj = new GameObject("Snow Effect");
            snowParticles = snowObj.AddComponent<ParticleSystem>();
            snowParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            var snowMain = snowParticles.main;
            snowMain.loop = true;
            snowMain.startSpeed = 2f;
            snowMain.startSize = 0.2f;
            snowMain.maxParticles = 2000;
            snowMain.duration = 1f;
            
            var snowEmission = snowParticles.emission;
            snowEmission.rateOverTime = 200;
            
            var snowShape = snowParticles.shape;
            snowShape.shapeType = ParticleSystemShapeType.Box;
            snowShape.scale = new Vector3(100f, 0f, 100f);

            // Sandstorm
            var sandObj = new GameObject("Sandstorm Effect");
            sandstormParticles = sandObj.AddComponent<ParticleSystem>();
            sandstormParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            var sandMain = sandstormParticles.main;
            sandMain.loop = true;
            sandMain.startSpeed = 15f;
            sandMain.startSize = 0.05f;
            sandMain.maxParticles = 5000;
            sandMain.duration = 1f;
            
            var sandEmission = sandstormParticles.emission;
            sandEmission.rateOverTime = 1000;
            
            var sandShape = sandstormParticles.shape;
            sandShape.shapeType = ParticleSystemShapeType.Box;
            sandShape.scale = new Vector3(100f, 20f, 100f);

            // Lightning
            lightningEffect = new GameObject("Lightning Effect");
            lightningEffect.SetActive(false);
        }

        public void Update(float deltaTime)
        {
            UpdateTimeVisualization();
            UpdateWeatherVisualization();
            UpdateAtmosphericEffects(deltaTime);
        }

        private void UpdateTimeVisualization()
        {
            float currentHour = timeSystem.GetCurrentHour();
            var dayPhase = timeSystem.GetCurrentPhase();

            // Update directional light rotation based on time
            float rotationAngle = (currentHour / 24f) * 360f - 90f;
            directionalLight.transform.rotation = Quaternion.Euler(rotationAngle, 170f, 0f);

            // Update light color and intensity based on time of day
            switch (dayPhase)
            {
                case TimeSystem.DayPhase.Dawn:
                    directionalLight.intensity = Mathf.Lerp(0.2f, 1f, (currentHour - 5f) / 2f);
                    directionalLight.color = Color.Lerp(nightColor, dayColor, (currentHour - 5f) / 2f);
                    break;
                case TimeSystem.DayPhase.Day:
                    directionalLight.intensity = baseIntensity;
                    directionalLight.color = dayColor;
                    break;
                case TimeSystem.DayPhase.Dusk:
                    directionalLight.intensity = Mathf.Lerp(1f, 0.2f, (currentHour - 17f) / 2f);
                    directionalLight.color = Color.Lerp(dayColor, nightColor, (currentHour - 17f) / 2f);
                    break;
                case TimeSystem.DayPhase.Night:
                    directionalLight.intensity = 0.2f;
                    directionalLight.color = nightColor;
                    break;
            }
        }

        private void UpdateWeatherVisualization()
        {
            var currentWeather = weatherSystem.GetCurrentWeather();
            var intensity = weatherSystem.GetWeatherIntensity();

            // Stop all particle systems first
            rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            snowParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sandstormParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            lightningEffect.SetActive(false);

            // Update and start appropriate system
            switch (currentWeather)
            {
                case WeatherSystem.WeatherState.Rain:
                    var rainEmission = rainParticles.emission;
                    rainEmission.rateOverTime = 500f * intensity;
                    rainParticles.Play();
                    break;
                case WeatherSystem.WeatherState.Snow:
                    var snowEmission = snowParticles.emission;
                    snowEmission.rateOverTime = 200f * intensity;
                    snowParticles.Play();
                    break;
                case WeatherSystem.WeatherState.Thunder:
                    var thunderEmission = rainParticles.emission;
                    thunderEmission.rateOverTime = 700f * intensity;
                    rainParticles.Play();
                    if (Random.value < intensity * 0.1f)
                    {
                        StartLightningEffect();
                    }
                    break;
                case WeatherSystem.WeatherState.Sandstorm:
                    var sandEmission = sandstormParticles.emission;
                    sandEmission.rateOverTime = 1000f * intensity;
                    sandstormParticles.Play();
                    break;
            }
        }

        private void UpdateAtmosphericEffects(float deltaTime)
        {
            var currentWeather = weatherSystem.GetCurrentWeather();
            var intensity = weatherSystem.GetWeatherIntensity();

            // Update fog based on weather
            switch (currentWeather)
            {
                case WeatherSystem.WeatherState.Clear:
                    RenderSettings.fog = false;
                    break;
                case WeatherSystem.WeatherState.Rain:
                    RenderSettings.fog = true;
                    RenderSettings.fogColor = new Color(0.5f, 0.5f, 0.5f);
                    RenderSettings.fogDensity = 0.01f * intensity;
                    break;
                case WeatherSystem.WeatherState.Snow:
                    RenderSettings.fog = true;
                    RenderSettings.fogColor = new Color(0.9f, 0.9f, 0.9f);
                    RenderSettings.fogDensity = 0.02f * intensity;
                    break;
                case WeatherSystem.WeatherState.Sandstorm:
                    RenderSettings.fog = true;
                    RenderSettings.fogColor = new Color(0.8f, 0.7f, 0.5f);
                    RenderSettings.fogDensity = 0.03f * intensity;
                    break;
            }
        }

        private void StartLightningEffect()
        {
            lightningEffect.SetActive(true);
            directionalLight.intensity = baseIntensity * 3f;
            Object.Destroy(lightningEffect, 0.1f); // Flash duration
        }

        public void Cleanup()
        {
            if (rainParticles != null)
            {
                rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Object.Destroy(rainParticles.gameObject);
            }
            if (snowParticles != null)
            {
                snowParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Object.Destroy(snowParticles.gameObject);
            }
            if (sandstormParticles != null)
            {
                sandstormParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Object.Destroy(sandstormParticles.gameObject);
            }
            if (lightningEffect != null)
            {
                Object.Destroy(lightningEffect);
            }
        }
    }
}
