using UnityEngine;
using ECS.Core;

namespace ECS.Components
{
    public class NeedComponent : IComponent
    {
        // Basic needs (0-100 scale)
        public float Hunger { get; private set; }
        public float Thirst { get; private set; }
        public float Energy { get; private set; }
        public float Social { get; private set; }

        // Need decay rates (units per second)
        public float HungerDecayRate { get; private set; }
        public float ThirstDecayRate { get; private set; }
        public float EnergyDecayRate { get; private set; }
        public float SocialDecayRate { get; private set; }

        // Thresholds for urgent needs
        public const float CRITICAL_THRESHOLD = 20f;
        public const float URGENT_THRESHOLD = 40f;
        public const float SATISFIED_THRESHOLD = 80f;

        public NeedComponent(
            float hungerDecayRate = 1f,
            float thirstDecayRate = 1.5f,
            float energyDecayRate = 0.8f,
            float socialDecayRate = 0.5f)
        {
            // Initialize needs at maximum
            Hunger = 100f;
            Thirst = 100f;
            Energy = 100f;
            Social = 100f;

            // Set decay rates
            HungerDecayRate = hungerDecayRate;
            ThirstDecayRate = thirstDecayRate;
            EnergyDecayRate = energyDecayRate;
            SocialDecayRate = socialDecayRate;
        }

        public void UpdateNeeds(float deltaTime)
        {
            Hunger = Mathf.Max(0, Hunger - (HungerDecayRate * deltaTime));
            Thirst = Mathf.Max(0, Thirst - (ThirstDecayRate * deltaTime));
            Energy = Mathf.Max(0, Energy - (EnergyDecayRate * deltaTime));
            Social = Mathf.Max(0, Social - (SocialDecayRate * deltaTime));
        }

        public void ModifyNeed(NeedType need, float amount)
        {
            switch (need)
            {
                case NeedType.Hunger:
                    Hunger = Mathf.Clamp(Hunger + amount, 0f, 100f);
                    break;
                case NeedType.Thirst:
                    Thirst = Mathf.Clamp(Thirst + amount, 0f, 100f);
                    break;
                case NeedType.Energy:
                    Energy = Mathf.Clamp(Energy + amount, 0f, 100f);
                    break;
                case NeedType.Social:
                    Social = Mathf.Clamp(Social + amount, 0f, 100f);
                    break;
            }
        }

        public bool IsNeedCritical(NeedType need)
        {
            return GetNeedValue(need) <= CRITICAL_THRESHOLD;
        }

        public bool IsNeedUrgent(NeedType need)
        {
            return GetNeedValue(need) <= URGENT_THRESHOLD;
        }

        public bool IsNeedSatisfied(NeedType need)
        {
            return GetNeedValue(need) >= SATISFIED_THRESHOLD;
        }

        public float GetNeedValue(NeedType need)
        {
            switch (need)
            {
                case NeedType.Hunger:
                    return Hunger;
                case NeedType.Thirst:
                    return Thirst;
                case NeedType.Energy:
                    return Energy;
                case NeedType.Social:
                    return Social;
                default:
                    return 0f;
            }
        }

        public NeedType GetMostUrgentNeed()
        {
            float lowestValue = float.MaxValue;
            NeedType mostUrgent = NeedType.None;

            void CheckNeed(NeedType need, float value)
            {
                if (value < lowestValue)
                {
                    lowestValue = value;
                    mostUrgent = need;
                }
            }

            CheckNeed(NeedType.Hunger, Hunger);
            CheckNeed(NeedType.Thirst, Thirst);
            CheckNeed(NeedType.Energy, Energy);
            CheckNeed(NeedType.Social, Social);

            return mostUrgent;
        }
    }

    public enum NeedType
    {
        None,
        Hunger,
        Thirst,
        Energy,
        Social
    }
}
