using UnityEngine;
using ECS.Core;
using ECS.Components;
using System.Collections.Generic;

namespace ECS.Systems
{
    public class NavigationSystem : ISystem
    {
        private World world;
        private List<Entity> entities;
        private const float UPDATE_INTERVAL = 0.5f;
        private float nextUpdateTime;

        // Navigation settings
        private const float OBSTACLE_DETECTION_RADIUS = 2f;
        private const float PATH_NODE_SPACING = 5f;
        private const float ARRIVAL_DISTANCE = 1f;

        public NavigationSystem(World world)
        {
            this.world = world;
            this.entities = world.GetEntities();
            this.nextUpdateTime = Time.time + UPDATE_INTERVAL;
        }

        public void Update(float deltaTime)
        {
            if (Time.time < nextUpdateTime) return;
            nextUpdateTime = Time.time + UPDATE_INTERVAL;

            foreach (var entity in entities)
            {
                var position = entity.GetComponent<Position3DComponent>();
                var behavior = entity.GetComponent<BehaviorComponent>();

                if (position == null || behavior == null) continue;

                // Only update navigation for entities with a target
                if (behavior.TargetPosition.HasValue)
                {
                    UpdateEntityNavigation(entity, position, behavior);
                }
            }
        }

        private void UpdateEntityNavigation(
            Entity entity,
            Position3DComponent position,
            BehaviorComponent behavior)
        {
            Vector3 currentPos = position.Position;
            Vector3 targetPos = behavior.TargetPosition.Value;

            // Check if we've arrived at the target
            if (Vector3.Distance(currentPos, targetPos) <= ARRIVAL_DISTANCE)
            {
                OnDestinationReached(entity, behavior);
                return;
            }

            // Check for obstacles
            if (DetectObstacles(currentPos, targetPos, out Vector3 avoidanceDirection))
            {
                // Adjust path to avoid obstacles
                Vector3 newDirection = CalculateAvoidancePath(
                    currentPos,
                    targetPos,
                    avoidanceDirection
                );
                
                // Update movement direction
                position.UpdateRotation(Quaternion.LookRotation(newDirection));
            }
            else
            {
                // Direct path to target
                Vector3 direction = (targetPos - currentPos).normalized;
                position.UpdateRotation(Quaternion.LookRotation(direction));
            }
        }

        private bool DetectObstacles(
            Vector3 currentPos,
            Vector3 targetPos,
            out Vector3 avoidanceDirection)
        {
            avoidanceDirection = Vector3.zero;
            Vector3 moveDirection = (targetPos - currentPos).normalized;

            // Check for other entities that might be obstacles
            foreach (var other in entities)
            {
                if (other.GetComponent<Position3DComponent>() is Position3DComponent otherPos)
                {
                    Vector3 toOther = otherPos.Position - currentPos;
                    float distance = toOther.magnitude;

                    // Skip if too far
                    if (distance > OBSTACLE_DETECTION_RADIUS) continue;

                    // Check if the entity is in our path
                    float dot = Vector3.Dot(moveDirection, toOther.normalized);
                    if (dot > 0.5f) // Entity is roughly in front of us
                    {
                        // Calculate avoidance vector
                        Vector3 avoidance = Vector3.Cross(Vector3.up, toOther).normalized;
                        float weight = 1f - (distance / OBSTACLE_DETECTION_RADIUS);
                        avoidanceDirection += avoidance * weight;
                    }
                }
            }

            return avoidanceDirection.sqrMagnitude > 0.01f;
        }

        private Vector3 CalculateAvoidancePath(
            Vector3 currentPos,
            Vector3 targetPos,
            Vector3 avoidanceDirection)
        {
            Vector3 desiredDirection = (targetPos - currentPos).normalized;
            
            // Blend between desired direction and avoidance
            float avoidanceStrength = Mathf.Clamp01(avoidanceDirection.magnitude);
            Vector3 blendedDirection = Vector3.Lerp(
                desiredDirection,
                avoidanceDirection.normalized,
                avoidanceStrength * 0.7f // Adjust influence of avoidance
            );

            return blendedDirection.normalized;
        }

        private void OnDestinationReached(Entity entity, BehaviorComponent behavior)
        {
            // If we were moving to a target, switch to idle
            if (behavior.CurrentState == BehaviorState.MovingToTarget)
            {
                behavior.UpdateState(BehaviorState.Idle);
            }
        }

        public bool IsPathClear(Vector3 start, Vector3 end)
        {
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);
            
            // Check points along the path
            for (float d = 0; d < distance; d += PATH_NODE_SPACING)
            {
                Vector3 point = start + direction * d;
                if (IsPointBlocked(point))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsPointBlocked(Vector3 point)
        {
            foreach (var entity in entities)
            {
                if (entity.GetComponent<Position3DComponent>() is Position3DComponent pos)
                {
                    float distance = Vector3.Distance(point, pos.Position);
                    if (distance < OBSTACLE_DETECTION_RADIUS)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Vector3 FindNearestAccessiblePoint(Vector3 target, Vector3 from)
        {
            if (IsPathClear(from, target))
            {
                return target;
            }

            // Search in expanding circles
            float searchRadius = PATH_NODE_SPACING;
            const float MAX_SEARCH_RADIUS = 50f;
            const int POINTS_PER_RING = 8;

            while (searchRadius < MAX_SEARCH_RADIUS)
            {
                for (int i = 0; i < POINTS_PER_RING; i++)
                {
                    float angle = (i / (float)POINTS_PER_RING) * Mathf.PI * 2f;
                    Vector3 offset = new Vector3(
                        Mathf.Cos(angle) * searchRadius,
                        0f,
                        Mathf.Sin(angle) * searchRadius
                    );
                    Vector3 testPoint = target + offset;

                    if (IsPathClear(from, testPoint))
                    {
                        return testPoint;
                    }
                }
                searchRadius += PATH_NODE_SPACING;
            }

            return target; // Fall back to original target if no clear path found
        }
    }
}
