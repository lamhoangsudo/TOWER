using Nav3D.Common;
using Nav3D.API;
using System.Linq;
using System;
using System.Threading;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using Nav3D.Common.Debug;
#endif

namespace Nav3D.Pathfinding
{
    public class Path : IDisposable, ICurve
    {
        #region Constants

        const int DEFAULT_SEARCH_TIMEOUT = 2000;
        const int DEFAULT_SMOOTH_RATIO   = 3;

        #endregion

        #region Constants : Log

        readonly string LOG_PATH_CTOR    = $"{nameof(Path)}.ctor";
        readonly string LOG_PATH_SUCCESS = $"{nameof(Path)}.{nameof(DoOnSuccess)}: Targets: {{0}}";
        readonly string LOG_PATH_FAIL    = $"{nameof(Path)}.{nameof(DoOnFail)}: Targets: {{0}}";
        readonly string LOG_PATH_DISPOSE = $"{nameof(Path)}.{nameof(Dispose)}";
        readonly string LOG_FIND_PATH    = $"{nameof(Path)}.{nameof(Find)}: Targets: {{0}}";
        readonly string LOG_PATH_UPDATE  = $"{nameof(Path)}.{nameof(Update)}";

        #endregion

        #region Attributes

        readonly string m_RequesterID;
        
        protected Vector3[] m_Targets;
        bool                m_Loop;
        bool                m_SkipUnpassableTargets;
        bool                m_TryRepositionStartIfOccupied;
        bool                m_TryRepositionTargetIfOccupied;

        Action                   m_OnSuccessCached;
        Action<PathfindingError> m_OnFailCached;

        protected int m_SmoothRatio = DEFAULT_SMOOTH_RATIO;
        int           m_Timeout     = DEFAULT_SEARCH_TIMEOUT;

        bool m_Smooth;

        Vector3[] m_TrajectoryOriginal;
        Vector3[] m_TrajectoryOptimized;
        Vector3[] m_TrajectoryFinal;
        int[]     m_TargetIndices;

        Bounds m_PathBounds;

        readonly Action                   m_OnPathfindingSuccessCallback;
        readonly Action<PathfindingError> m_OnPathfindingFailCallback;
        readonly Action                   m_OnPathfindingCompleteCallback;

        readonly Log m_Log;

        protected CancellationTokenSource m_UpdateTokenSource;

        PathFollowData m_LastGivenFollowData;

        #endregion

        #region Properties

        /// <summary>
        /// Final trajectory processed according to pathfinding parameters.
        /// </summary>
        //The internal logic is such that m_TrajectorySmoothed contains the actual trajectory in accordance with all requirements.
        public Vector3[] Trajectory => m_TrajectoryFinal;

        /// <summary>
        /// Initial trajectory obtained after searching for A* in octrees.
        /// </summary>
        public Vector3[] TrajectoryOriginal => m_TrajectoryOriginal;

        /// <summary>
        /// Optimized initial trajectory.
        /// </summary>
        public Vector3[] TrajectoryOptimized => m_TrajectoryOptimized;

        /// <summary>
        /// Smoothed optimized trajectory.
        /// </summary>
        public Vector3[] TrajectorySmoothed => m_TrajectoryFinal;

        /// <summary>
        /// Bounds enclosing all path segments.
        /// </summary>
        public Bounds Bounds => m_PathBounds;

        /// <summary>
        /// Is smoothing procedure applies to the path.
        /// </summary>
        public bool Smooth
        {
            get => m_Smooth;
            set
            {
                if (m_Smooth == value)
                    return;

                m_Smooth = value;
                IsValid  = false;
            }
        }

        /// <summary>
        /// Smooth samples per min bucket volume.
        /// </summary>
        public int SmoothRatio
        {
            get => m_SmoothRatio;
            set
            {
                if (value < 1 || value == m_SmoothRatio)
                    return;

                m_SmoothRatio = value;
            }
        }

        /// <summary>
        /// Max alowed pathfinding process duration.
        /// </summary>
        public int Timeout
        {
            get => m_Timeout;
            set
            {
                if (value < 1 || value == m_Timeout)
                    return;

                m_Timeout = value;
            }
        }

        /// <summary>
        /// Is found path trajectory meets all pathfinding requirements.
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Resulting path represented as Rays sequence.
        /// </summary>
        public Segment3[] Segments { get; private set; }

