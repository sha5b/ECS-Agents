using UnityEngine;
using ECS.Core;

namespace ECS.Components
{
    public class Position3DComponent : IComponent
    {
        public Vector3 Position { get; private set; }
        public Vector3 Scale { get; private set; }
        public Quaternion Rotation { get; private set; }

        public Position3DComponent(Vector3 position, Vector3 scale = default, Quaternion rotation = default)
        {
            Position = position;
            Scale = scale == default ? Vector3.one : scale;
            Rotation = rotation == default ? Quaternion.identity : rotation;
        }

        public void UpdatePosition(Vector3 newPosition)
        {
            Position = newPosition;
        }

        public void UpdateScale(Vector3 newScale)
        {
            Scale = newScale;
        }

        public void UpdateRotation(Quaternion newRotation)
        {
            Rotation = newRotation;
        }
    }
}
