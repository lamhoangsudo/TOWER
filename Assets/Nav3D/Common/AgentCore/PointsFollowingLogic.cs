using System;
using UnityEngine;
using Nav3D.API;
using Nav3D.Common;

namespace Nav3D.Agents
{
    public class PointsFollowingLogic : MovementLogic
    {
        #region Constants

        readonly string LOG_CTOR              = $"{nameof(PointsFollowingLogic)}.ctor";
        readonly string LOG_ON_TARGET_REACHED = $"{nameof(PointsFollowingLogic)}.{nameof(DoOnTargetReached)}, Loop:{{0}}";

        #endregion

        #region Attributes

        readonly Vector3[] m_Points;

        readonly Action<Vector3> m_OnPointPassed;
        readonly Action          m_OnFinished;

        readonly bool m_Loop;
        readonly bool m_SkipUnpassableTargets;
        readonly bool m_TryRepositionTargetIfOccupied;

        bool m_IsLastPointReached;
        bool m_IsAnyTargetPassed; //Except the first one because movement take start from it

        #endregion

        #region Properties

        protected override bool TryRepositionStartIfOccupied  => !m_Loop;
        protected override bool TryRepositionTargetIfOccupied => m_TryRepositionTargetIfOccupied;

        #endregion

        #region Constructors

        public PointsFollowingLogic(
                Transform                _AgentTransform,
                Nav3DAgentMover          _Mover,
                Vector3[]                _Points,
                bool                     _Loop,
                bool                     _SkipUnpassableTargets,
                bool                     _TryRepositionTargetIfOccupied,
                float                    _ReachDistance,
                Action<Vector3>          _OnPointPassed,
                Action                   _OnFinished,
                Action<Vector3[]>        _OnPathUpdated,
                Action<PathfindingError> _OnPathfindingFail,
                Log                      _Log = null
            )
            : base(_AgentTransform, _Mover, _ReachDistance, _OnPathUpdated, _OnPathfindingFail, _Log)
        {
            m_Points = _Points;

            m_OnPointPassed = _OnPointPassed;
            m_OnFinished    = _OnFinished;

            m_Loop                          = _Loop;
            m_SkipUnpassableTargets         = _SkipUnpassableTargets;
            m_TryRepositionTargetIfOccupied = _TryRepositionTargetIfOccupied;

            m_IsAnyTargetPassed = false;

            m_Log?.Write(LOG_CTOR);
        }

        #endregion

        #region Public methods

        public override void Init()
        {
            m_Mover.BeginMoveToPoints(
                    m_Points,
                    m_Loop,
                    m_SkipUnpassableTargets,
                    TryRepositionStartIfOccupied,
                    m_OnPathUpdated,
                    OnTargetPassed,
                    LastPointReached,
                    DoOnPathfindingFailed
                );

            base.Init();
        }

        public override Vector3 GetFrameVelocity()
        {
            if (!IsValid)
                return Vector3.zero;

            return m_Mover.GetFrameVelocity();
        }

        public override void DoOnDeviatedFromPath()
        {
        }

        public override void DoOnTargetReached()
        {
            m_Log?.WriteFormat(LOG_ON_TARGET_REACHED, m_Loop);

            if (m_Loop)
            {
                m_Mover.ReinitPathFollowingData();

                m_IsLastPointReached = false;
            }
            else
            {
                m_Mover.DisposePath();
                Dispose();
                
                m_OnFinished?.Invoke();
            }
        }

        #endregion

        #region Service methods

        void OnTargetPassed(Vector3 _Target)
        {
            //if we have loop case, and we passed any target first time
            if (m_Loop && m_Points.Length > 2 && _Target != m_Points[0] && !m_IsAnyTargetPassed)
            {
                m_Mover.ReinitPath();
                m_IsAnyTargetPassed = true;
            }

            m_OnPointPassed?.Invoke(_Target);
        }

        void LastPointReached()
        {
            m_IsLastPointReached = true;
        }

        protected override bool CheckTargetReached()
        {
            return m_IsLastPointReached;
        }

        #endregion
    }
}