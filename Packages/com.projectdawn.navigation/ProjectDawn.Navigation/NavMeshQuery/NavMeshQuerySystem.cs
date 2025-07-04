#pragma warning disable CS0618
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Burst;
using Unity.Assertions;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using UnityEngine;
using static Unity.Entities.SystemAPI;
using System.Diagnostics;

namespace ProjectDawn.Navigation
{
    [System.Serializable]
    class NavMeshSubSettings : ISubSettings
    {
        [SerializeField]
        [Tooltip("The maximum number of iterations the agent will use to find a path is determined. Each iteration represents visiting a single node. A larger number results in a more accurate path, but it also incurs a greater performance cost.")]
        int m_MaxIterations = 1024;

        [SerializeField]
        [Tooltip("The maximum number of iterations the agent will use per frame. A langer number results in faster path finding, but it also incurs a greater performance cost.")]
        int m_IterationsPerFrame = 1024;

        [SerializeField]
        [Tooltip("The maximum size of the agents path.")]
        int m_MaxPath = 1024;

        /// <summary>
        /// The maximum number of iterations the agent will use to find a path is determined. Each iteration represents visiting a single node. A larger number results in a more accurate path, but it also incurs a greater performance cost.
        /// </summary>
        public int MaxIterations => m_MaxIterations;
        /// <summary>
        /// The maximum number of iterations the agent will use per frame. A langer number results in faster path finding, but it also incurs a greater performance cost.
        /// </summary>
        public int IterationsPerFrame => m_IterationsPerFrame;
        /// <summary>
        /// The maximum size of the agents path.
        /// </summary>
        public int MaxPath => m_MaxPath;
    }

    /// <summary>
    /// The status of navmesh query.
    /// </summary>
    public enum NavMeshQueryStatus
    {
        None,
        /// <summary>
        /// Path is allocated.
        /// </summary>
        Allocated,
        /// <summary>
        /// Path is in progress.
        /// </summary>
        InProgress,
        /// <summary>
        /// Failed to construct path.
        /// </summary>
        Failed,
        /// <summary>
        /// Path is finished.
        /// </summary>
        [Obsolete("This enum Finished is obsolete, please use FinishedFullPath or FinishedPartialPath", true)]
        Finished,
        /// <summary>
        /// Full path is finished, where destination is reachable.
        /// </summary>
        FinishedFullPath,
        /// <summary>
        /// Partial path is finished, where it leads close enough to destination.
        /// </summary>
        FinishedPartialPath,
        /// <summary>
        /// Path is already released.
        /// </summary>
        Free,
    }

