using UnityEngine;
using ECS.Core;

namespace ECS.Components
{
    public class PhysicalEntityComponent : IComponent
    {
        public GameObject GameObject { get; private set; }
        public Rigidbody Rigidbody { get; private set; }
        public Vector3 SpawnPosition { get; private set; }
        public Material Material { get; private set; }

        public PhysicalEntityComponent(Vector3 spawnPosition)
        {
            SpawnPosition = spawnPosition;
            CreatePhysicalRepresentation();
        }

        private void CreatePhysicalRepresentation()
        {
            // Create game object
            GameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.name = "Entity Box";
            GameObject.transform.position = SpawnPosition;
            GameObject.transform.localScale = new Vector3(2f, 2f, 2f);

            // Add rigidbody for physics
            Rigidbody = GameObject.AddComponent<Rigidbody>();
            Rigidbody.mass = 1f;
            Rigidbody.linearDamping = 0.5f;
            Rigidbody.angularDamping = 0.5f;

            // Create and apply material
            Material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Material.name = "Entity Material";
            Material.SetColor("_BaseColor", Random.ColorHSV(0f, 1f, 0.5f, 0.7f, 0.7f, 1f));
            GameObject.GetComponent<Renderer>().material = Material;
        }

        public void UpdateMaterial(float temperature, float weatherEffect)
        {
            if (Material == null) return;

            // Get the base color
            Color baseColor = Material.GetColor("_BaseColor");

            // Darken color in cold temperatures
            if (temperature < 0)
            {
                baseColor *= Mathf.Lerp(1f, 0.7f, Mathf.Abs(temperature) / 20f);
            }

            // Add frost effect in extreme cold
            if (temperature < -10)
            {
                baseColor = Color.Lerp(baseColor, Color.white, Mathf.Abs(temperature + 10) / 20f);
                Material.SetFloat("_Smoothness", Mathf.Lerp(0.3f, 0.8f, Mathf.Abs(temperature + 10) / 20f));
            }

            // Apply weather effects
            Material.SetColor("_BaseColor", baseColor);
        }

        public void Cleanup()
        {
            if (GameObject != null)
            {
                GameObject.Destroy(GameObject);
            }
            if (Material != null)
            {
                GameObject.Destroy(Material);
            }
        }
    }
}
