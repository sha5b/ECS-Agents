using UnityEngine;
using ECS.Core;
using System.Collections.Generic;

namespace ECS.Components
{
    public enum BehaviorState
    {
        Idle,
        SeekingFood,
        SeekingWater,
        Resting,
        Socializing,
        Working,
        Exploring,
        MovingToTarget,
        Walking,
        Running,
        Turning
    }

    public class BehaviorComponent : IComponent
    {
        // Current state and goal
        public BehaviorState CurrentState { get; private set; }
        public Vector3? TargetPosition { get; private set; }
        public Entity TargetEntity { get; private set; }

        // Personality traits (0-1 scale)
        public float Sociability { get; private set; }
        public float Productivity { get; private set; }
        public float Curiosity { get; private set; }
        public float Resilience { get; private set; }

        // Memory of important locations
        private Dictionary<string, Vector3> knownLocations;
        // Memory of interactions with other entities
        private Dictionary<int, float> relationships; // Entity ID -> Relationship value (-1 to 1)

        // Decision-making cooldown
        private float decisionCooldown;
        private const float MIN_DECISION_INTERVAL = 1.0f;

        public BehaviorComponent(
            float sociability = 0.5f,
            float productivity = 0.5f,
            float curiosity = 0.5f,
            float resilience = 0.5f)
        {
            CurrentState = BehaviorState.Idle;
            TargetPosition = null;
            TargetEntity = null;

            // Initialize personality traits
            Sociability = Mathf.Clamp01(sociability);
            Productivity = Mathf.Clamp01(productivity);
            Curiosity = Mathf.Clamp01(curiosity);
            Resilience = Mathf.Clamp01(resilience);

            // Initialize memories
            knownLocations = new Dictionary<string, Vector3>();
            relationships = new Dictionary<int, float>();

            decisionCooldown = 0f;
        }

        public void UpdateState(BehaviorState newState, Vector3? targetPos = null, Entity targetEntity = null)
        {
            if (CurrentState != newState)
            {
                Debug.Log($"Behavior state changed from {CurrentState} to {newState}");
                CurrentState = newState;
            }

            TargetPosition = targetPos;
            TargetEntity = targetEntity;
        }

        public void RememberLocation(string key, Vector3 position)
        {
            knownLocations[key] = position;
        }

        public bool TryGetRememberedLocation(string key, out Vector3 position)
        {
            return knownLocations.TryGetValue(key, out position);
        }

        public void UpdateRelationship(Entity other, float delta)
        {
            if (!relationships.ContainsKey(other.Id))
            {
                relationships[other.Id] = 0f;
            }

            relationships[other.Id] = Mathf.Clamp(relationships[other.Id] + delta, -1f, 1f);
        }

        public float GetRelationship(Entity other)
        {
            return relationships.TryGetValue(other.Id, out float value) ? value : 0f;
        }

        public bool CanMakeNewDecision(float deltaTime)
        {
            decisionCooldown -= deltaTime;
            if (decisionCooldown <= 0)
            {
                decisionCooldown = MIN_DECISION_INTERVAL;
                return true;
            }
            return false;
        }

        public void ForgetLocation(string key)
        {
            knownLocations.Remove(key);
        }

        public Dictionary<string, Vector3> GetAllKnownLocations()
        {
            return new Dictionary<string, Vector3>(knownLocations);
        }

        public List<Entity> GetFriendlyEntities(float threshold = 0.5f)
        {
            var friendly = new List<Entity>();
            foreach (var relation in relationships)
            {
                if (relation.Value >= threshold)
                {
                    // Note: This requires a way to get Entity by ID from the World
                    // Will be implemented when we create the EntityManager
                    // friendly.Add(World.GetEntityById(relation.Key));
                }
            }
            return friendly;
        }

        // Personality-based decision modifiers
        public float GetSocialPreference()
        {
            return Mathf.Lerp(0.3f, 1f, Sociability);
        }

        public float GetExplorationPreference()
        {
            return Mathf.Lerp(0.2f, 0.8f, Curiosity);
        }

        public float GetWorkPreference()
        {
            return Mathf.Lerp(0.4f, 1f, Productivity);
        }

        public float GetNeedUrgencyModifier()
        {
            return Mathf.Lerp(1.2f, 0.8f, Resilience);
        }
    }
}
