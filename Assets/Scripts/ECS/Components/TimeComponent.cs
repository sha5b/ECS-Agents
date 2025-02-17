using UnityEngine;
using ECS.Core;

namespace ECS.Components
{
    public class TimeComponent : IComponent
    {
        // Time awareness properties
        public bool IsActiveAtNight { get; private set; }
        public bool IsActiveAtDay { get; private set; }
        public float ActivityStartTime { get; private set; }
        public float ActivityEndTime { get; private set; }

        // Custom behavior based on time of day
        public float DayTimeMultiplier { get; private set; }
        public float NightTimeMultiplier { get; private set; }

        public TimeComponent(
            bool activeAtNight = true,
            bool activeAtDay = true,
            float activityStartTime = 0f,
            float activityEndTime = 24f,
            float dayMultiplier = 1f,
            float nightMultiplier = 1f)
        {
            IsActiveAtNight = activeAtNight;
            IsActiveAtDay = activeAtDay;
            ActivityStartTime = Mathf.Clamp(activityStartTime, 0f, 24f);
            ActivityEndTime = Mathf.Clamp(activityEndTime, 0f, 24f);
            DayTimeMultiplier = dayMultiplier;
            NightTimeMultiplier = nightMultiplier;
        }

        public bool IsActiveAtTime(float currentHour)
        {
            bool isNightTime = currentHour < 6f || currentHour > 18f;
            
            // First check day/night activity
            if (isNightTime && !IsActiveAtNight) return false;
            if (!isNightTime && !IsActiveAtDay) return false;

            // Then check specific time range
            if (ActivityStartTime < ActivityEndTime)
            {
                return currentHour >= ActivityStartTime && currentHour <= ActivityEndTime;
            }
            else // Handles ranges that cross midnight
            {
                return currentHour >= ActivityStartTime || currentHour <= ActivityEndTime;
            }
        }

        public float GetCurrentMultiplier(float currentHour)
        {
            return (currentHour < 6f || currentHour > 18f) ? NightTimeMultiplier : DayTimeMultiplier;
        }
    }
}