    /// <summary>
    /// System for requesting NavMesh path.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public unsafe partial struct NavMeshQuerySystem : ISystem
    {
        const int MaxCostLength = 32;

        int MaxIterations;
        int IterationsPerFrame;
        int MaxPathSize;

        NativeQueue<NavMeshQueryHandle> m_Free;

        UnsafeList<NavMeshQuery>* m_Queries;
        NativeList<PolygonId> m_Paths;
        NativeList<NavMeshQueryData> m_Data;
        NativeList<NavMeshQueryStatus> m_Status;
        NativeList<int> m_PathLength;
        NativeList<JobHandle> m_JobHandles;
        NativeList<float> m_Costs;
        NavMeshQuery m_QueryForOtherOperations;
        NavMeshWorld m_World;

        //[BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            if (!Application.isPlaying)
                throw new InvalidOperationException("NavMeshQuerySystem does not support lifetime in edit time as unity navmesh does not support it too.");

            MaxIterations = AgentsNavigationSettings.Get<NavMeshSubSettings>().MaxIterations;
            IterationsPerFrame = AgentsNavigationSettings.Get<NavMeshSubSettings>().IterationsPerFrame;
            MaxPathSize = AgentsNavigationSettings.Get<NavMeshSubSettings>().MaxPath;

            m_World = NavMeshWorld.GetDefaultWorld();
            m_Free = new NativeQueue<NavMeshQueryHandle>(Allocator.Persistent);
            m_Queries = UnsafeList<NavMeshQuery>.Create(1, Allocator.Persistent);
            m_Paths = new NativeList<PolygonId>(1, Allocator.Persistent);
            m_Data = new NativeList<NavMeshQueryData>(Allocator.Persistent);
            m_Status = new NativeList<NavMeshQueryStatus>(Allocator.Persistent);
            m_JobHandles = new NativeList<JobHandle>(Allocator.Persistent);
            m_PathLength = new NativeList<int>(Allocator.Persistent);
            m_Costs = new NativeList<float>(Allocator.Persistent);

            // Create dummy at index 0, so we could have it reserved for null
            m_Queries->Add(new NavMeshQuery());
            m_Paths.Length = MaxPathSize;
            m_Data.Add(new NavMeshQueryData());
            m_Status.Add(NavMeshQueryStatus.FinishedFullPath);
            m_JobHandles.Add(new JobHandle());
            m_Costs.Length = MaxCostLength;
            m_PathLength.Add(0);

            m_QueryForOtherOperations = new NavMeshQuery(m_World, Allocator.Persistent);

            state.EntityManager.AddComponentData(state.SystemHandle, new Singleton
            {
                m_Free = m_Free,
                m_Queries = m_Queries,
                m_Paths = m_Paths,
                m_Data = m_Data,
                m_Status = m_Status,
                m_PathLength = m_PathLength,
                m_JobHandles = m_JobHandles,
                m_Costs = m_Costs,
                m_QueryForOtherOperations = m_QueryForOtherOperations,
                m_World = m_World,
                MaxPathSize = MaxPathSize,
            });

            state.RequireForUpdate<Agent>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            for (int index = 1; index < m_Data.Length; ++index)
            {
                m_Queries->ElementAt(index).Dispose();
            }

            UnsafeList<NavMeshQuery>.Destroy(m_Queries);
            m_Paths.Dispose();
            m_Free.Dispose();
            m_Data.Dispose();
            m_Status.Dispose();
            m_JobHandles.Dispose();
            m_PathLength.Dispose();
            m_Costs.Dispose();

            m_QueryForOtherOperations.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            GetSingletonRW<Singleton>();

            // Complete jobs that used singleton as this update will be accessing the data
            state.CompleteDependency();

            // At handle 0 is dummy query
            for (int index = 1; index < m_Data.Length; ++index)
            {
                // Lazy init query
                if (m_Status[index] == NavMeshQueryStatus.None)
                {
                    m_Queries->ElementAt(index) = new NavMeshQuery(m_World, Allocator.Persistent, MaxIterations);
                    m_Status[index] = NavMeshQueryStatus.Allocated;
                }

                NavMeshQueryData data = m_Data[index];
                NavMeshQuery query = m_Queries->ElementAt(index);
                NavMeshQueryStatus status = m_Status[index];

                if (status == NavMeshQueryStatus.Allocated || status == NavMeshQueryStatus.InProgress)
                {
                    var job = new NavMeshQueryJob
                    {
                        Query = query,
                        Data = data,
                        Path = (PolygonId*) m_Paths.GetUnsafePtr() + index * MaxPathSize,
                        Status = ((NavMeshQueryStatus*) m_Status.GetUnsafePtr()) + index,
                        PathLength = ((int*) m_PathLength.GetUnsafePtr()) + index,
                        Costs = (float*) m_Costs.GetUnsafePtr() + index * MaxCostLength,
                        MaxIterations = IterationsPerFrame,
                        MaxPathSize = MaxPathSize,
                    };

                    m_JobHandles[index] = job.Schedule(state.Dependency);
                }
            }

            state.Dependency = JobHandle.CombineDependencies(m_JobHandles.AsArray());

            // TODO: Investigate if we need to add this dependency for all systems that will query navmesh singleton
            m_World.AddDependency(state.Dependency);
        }

        public unsafe struct Singleton : IComponentData
        {
            internal NativeQueue<NavMeshQueryHandle> m_Free;

            [NativeDisableUnsafePtrRestriction]
            internal UnsafeList<NavMeshQuery>* m_Queries;
            [NativeDisableUnsafePtrRestriction]
            internal NativeList<PolygonId> m_Paths;
            internal NativeList<NavMeshQueryData> m_Data;
            internal NativeList<NavMeshQueryStatus> m_Status;
            internal NativeList<int> m_PathLength;
            [NativeDisableUnsafePtrRestriction]
            internal NativeList<float> m_Costs;
            internal NativeList<JobHandle> m_JobHandles;
            internal NavMeshQuery m_QueryForOtherOperations;
            [NativeDisableUnsafePtrRestriction]
            internal NavMeshWorld m_World;
            internal int MaxPathSize;

