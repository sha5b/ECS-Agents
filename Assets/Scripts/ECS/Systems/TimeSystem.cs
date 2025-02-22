using UnityEngine;
using ECS.Core;
using ECS.Components;
using System;
using System.Collections.Generic;

namespace ECS.Systems
{
    public enum DayPeriod
    {
        Dawn,   // Early morning
        Day,    // Full daylight
        Dusk,   // Evening
        Night   // Darkness
    }

    public class TimeSystem : ISystem
    {
        private World world;
        private float currentTime; // Hours in 24-hour format
        private float timeScale = 60f; // 1 real second = 1 minute in-game
        private int currentDay;
        private DayPeriod currentPeriod;

        // Time periods
        public const float DAWN_START = 5f;
        public const float DAY_START = 7f;
        public const float DUSK_START = 17f;
        public const float NIGHT_START = 19f;

        // Events
        public event Action<DayPeriod> OnPeriodChange;
        public event Action OnDayChange;
        public event Action<float> OnHourChange;

        public TimeSystem(World world, float startTime = 6f)
        {
            this.world = world;
            this.currentTime = startTime;
            this.currentDay = 1;
            this.currentPeriod = GetDayPeriod(startTime);
        }

        public void Update(float deltaTime)
        {
            float previousTime = currentTime;
            int previousDay = currentDay;
            DayPeriod previousPeriod = currentPeriod;

            // Update time
            float minutesElapsed = deltaTime * timeScale;
            currentTime += minutesElapsed / 60f; // Convert minutes to hours

            // Handle day rollover
            if (currentTime >= 24f)
            {
                currentTime -= 24f;
                currentDay++;
                OnDayChange?.Invoke();
            }

            // Check for hour changes
            int previousHour = Mathf.FloorToInt(previousTime);
            int currentHour = Mathf.FloorToInt(currentTime);
            if (currentHour != previousHour)
            {
                OnHourChange?.Invoke(currentHour);
            }

            // Update day period
            currentPeriod = GetDayPeriod(currentTime);
            if (currentPeriod != previousPeriod)
            {
                OnPeriodChange?.Invoke(currentPeriod);
                HandlePeriodChange(previousPeriod, currentPeriod);
            }
        }

        private DayPeriod GetDayPeriod(float time)
        {
            if (time >= NIGHT_START || time < DAWN_START) return DayPeriod.Night;
            if (time >= DUSK_START) return DayPeriod.Dusk;
            if (time >= DAY_START) return DayPeriod.Day;
            return DayPeriod.Dawn;
        }

        private void HandlePeriodChange(DayPeriod oldPeriod, DayPeriod newPeriod)
        {
            // Reset daily activities at dawn
            if (newPeriod == DayPeriod.Dawn)
            {
                ResetDailyActivities();
            }

            // Modify NPC behavior based on time of day
            ModifyNPCBehavior(newPeriod);
        }

        private void ResetDailyActivities()
        {
            foreach (var entity in world.GetEntities())
            {
                // Reset social interactions
                var social = entity.GetComponent<SocialComponent>();
                if (social != null)
                {
                    social.ResetDaily();
                }

                // Reset task priorities
                var task = entity.GetComponent<TaskComponent>();
                if (task != null && task.HasActiveTask())
                {
                    // Interrupt non-critical tasks at dawn
                    if (task.CurrentTask.Priority != TaskPriority.Critical)
                    {
                        task.InterruptCurrentTask();
                    }
                }
            }
        }

        private void ModifyNPCBehavior(DayPeriod period)
        {
            foreach (var entity in world.GetEntities())
            {
                var behavior = entity.GetComponent<BehaviorComponent>();
                var needs = entity.GetComponent<NeedComponent>();
                
                if (behavior == null || needs == null) continue;

                switch (period)
                {
                    case DayPeriod.Night:
                        // Increase energy recovery during night
                        needs.ModifyNeed(NeedType.Energy, 20f);
                        if (behavior.CurrentState != BehaviorState.Resting)
                        {
                            behavior.UpdateState(BehaviorState.Resting);
                        }
                        break;

                    case DayPeriod.Dawn:
                        // Increase hunger at start of day
                        needs.ModifyNeed(NeedType.Hunger, -10f);
                        needs.ModifyNeed(NeedType.Thirst, -10f);
                        break;

                    case DayPeriod.Day:
                        // Normal behavior during day
                        if (behavior.CurrentState == BehaviorState.Resting)
                        {
                            behavior.UpdateState(BehaviorState.Idle);
                        }
                        break;

                    case DayPeriod.Dusk:
                        // Increase social activity in evening
                        if (behavior.CurrentState == BehaviorState.Idle)
                        {
                            behavior.UpdateState(BehaviorState.Socializing);
                        }
                        break;
                }
            }
        }

        // Public getters
        public float GetCurrentTime() => currentTime;
        public int GetCurrentDay() => currentDay;
        public DayPeriod GetCurrentPeriod() => currentPeriod;
        public float GetTimeScale() => timeScale;

        // Public setters
        public void SetTimeScale(float scale)
        {
            timeScale = Mathf.Clamp(scale, 1f, 120f); // 1 second to 2 minutes per real second
        }

        public void SetTime(float time)
        {
            currentTime = Mathf.Repeat(time, 24f);
            currentPeriod = GetDayPeriod(currentTime);
        }
    }
}
