using System;
using UnityEngine;
using Nav3D.API;
using Nav3D.Common;

namespace Nav3D.Agents
{
    public class TargetFollowingLogic : MovementLogic
    {
        #region Constants

        readonly string LOG_CTOR = $"{nameof(TargetFollowingLogic)}.ctor";

        #endregion

        #region Attributes

        readonly Transform m_Target;

        readonly Action m_OnReach;
        readonly Action m_OnFollowingFailed;

        readonly float m_SqrOffsetUpdate;

        Vector3 m_LastTargetPosition;

        readonly bool m_FollowContinuously;

        bool m_CurrentTargetPositionReached;

        #endregion

        #region Properties

        protected override bool TryRepositionStartIfOccupied => true;
        protected override bool TryRepositionTargetIfOccupied  => m_FollowContinuously;

        #endregion

        #region Constructors

        public TargetFollowingLogic(
                Transform                _AgentTransform,
                Nav3DAgentMover          _Mover,
                Transform                _Target,
                float                    _TargetOffsetUpdate,
                float                    _ReachDistance,
                bool                     _FollowContinuously,
                Action                   _OnReach,
                Action<Vector3[]>        _OnPathUpdated,
                Action                   _OnFollowingFailed,
                Action<PathfindingError> _OnPathfindingFail,
                Log                      _Log = null
            ) :
            base(_AgentTransform, _Mover, _ReachDistance, _OnPathUpdated, _OnPathfindingFail, _Log)
        {
            m_Target             = _Target;
            m_OnReach            = _OnReach;
            m_OnFollowingFailed  = _OnFollowingFailed;
            m_LastTargetPosition = m_Target.position;

            m_SqrOffsetUpdate    = _TargetOffsetUpdate * _TargetOffsetUpdate;
            m_FollowContinuously = _FollowContinuously;

            m_Log?.Write(LOG_CTOR);
        }

        #endregion

        #region Public methods

        public override void Init()
        {
            m_Mover.BeginMoveTo(m_LastTargetPosition, TryRepositionStartIfOccupied, m_OnPathUpdated, DoOnPathfindingFailed);

            base.Init();
        }

        public override void DoOnFrameStart()
        {
            if (!IsValid)
                return;

            if (m_Target == null || !m_Target.gameObject.activeInHierarchy)
            {
                Invalidate();
                m_OnFollowingFailed?.Invoke();
                return;
            }

            if ((m_LastTargetPosition - m_Target.position).sqrMagnitude > m_SqrOffsetUpdate)
            {
                m_LastTargetPosition           = m_Target.position;
                m_CurrentTargetPositionReached = false;

                m_Mover.BeginMoveTo(m_LastTargetPosition, TryRepositionStartIfOccupied, m_OnPathUpdated, DoOnPathfindingFailed);
            }
        }

        public override Vector3 GetFrameVelocity()
        {
            if (!IsValid)
                return Vector3.zero;

            if (m_CurrentTargetPositionReached)
                return Vector3.zero;

            return m_Mover.GetFrameVelocity();
        }

        public override void DoOnTargetReached()
        {
            m_CurrentTargetPositionReached = true;

            if (m_FollowContinuously)
                return;

            m_Mover.DisposePath();
            Dispose();

            m_OnReach?.Invoke();
        }

        public override void DoOnDeviatedFromPath()
        {
            m_Mover.BeginMoveTo(m_LastTargetPosition, TryRepositionStartIfOccupied, m_OnPathUpdated, DoOnPathfindingFailed);
        }

        #endregion

        #region Service methods

        protected override bool CheckTargetReached()
        {
            return (m_Mover.CurrentPosition - m_LastTargetPosition).sqrMagnitude < m_SqrTargetReachDistance;
        }

        #endregion
    }
}