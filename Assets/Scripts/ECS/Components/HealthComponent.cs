using UnityEngine;
using ECS.Core;

namespace ECS.Components
{
    public class HealthComponent : IComponent
    {
        public float MaxHealth { get; private set; }
        public float CurrentHealth { get; private set; }

        public HealthComponent(float maxHealth)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        }

        public void Heal(float amount)
        {
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        }

        public bool IsDead()
        {
            return CurrentHealth <= 0;
        }
    }
}
