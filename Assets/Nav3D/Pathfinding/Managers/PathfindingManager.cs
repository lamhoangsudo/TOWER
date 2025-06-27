using Nav3D.API;
using Nav3D.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Nav3D.Pathfinding
{
    public partial class PathfindingManager : MonoBehaviour
    {
        #region Constants : Log

        readonly string LOG_REQUEST_PATHFINDING = $"{nameof(PathfindingManager)}.{nameof(RequestPathfinding)}: OrderID: {{0}}, Points: {{1}}, Looped {{2}}";

        #endregion

        #region Attributes

        int m_PathFindingTasksMaxCount = Environment.ProcessorCount - 1;

        float m_StorageBucketSize;

        //Paths that were found in scene space.
        //Need to be stored to determine if any update to the obstacle will invalidate some path.
        //In this case, the invalid path needs to be updated.
        CurvesSpatialHashMap<Path> m_PathsStorage;

        OrdersExecutor<Path, PathfindingOrder> m_PathfindingOrdersExecutor;

        #endregion

        #region Properties

        public static bool               Doomed   { get; private set; } = false;
        public static PathfindingManager Instance => Singleton<PathfindingManager>.Instance;

        public int PathFindingTasksMaxCount
        {
            get => m_PathFindingTasksMaxCount;
            set
            {
                if (value == m_PathFindingTasksMaxCount)
                    return;

                m_PathFindingTasksMaxCount = Mathf.Max(value, 1);

                RecreateOrdersExecutor(m_PathFindingTasksMaxCount);
            }
        }

        public int PathFindingTasksOperatingCount => m_PathfindingOrdersExecutor?.CurrentAliveOrders ?? 0;

        #endregion

        #region Serialized fields

        [SerializeField] bool m_DrawPathsStorage;
        [SerializeField] bool m_DisplayExecutorStats;

        #endregion

        #region Public methods

        public void Initialize(float _StorageBucketSize)
        {
            RecreateOrdersExecutor(m_PathFindingTasksMaxCount);

            m_StorageBucketSize = _StorageBucketSize;

            m_PathsStorage = new CurvesSpatialHashMap<Path>(m_StorageBucketSize);
        }

        public void Uninitialize(bool _NeedDestroy = true)
        {
            m_PathfindingOrdersExecutor?.Dispose();

            if (!_NeedDestroy)
                return;

            UtilsCommon.SmartDestroy(this);
        }

        public void RequestPathfinding(
                Path                      _Requester,
                Vector3[]                 _Points,
                bool                      _Loop,
                bool                      _SkipUnpassableTargets,
                bool                      _TryRepositionStartIfOccupied,
                bool                      _TryRepositionTargetIfOccupied,
                bool                      _Smooth,
                int                       _PerMinBucketSmoothSamples,
                CancellationToken         _CancellationToken,
                int                       _Timeout,
                Action<PathfindingResult> _OnSuccess,
                Action<PathfindingError>  _OnFail = null,
                Log                       _Log    = null
            )
        {
            _Log?.WriteFormat(LOG_REQUEST_PATHFINDING, _Requester.RequesterID, UtilsCommon.GetPointsString(_Points), _Loop);


            m_PathfindingOrdersExecutor.EnqueueOrder(
                    _Requester,
                    new PathfindingOrder(
                            _Requester.RequesterID,
                            _Points,
                            _Loop,
                            _SkipUnpassableTargets,
                            _TryRepositionStartIfOccupied,
                            _TryRepositionTargetIfOccupied,
                            _Smooth,
                            _PerMinBucketSmoothSamples,
                            _CancellationToken,
                            _Timeout,
                            Pathfinder.FindPath,
                            _OnSuccess,
                            _OnFail,
                            _Log: _Log
                        ),
                    _Log
                );
        }

        public string GetStatus(Path _Requester)
        {
            return m_PathfindingOrdersExecutor.GetOrderStatus(_Requester);
        }

        public void UpdateAllBoundsCrossingPaths(Bounds _Bounds)
        {
            if (!m_PathsStorage.TryGetIntersectingCurves(_Bounds, out HashSet<Path> intersectingPaths))
                return;

            intersectingPaths.ForEach(_Path => _Path.Update());
        }

        public void UpdatePathInStorage(Path _Path)
        {
            m_PathsStorage.Update(_Path);
        }

        public void RemovePathFromStorage(Path _Path)
        {
            m_PathsStorage.Unregister(_Path);
        }

        public void DisposePath(Path _Path)
        {
            m_PathfindingOrdersExecutor.TryRemoveOrder(_Path);
            m_PathsStorage.Unregister(_Path);
        }

        #endregion

        #region Service methods

        void RecreateOrdersExecutor(int _MaxAliveOrders)
        {
            m_PathfindingOrdersExecutor = new OrdersExecutor<Path, PathfindingOrder>(_MaxAliveOrders);
        }

        void DrawStorage()
        {
            if (!m_DrawPathsStorage)
                return;

            m_PathsStorage.Draw();
        }

        void DisplayOrdersExecutorStats()
        {
            GUILayout.Label($"Executor pending orders: {m_PathfindingOrdersExecutor.PendingOrders}");
            GUILayout.Label($"Executor alive orders: {m_PathfindingOrdersExecutor.CurrentAliveOrders}/{m_PathfindingOrdersExecutor.MaxAliveOrders}");

            GUILayout.Label(m_PathfindingOrdersExecutor.GetRunningOrdersInfo());
        }

        #endregion

        #region Unity events

        void Awake()
        {
            Doomed = false;
        }

        void OnDestroy()
        {
            Doomed = true;

            Uninitialize(false);
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !enabled)
                return;

            DrawStorage();
        }

        void OnGUI()
        {
            if (!Application.isPlaying || !enabled)
                return;

            if (m_DisplayExecutorStats)
                DisplayOrdersExecutorStats();
        }

        #endregion
    }
}
