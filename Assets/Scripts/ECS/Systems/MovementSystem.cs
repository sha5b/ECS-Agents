using UnityEngine;
using ECS.Core;
using ECS.Components;
using System.Collections.Generic;

namespace ECS.Systems
{
    public class MovementSystem : ISystem
    {
        private World world;
        private List<Entity> entities;

        // Movement settings
        private const float BASE_MOVEMENT_SPEED = 5f;
        private const float RUNNING_SPEED_MULTIPLIER = 1.5f;
        private const float ARRIVAL_DISTANCE = 0.5f;
        private const float PATH_UPDATE_INTERVAL = 0.5f;

        public MovementSystem(World world)
        {
            this.world = world;
            this.entities = world.GetEntities();
        }

        public void Update(float deltaTime)
        {
            foreach (var entity in entities)
            {
                var position = entity.GetComponent<Position3DComponent>();
                var behavior = entity.GetComponent<BehaviorComponent>();
                
                if (position == null || behavior == null) continue;

                // Handle movement based on behavior state
                switch (behavior.CurrentState)
                {
                    case BehaviorState.MovingToTarget:
                    case BehaviorState.SeekingFood:
                    case BehaviorState.SeekingWater:
                    case BehaviorState.Exploring:
                        HandleMovement(entity, position, behavior, deltaTime);
                        break;

                    case BehaviorState.Socializing:
                        if (behavior.TargetEntity != null)
                        {
                            HandleSocialMovement(entity, position, behavior, deltaTime);
                        }
                        break;
                }
            }
        }

        private void HandleMovement(
            Entity entity,
            Position3DComponent position,
            BehaviorComponent behavior,
            float deltaTime)
        {
            if (!behavior.TargetPosition.HasValue) return;

            Vector3 targetPos = behavior.TargetPosition.Value;
            Vector3 currentPos = position.Position;
            
            // Calculate direction and distance
            Vector3 direction = (targetPos - currentPos).normalized;
            float distance = Vector3.Distance(currentPos, targetPos);

            // Check if we've arrived
            if (distance <= ARRIVAL_DISTANCE)
            {
                OnDestinationReached(entity, behavior);
                return;
            }

            // Calculate movement speed
            float speed = BASE_MOVEMENT_SPEED;
            if (behavior.CurrentState == BehaviorState.SeekingFood || 
                behavior.CurrentState == BehaviorState.SeekingWater)
            {
                speed *= RUNNING_SPEED_MULTIPLIER;
            }

            // Move towards target
            Vector3 newPosition = currentPos + direction * speed * deltaTime;

            // Update position and rotation
            position.UpdatePosition(newPosition);
            position.UpdateRotation(Quaternion.LookRotation(direction));
        }

        private void HandleSocialMovement(
            Entity entity,
            Position3DComponent position,
            BehaviorComponent behavior,
            float deltaTime)
        {
            var targetEntity = behavior.TargetEntity;
            var targetPos = targetEntity.GetComponent<Position3DComponent>();
            
            if (targetPos == null) return;

            // Keep a comfortable distance for social interaction
            const float SOCIAL_DISTANCE = 2f;
            Vector3 direction = (targetPos.Position - position.Position).normalized;
            float distance = Vector3.Distance(position.Position, targetPos.Position);

            if (distance > SOCIAL_DISTANCE * 1.5f)
            {
                // Move closer
                Vector3 newPosition = position.Position + direction * BASE_MOVEMENT_SPEED * deltaTime;
                position.UpdatePosition(newPosition);
                position.UpdateRotation(Quaternion.LookRotation(direction));
            }
            else if (distance < SOCIAL_DISTANCE * 0.5f)
            {
                // Move away
                Vector3 newPosition = position.Position - direction * BASE_MOVEMENT_SPEED * deltaTime;
                position.UpdatePosition(newPosition);
                position.UpdateRotation(Quaternion.LookRotation(-direction));
            }
            else
            {
                // Stay in place but look at target
                position.UpdateRotation(Quaternion.LookRotation(direction));
            }
        }

        private void OnDestinationReached(Entity entity, BehaviorComponent behavior)
        {
            switch (behavior.CurrentState)
            {
                case BehaviorState.MovingToTarget:
                    behavior.UpdateState(BehaviorState.Idle);
                    break;

                case BehaviorState.Exploring:
                    // Choose a new random destination
                    Vector3 randomDestination = GetRandomDestination();
                    behavior.UpdateState(BehaviorState.Exploring, randomDestination);
                    break;

                case BehaviorState.SeekingFood:
                case BehaviorState.SeekingWater:
                    // Remember this location for future reference
                    string locationType = behavior.CurrentState == BehaviorState.SeekingFood ? "FoodSource" : "WaterSource";
                    behavior.RememberLocation(locationType, behavior.TargetPosition.Value);
                    behavior.UpdateState(BehaviorState.Idle);
                    break;
            }
        }

        private Vector3 GetRandomDestination()
        {
            // For now, return a random point within a reasonable range
            // This should be replaced with proper terrain-aware positioning
            float range = 50f;
            return new Vector3(
                Random.Range(-range, range),
                0, // Assuming flat terrain for now
                Random.Range(-range, range)
            );
        }

        // Helper method to check if position is valid (e.g., not inside obstacles, within bounds)
        private bool IsValidPosition(Vector3 position)
        {
            // This should be implemented based on your world's constraints
            // For now, just ensure it's within a reasonable height range
            return position.y >= 0 && position.y <= 100;
        }
    }
}
