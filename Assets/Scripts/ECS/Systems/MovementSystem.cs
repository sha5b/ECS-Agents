using UnityEngine;
using ECS.Core;
using ECS.Components;
using System.Collections.Generic;
using System;

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
        private const float ROTATION_THRESHOLD = 0.1f;
        private const float ROTATION_SPEED = 10f;

        // Animation thresholds
        private const float WALK_SPEED_THRESHOLD = 0.1f;
        private const float RUN_SPEED_THRESHOLD = 4f;

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
                var physical = entity.GetComponent<PhysicalComponent>();
                var body = entity.GetComponent<BodyComponent>();
                
                if (position == null || behavior == null || physical == null) continue;

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

            // Calculate rotation
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            bool isTurning = Quaternion.Angle(position.Rotation, targetRotation) > ROTATION_THRESHOLD;

            // Check if we've arrived
            if (distance <= ARRIVAL_DISTANCE)
            {
                OnDestinationReached(entity, behavior);
                return;
            }

            // Get physical component
            var physical = entity.GetComponent<PhysicalComponent>();
            var body = entity.GetComponent<BodyComponent>();
            if (physical == null) return;

            // Calculate base movement speed
            float speed = physical.MovementSpeed;
            if (behavior.CurrentState == BehaviorState.SeekingFood || 
                behavior.CurrentState == BehaviorState.SeekingWater)
            {
                speed *= RUNNING_SPEED_MULTIPLIER;
            }

            // Calculate current speed based on turning
            float currentSpeed = speed * (isTurning ? 0.5f : 1f);
            Vector3 newPosition = currentPos + direction * currentSpeed * deltaTime;

            // Smoothly rotate towards target
            Quaternion newRotation = Quaternion.Lerp(
                position.Rotation,
                targetRotation,
                ROTATION_SPEED * deltaTime
            );

            // Update movement through physics
            if (body?.ModelInstance != null)
            {
                var rb = body.ModelInstance.GetComponentInChildren<Rigidbody>();
                if (rb != null)
                {
                    // Calculate movement force
                    Vector3 moveForce = direction * currentSpeed * 10f; // Multiply by force factor
                    rb.AddForce(moveForce, ForceMode.Force);

                    // Update rotation
                    body.ModelInstance.transform.rotation = newRotation;
                    position.UpdateRotation(newRotation);
                }

                // Update position from physics in SpawnerSystem
                
                // Calculate animation parameters
                float speedRatio = currentSpeed / physical.MaxSpeed;
                float staminaRatio = physical.GetStaminaPercentage();
                
                body.UpdateAnimationState(speedRatio, isTurning, staminaRatio);

                // Set appropriate animation state
                if (speedRatio > RUN_SPEED_THRESHOLD)
                {
                    body.PlayAnimation("Run");
                }
                else if (speedRatio > WALK_SPEED_THRESHOLD)
                {
                    body.PlayAnimation("Walk");
                }
                else
                {
                    body.PlayAnimation("Idle");
                }
            }
        }

        private void HandleSocialMovement(
            Entity entity,
            Position3DComponent position,
            BehaviorComponent behavior,
            float deltaTime)
        {
            var targetEntity = behavior.TargetEntity;
            var targetPos = targetEntity.GetComponent<Position3DComponent>();
            var physical = entity.GetComponent<PhysicalComponent>();
            var body = entity.GetComponent<BodyComponent>();
            
            if (targetPos == null || physical == null) return;

            // Keep a comfortable distance for social interaction
            const float SOCIAL_DISTANCE = 2f;
            Vector3 direction = (targetPos.Position - position.Position).normalized;
            float distance = Vector3.Distance(position.Position, targetPos.Position);

            if (distance > SOCIAL_DISTANCE * 1.5f)
            {
                // Move closer through physics
                if (body?.ModelInstance != null)
                {
                    var rb = body.ModelInstance.GetComponentInChildren<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 moveForce = direction * BASE_MOVEMENT_SPEED * 10f;
                        rb.AddForce(moveForce, ForceMode.Force);
                        body.ModelInstance.transform.rotation = Quaternion.LookRotation(direction);
                        position.UpdateRotation(Quaternion.LookRotation(direction));
                    }
                }
            }
            else if (distance < SOCIAL_DISTANCE * 0.5f)
            {
                // Move away through physics
                if (body?.ModelInstance != null)
                {
                    var rb = body.ModelInstance.GetComponentInChildren<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 moveForce = -direction * BASE_MOVEMENT_SPEED * 10f;
                        rb.AddForce(moveForce, ForceMode.Force);
                        body.ModelInstance.transform.rotation = Quaternion.LookRotation(-direction);
                        position.UpdateRotation(Quaternion.LookRotation(-direction));
                    }
                }
            }
            else
            {
                // Stay in place but look at target
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                Quaternion newRotation = Quaternion.Lerp(
                    position.Rotation,
                    targetRotation,
                    ROTATION_SPEED * deltaTime
                );
                position.UpdateRotation(newRotation);

                // Update body animations
                if (body != null)
                {
                    body.UpdatePosition(position.Position, newRotation);
                    body.UpdateAnimationState(0f, false, physical.GetStaminaPercentage());
                    body.PlayAnimation("Idle");
                }
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
                UnityEngine.Random.Range(-range, range),
                0, // Assuming flat terrain for now
                UnityEngine.Random.Range(-range, range)
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
