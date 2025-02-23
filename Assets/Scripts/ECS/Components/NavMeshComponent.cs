using UnityEngine;
using UnityEngine.AI;
using ECS.Core;
using System.Collections.Generic;
using System.Linq;

namespace ECS.Components
{
    public class NavMeshComponent : IComponent
    {
        // NavMesh data
        public NavMeshData NavMeshData { get; private set; }
        public NavMeshDataInstance NavMeshInstance { get; private set; }
        // Connection points for navigation
        private struct NavConnection
        {
            public Vector3 Start;
            public Vector3 End;
            public float Width;
        }
        private List<NavConnection> connections;

        // Agent settings
        public float AgentRadius { get; private set; }
        public float AgentHeight { get; private set; }
        public float MaxSlope { get; private set; }
        public float StepHeight { get; private set; }

        // Path caching
        private Dictionary<Vector3, NavMeshPath> pathCache;
        private const int MAX_CACHE_SIZE = 100;
        private const float CACHE_TIMEOUT = 5f; // Seconds before a cached path is considered stale

        public NavMeshComponent(
            float agentRadius = 0.5f,
            float agentHeight = 2f,
            float maxSlope = 45f,
            float stepHeight = 0.4f)
        {
            AgentRadius = agentRadius;
            AgentHeight = agentHeight;
            MaxSlope = maxSlope;
            StepHeight = stepHeight;

            connections = new List<NavConnection>();
            pathCache = new Dictionary<Vector3, NavMeshPath>();

            InitializeNavMesh();
        }

        private void InitializeNavMesh()
        {
            // Create NavMesh settings
            var settings = new NavMeshBuildSettings
            {
                agentRadius = AgentRadius,
                agentHeight = AgentHeight,
                agentSlope = MaxSlope,
                agentClimb = StepHeight,
                minRegionArea = 1f,
                overrideVoxelSize = false,
                overrideTileSize = false,
                tileSize = 256
            };

            // Create new NavMesh data
            NavMeshData = new NavMeshData();
            NavMeshInstance = NavMesh.AddNavMeshData(NavMeshData);
        }

        public void UpdateNavMesh(Mesh terrainMesh, Vector3 position)
        {
            // Create sources for NavMesh building
            var sources = new List<NavMeshBuildSource>();
            
            // Add terrain mesh as source
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Mesh,
                sourceObject = terrainMesh,
                transform = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one),
                area = 0 // Walkable area
            };
            sources.Add(source);

            // Build NavMesh
            var bounds = new Bounds(position, new Vector3(100f, 100f, 100f)); // Adjust bounds as needed
            NavMeshBuilder.UpdateNavMeshData(
                NavMeshData,
                new NavMeshBuildSettings(),
                sources,
                bounds
            );
        }

        public void AddConnection(Vector3 start, Vector3 end, float width = 1f)
        {
            connections.Add(new NavConnection 
            { 
                Start = start, 
                End = end, 
                Width = width 
            });
        }

        public bool TryGetNearestConnection(Vector3 position, float maxDistance, out Vector3 connectionPoint)
        {
            connectionPoint = Vector3.zero;
            float nearestDistance = float.MaxValue;
            bool found = false;

            foreach (var connection in connections)
            {
                // Check start point
                float distToStart = Vector3.Distance(position, connection.Start);
                if (distToStart < nearestDistance && distToStart <= maxDistance)
                {
                    nearestDistance = distToStart;
                    connectionPoint = connection.Start;
                    found = true;
                }

                // Check end point
                float distToEnd = Vector3.Distance(position, connection.End);
                if (distToEnd < nearestDistance && distToEnd <= maxDistance)
                {
                    nearestDistance = distToEnd;
                    connectionPoint = connection.End;
                    found = true;
                }
            }

            return found;
        }

        public bool TryGetCachedPath(Vector3 destination, out NavMeshPath path)
        {
            return pathCache.TryGetValue(destination, out path);
        }

        public void CachePath(Vector3 destination, NavMeshPath path)
        {
            // Remove oldest path if cache is full
            if (pathCache.Count >= MAX_CACHE_SIZE)
            {
                pathCache.Remove(pathCache.Keys.GetEnumerator().Current);
            }
            pathCache[destination] = path;
        }

        public void ClearCache()
        {
            pathCache.Clear();
        }

        public void Cleanup()
        {
            // Remove NavMesh instance
            if (NavMeshInstance.valid)
            {
                NavMeshInstance.Remove();
            }

            // Clear connections
            connections.Clear();

            // Clear cache
            ClearCache();
        }
    }
}
