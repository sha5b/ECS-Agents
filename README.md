# Unity ECS Implementation Guide

This document provides a comprehensive guide to our Entity Component System (ECS) implementation, inspired by systems like those found in The Legend of Zelda: Breath of the Wild.

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Core Components](#core-components)
3. [Environmental Systems](#environmental-systems)
4. [System Interactions](#system-interactions)
5. [Usage Examples](#usage-examples)
6. [Best Practices](#best-practices)

## Architecture Overview

Our ECS implementation consists of four main parts:

1. **Entity**: A container for components
2. **Components**: Data containers
3. **Systems**: Logic processors
4. **World**: Manager for entities and systems

### Directory Structure
```
Assets/Scripts/
├── ECS/
│   ├── Core/
│   │   ├── Entity.cs
│   │   ├── IComponent.cs
│   │   ├── ISystem.cs
│   │   └── World.cs
│   ├── Components/
│   │   ├── HealthComponent.cs
│   │   ├── TemperatureComponent.cs
│   │   ├── TimeComponent.cs
│   │   └── WeatherComponent.cs
│   └── Systems/
│       ├── TemperatureSystem.cs
│       ├── TimeSystem.cs
│       └── WeatherSystem.cs
└── Demo/
    └── DemoManager.cs
```

## Environmental Systems

### Time System
The TimeSystem manages the day/night cycle and time-based behaviors:
- Day phases: Dawn (5-7), Day (7-17), Dusk (17-19), Night (19-5)
- Affects entity activity patterns
- Influences weather probabilities
- Modifies temperature based on time of day

```csharp
// Example: Creating a time-aware entity
var entity = world.CreateEntity();
entity.AddComponent(new TimeComponent(
    activeAtNight: true,
    activeAtDay: false,
    activityStartTime: 20f, // 8 PM
    activityEndTime: 6f     // 6 AM
));
```

### Weather System
The WeatherSystem manages environmental conditions:
- Weather states: Clear, Cloudy, Rain, Thunder, Snow, Sandstorm
- Dynamic weather transitions
- Time-based weather probabilities
- Weather effects on entities

```csharp
// Example: Creating a weather-resistant entity
entity.AddComponent(new WeatherComponent(
    rainResistance: WeatherComponent.WeatherResistance.High,
    thunderResistance: WeatherComponent.WeatherResistance.Medium,
    snowResistance: WeatherComponent.WeatherResistance.Immune,
    sandstormResistance: WeatherComponent.WeatherResistance.Low
));
```

### Temperature System
The TemperatureSystem handles environmental and entity temperatures:
- Base temperature varies by time of day
- Weather affects temperature
- Entities have optimal temperature ranges
- Temperature stress causes damage

```csharp
// Example: Creating a temperature-sensitive entity
entity.AddComponent(new TemperatureComponent(
    optimalTemperature: 20f,
    tolerance: 10f,
    adaptRate: 1f
));
```

## System Interactions

### Time → Weather
- Different weather probabilities for each day phase
- Dawn: Higher chance of clear weather
- Day: Balanced probabilities
- Dusk: Higher chance of rain
- Night: Higher chance of clear skies

### Weather → Temperature
- Rain reduces temperature
- Snow indicates cold temperatures
- Clear weather allows normal temperature progression
- Sandstorms increase temperature

### Temperature → Health
- Temperature outside tolerance causes stress
- High stress causes damage
- Damage rate based on stress level
- Entities can have different resistances

## Usage Examples

### Creating a Complete Entity
```csharp
var entity = world.CreateEntity();

// Add health tracking
entity.AddComponent(new HealthComponent(100f));

// Add temperature sensitivity
entity.AddComponent(new TemperatureComponent(20f, 10f, 1f));

// Add time-based behavior
entity.AddComponent(new TimeComponent(true, true, 6f, 22f));

// Add weather resistance
entity.AddComponent(new WeatherComponent(
    WeatherComponent.WeatherResistance.Medium,
    WeatherComponent.WeatherResistance.High,
    WeatherComponent.WeatherResistance.Low,
    WeatherComponent.WeatherResistance.Immune
));
```

### Setting Up the World
```csharp
// Create world
var world = gameObject.AddComponent<World>();

// Create and add systems
var timeSystem = new TimeSystem(world, 12f);
var temperatureSystem = new TemperatureSystem(world);
var weatherSystem = new WeatherSystem(world, timeSystem);

world.AddSystem(timeSystem);
world.AddSystem(temperatureSystem);
world.AddSystem(weatherSystem);
```

## Best Practices

1. **System Order**
   - TimeSystem should update first
   - WeatherSystem should update second
   - TemperatureSystem should update last
   - This ensures proper environmental state propagation

2. **Component Design**
   - Keep components data-only
   - Use immutable properties where possible
   - Include methods only for data manipulation

3. **System Design**
   - Systems should focus on one aspect
   - Check for required components before processing
   - Consider dependencies between systems

4. **Entity Management**
   - Create entities through the World class
   - Destroy entities when no longer needed
   - Keep track of entity references where necessary

5. **Performance Considerations**
   - Cache component references when possible
   - Use HasComponent checks before GetComponent
   - Consider using object pooling for frequently created/destroyed entities

## Extending the System

To add new environmental effects:

1. Create new components for new data types
2. Create new systems to process the components
3. Add the systems to the World
4. Create entities with the new components

Example workflow for adding a new feature:
1. Identify the data needed (create components)
2. Identify the logic needed (create systems)
3. Integrate with existing systems if necessary
4. Test with sample entities

Remember: Components are for data, Systems are for logic, and Entities are just containers for Components.
