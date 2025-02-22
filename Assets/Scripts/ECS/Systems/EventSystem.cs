using UnityEngine;
using ECS.Core;
using ECS.Components;
using System;
using System.Collections.Generic;

namespace ECS.Systems
{
    public enum EventType
    {
        ResourceDiscovered,    // New resource found
        ResourceDepleted,      // Resource exhausted
        SocialGathering,      // Spontaneous gathering of NPCs
        TaskOpportunity,      // Special task available
        AreaClosed,           // Area becomes temporarily inaccessible
        AreaOpened            // Area becomes accessible again
    }

    public class EventSystem : ISystem
    {
        private World world;
        private List<Entity> entities;
        private Queue<WorldEvent> eventQueue;
        private List<WorldEvent> activeEvents;
        private float nextEventCheck;

        // Event generation settings
        private const float EVENT_CHECK_INTERVAL = 10f;
        private const float MIN_EVENT_DURATION = 30f;
        private const float MAX_EVENT_DURATION = 300f;
        private const int MAX_CONCURRENT_EVENTS = 3;

        public EventSystem(World world)
        {
            this.world = world;
            this.entities = world.GetEntities();
            this.eventQueue = new Queue<WorldEvent>();
            this.activeEvents = new List<WorldEvent>();
            this.nextEventCheck = Time.time + EVENT_CHECK_INTERVAL;
        }

        public void Update(float deltaTime)
        {
            // Update active events
            UpdateActiveEvents(deltaTime);

            // Check for new events
            if (Time.time >= nextEventCheck)
            {
                GenerateNewEvents();
                nextEventCheck = Time.time + EVENT_CHECK_INTERVAL;
            }

            // Process event queue
            while (eventQueue.Count > 0 && activeEvents.Count < MAX_CONCURRENT_EVENTS)
            {
                ActivateNextEvent();
            }
        }

        private void UpdateActiveEvents(float deltaTime)
        {
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                var worldEvent = activeEvents[i];
                worldEvent.TimeRemaining -= deltaTime;

                if (worldEvent.TimeRemaining <= 0)
                {
                    EndEvent(worldEvent);
                    activeEvents.RemoveAt(i);
                }
                else
                {
                    UpdateEventEffects(worldEvent, deltaTime);
                }
            }
        }

        private void GenerateNewEvents()
        {
            // Check resource-related events
            GenerateResourceEvents();

            // Check social events
            GenerateSocialEvents();

            // Check area events
            GenerateAreaEvents();
        }

        private void GenerateResourceEvents()
        {
            // Calculate resource distribution
            var resources = new List<Entity>();
            foreach (var entity in entities)
            {
                if (entity.GetComponent<ResourceComponent>() != null)
                {
                    resources.Add(entity);
                }
            }

            // Generate resource discovery events in areas with few resources
            if (resources.Count < 5 && UnityEngine.Random.value < 0.3f)
            {
                QueueEvent(new WorldEvent
                {
                    Type = EventType.ResourceDiscovered,
                    Location = GenerateValidLocation(),
                    TimeRemaining = UnityEngine.Random.Range(MIN_EVENT_DURATION, MAX_EVENT_DURATION),
                    Radius = 10f
                });
            }
        }

        private void GenerateSocialEvents()
        {
            // Check for potential social gathering conditions
            var npcsInArea = new List<Entity>();
            foreach (var entity in entities)
            {
                if (entity.GetComponent<SocialComponent>() != null)
                {
                    npcsInArea.Add(entity);
                }
            }

            // Generate social gathering if enough NPCs are nearby
            if (npcsInArea.Count >= 3 && UnityEngine.Random.value < 0.2f)
            {
                Vector3 centerPoint = CalculateGroupCenter(npcsInArea);
                QueueEvent(new WorldEvent
                {
                    Type = EventType.SocialGathering,
                    Location = centerPoint,
                    TimeRemaining = UnityEngine.Random.Range(MIN_EVENT_DURATION, MAX_EVENT_DURATION / 2f),
                    Radius = 15f
                });
            }
        }

        private void GenerateAreaEvents()
        {
            // Randomly close/open areas to create dynamic navigation challenges
            if (UnityEngine.Random.value < 0.1f)
            {
                Vector3 location = GenerateValidLocation();
                QueueEvent(new WorldEvent
                {
                    Type = UnityEngine.Random.value < 0.5f ? EventType.AreaClosed : EventType.AreaOpened,
                    Location = location,
                    TimeRemaining = UnityEngine.Random.Range(MIN_EVENT_DURATION, MAX_EVENT_DURATION),
                    Radius = UnityEngine.Random.Range(5f, 20f)
                });
            }
        }

