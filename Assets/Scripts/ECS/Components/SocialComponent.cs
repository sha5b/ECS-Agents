using UnityEngine;
using ECS.Core;
using System.Collections.Generic;

namespace ECS.Components
{
    public enum RelationType
    {
        Stranger,
        Acquaintance,
        Friend,
        Close
    }

    public class SocialComponent : IComponent
    {
        // Relationship data
        private Dictionary<int, Relationship> relationships;
        
        // Social traits
        public float Extroversion { get; private set; }
        public float Agreeableness { get; private set; }
        public float Trustworthiness { get; private set; }

        // Social stats
        public float SocialEnergy { get; private set; }
        public float SocialSatisfaction { get; private set; }
        public int DailyInteractions { get; private set; }

        // Constants
        private const float MAX_SOCIAL_ENERGY = 100f;
        private const float ENERGY_RECOVERY_RATE = 5f;
        private const int MAX_DAILY_INTERACTIONS = 10;
        private const float RELATIONSHIP_DECAY_RATE = 0.1f;

        public SocialComponent(
            float extroversion = 0.5f,
            float agreeableness = 0.5f,
            float trustworthiness = 0.5f)
        {
            relationships = new Dictionary<int, Relationship>();
            
            Extroversion = Mathf.Clamp01(extroversion);
            Agreeableness = Mathf.Clamp01(agreeableness);
            Trustworthiness = Mathf.Clamp01(trustworthiness);
            
            SocialEnergy = MAX_SOCIAL_ENERGY;
            SocialSatisfaction = 0.5f;
            DailyInteractions = 0;
        }

        public void UpdateSocialState(float deltaTime)
        {
            // Recover social energy over time
            if (SocialEnergy < MAX_SOCIAL_ENERGY)
            {
                SocialEnergy = Mathf.Min(MAX_SOCIAL_ENERGY, 
                    SocialEnergy + (ENERGY_RECOVERY_RATE * deltaTime));
            }

            // Decay relationships over time
            foreach (var relationship in relationships.Values)
            {
                relationship.Strength *= (1f - RELATIONSHIP_DECAY_RATE * deltaTime);
                if (relationship.Strength < 0.2f)
                {
                    relationship.Type = RelationType.Stranger;
                }
            }
        }

        public bool CanInteract()
        {
            return SocialEnergy > 20f && DailyInteractions < MAX_DAILY_INTERACTIONS;
        }

        public void Interact(Entity other, float duration, float quality)
        {
            if (!CanInteract()) return;

            // Update social energy
            float energyCost = duration * (1f - Extroversion * 0.5f);
            SocialEnergy = Mathf.Max(0, SocialEnergy - energyCost);

            // Update relationship
            if (!relationships.ContainsKey(other.Id))
            {
                relationships[other.Id] = new Relationship
                {
                    Type = RelationType.Stranger,
                    Strength = 0f,
                    LastInteractionTime = Time.time
                };
            }

            var relationship = relationships[other.Id];
            
            // Calculate interaction impact
            float impact = quality * duration * Agreeableness;
            relationship.Strength = Mathf.Clamp01(relationship.Strength + impact);
            relationship.LastInteractionTime = Time.time;

            // Update relationship type based on strength
            relationship.Type = relationship.Strength switch
            {
                float s when s >= 0.8f => RelationType.Close,
                float s when s >= 0.5f => RelationType.Friend,
                float s when s >= 0.2f => RelationType.Acquaintance,
                _ => RelationType.Stranger
            };

            // Update social satisfaction
            float satisfactionChange = quality * 0.2f;
            SocialSatisfaction = Mathf.Clamp01(SocialSatisfaction + satisfactionChange);

            DailyInteractions++;
        }

        public void ResetDaily()
        {
            DailyInteractions = 0;
        }

        public RelationType GetRelationType(Entity other)
        {
            return relationships.TryGetValue(other.Id, out var relationship) 
                ? relationship.Type 
                : RelationType.Stranger;
        }

        public float GetRelationshipStrength(Entity other)
        {
            return relationships.TryGetValue(other.Id, out var relationship) 
                ? relationship.Strength 
                : 0f;
        }

        public List<Entity> GetRelationships(RelationType minType)
        {
            var result = new List<Entity>();
            foreach (var kvp in relationships)
            {
                if (kvp.Value.Type >= minType)
                {
                    // Note: This requires a way to get Entity by ID from the World
                    // Will be implemented when we create the EntityManager
                    // result.Add(World.GetEntityById(kvp.Key));
                }
            }
            return result;
        }

        private class Relationship
        {
            public RelationType Type;
            public float Strength;
            public float LastInteractionTime;
        }
    }
}
