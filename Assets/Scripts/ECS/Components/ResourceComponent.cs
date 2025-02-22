using UnityEngine;
using ECS.Core;
using System.Collections.Generic;

namespace ECS.Components
{
    public enum ResourceType
    {
        Food,
        Water,
        Wood,
        Stone,
        Metal,
        RestSpot
    }

    public class ResourceComponent : IComponent
    {
        // Resource properties
        public ResourceType Type { get; private set; }
        public float Quantity { get; private set; }
        public float MaxQuantity { get; private set; }
        public float ReplenishRate { get; private set; }
        public float Quality { get; private set; } // 0-1 scale, affects satisfaction when used
        public float InteractionRadius { get; private set; }
        public bool IsInfinite { get; private set; }
        public bool IsDepletable { get; private set; }

        // Resource availability
        public bool IsAvailable => IsInfinite || Quantity > 0;
        public float AvailabilityPercentage => IsInfinite ? 1f : Mathf.Clamp01(Quantity / MaxQuantity);

        // Usage tracking
        private Dictionary<int, float> lastUsageTime; // Entity ID -> Last usage timestamp
        private const float USAGE_COOLDOWN = 1f; // Minimum time between uses

        public ResourceComponent(
            ResourceType type,
            float maxQuantity = 100f,
            float initialQuantity = 100f,
            float replenishRate = 1f,
            float quality = 1f,
            float interactionRadius = 2f,
            bool isInfinite = false,
            bool isDepletable = true)
        {
            Type = type;
            MaxQuantity = maxQuantity;
            Quantity = initialQuantity;
            ReplenishRate = replenishRate;
            Quality = Mathf.Clamp01(quality);
            InteractionRadius = interactionRadius;
            IsInfinite = isInfinite;
            IsDepletable = isDepletable;

            lastUsageTime = new Dictionary<int, float>();
        }

        public void UpdateResource(float deltaTime)
        {
            if (!IsInfinite && IsDepletable && Quantity < MaxQuantity)
            {
                Quantity = Mathf.Min(MaxQuantity, Quantity + (ReplenishRate * deltaTime));
            }
        }

        public bool CanBeUsedBy(Entity entity)
        {
            if (!IsAvailable) return false;

            // Check usage cooldown
            if (lastUsageTime.TryGetValue(entity.Id, out float lastTime))
            {
                if (Time.time - lastTime < USAGE_COOLDOWN)
                {
                    return false;
                }
            }

            return true;
        }

        public float Use(Entity entity, float amount)
        {
            if (!CanBeUsedBy(entity))
            {
                return 0f;
            }

            float actualAmount;
            if (IsInfinite)
            {
                actualAmount = amount;
            }
            else
            {
                actualAmount = Mathf.Min(amount, Quantity);
                if (IsDepletable)
                {
                    Quantity -= actualAmount;
                }
            }

            // Record usage time
            lastUsageTime[entity.Id] = Time.time;

            // Apply quality modifier
            return actualAmount * Quality;
        }

        public float GetSatisfactionValue(float amount)
        {
            // Calculate how much satisfaction this resource provides
            // Based on quality and quantity available
            float availabilityModifier = IsInfinite ? 1f : AvailabilityPercentage;
            return amount * Quality * availabilityModifier;
        }

        public void Replenish(float amount)
        {
            if (!IsInfinite && IsDepletable)
            {
                Quantity = Mathf.Min(MaxQuantity, Quantity + amount);
            }
        }

        public void SetQuality(float newQuality)
        {
            Quality = Mathf.Clamp01(newQuality);
        }

        public void SetInteractionRadius(float newRadius)
        {
            InteractionRadius = Mathf.Max(0.1f, newRadius);
        }

        public bool IsWithinRange(Vector3 position, Vector3 targetPosition)
        {
            return Vector3.Distance(position, targetPosition) <= InteractionRadius;
        }

        // Factory methods for common resource types
        public static ResourceComponent CreateFoodSource(float quantity = 100f, float quality = 1f)
        {
            return new ResourceComponent(
                ResourceType.Food,
                maxQuantity: quantity,
                initialQuantity: quantity,
                replenishRate: 5f,
                quality: quality,
                interactionRadius: 2f,
                isInfinite: false,
                isDepletable: true
            );
        }

        public static ResourceComponent CreateWaterSource(bool infinite = true)
        {
            return new ResourceComponent(
                ResourceType.Water,
                maxQuantity: 1000f,
                initialQuantity: 1000f,
                replenishRate: 10f,
                quality: 1f,
                interactionRadius: 3f,
                isInfinite: infinite,
                isDepletable: !infinite
            );
        }

        public static ResourceComponent CreateRestSpot()
        {
            return new ResourceComponent(
                ResourceType.RestSpot,
                maxQuantity: 1f,
                initialQuantity: 1f,
                replenishRate: 0f,
                quality: 1f,
                interactionRadius: 2f,
                isInfinite: true,
                isDepletable: false
            );
        }
    }
}
