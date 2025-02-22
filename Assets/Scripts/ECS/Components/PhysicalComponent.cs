using UnityEngine;
using ECS.Core;

namespace ECS.Components
{
    public class PhysicalComponent : IComponent
    {
        // Physical attributes
        public float Size { get; private set; }
        public float Mass { get; private set; }
        public float MaxSpeed { get; private set; }
        public float Strength { get; private set; }
        public float Stamina { get; private set; }

        // Current state
        public float CurrentStamina { get; private set; }
        public float MovementSpeed { get; private set; }
        public bool IsExhausted => CurrentStamina <= 0;

        // Recovery rates
        private const float STAMINA_RECOVERY_RATE = 5f;
        private const float STAMINA_DEPLETION_MULTIPLIER = 2f;

        public PhysicalComponent(
            float size = 1f,
            float mass = 70f,
            float maxSpeed = 5f,
            float strength = 1f,
            float stamina = 100f)
        {
            Size = Mathf.Max(0.1f, size);
            Mass = Mathf.Max(1f, mass);
            MaxSpeed = Mathf.Max(0.1f, maxSpeed);
            Strength = Mathf.Max(0.1f, strength);
            Stamina = Mathf.Max(10f, stamina);

            CurrentStamina = Stamina;
            MovementSpeed = MaxSpeed;
        }

        public void UpdatePhysicalState(float deltaTime, float activityIntensity = 0f)
        {
            // Update stamina based on activity
            if (activityIntensity > 0)
            {
                // Deplete stamina based on activity intensity
                float staminaCost = activityIntensity * STAMINA_DEPLETION_MULTIPLIER * deltaTime;
                CurrentStamina = Mathf.Max(0f, CurrentStamina - staminaCost);

                // Adjust movement speed based on stamina
                float staminaRatio = CurrentStamina / Stamina;
                MovementSpeed = Mathf.Lerp(MaxSpeed * 0.5f, MaxSpeed, staminaRatio);
            }
            else
            {
                // Recover stamina when not active
                CurrentStamina = Mathf.Min(
                    Stamina,
                    CurrentStamina + (STAMINA_RECOVERY_RATE * deltaTime)
                );

                // Restore movement speed
                MovementSpeed = Mathf.Lerp(MovementSpeed, MaxSpeed, deltaTime);
            }
        }

        public bool CanPerformAction(float staminaCost)
        {
            return CurrentStamina >= staminaCost;
        }

        public void ConsumeStamina(float amount)
        {
            CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
        }

        public float GetCarryingCapacity()
        {
            return Mass * Strength;
        }

        public float GetEffectiveStrength()
        {
            // Strength is reduced when stamina is low
            return Strength * (CurrentStamina / Stamina);
        }

        public void ModifyAttribute(string attribute, float amount)
        {
            switch (attribute.ToLower())
            {
                case "size":
                    Size = Mathf.Max(0.1f, Size + amount);
                    break;
                case "mass":
                    Mass = Mathf.Max(1f, Mass + amount);
                    break;
                case "maxspeed":
                    MaxSpeed = Mathf.Max(0.1f, MaxSpeed + amount);
                    break;
                case "strength":
                    Strength = Mathf.Max(0.1f, Strength + amount);
                    break;
                case "stamina":
                    Stamina = Mathf.Max(10f, Stamina + amount);
                    CurrentStamina = Mathf.Min(CurrentStamina, Stamina);
                    break;
            }
        }

        public void RestoreStamina(float amount)
        {
            CurrentStamina = Mathf.Min(Stamina, CurrentStamina + amount);
        }

        public float GetStaminaPercentage()
        {
            return CurrentStamina / Stamina;
        }
    }
}