            public NavMeshWorld World => m_World;

            /// <summary>
            /// Creates new navmesh query to construct optimal path.
            /// </summary>
            public NavMeshQueryHandle CreateQuery(NavMeshLocation from, NavMeshLocation to, int agentTypeId = 0, int areaMask = -1, NativeArray<float> costs = default)
            {
                NavMeshQuerySystem.CheckLocation(from);
                NavMeshQuerySystem.CheckLocation(to);

                var hash = GetHash(from, to, agentTypeId);

                // Try to find unused query in pool
                if (m_Free.TryDequeue(out NavMeshQueryHandle handle))
                {
                    m_Data[handle] = new NavMeshQueryData
                    {
                        From = from,
                        To = to,
                        Hash = hash,
                        AgentTypeId = agentTypeId,
                        AreaMask = areaMask,
                    };
                    m_Status[handle] = NavMeshQueryStatus.Allocated;
                    m_JobHandles[handle] = new JobHandle();
                    m_PathLength[handle] = 0;
                    if (costs.IsCreated)
                        UnsafeUtility.MemCpy(m_Costs.GetUnsafePtr() + handle * MaxCostLength, costs.GetUnsafeReadOnlyPtr(), sizeof(float) * MaxCostLength);
                    else
                        m_Costs[handle * MaxCostLength] = 0;
                    return handle;
                }
                else
                {
                    var data = new NavMeshQueryData
                    {
                        From = from,
                        To = to,
                        Hash = hash,
                        AgentTypeId = agentTypeId,
                        AreaMask = areaMask,
                    };
                    handle = new NavMeshQueryHandle { Index = m_Queries->Length };

                    m_Queries->Add(new NavMeshQuery());
                    m_Paths.Length += MaxPathSize;
                    m_Data.Add(data);
                    m_Status.Add(NavMeshQueryStatus.None);
                    m_JobHandles.Add(new JobHandle());
                    m_PathLength.Add(0);
                    m_Costs.Length += MaxCostLength;
                    if (costs.IsCreated)
                        UnsafeUtility.MemCpy(m_Costs.GetUnsafePtr() + handle * MaxCostLength, costs.GetUnsafeReadOnlyPtr(), sizeof(float) * MaxCostLength);
                    else
                        m_Costs[handle * MaxCostLength] = 0;

                    return handle;
                }
            }

            /// <summary>
            /// Release navmesh query for reuse.
            /// </summary>
            public void DestroyQuery(NavMeshQueryHandle handle)
            {
                CheckHandle(handle);
                m_Free.Enqueue(handle);
                m_Status[handle] = NavMeshQueryStatus.Free;
            }

            /// <summary>
            /// Returns path that is represented as array of nodes. Can be used for construcing <see cref="NavMeshFunnel"/>.
            /// </summary>
            public NativeSlice<PolygonId> GetPolygons(NavMeshQueryHandle handle)
            {
                CheckHandle(handle);
                return m_Paths.AsArray().Slice(MaxPathSize * handle, m_PathLength[handle]);
            }

            /// <summary>
            /// Returns status of navmesh query.
            /// </summary>
            public NavMeshQueryStatus GetStatus(NavMeshQueryHandle handle)
            {
                CheckHandle(handle);
                return m_Status[handle];
            }

            /// <summary>
            /// Returns true if navmesh query exists.
            /// </summary>
            public bool Exist(NavMeshQueryHandle handle)
            {
                return handle != NavMeshQueryHandle.Null && handle.Index < m_Queries->Length;
            }

            public PathQueryStatus Raycast(out NavMeshHit hit, NavMeshLocation start, Vector3 targetPosition, int areaMask = -1, NativeArray<float> costs = default)
            {
                return m_QueryForOtherOperations.Raycast(out hit, start, targetPosition, areaMask, costs);
            }

            /// <summary>
            /// Returns navmesh location from specified one.
            /// This is HPC# version of <see cref="NavMesh.SamplePosition(Vector3, out NavMeshHit, float, int)"/>.
            /// </summary>
            public NavMeshLocation MapLocation(float3 position, float3 extents, int agentTypeID, int areaMask = -1)
            {
                return m_QueryForOtherOperations.MapLocation(position, extents, agentTypeID, areaMask);
            }

