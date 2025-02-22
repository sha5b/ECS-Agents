using UnityEngine;
using ECS.Core;
using ECS.Components;
using System.Collections.Generic;

namespace ECS.Systems
{
    public class SocialSystem : ISystem
    {
        private World world;
        private List<Entity> entities;
        private float timeOfDay;
        private const float INTERACTION_CHECK_INTERVAL = 2f;
        private float nextInteractionCheck;

        public SocialSystem(World world)
        {
            this.world = world;
            this.entities = world.GetEntities();
            this.nextInteractionCheck = Time.time + INTERACTION_CHECK_INTERVAL;
        }

        public void Update(float deltaTime)
        {
            // Update social states
            foreach (var entity in entities)
            {
                var social = entity.GetComponent<SocialComponent>();
                if (social != null)
                {
                    social.UpdateSocialState(deltaTime);
                }
            }

            // Check for potential interactions
            if (Time.time >= nextInteractionCheck)
            {
                CheckForInteractions();
                nextInteractionCheck = Time.time + INTERACTION_CHECK_INTERVAL;
            }
        }

        private void CheckForInteractions()
        {
            // Find entities that can interact
            var potentialInteractors = new List<(Entity entity, Position3DComponent pos, SocialComponent social)>();
            
            foreach (var entity in entities)
            {
                var social = entity.GetComponent<SocialComponent>();
                var pos = entity.GetComponent<Position3DComponent>();
                var behavior = entity.GetComponent<BehaviorComponent>();

                if (social != null && pos != null && behavior != null &&
                    social.CanInteract() && 
                    behavior.CurrentState == BehaviorState.Socializing)
                {
                    potentialInteractors.Add((entity, pos, social));
                }
            }

            // Check for possible interactions between entities
            for (int i = 0; i < potentialInteractors.Count; i++)
            {
                var (entity1, pos1, social1) = potentialInteractors[i];

                for (int j = i + 1; j < potentialInteractors.Count; j++)
                {
                    var (entity2, pos2, social2) = potentialInteractors[j];

                    // Check if they're close enough to interact
                    float distance = Vector3.Distance(pos1.Position, pos2.Position);
                    if (distance <= 3f) // Interaction range
                    {
                        InitiateInteraction(entity1, entity2);
                    }
                }
            }
        }

        private void InitiateInteraction(Entity entity1, Entity entity2)
        {
            var social1 = entity1.GetComponent<SocialComponent>();
            var social2 = entity2.GetComponent<SocialComponent>();
            var memory1 = entity1.GetComponent<MemoryComponent>();
            var memory2 = entity2.GetComponent<MemoryComponent>();

            if (social1 == null || social2 == null) return;

            // Calculate interaction quality based on compatibility
            float compatibility = CalculateCompatibility(social1, social2);
            float duration = Random.Range(0.5f, 2f);

            // Perform interaction
            social1.Interact(entity2, duration, compatibility);
            social2.Interact(entity1, duration, compatibility);

            // Update memories
            if (memory1 != null)
            {
                float impression = CalculateImpression(social1, social2, compatibility);
                memory1.RememberEntity(entity2, impression);
            }

            if (memory2 != null)
            {
                float impression = CalculateImpression(social2, social1, compatibility);
                memory2.RememberEntity(entity1, impression);
            }

            Debug.Log($"Interaction between Entity {entity1.Id} and Entity {entity2.Id} (Quality: {compatibility:F2})");
        }

        private float CalculateCompatibility(SocialComponent social1, SocialComponent social2)
        {
            // Base compatibility on personality traits
            float extroversionDiff = Mathf.Abs(social1.Extroversion - social2.Extroversion);
            float agreeablenessDiff = Mathf.Abs(social1.Agreeableness - social2.Agreeableness);
            float trustDiff = Mathf.Abs(social1.Trustworthiness - social2.Trustworthiness);

            // Extroverts get along better with other extroverts
            float extroversionBonus = Mathf.Min(social1.Extroversion, social2.Extroversion);

            // Agreeable people get along better with everyone
            float agreeablenessBonus = (social1.Agreeableness + social2.Agreeableness) / 2f;

            // Calculate final compatibility
            float traitCompatibility = 1f - ((extroversionDiff + agreeablenessDiff + trustDiff) / 3f);
            float bonuses = (extroversionBonus + agreeablenessBonus) / 2f;

            return Mathf.Clamp01(traitCompatibility * 0.7f + bonuses * 0.3f);
        }

        private float CalculateImpression(
            SocialComponent observer,
            SocialComponent target,
            float interactionQuality)
        {
            // Base impression on interaction quality
            float baseImpression = interactionQuality;

            // Modify based on observer's traits
            float agreeablenessModifier = Mathf.Lerp(0.8f, 1.2f, observer.Agreeableness);
            float trustModifier = Mathf.Lerp(0.8f, 1.2f, observer.Trustworthiness);

            // Calculate final impression
            float impression = baseImpression * agreeablenessModifier * trustModifier;

            // Add some randomness
            impression += Random.Range(-0.1f, 0.1f);

            return Mathf.Clamp(impression, -1f, 1f);
        }

        public void ResetDailyInteractions()
        {
            foreach (var entity in entities)
            {
                var social = entity.GetComponent<SocialComponent>();
                if (social != null)
                {
                    social.ResetDaily();
                }
            }
        }
    }
}