        public PathfindingResult LastPathfindingResult { get; private set; }
        
        public string RequesterID => m_RequesterID;

        #endregion

        #region Constructors

        public Path(
                string                   _RequesterID,
                Vector3[]                _Targets,
                bool                     _Loop,
                bool                     _SkipUnpassableTargets,
                bool                     _TryRepositionStartIfOccupied,
                bool                     _TryRepositionTargetIfOccupied,
                Action                   _OnPathfindingSucceeded,
                Action<PathfindingError> _OnPathfindingFailed,
                Action                   _OnPathfindingCompleted,
                int?                     _Timeout = null,
                Log                      _Log     = null
            )
        {
            m_RequesterID = _RequesterID;
                
            m_Log = _Log;

            m_OnPathfindingSuccessCallback  = _OnPathfindingSucceeded;
            m_OnPathfindingFailCallback     = _OnPathfindingFailed;
            m_OnPathfindingCompleteCallback = _OnPathfindingCompleted;

            m_Log?.Write(LOG_PATH_CTOR);

            if (_Timeout.HasValue)
                Timeout = _Timeout.Value;

            m_Targets                       = _Targets;
            m_Loop                          = _Loop;
            m_SkipUnpassableTargets         = _SkipUnpassableTargets;
            m_TryRepositionStartIfOccupied  = _TryRepositionStartIfOccupied;
            m_TryRepositionTargetIfOccupied = _TryRepositionTargetIfOccupied;
        }

        ~Path()
        {
            Dispose();
        }

        #endregion

        #region ICurve

        public bool Intersects(Bounds _Bounds)
        {
            if (!IsValid)
                return false;

            foreach (Segment3 segment in Segments)
            {
                if (Bounds.IntersectSegment(segment))
                    return true;
            }

            return false;
        }

        #endregion

        #region Public methods

        public void Find(
                Vector3[]                _Targets                       = null,
                bool?                    _Loop                          = null,
                bool?                    _SkipUnpassableTargets         = null,
                bool?                    _TryRepositionStartIfOccupied  = null,
                bool?                    _TryRepositionTargetIfOccupied = null,
                bool?                    _Smooth                        = null,
                int?                     _SmoothRatio                   = null,
                int?                     _Timeout                       = null,
                Action                   _OnSuccess                     = null,
                Action<PathfindingError> _OnFail                        = null
            )
        {
            m_Log?.WriteFormat(LOG_FIND_PATH, UtilsCommon.GetPointsString(m_Targets));

            m_UpdateTokenSource?.Cancel();
            m_UpdateTokenSource = new CancellationTokenSource();

            if (_Timeout.HasValue)
                Timeout = _Timeout.Value;

            if (_Targets != null && !m_Targets.SequenceEqual(_Targets))
            {
                m_Targets = _Targets;
                IsValid = false;
            }

            if (_Smooth.HasValue)
                Smooth = _Smooth.Value;

            if (_SmoothRatio.HasValue)
                SmoothRatio = _SmoothRatio.Value;

            if (_Loop.HasValue)
                m_Loop = _Loop.Value;

            if (_SkipUnpassableTargets.HasValue)
                m_SkipUnpassableTargets = _SkipUnpassableTargets.Value;

            if (_TryRepositionStartIfOccupied.HasValue)
                m_TryRepositionStartIfOccupied = _TryRepositionStartIfOccupied.Value;

            if (_TryRepositionTargetIfOccupied.HasValue)
                m_TryRepositionTargetIfOccupied = _TryRepositionTargetIfOccupied.Value;

            m_OnSuccessCached = _OnSuccess;
            m_OnFailCached    = _OnFail;

            PerformPathfinding(_OnSuccess, _OnFail);
        }

        public void Update()
        {
            m_Log?.Write(LOG_PATH_UPDATE);

            m_UpdateTokenSource?.Cancel();
            m_UpdateTokenSource = new CancellationTokenSource();

            PerformPathfinding(m_OnSuccessCached, m_OnFailCached);
        }

        public bool GetFollowData(
                Action<Vector3>    _OnTargetPassed,
                Action             _OnLastTargetReached,
                Vector3            _FollowerPosition,
                float              _ReachDist,
                out PathFollowData _FollowData
            )
        {
            if (!IsValid)
            {
                _FollowData = null;
                return false;
            }

            _FollowData = m_LastGivenFollowData =
                              new PathFollowData(
                                  m_TrajectoryFinal,
                                  m_TargetIndices,
                                  _FollowerPosition,
                                  _ReachDist,
                                  _OnTargetPassed,
                                  _OnLastTargetReached,
                                  m_Log);
            return true;
        }