            /// <summary>
            /// Moves navmesh location to specified position.
            /// This is usually used for finding new position without steping over the obstacles.
            /// </summary>
            public NavMeshLocation MoveLocation(NavMeshLocation location, float3 target, int areaMask = -1)
            {
                return m_QueryForOtherOperations.MoveLocation(location, target, areaMask);
            }

            /// <summary>
            /// Creates navmesh funnel out of navmesh nodes.
            /// </summary>
            public bool TryCreateFunnel(ref NavMeshFunnel funnel, NativeSlice<PolygonId> path, float3 from, float3 to)
            {
                return funnel.TryCreateStraightPath(m_QueryForOtherOperations, path, from, to);
            }

            public void OptimizePath(ref NavMeshFunnel funnel, DynamicBuffer<PolygonId> path, NavMeshLocation from, int areaMask = -1, NativeArray<float> costs = default)
            {
                funnel.OptimizePath(m_QueryForOtherOperations, path, from, areaMask, costs);
            }

            /// <summary>
            /// Returns true, if all nodes in array are valid. Does not check, if the nodes are connected.
            /// </summary>
            public bool IsPathValid(NativeSlice<PolygonId> path)
            {
                for (int i = 0; i < path.Length; ++i)
                {
                    if (!m_QueryForOtherOperations.IsValid(path[i]))
                        return false;
                }
                return true;
            }

            /// <summary>
            /// Returns true, if node is valid.
            /// </summary>
            public bool IsValid(PolygonId path) => m_QueryForOtherOperations.IsValid(path);

            public NavMeshPolyTypes GetPolygonType(PolygonId path) => m_QueryForOtherOperations.GetPolygonType(path);

            public bool GetPortalPoints(PolygonId from, PolygonId to, out float3 portalLeft, out float3 portalRight)
            {
                var polyWorldToLocal = m_QueryForOtherOperations.PolygonWorldToLocalMatrix(from);

                if (!m_QueryForOtherOperations.GetPortalPoints(from, to, out var vl, out var vr))
                {
                    portalLeft = default;
                    portalRight = default;
                    return false;
                }

                portalLeft = polyWorldToLocal.MultiplyPoint(vl);
                portalRight = polyWorldToLocal.MultiplyPoint(vr);

                return true;
            }

            /// <summary>
            /// Returns surface information of navmesh node.
            /// </summary>
            public bool GetEdgesAndNeighbors(PolygonId node, NativeSlice<Vector3> vertices, NativeSlice<PolygonId> nodes, NativeSlice<byte> indices, out int numVertices, out int numNodes)
            {
                return m_QueryForOtherOperations.GetEdgesAndNeighbors(node, vertices, nodes, indices, out numVertices, out numNodes) == PathQueryStatus.Success;
            }

            public bool GetPortalPoints(PolygonId polygon, PolygonId neighbourPolygon, out Vector3 left, out Vector3 right)
            {
                return m_QueryForOtherOperations.GetPortalPoints(polygon, neighbourPolygon, out left, out right);
            }

            /// <summary>
            /// Progress path by either removing or adding nodes.
            /// If newPolygon exists in path, it shortens path up to that node.
            /// Otherwise it attempts to add newPolygon to path.
            /// This method can produce invalid path, if newPolygon is can not be connected to path, it is expected that funnel will request new path in that case.
            /// </summary>
            public void ProgressPath(ref DynamicBuffer<NavMeshNode> nodes, PolygonId previousPolygon, PolygonId newPolygon)
            {
                if (FindIndex(ref nodes, newPolygon, out int index))
                {
                    if (nodes.Length > 1)
                    {
                        for (int i = 0; i < index; ++i)
                        {
                            nodes.RemoveAt(0);
                        }
                    }
                }
                else
                {
                    if (FindIndex(ref nodes, previousPolygon, out int index2))
                    {
                        if (nodes.Length > 1)
                        {
                            for (int i = 0; i < index2 + 1; ++i)
                            {
                                nodes.RemoveAt(0);
                            }
                        }
                    }
                    if (previousPolygon != newPolygon)
                    {
                        nodes.Insert(0, new NavMeshNode { Value = newPolygon });
                    }
                }
            }

            static bool FindIndex(ref DynamicBuffer<NavMeshNode> nodes, PolygonId newPolygon, out int index)
            {
                for (int i = 0; i < nodes.Length; ++i)
                {
                    if (nodes[i].Value == newPolygon)
                    {
                        index = i;
                        return true;
                    }
                }
                index = -1;
                return false;
            }

