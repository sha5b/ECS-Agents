# Unity ECS Implementation Guide

This document provides a comprehensive guide to our Entity Component System (ECS) implementation, inspired by systems like those found in The Legend of Zelda: Breath of the Wild.

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Core Components](#core-components)
3. [Creating Components](#creating-components)
4. [Creating Systems](#creating-systems)
5. [World Management](#world-management)
6. [Usage Examples](#usage-examples)

## Architecture Overview

Our ECS implementation consists of four main parts:

1. **Entity**: A container for components
2. **Components**: Data containers
3. **Systems**: Logic processors
4. **World**: Manager for entities and systems

### Directory Structure
```
Assets/Scripts/ECS/
├── Core/
│   ├── Entity.cs
│   ├── IComponent.cs
│   ├── ISystem.cs
│   └── World.cs
├── Components/
│   ├── HealthComponent.cs
│   └── TemperatureComponent.cs
└── Systems/
    └── TemperatureSystem.cs
```

## Core Components

### Entity (Entity.cs)
```csharp
public class Entity
{
    public int Id { get; private set; }
    private Dictionary<Type, IComponent> components;

    // Add component to entity
    public void AddComponent(IComponent component)
    
    // Get component from entity
    public T GetComponent<T>() where T : class, IComponent
    
    // Check if entity has component
    public bool HasComponent<T>() where T : IComponent
    
    // Remove component from entity
    public void RemoveComponent<T>() where T : IComponent
}
```

### IComponent (IComponent.cs)
```csharp
public interface IComponent
{
    // Marker interface for components
}
```

### ISystem (ISystem.cs)
```csharp
public interface ISystem
{
    void Update(float deltaTime);
}
```

### World (World.cs)
```csharp
public class World : MonoBehaviour
{
    private List<Entity> entities;
    private List<ISystem> systems;
    
    public Entity CreateEntity()
    public void DestroyEntity(Entity entity)
    public void AddSystem(ISystem system)
    public List<Entity> GetEntities()
}
```

## Creating Components

Components should:
1. Implement IComponent
2. Contain only data, no logic
3. Be immutable where possible

Example Component:
```csharp
public class HealthComponent : IComponent
{
    public float MaxHealth { get; private set; }
    public float CurrentHealth { get; private set; }

    public HealthComponent(float maxHealth)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
    }
}
```

## Creating Systems

Systems should:
1. Implement ISystem
2. Process entities with specific components
3. Contain logic for updating components

Example System:
```csharp
public class TemperatureSystem : ISystem
{
    private World world;
    private float environmentalTemperature = 20f;

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
            var healthComponent = entity.GetComponent<HealthComponent>();

            // Process entity components...
        }
    }
}
```

## World Management

The World class manages all entities and systems. To set up:

1. Create a World instance:
```csharp
World world = gameObject.AddComponent<World>();
```

2. Create and add systems:
```csharp
var temperatureSystem = new TemperatureSystem(world);
world.AddSystem(temperatureSystem);
```

3. Create entities and add components:
```csharp
var entity = world.CreateEntity();
entity.AddComponent(new HealthComponent(100f));
entity.AddComponent(new TemperatureComponent(20f, 10f, 1f));
```

## Usage Examples

### Creating an Entity with Multiple Components
```csharp
// Create entity
var entity = world.CreateEntity();

// Add health component
entity.AddComponent(new HealthComponent(100f));

// Add temperature component
entity.AddComponent(new TemperatureComponent(
    optimalTemperature: 20f,
    tolerance: 10f,
    adaptRate: 1f
));
```

### Creating a New Component
```csharp
public class StaminaComponent : IComponent
{
    public float MaxStamina { get; private set; }
    public float CurrentStamina { get; private set; }
    
    public StaminaComponent(float maxStamina)
    {
        MaxStamina = maxStamina;
        CurrentStamina = maxStamina;
    }
    
    public void UseStamina(float amount)
    {
        CurrentStamina = Mathf.Max(0, CurrentStamina - amount);
    }
    
    public void Recover(float amount)
    {
        CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + amount);
    }
}
```

### Creating a New System
```csharp
public class StaminaSystem : ISystem
{
    private World world;
    private float recoveryRate = 10f; // Stamina recovery per second
    
    public StaminaSystem(World world)
    {
        this.world = world;
    }
    
    public void Update(float deltaTime)
    {
        foreach (var entity in world.GetEntities())
        {
            if (!entity.HasComponent<StaminaComponent>())
                continue;
                
            var stamina = entity.GetComponent<StaminaComponent>();
            stamina.Recover(recoveryRate * deltaTime);
        }
    }
}
```

### System Integration Example
```csharp
public class GameManager : MonoBehaviour
{
    private World world;
    
    void Start()
    {
        // Create world
        world = gameObject.AddComponent<World>();
        
        // Add systems
        world.AddSystem(new TemperatureSystem(world));
        world.AddSystem(new StaminaSystem(world));
        
        // Create player entity
        var player = world.CreateEntity();
        player.AddComponent(new HealthComponent(100f));
        player.AddComponent(new StaminaComponent(100f));
        player.AddComponent(new TemperatureComponent(20f, 10f, 1f));
    }
}
```

## Best Practices

1. **Component Design**
   - Keep components data-only
   - Use immutable properties where possible
   - Include methods only for data manipulation

2. **System Design**
   - Systems should focus on one specific aspect
   - Check for required components before processing
   - Use dependency injection for external dependencies

3. **Entity Management**
   - Create entities through the World class
   - Destroy entities when no longer needed
   - Keep track of entity references where necessary

4. **Performance Considerations**
   - Cache component references when possible
   - Use HasComponent checks before GetComponent
   - Consider using object pooling for frequently created/destroyed entities

## Extending the System

To add new functionality:

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