        public void Dispose()
        {
            m_LastGivenFollowData?.Invalidate();
            m_UpdateTokenSource.Cancel();

            if (!PathfindingManager.Doomed)
                PathfindingManager.Instance.DisposePath(this);

            m_Log?.Write(LOG_PATH_DISPOSE);
        }

        public string GetPathfindingStatus()
        {
            if (!PathfindingManager.Doomed)
                return PathfindingManager.Instance.GetStatus(this);

            return string.Empty;
        }

        #if UNITY_EDITOR

        public void Draw()
        {
            if (m_TrajectoryFinal == null)
                return;

            const float TARGET_RADIUS = 0.1f;

            using (UtilsGizmos.ColorPermanence)
            {
                Gizmos.color = Color.cyan;

                for (int i = 1; i < m_TrajectoryFinal.Length; i++)
                {
                    Gizmos.DrawLine(m_TrajectoryFinal[i - 1], m_TrajectoryFinal[i]);
                }

                GUIStyle style = new GUIStyle
                {
                    normal =
                    {
                        textColor = Gizmos.color
                    }
                };

                for (int i = 0; i < m_Targets.Length; i++)
                {
                    Vector3 target = m_Targets[i];
                    Gizmos.DrawWireSphere(target, TARGET_RADIUS);
                    Handles.Label(target, $"[{i}]", style);
                }
            }
        }

        #endif

        #endregion

        #region Service methods

        protected virtual void PerformPathfinding(Action _OnSuccess, Action<PathfindingError> _OnFail)
        {
            PathfindingManager.Instance.RequestPathfinding(
                    this,
                    m_Targets,
                    m_Loop,
                    m_SkipUnpassableTargets,
                    m_TryRepositionStartIfOccupied,
                    m_TryRepositionTargetIfOccupied,
                    m_Smooth,
                    m_SmoothRatio,
                    m_UpdateTokenSource.Token,
                    Timeout,
                    _Result => DoOnSuccess(_Result, _OnSuccess),
                    _Error => DoOnFail(_Error, _OnFail),
                    _Log: m_Log
                );
        }

        void CleanCachedCallbacks()
        {
            m_OnSuccessCached = null;
            m_OnFailCached    = null;
        }

        protected virtual void DoOnSuccess(PathfindingResult _Result, Action _OnSuccess)
        {
            m_LastGivenFollowData?.Invalidate();

            LastPathfindingResult = _Result;

            m_TrajectoryOriginal  = _Result.RawPath;
            m_TrajectoryOptimized = _Result.PathOptimized;
            m_TrajectoryFinal     = _Result.PathSmoothed;
            m_TargetIndices        = _Result.TargetIndices;

            m_PathBounds = ExtensionBounds.PointBounds(m_TrajectoryFinal);
            Segments     = UtilsMath.PointSequenceToSegments(m_TrajectoryFinal);

            IsValid = true;

            PathfindingManager.Instance.UpdatePathInStorage(this);

            m_Log?.WriteFormat(LOG_PATH_SUCCESS, UtilsCommon.GetPointsString(m_Targets));

            ThreadDispatcher.BeginInvoke(_OnSuccess);

            CleanCachedCallbacks();

            ThreadDispatcher.BeginInvoke(m_OnPathfindingSuccessCallback.Invoke);
            ThreadDispatcher.BeginInvoke(m_OnPathfindingCompleteCallback.Invoke);
        }

        protected void DoOnFail(PathfindingError _Error, Action<PathfindingError> _OnFail)
        {
            m_Log?.WriteFormat(LOG_PATH_FAIL, UtilsCommon.GetPointsString(m_Targets));

            IsValid = false;

            PathfindingManager.Instance.RemovePathFromStorage(this);

            if (_OnFail != null)
                ThreadDispatcher.BeginInvoke(() => _OnFail.Invoke(_Error));

            CleanCachedCallbacks();

            if (m_OnPathfindingFailCallback != null)
                ThreadDispatcher.BeginInvoke(() => m_OnPathfindingFailCallback.Invoke(_Error));

            ThreadDispatcher.BeginInvoke(m_OnPathfindingCompleteCallback.Invoke);
        }

        #endregion
    }
}
