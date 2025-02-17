using System;
using System.Collections.Generic;
using UnityEngine;

namespace ECS.Core
{
    public class Entity
    {
        public int Id { get; private set; }
        private Dictionary<Type, IComponent> components;

        public Entity(int id)
        {
            Id = id;
            components = new Dictionary<Type, IComponent>();
        }

        public void AddComponent(IComponent component)
        {
            var type = component.GetType();
            if (!components.ContainsKey(type))
            {
                components[type] = component;
            }
        }

        public T GetComponent<T>() where T : class, IComponent
        {
            var type = typeof(T);
            if (components.ContainsKey(type))
            {
                return components[type] as T;
            }
            return null;
        }

        public bool HasComponent<T>() where T : IComponent
        {
            return components.ContainsKey(typeof(T));
        }

        public void RemoveComponent<T>() where T : IComponent
        {
            var type = typeof(T);
            if (components.ContainsKey(type))
            {
                components.Remove(type);
            }
        }
    }
}
