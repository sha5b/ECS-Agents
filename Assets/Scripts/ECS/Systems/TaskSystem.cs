using UnityEngine;
using ECS.Core;
using ECS.Components;
using System.Collections.Generic;

namespace ECS.Systems
{
    public class TaskSystem : ISystem
    {
        private World world;
        private List<Entity> entities;
        private Dictionary<string, TaskDefinition> taskDefinitions;

        public TaskSystem(World world)
        {
            this.world = world;
            this.entities = world.GetEntities();
            InitializeTaskDefinitions();
        }

        private void InitializeTaskDefinitions()
        {
            taskDefinitions = new Dictionary<string, TaskDefinition>
            {
                // Resource gathering tasks
                ["gather_food"] = new TaskDefinition
                {
                    BaseDuration = 10f,
                    BaseProgress = 0.1f,
                    RequiredState = BehaviorState.SeekingFood,
                    ModifierComponent = typeof(ResourceComponent)
                },
                
                ["gather_water"] = new TaskDefinition
                {
                    BaseDuration = 8f,
                    BaseProgress = 0.15f,
                    RequiredState = BehaviorState.SeekingWater,
                    ModifierComponent = typeof(ResourceComponent)
                },

                // Social tasks
                ["socialize"] = new TaskDefinition
                {
                    BaseDuration = 15f,
                    BaseProgress = 0.05f,
                    RequiredState = BehaviorState.Socializing,
                    ModifierComponent = typeof(SocialComponent)
                },

                // Rest tasks
                ["rest"] = new TaskDefinition
                {
                    BaseDuration = 20f,
                    BaseProgress = 0.08f,
                    RequiredState = BehaviorState.Resting,
                    ModifierComponent = null
                },

                // Work tasks
                ["work"] = new TaskDefinition
                {
                    BaseDuration = 30f,
                    BaseProgress = 0.03f,
                    RequiredState = BehaviorState.Working,
                    ModifierComponent = null
                },

                // Exploration tasks
                ["explore"] = new TaskDefinition
                {
                    BaseDuration = 25f,
                    BaseProgress = 0.04f,
                    RequiredState = BehaviorState.Exploring,
                    ModifierComponent = null
                }
            };
        }

        public void Update(float deltaTime)
        {
            foreach (var entity in entities)
            {
                var task = entity.GetComponent<TaskComponent>();
                var behavior = entity.GetComponent<BehaviorComponent>();
                
                if (task == null || behavior == null) continue;

                // Update current task if one exists
                if (task.HasActiveTask())
                {
                    UpdateTaskProgress(entity, task, behavior, deltaTime);
                }
                // Otherwise, check if we need to assign a new task
                else if (behavior.CurrentState != BehaviorState.Idle)
                {
                    AssignTaskForState(entity, task, behavior.CurrentState);
                }
            }
        }

        private void UpdateTaskProgress(
            Entity entity,
            TaskComponent task,
            BehaviorComponent behavior,
            float deltaTime)
        {
            var currentTask = task.CurrentTask;
            if (!taskDefinitions.TryGetValue(currentTask.Id, out var definition))
            {
                task.CancelCurrentTask();
                return;
            }

            // Check if entity is still in the correct state for this task
            if (behavior.CurrentState != definition.RequiredState)
            {
                task.InterruptCurrentTask();
                return;
            }

            // Calculate progress rate
            float progressRate = CalculateProgressRate(entity, definition);
            
            // Update task progress
            task.UpdateCurrentTask(deltaTime, progressRate * deltaTime);
        }

        private float CalculateProgressRate(Entity entity, TaskDefinition definition)
        {
            float baseProgress = definition.BaseProgress;
            float modifier = 1f;

            // Apply modifiers based on components
            if (definition.ModifierComponent != null)
            {
                if (definition.ModifierComponent == typeof(ResourceComponent))
                {
                    var resource = entity.GetComponent<ResourceComponent>();
                    if (resource != null)
                    {
                        modifier *= resource.Quality;
                    }
                }
                else if (definition.ModifierComponent == typeof(SocialComponent))
                {
                    var social = entity.GetComponent<SocialComponent>();
                    if (social != null)
                    {
                        modifier *= social.SocialEnergy / 100f;
                    }
                }
            }

            // Apply personality modifiers from behavior component
            var behavior = entity.GetComponent<BehaviorComponent>();
            if (behavior != null)
            {
                switch (definition.RequiredState)
                {
                    case BehaviorState.Socializing:
                        modifier *= behavior.GetSocialPreference();
                        break;
                    case BehaviorState.Working:
                        modifier *= behavior.GetWorkPreference();
                        break;
                    case BehaviorState.Exploring:
                        modifier *= behavior.GetExplorationPreference();
                        break;
                }
            }

            return baseProgress * modifier;
        }

        private void AssignTaskForState(Entity entity, TaskComponent task, BehaviorState state)
        {
            string taskId = state switch
            {
                BehaviorState.SeekingFood => "gather_food",
                BehaviorState.SeekingWater => "gather_water",
                BehaviorState.Socializing => "socialize",
                BehaviorState.Resting => "rest",
                BehaviorState.Working => "work",
                BehaviorState.Exploring => "explore",
                _ => null
            };

            if (taskId != null && taskDefinitions.TryGetValue(taskId, out var definition))
            {
                TaskPriority priority = GetTaskPriority(entity, state);
                task.AddTask(taskId, priority, definition.BaseDuration);
            }
        }

        private TaskPriority GetTaskPriority(Entity entity, BehaviorState state)
        {
            var needs = entity.GetComponent<NeedComponent>();
            if (needs == null) return TaskPriority.Normal;

            // Check if this task addresses a critical need
            bool isCritical = state switch
            {
                BehaviorState.SeekingFood => needs.IsNeedCritical(NeedType.Hunger),
                BehaviorState.SeekingWater => needs.IsNeedCritical(NeedType.Thirst),
                BehaviorState.Resting => needs.IsNeedCritical(NeedType.Energy),
                BehaviorState.Socializing => needs.IsNeedCritical(NeedType.Social),
                _ => false
            };

            if (isCritical) return TaskPriority.Critical;

            // Check if this task addresses an urgent need
            bool isUrgent = state switch
            {
                BehaviorState.SeekingFood => needs.IsNeedUrgent(NeedType.Hunger),
                BehaviorState.SeekingWater => needs.IsNeedUrgent(NeedType.Thirst),
                BehaviorState.Resting => needs.IsNeedUrgent(NeedType.Energy),
                BehaviorState.Socializing => needs.IsNeedUrgent(NeedType.Social),
                _ => false
            };

            return isUrgent ? TaskPriority.High : TaskPriority.Normal;
        }

        private class TaskDefinition
        {
            public float BaseDuration;
            public float BaseProgress;
            public BehaviorState RequiredState;
            public System.Type ModifierComponent;
        }
    }
}
