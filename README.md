# Unity ECS NPC Simulation

This project implements an Entity Component System (ECS) for NPC simulation with behavior, needs, and resource management.

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Core Systems](#core-systems)
3. [Components](#components)
4. [NPC Behavior](#npc-behavior)
5. [Resource Management](#resource-management)
6. [Usage Examples](#usage-examples)

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
│   │   ├── BehaviorComponent.cs
│   │   ├── NeedComponent.cs
│   │   ├── Position3DComponent.cs
│   │   └── ResourceComponent.cs
│   └── Systems/
│       ├── BehaviorSystem.cs
│       ├── MovementSystem.cs
│       ├── NeedSystem.cs
│       ├── ResourceSystem.cs
│       ├── SpawnerSystem.cs
│       └── TerrainGeneratorSystem.cs
```

## Core Systems

### TerrainGeneratorSystem
Creates a flat plane for NPCs to move on:
- Generates basic terrain
- Handles collision detection
- Provides movement surface

### SpawnerSystem
Manages entity creation:
- Spawns NPCs with randomized traits
- Creates resources in the world
- Maintains entity population

### NeedSystem
Manages NPC needs:
- Hunger
- Thirst
- Energy
- Social interaction
- Tracks need satisfaction levels
- Triggers behavior changes based on needs

### BehaviorSystem
Handles NPC decision making:
- State management
- Need-based decisions
- Personality-driven actions
- Memory of important locations

### MovementSystem
Controls entity movement:
- Position updates
- Rotation handling
- Target following
- Collision avoidance

### ResourceSystem
Manages world resources:
- Food sources
- Water sources
- Rest spots
- Resource depletion and replenishment

## Components

### Position3DComponent
```csharp
public class Position3DComponent : IComponent
{
    public Vector3 Position { get; private set; }
    public Vector3 Scale { get; private set; }
    public Quaternion Rotation { get; private set; }
}
```

### NeedComponent
```csharp
public class NeedComponent : IComponent
{
    public float Hunger { get; private set; }
    public float Thirst { get; private set; }
    public float Energy { get; private set; }
    public float Social { get; private set; }
}
```

### BehaviorComponent
```csharp
public class BehaviorComponent : IComponent
{
    public BehaviorState CurrentState { get; private set; }
    public float Sociability { get; private set; }
    public float Productivity { get; private set; }
    public float Curiosity { get; private set; }
    public float Resilience { get; private set; }
}
```

### ResourceComponent
```csharp
public class ResourceComponent : IComponent
{
    public ResourceType Type { get; private set; }
    public float Quantity { get; private set; }
    public float Quality { get; private set; }
    public bool IsInfinite { get; private set; }
}
```

## NPC Behavior

NPCs make decisions based on:
1. Current needs (hunger, thirst, energy, social)
2. Personality traits (sociability, productivity, curiosity, resilience)
3. Memory of resource locations
4. Available resources in the environment

### Behavior States
- Idle
- SeekingFood
- SeekingWater
- Resting
- Socializing
- Working
- Exploring
- MovingToTarget

## Resource Management

Resources in the world:
- Food sources (depletable)
- Water sources (can be infinite)
- Rest spots (infinite)
- Resource quality affects need satisfaction
- Resources replenish over time
- NPCs remember resource locations

## Usage Examples

### Creating an NPC
```csharp
var npc = world.CreateEntity();

// Add core components
npc.AddComponent(new Position3DComponent(position));
npc.AddComponent(new NeedComponent());
npc.AddComponent(new BehaviorComponent(
    sociability: Random.Range(0.3f, 1f),
    productivity: Random.Range(0.3f, 1f),
    curiosity: Random.Range(0.3f, 1f),
    resilience: Random.Range(0.3f, 1f)
));
```

### Creating a Resource
```csharp
var resource = world.CreateEntity();

// Add components
resource.AddComponent(new Position3DComponent(position));
resource.AddComponent(ResourceComponent.CreateFoodSource(
    quantity: 100f,
    quality: 0.8f
));
```

### Setting Up the World
```csharp
// Create world
var world = gameObject.AddComponent<World>();

// Create systems
var terrainSystem = new TerrainGeneratorSystem(world);
var needSystem = new NeedSystem(world);
var resourceSystem = new ResourceSystem(world, needSystem);
var behaviorSystem = new BehaviorSystem(world, needSystem);
var movementSystem = new MovementSystem(world);
var spawnerSystem = new SpawnerSystem(world);

// Add systems in update order
world.AddSystem(terrainSystem);    // First: Create and manage terrain
world.AddSystem(needSystem);       // Second: Update NPC needs
world.AddSystem(behaviorSystem);   // Third: Make decisions based on needs
world.AddSystem(movementSystem);   // Fourth: Move entities based on decisions
world.AddSystem(resourceSystem);   // Fifth: Handle resource interactions
world.AddSystem(spawnerSystem);    // Last: Spawn new entities if needed
```
