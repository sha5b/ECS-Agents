using System.Collections.Generic;
using UnityEngine;

namespace ECS.Core
{
    public class World : MonoBehaviour
    {
        private List<Entity> entities;
        private List<ISystem> systems;
        private int nextEntityId = 0;

        private void Awake()
        {
            entities = new List<Entity>();
            systems = new List<ISystem>();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            foreach (var system in systems)
            {
                system.Update(deltaTime);
            }
        }

        public Entity CreateEntity()
        {
            var entity = new Entity(nextEntityId++);
            entities.Add(entity);
            return entity;
        }

        public void DestroyEntity(Entity entity)
        {
            entities.Remove(entity);
        }

        public void AddSystem(ISystem system)
        {
            systems.Add(system);
        }

        public List<Entity> GetEntities()
        {
            return entities;
        }
    }
}
