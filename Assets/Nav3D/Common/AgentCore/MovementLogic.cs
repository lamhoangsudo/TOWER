using System;
using UnityEngine;
using Nav3D.API;
using Nav3D.Common;

namespace Nav3D.Agents
{
    public abstract class MovementLogic : IDisposable
    {
        #region Attributes

        readonly Transform m_AgentTransform;

        protected readonly float m_SqrTargetReachDistance;

        readonly           Action<PathfindingError> m_OnPathfindingFail;
        protected readonly Nav3DAgentMover          m_Mover;

        protected readonly Action<Vector3[]> m_OnPathUpdated;

        protected readonly Log m_Log;

        #endregion

        #region Properties

        public bool IsValid { get; private set; }

        protected abstract bool TryRepositionStartIfOccupied  { get; }
        protected abstract bool TryRepositionTargetIfOccupied { get; }

        public Vector3[] CurrentPath => m_Mover?.CurrentPath;

        #endregion

        #region Constructors

        protected MovementLogic(
                Transform                _AgentTransform,
                Nav3DAgentMover          _Mover,
                float                    _ReachDistance,
                Action<Vector3[]>        _OnPathUpdated,
                Action<PathfindingError> _OnPathfindingFail,
                Log                      _Log
            )
        {
            m_Log = _Log;

            m_AgentTransform    = _AgentTransform;
            m_Mover             = _Mover;
            m_OnPathUpdated     = _OnPathUpdated;
            m_OnPathfindingFail = _OnPathfindingFail;

            m_SqrTargetReachDistance = _ReachDistance * _ReachDistance;

            m_Mover.SetCurrentPosition(_AgentTransform.position);
        }

        #endregion

        #region Public methods

        public virtual void Init()
        {
            IsValid = true;
        }

        public virtual void DoOnFrameStart()
        {
        }

        public virtual void DoOnFrameEnd()
        {
            m_Mover.SetCurrentPosition(m_AgentTransform.position);

            if (!IsValid)
                return;

            if (m_Mover.IsDeviatedFromPathLastFrame())
            {
                DoOnDeviatedFromPath();

                return;
            }

            if (CheckTargetReached())
            {
                DoOnTargetReached();
            }
        }

        public abstract void DoOnDeviatedFromPath();
        public abstract void DoOnTargetReached();

        public abstract Vector3 GetFrameVelocity();

        public virtual void Dispose()
        {
            Invalidate();
            m_Mover.DisposePath();
        }

        #endregion

        #region Service methods

        protected void DoOnPathfindingFailed(PathfindingError _Error)
        {
            if (_Error.Reason == PathfindingResultCode.CANCELLED)
                return;

            Invalidate();

            m_OnPathfindingFail?.Invoke(_Error);
        }

        protected abstract bool CheckTargetReached();

        protected void Invalidate()
        {
            IsValid = false;
        }

        #endregion
    }
}