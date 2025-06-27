using System;
using System.Linq;
using Nav3D.Common;
using UnityEngine;
using Nav3D.Pathfinding;

namespace Nav3D.API
{
    public class Nav3DPath : IDisposable
    {
        #region Constants
        
        const string TARGETS_LIST_IS_EMPTY_ERROR = "Error. Targets array is empty!";

        #endregion

        #region Events

        public event Action                   OnPathfindingSuccess;
        public event Action<PathfindingError> OnPathfindingFail;
        public event Action                   OnPathfindingComplete;

        #endregion

        #region Atributes

        Path m_Path;

        readonly string m_RequesterID;
        readonly Log m_Log;

        bool m_Smooth;
        int  m_SmoothRatio;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the pathfinding process was completed successfully using the current pathfinding parameters.
        /// Respectively changing any pathfinding setting will invalidate path instance.
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Whether the last pathfinding request still performing.
        /// </summary>
        public bool IsPathfindingInProgress { get; private set; }
        
        /// <summary>
        /// The last pathfinding resulting trajectory. 
        /// </summary>
        public Vector3[] Trajectory => m_Path?.Trajectory;

        /// <summary>
        /// The bounds of the volume that embraces all path geometry.
        /// </summary>
        public Bounds? Bounds => m_Path?.Bounds;

        /// <summary>
        /// Is smoothing applies to the found path trajectory.
        /// </summary>
        public bool Smooth
        {
            get => m_Smooth;
            set
            {
                if (m_Smooth == value)
                    return;

                m_Smooth = value;
                Invalidate();
            }
        }

        /// <summary>
        /// The number of the smoothing samples per one min. bucket size volume.
        /// </summary>
        public int SmoothRatio
        {
            get => m_SmoothRatio;
            set
            {
                if (m_SmoothRatio == value)
                    return;

                m_SmoothRatio = value;

                if (m_Smooth)
                    Invalidate();
            }
        }

        /// <summary>
        /// The time limit for pathfinding process duration.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Whether to try to move start point to the free space if it is inside of occupied space
        /// </summary>
        public bool TryRepositionStartIfOccupied { get; set; }
        /// <summary>
        /// Whether to try to move target point to the free space if it is inside of occupied space
        /// </summary>
        public bool TryRepositionTargetIfOccupied  { get; set; }

        /// <summary>
        /// The result of the last successful pathfinding.
        /// </summary>
        public PathfindingResult LastPathfindingResult => m_Path?.LastPathfindingResult;

        #endregion

        #region Constructors
        
        public Nav3DPath(string _RequesterID = null)
        {
            m_RequesterID = _RequesterID ?? Guid.NewGuid().ToString();
        }
        
        public Nav3DPath(string _RequesterID, Log _Log = null)
        {
            m_RequesterID = _RequesterID;
            m_Log = _Log;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Starts pathfinding from A to B.
        /// </summary>
        public void Find(
                Vector3                  _Start,
                Vector3                  _End,
                Action                   _OnSuccess = null,
                Action<PathfindingError> _OnFail    = null
            )
        {
            if (!Nav3DManager.Inited)
                throw new Nav3DManager.Nav3DManagerNotInitializedException();

            InitPath(new[] { _Start, _End }, false, false, TryRepositionStartIfOccupied, TryRepositionTargetIfOccupied, _OnSuccess, _OnFail);
        }

        /// <summary>
        /// Starts pathfinding through all target points.
        /// </summary>
        public void Find(
                Vector3[]                _Targets,
                bool                     _Loop,
                bool                     _SkipUnpassableTargets = false,
                Action                   _OnSuccess             = null,
                Action<PathfindingError> _OnFail                = null
            )
        {
            if (!Nav3DManager.Inited)
                throw new Nav3DManager.Nav3DManagerNotInitializedException();

            if (_Targets == null || !_Targets.Any())
                throw new ArgumentException(TARGETS_LIST_IS_EMPTY_ERROR);

            InitPath(_Targets, _Loop, _SkipUnpassableTargets, TryRepositionStartIfOccupied, TryRepositionTargetIfOccupied, _OnSuccess, _OnFail);
        }

        /// <summary>
        /// Perform pathfinding using recent conditions. 
        /// </summary>
        public void Update()
        {
            m_Path?.Update();
        }
        
        public void Dispose()
        {
            m_Path.Dispose();
            m_Path = null;
        }

        public string GetPathfindingStatus()
        {
            return m_Path?.GetPathfindingStatus();
        }
        
        public bool TryGetFollowData(
            Action<Vector3>    _OnTargetPassed,
            Action             _OnLastTargetReached,
            Vector3            _FollowerPosition,
            float              _ReachDist,
            out PathFollowData _FollowData)
        {
            if (!IsValid)
            {
                _FollowData = null;
                return false;
            }

            return m_Path.GetFollowData(_OnTargetPassed, _OnLastTargetReached, _FollowerPosition, _ReachDist, out _FollowData);
        }

        #if UNITY_EDITOR

        public void Draw()
        {
            m_Path?.Draw();
        }

        #endif

        #endregion

        #region Service methods

        void Invalidate()
        {
            IsValid = false;
        }

        void OnPathfindingSucceed()
        {
            IsPathfindingInProgress = false;
            IsValid                 = true;
        }

        void InvokeOnPathfindingSuccess()
        {
            OnPathfindingSuccess?.Invoke();
        }

        void InvokeOnPathfindingFail(PathfindingError _Error)
        {
            OnPathfindingFail?.Invoke(_Error);
        }

        void InvokeOnPathfindingComplete()
        {
            OnPathfindingComplete?.Invoke();
        }

        void InitPath(
                Vector3[]                _Targets,
                bool                     _Loop,
                bool                     _SkipUnpassableTargets,
                bool                     _TryRepositionStartIfOccupied,
                bool                     _TryRepositionTargetIfOccupied,
                Action                   _OnSuccess = null,
                Action<PathfindingError> _OnFail    = null
            )
        {
            IsPathfindingInProgress = true;

            (m_Path ??= new Path(
                    m_RequesterID,
                    _Targets,
                    _Loop,
                    _SkipUnpassableTargets,
                    _TryRepositionStartIfOccupied,
                    _TryRepositionTargetIfOccupied,
                    InvokeOnPathfindingSuccess,
                    InvokeOnPathfindingFail,
                    InvokeOnPathfindingComplete,
                    Timeout,
                    m_Log
                )).Find(
                    _Targets,
                    _Loop,
                    _SkipUnpassableTargets,
                    _Smooth: Smooth,
                    _SmoothRatio: SmoothRatio,
                    _OnSuccess: OnPathfindingSucceed + _OnSuccess,
                    _OnFail: _FailArgs =>
                    {
                        IsPathfindingInProgress = false;
                        Invalidate();
                        _OnFail?.Invoke(_FailArgs);
                    }
                );
        }

        #endregion
    }
}