using UnityEngine;
using ECS.Core;
using System.Collections.Generic;
using System.Linq;

namespace ECS.Components
{
    public enum TaskPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    public class TaskComponent : IComponent
    {
        // Current task
        public Task CurrentTask { get; private set; }
        
        // Task queue
        private Queue<Task> taskQueue;
        private const int MAX_QUEUE_SIZE = 5;

        // Task completion history
        private Dictionary<string, TaskHistory> taskHistory;

        public TaskComponent()
        {
            taskQueue = new Queue<Task>();
            taskHistory = new Dictionary<string, TaskHistory>();
            CurrentTask = null;
        }

        public void AddTask(string taskId, TaskPriority priority, float expectedDuration)
        {
            var task = new Task
            {
                Id = taskId,
                Priority = priority,
                StartTime = Time.time,
                ExpectedDuration = expectedDuration,
                Progress = 0f,
                Status = TaskStatus.Queued
            };

            // Handle critical tasks immediately
            if (priority == TaskPriority.Critical)
            {
                InterruptCurrentTask();
                CurrentTask = task;
                CurrentTask.Status = TaskStatus.Active;
                return;
            }

            // Add to queue if there's space
            if (taskQueue.Count < MAX_QUEUE_SIZE)
            {
                taskQueue.Enqueue(task);
            }
            else
            {
                // Replace lowest priority task if new task is more important
                var queuedTasks = taskQueue.ToArray();
                var lowestPriority = queuedTasks[0];
                foreach (var queuedTask in queuedTasks)
                {
                    if (queuedTask.Priority < lowestPriority.Priority)
                    {
                        lowestPriority = queuedTask;
                    }
                }

                if (lowestPriority.Priority < priority)
                {
                    taskQueue = new Queue<Task>(queuedTasks.Where(t => t != lowestPriority));
                    taskQueue.Enqueue(task);
                }
            }
        }

        public void UpdateCurrentTask(float deltaTime, float progressAmount)
        {
            if (CurrentTask == null)
            {
                // Get next task from queue
                if (taskQueue.Count > 0)
                {
                    CurrentTask = taskQueue.Dequeue();
                    CurrentTask.Status = TaskStatus.Active;
                    CurrentTask.StartTime = Time.time;
                }
                return;
            }

            // Update progress
            CurrentTask.Progress = Mathf.Min(1f, CurrentTask.Progress + progressAmount);

            // Check for completion
            if (CurrentTask.Progress >= 1f)
            {
                CompleteCurrentTask(TaskStatus.Completed);
            }
            // Check for timeout
            else if (Time.time - CurrentTask.StartTime > CurrentTask.ExpectedDuration * 2f)
            {
                CompleteCurrentTask(TaskStatus.TimedOut);
            }
        }

        public void InterruptCurrentTask()
        {
            if (CurrentTask != null)
            {
                CompleteCurrentTask(TaskStatus.Interrupted);
            }
        }

        public void CancelCurrentTask()
        {
            if (CurrentTask != null)
            {
                CompleteCurrentTask(TaskStatus.Cancelled);
            }
        }

        private void CompleteCurrentTask(TaskStatus status)
        {
            if (CurrentTask == null) return;

            // Update task history
            if (!taskHistory.ContainsKey(CurrentTask.Id))
            {
                taskHistory[CurrentTask.Id] = new TaskHistory();
            }

            var history = taskHistory[CurrentTask.Id];
            history.CompletionCount++;
            history.LastCompletionTime = Time.time;
            history.LastStatus = status;
            history.AverageCompletionTime = Mathf.Lerp(
                history.AverageCompletionTime,
                Time.time - CurrentTask.StartTime,
                1f / history.CompletionCount
            );

            CurrentTask.Status = status;
            CurrentTask = null;
        }

        public bool HasActiveTask()
        {
            return CurrentTask != null;
        }

        public int QueuedTaskCount()
        {
            return taskQueue.Count;
        }

        public float GetTaskEfficiency(string taskId)
        {
            if (taskHistory.TryGetValue(taskId, out var history))
            {
                float successRate = (float)history.CompletionCount / 
                    (history.CompletionCount + history.FailureCount);
                return successRate;
            }
            return 0f;
        }

        public class Task
        {
            public string Id;
            public TaskPriority Priority;
            public float StartTime;
            public float ExpectedDuration;
            public float Progress;
            public TaskStatus Status;
        }

        private class TaskHistory
        {
            public int CompletionCount;
            public int FailureCount;
            public float LastCompletionTime;
            public float AverageCompletionTime;
            public TaskStatus LastStatus;
        }
    }

    public enum TaskStatus
    {
        Queued,
        Active,
        Completed,
        Failed,
        Interrupted,
        Cancelled,
        TimedOut
    }
}