        private void ActivateNextEvent()
        {
            if (eventQueue.Count == 0) return;

            var worldEvent = eventQueue.Dequeue();
            activeEvents.Add(worldEvent);

            // Notify relevant entities about the event
            NotifyEntitiesOfEvent(worldEvent);
        }

        private void EndEvent(WorldEvent worldEvent)
        {
            // Clean up event effects
            switch (worldEvent.Type)
            {
                case EventType.ResourceDiscovered:
                    // Remove temporary resource markers
                    break;

                case EventType.SocialGathering:
                    // End social behavior boosts
                    foreach (var entity in entities)
                    {
                        var social = entity.GetComponent<SocialComponent>();
                        if (social != null)
                        {
                            float distance = Vector3.Distance(
                                entity.GetComponent<Position3DComponent>()?.Position ?? Vector3.zero,
                                worldEvent.Location
                            );
                            if (distance <= worldEvent.Radius)
                            {
                                // Reset any social gathering bonuses
                            }
                        }
                    }
                    break;

                case EventType.AreaClosed:
                    // Reopen the area
                    QueueEvent(new WorldEvent
                    {
                        Type = EventType.AreaOpened,
                        Location = worldEvent.Location,
                        TimeRemaining = 0f,
                        Radius = worldEvent.Radius
                    });
                    break;
            }
        }

        private void UpdateEventEffects(WorldEvent worldEvent, float deltaTime)
        {
            switch (worldEvent.Type)
            {
                case EventType.SocialGathering:
                    // Update social gathering effects
                    foreach (var entity in entities)
                    {
                        var social = entity.GetComponent<SocialComponent>();
                        var position = entity.GetComponent<Position3DComponent>();
                        if (social != null && position != null)
                        {
                            float distance = Vector3.Distance(position.Position, worldEvent.Location);
                            if (distance <= worldEvent.Radius)
                            {
                                // Boost social satisfaction for participating entities
                                social.UpdateSocialState(deltaTime * 2f); // Enhanced social benefits
                            }
                        }
                    }
                    break;

                case EventType.ResourceDiscovered:
                    // Maintain resource discovery effects
                    // This might involve spawning actual resources after a delay
                    if (worldEvent.TimeRemaining < MAX_EVENT_DURATION * 0.8f)
                    {
                        SpawnDiscoveredResource(worldEvent);
                    }
                    break;
            }
        }

        private void NotifyEntitiesOfEvent(WorldEvent worldEvent)
        {
            foreach (var entity in entities)
            {
                var memory = entity.GetComponent<MemoryComponent>();
                var position = entity.GetComponent<Position3DComponent>();

                if (memory != null && position != null)
                {
                    float distance = Vector3.Distance(position.Position, worldEvent.Location);
                    if (distance <= worldEvent.Radius * 2f) // Notification radius larger than effect radius
                    {
                        // Add event to entity's memory
                        memory.RememberLocation(
                            $"event_{worldEvent.Type}_{Time.time}",
                            worldEvent.Location,
                            1f - (distance / (worldEvent.Radius * 2f))
                        );
                    }
                }
            }
        }

        private Vector3 GenerateValidLocation()
        {
            // This should be improved to work with your actual world bounds
            return new Vector3(
                UnityEngine.Random.Range(-50f, 50f),
                0f,
                UnityEngine.Random.Range(-50f, 50f)
            );
        }

        private Vector3 CalculateGroupCenter(List<Entity> entities)
        {
            if (entities.Count == 0) return Vector3.zero;

            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (var entity in entities)
            {
                var position = entity.GetComponent<Position3DComponent>();
                if (position != null)
                {
                    sum += position.Position;
                    count++;
                }
            }

            return count > 0 ? sum / count : Vector3.zero;
        }

        private void SpawnDiscoveredResource(WorldEvent worldEvent)
        {
            // This should be handled by your resource spawning system
            // For now, just create a basic resource entity
            var resourceEntity = world.CreateEntity();
            resourceEntity.AddComponent(new Position3DComponent(worldEvent.Location));
            
            // Use the factory method for creating food sources
            resourceEntity.AddComponent(
                ResourceComponent.CreateFoodSource(
                    quantity: 100f,
                    quality: UnityEngine.Random.Range(0.5f, 1f)
                )
            );
        }

        private void QueueEvent(WorldEvent worldEvent)
        {
            eventQueue.Enqueue(worldEvent);
        }

        private class WorldEvent
        {
            public EventType Type;
            public Vector3 Location;
            public float TimeRemaining;
            public float Radius;
        }
    }
}
