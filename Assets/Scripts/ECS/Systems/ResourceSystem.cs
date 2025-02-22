using UnityEngine;
using ECS.Core;
using ECS.Components;
using System.Collections.Generic;
using System.Linq;

namespace ECS.Systems
{
    public class ResourceSystem : ISystem
    {
        private World world;
        private List<Entity> entities;
        private NeedSystem needSystem;

        // Resource spawn settings
        private const float RESOURCE_SPAWN_INTERVAL = 30f;
        private float nextSpawnTime;

        // Resource distribution settings
        private const float MIN_RESOURCE_SPACING = 10f;
        private const int MAX_RESOURCES_PER_TYPE = 10;
        private const float WORLD_BOUNDS = 100f;

        public ResourceSystem(World world, NeedSystem needSystem)
        {
            this.world = world;
            this.entities = world.GetEntities();
            this.needSystem = needSystem;
            this.nextSpawnTime = Time.time + RESOURCE_SPAWN_INTERVAL;
        }

        public void Update(float deltaTime)
        {
            // Update all resources
            foreach (var entity in entities)
            {
                var resource = entity.GetComponent<ResourceComponent>();
                if (resource == null) continue;

                resource.UpdateResource(deltaTime);
            }

            // Handle resource spawning
            if (Time.time >= nextSpawnTime)
            {
                SpawnNewResources();
                nextSpawnTime = Time.time + RESOURCE_SPAWN_INTERVAL;
            }

            // Handle entity-resource interactions
            HandleResourceInteractions(deltaTime);
        }

        private void HandleResourceInteractions(float deltaTime)
        {
            foreach (var entity in entities)
            {
                var behavior = entity.GetComponent<BehaviorComponent>();
                var position = entity.GetComponent<Position3DComponent>();
                var needs = entity.GetComponent<NeedComponent>();

                if (behavior == null || position == null || needs == null) continue;

                // Check if entity is near its target resource
                if (behavior.CurrentState == BehaviorState.SeekingFood ||
                    behavior.CurrentState == BehaviorState.SeekingWater)
                {
                    var nearbyResource = FindNearestResource(
                        position.Position,
                        behavior.CurrentState == BehaviorState.SeekingFood ? 
                            ResourceType.Food : ResourceType.Water
                    );

                    if (nearbyResource != null)
                    {
                        var resourceComponent = nearbyResource.GetComponent<ResourceComponent>();
                        if (resourceComponent.IsWithinRange(position.Position, GetEntityPosition(nearbyResource)))
                        {
                            ConsumeResource(entity, nearbyResource);
                        }
                    }
                }
            }
        }

        private void ConsumeResource(Entity consumer, Entity resource)
        {
            var resourceComponent = resource.GetComponent<ResourceComponent>();
            var needs = consumer.GetComponent<NeedComponent>();
            var behavior = consumer.GetComponent<BehaviorComponent>();

            if (resourceComponent == null || needs == null || behavior == null) return;

            float consumeAmount = 20f; // Base consumption amount
            float satisfactionAmount = resourceComponent.Use(consumer, consumeAmount);

            if (satisfactionAmount > 0)
            {
                // Update the corresponding need
                switch (resourceComponent.Type)
                {
                    case ResourceType.Food:
                        needs.ModifyNeed(NeedType.Hunger, satisfactionAmount);
                        break;
                    case ResourceType.Water:
                        needs.ModifyNeed(NeedType.Thirst, satisfactionAmount);
                        break;
                    case ResourceType.RestSpot:
                        needs.ModifyNeed(NeedType.Energy, satisfactionAmount);
                        break;
                }

                // Remember this location if it was satisfying
                if (satisfactionAmount >= consumeAmount * 0.8f)
                {
                    var resourcePos = GetEntityPosition(resource);
                    string locationType = resourceComponent.Type switch
                    {
                        ResourceType.Food => "FoodSource",
                        ResourceType.Water => "WaterSource",
                        ResourceType.RestSpot => "RestPlace",
                        _ => null
                    };

                    if (locationType != null)
                    {
                        behavior.RememberLocation(locationType, resourcePos);
                    }
                }

                // If need is satisfied, return to idle
                switch (resourceComponent.Type)
                {
                    case ResourceType.Food when !needs.IsNeedUrgent(NeedType.Hunger):
                    case ResourceType.Water when !needs.IsNeedUrgent(NeedType.Thirst):
                    case ResourceType.RestSpot when !needs.IsNeedUrgent(NeedType.Energy):
                        behavior.UpdateState(BehaviorState.Idle);
                        break;
                }
            }
        }

        private void SpawnNewResources()
        {
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                if (type == ResourceType.RestSpot) continue; // Rest spots are placed differently

                var existingCount = CountResourcesOfType(type);
                if (existingCount >= MAX_RESOURCES_PER_TYPE) continue;

                int numToSpawn = MAX_RESOURCES_PER_TYPE - existingCount;
                for (int i = 0; i < numToSpawn; i++)
                {
                    SpawnResource(type);
                }
            }
        }

        private void SpawnResource(ResourceType type)
        {
            Vector3 position = FindValidResourcePosition();
            if (position == Vector3.zero) return; // No valid position found

            var entity = world.CreateEntity();
            
            // Add position component
            entity.AddComponent(new Position3DComponent(position));

            // Add resource component based on type
            ResourceComponent resource = type switch
            {
                ResourceType.Food => ResourceComponent.CreateFoodSource(
                    quantity: Random.Range(50f, 150f),
                    quality: Random.Range(0.6f, 1f)
                ),
                ResourceType.Water => ResourceComponent.CreateWaterSource(
                    infinite: Random.value > 0.7f // 30% chance of infinite water source
                ),
                _ => new ResourceComponent(type) // Default initialization for other types
            };

            entity.AddComponent(resource);
        }

        private Vector3 FindValidResourcePosition()
        {
            const int MAX_ATTEMPTS = 30;
            for (int i = 0; i < MAX_ATTEMPTS; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-WORLD_BOUNDS, WORLD_BOUNDS),
                    0, // Assuming resources are placed on ground level
                    Random.Range(-WORLD_BOUNDS, WORLD_BOUNDS)
                );

                // Check if position is far enough from other resources
                if (IsValidResourcePosition(position))
                {
                    return position;
                }
            }
            return Vector3.zero; // No valid position found
        }

        private bool IsValidResourcePosition(Vector3 position)
        {
            foreach (var entity in entities)
            {
                var resource = entity.GetComponent<ResourceComponent>();
                var pos = entity.GetComponent<Position3DComponent>();
                
                if (resource != null && pos != null)
                {
                    if (Vector3.Distance(position, pos.Position) < MIN_RESOURCE_SPACING)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private int CountResourcesOfType(ResourceType type)
        {
            return entities.Count(e => 
            {
                var resource = e.GetComponent<ResourceComponent>();
                return resource != null && resource.Type == type;
            });
        }

        public Entity FindNearestResource(Vector3 position, ResourceType type)
        {
            Entity nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var entity in entities)
            {
                var resource = entity.GetComponent<ResourceComponent>();
                var pos = entity.GetComponent<Position3DComponent>();

                if (resource == null || pos == null || resource.Type != type || !resource.IsAvailable)
                    continue;

                float distance = Vector3.Distance(position, pos.Position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = entity;
                }
            }

            return nearest;
        }

        private Vector3 GetEntityPosition(Entity entity)
        {
            var position = entity.GetComponent<Position3DComponent>();
            return position?.Position ?? Vector3.zero;
        }
    }
}
