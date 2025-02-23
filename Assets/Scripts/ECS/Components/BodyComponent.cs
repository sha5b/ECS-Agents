using UnityEngine;
using System.Collections.Generic;
using ECS.Core;

namespace ECS.Components
{
    public class BodyComponent : IComponent
    {
        // Core references
        public GameObject ModelPrefab { get; private set; }
        public GameObject ModelInstance { get; private set; }
        public Animator Animator { get; private set; }

        // Animation parameters
        public float AnimationBlendSpeed { get; private set; } = 0.2f;
        public Dictionary<string, float> AnimationWeights { get; private set; }

        // Animation parameter names
        private const string SPEED_PARAM = "Speed";
        private const string TURNING_PARAM = "IsTurning";
        private const string STAMINA_PARAM = "StaminaLevel";

        public BodyComponent(GameObject prefab)
        {
            ModelPrefab = prefab;
            AnimationWeights = new Dictionary<string, float>();
            InitializeModel();
        }

        private void InitializeModel()
        {
            if (ModelPrefab != null)
            {
                ModelInstance = GameObject.Instantiate(ModelPrefab);
                Animator = ModelInstance.GetComponent<Animator>();
                
                if (Animator == null)
                {
                    Debug.LogWarning("No Animator component found on model prefab");
                }
            }
        }

        public void UpdatePosition(Vector3 position, Quaternion rotation)
        {
            if (ModelInstance != null)
            {
                ModelInstance.transform.position = position;
                ModelInstance.transform.rotation = rotation;
            }
        }

        public void UpdateAnimationState(float speed, bool isTurning, float staminaLevel)
        {
            if (Animator != null)
            {
                // Update animation parameters
                Animator.SetFloat(SPEED_PARAM, speed, AnimationBlendSpeed, Time.deltaTime);
                Animator.SetBool(TURNING_PARAM, isTurning);
                Animator.SetFloat(STAMINA_PARAM, staminaLevel);
            }
        }

        public void SetAnimationWeight(string stateName, float weight)
        {
            AnimationWeights[stateName] = Mathf.Clamp01(weight);
            if (Animator != null)
            {
                Animator.SetLayerWeight(Animator.GetLayerIndex(stateName), AnimationWeights[stateName]);
            }
        }

        public void PlayAnimation(string animationName, float crossFadeTime = 0.25f)
        {
            if (Animator != null)
            {
                Animator.CrossFade(animationName, crossFadeTime);
            }
        }

        public void Cleanup()
        {
            if (ModelInstance != null)
            {
                GameObject.Destroy(ModelInstance);
            }
        }
    }
}