            uint GetHash(NavMeshLocation from, NavMeshLocation to, int agentTypeId)
            {
                return math.hash(new int3(from.polygon.GetHashCode(), to.polygon.GetHashCode(), agentTypeId));
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            void CheckHandle(NavMeshQueryHandle handle)
            {
                if (!Exist(handle))
                    throw new ArgumentNullException("handle");
            }
        }

        [BurstCompile]
        unsafe struct NavMeshQueryJob : IJob
        {
            public NavMeshQuery Query;
            public NavMeshQueryData Data;
            [NativeDisableUnsafePtrRestriction]
            public PolygonId* Path;
            [NativeDisableUnsafePtrRestriction]
            public NavMeshQueryStatus* Status;
            [NativeDisableUnsafePtrRestriction]
            public int* PathLength;
            [NativeDisableUnsafePtrRestriction]
            public float* Costs;
            public int MaxIterations;
            public int MaxPathSize;

            [NativeDisableContainerSafetyRestriction]
            NativeArray<float> AreaCosts;

            public void Execute()
            {
                if (*Status == NavMeshQueryStatus.Allocated)
                {
                    NavMeshLocation from = Data.From;
                    NavMeshLocation to = Data.To;

                    NavMeshQuerySystem.CheckLocation(from);
                    NavMeshQuerySystem.CheckLocation(to);

                    // Check if locations valid, for example if navmesh changed
                    if (!Query.IsValid(from))
                    {
                        *Status = NavMeshQueryStatus.Failed;
                        return;
                    }
                    if (!Query.IsValid(to))
                    {
                        *Status = NavMeshQueryStatus.Failed;
                        return;
                    }

                    PathQueryStatus beginStatus;
                    if (*Costs == 0) // First cost value indicates, if cost area is used
                    {
                        beginStatus = Query.BeginFindPath(from, to, Data.AreaMask);
                    }
                    else
                    {
                        var costs = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<float>(Costs, MaxCostLength, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref costs, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
                        beginStatus = Query.BeginFindPath(from, to, Data.AreaMask, costs);
                    }

                    if (beginStatus == PathQueryStatus.InProgress || beginStatus == PathQueryStatus.Success)
                    {
                        *Status = NavMeshQueryStatus.InProgress;
                    }
                    else
                    {
                        *Status = NavMeshQueryStatus.Failed;
                        ThrowFailure(beginStatus);
                        return;
                    }
                }

                Assert.AreNotEqual(MaxIterations, 0);
                var updateStatus = Query.UpdateFindPath(MaxIterations, out int iterationsPerformed);
                switch (updateStatus)
                {
                    case PathQueryStatus.InProgress:
                        break;

                    case PathQueryStatus.Success:
                    case PathQueryStatus.InProgress | PathQueryStatus.OutOfNodes:
                    case PathQueryStatus.Success | PathQueryStatus.OutOfNodes:
                        var endStatus = Query.EndFindPath(out int polySize);
                        if ((endStatus & PathQueryStatus.Success) != 0)
                        {
                            var polygons = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<PolygonId>(Path, sizeof(PolygonId), polySize);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                            NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref polygons, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
                            Query.GetPathResult(polygons);

                            *Status = endStatus == PathQueryStatus.Success ? NavMeshQueryStatus.FinishedFullPath : NavMeshQueryStatus.FinishedPartialPath;
                            *PathLength = polySize;
                        }
                        else
                        {
                            *Status = NavMeshQueryStatus.Failed;
                            ThrowFailure(endStatus);
                        }
                        break;

                    default:
                        *Status = NavMeshQueryStatus.Failed;
                        ThrowFailure(updateStatus);
                        break;
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            static void ThrowFailure(PathQueryStatus status)
            {
                throw new InvalidOperationException($"Failed to query NavMesh path with error code {status}.");
            }
        }

        /// <summary>
        /// Data of navmesh query.
        /// </summary>
        internal struct NavMeshQueryData
        {
            public NavMeshLocation From;
            public NavMeshLocation To;
            public uint Hash;
            public int AgentTypeId;
            public int AreaMask;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        static void CheckLocation(NavMeshLocation location)
        {
            if (location.polygon.IsNull())
                UnityEngine.Debug.LogWarning($"NavMeshLocation can not the null.");
        }
    }
}
#pragma warning restore CS0618
