using System;
using System.Text;
using Nav3D.API;
using Nav3D.Common;
using Nav3D.LocalAvoidance;
using Nav3D.LocalAvoidance.SupportingMath;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nav3D.Agents
{
    class Nav3DAgentMoverLocal : Nav3DAgentMover
    {
        #region Constants

        readonly string LOG_BEGIN_MOVE_TO_POINT  = $"{nameof(Nav3DAgentMoverLocal)}.{nameof(BeginMoveTo)}: {{0}}";
        readonly string LOG_BEGIN_MOVE_TO_POINTS = $"{nameof(Nav3DAgentMoverLocal)}.{nameof(BeginMoveToPoints)}: {{0}}";

        readonly string LOG_RESTART_FOLLOWING = $"{nameof(Nav3DAgentMoverGlobal)}.{nameof(ReinitPathFollowingData)}";
        
        #endregion

        #region Attributes

        int m_CurTargetIndex;

        Vector3 m_CurTarget;

        Vector3 m_LocalAvoidanceVelocity;
        Vector3 m_VPreOptMovers = Vector3.zero;
        Vector3 m_VPreOptStatic = Vector3.zero;

        #endregion

        #region Properties

        public override bool Avoiding => !CoDirectionalAvoidanceInterrupt;

        Vector3 VPref => (m_CurTarget - m_CurrentPosition).normalized * m_Config.Speed;

        public override Vector3[] CurrentPath => new[] { m_CurrentPosition, m_CurTarget };

        #endregion

        #region Constructors

        public Nav3DAgentMoverLocal(Vector3 _Position, Nav3DAgentConfig _Config, Nav3DAgent _Agent, Log _Log)
            : base(_Position, _Config, _Agent, _Log)
        {
            InitLocalAvoidanceSolvers();
        }

        #endregion

        #region Public methods

        public override void BeginMoveTo(Vector3 _Point, bool _TryRepositionStartIfOccupied, Action<Vector3[]> _OnPathUpdated, Action<PathfindingError> _OnPathfindingFail = null)
        {
            if (m_Config.UseLog)
                m_Log.WriteFormat(LOG_BEGIN_MOVE_TO_POINT, _Point.ToStringExt());

            m_Targets = new[] { _Point };

            m_OnPathUpdated = _OnPathUpdated;

            m_CurTargetIndex = 0;
            m_CurTarget      = m_Targets[m_CurTargetIndex];
        }

        public override void BeginMoveToPoints(
                Vector3[]                _Points,
                bool                     _Loop,
                bool                     _SkipUnpassableTargets,
                bool                     _TryRepositionStartIfOccupied,
                Action<Vector3[]>        _OnPathUpdated,
                Action<Vector3>          _OnTargetPassed     = null,
                Action                   _OnLastPointReached = null,
                Action<PathfindingError> _OnPathfindingFail  = null
            )
        {
            if (m_Config.UseLog)
                m_Log.WriteFormat(LOG_BEGIN_MOVE_TO_POINTS, UtilsCommon.GetPointsString(_Points));

            m_Targets             = _Points;
            m_OnTargetPassed      = _OnTargetPassed;
            m_OnLastTargetReached = _OnLastPointReached;

            m_OnPathUpdated = _OnPathUpdated;

            m_CurTargetIndex = 0;
            m_CurTarget      = m_Targets[m_CurTargetIndex];
        }

        public override void ReinitPathFollowingData()
        {
            m_Log?.Write(LOG_RESTART_FOLLOWING);
            
            m_CurTargetIndex = 0;
            m_CurTarget      = m_Targets[m_CurTargetIndex];
        }

        public override void UpdateRivalVO(Nav3DAgentMover _RivalMover, VOAgent _RivalVO)
        {
            m_MoversVOsProvider?.UpdateRivalVO(_RivalMover, _RivalVO);
        }

        public override void ReinitPath()
        {
        }

        public override string GetInnerStatusString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("Status:");
            stringBuilder.AppendLine($"Mover type: {nameof(Nav3DAgentMoverLocal)}");

            return stringBuilder.ToString();
        }
        
        #if UNITY_EDITOR

        public override void Draw(bool _DrawRadius, bool _DrawPath, bool _DrawVelocities)
        {
            if (_DrawPath)
            {
                const float TARGET_RADIUS = 0.1f;
                
                Gizmos.color = Color.cyan;

                for (int i = 1; i < m_Targets.Length; i++)
                {
                    Gizmos.DrawLine(m_Targets[i - 1], m_Targets[i]);
                }

                foreach (Vector3 target in m_Targets)
                {
                    Gizmos.DrawWireSphere(target,TARGET_RADIUS);
                }
            }

            if (_DrawVelocities)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(m_CurrentPosition, m_CurrentPosition + VPref.normalized);

                Gizmos.color = Color.green;
                Gizmos.DrawLine(m_CurrentPosition, m_CurrentPosition + m_LocalAvoidanceVelocity.normalized);
            }
            
            base.Draw(_DrawRadius, _DrawPath, _DrawVelocities);
        }
        
        public override void SetCurrentPosition(Vector3 _Position)
        {
            base.SetCurrentPosition(_Position);

            m_OnPathUpdated?.Invoke(new[] { m_CurrentPosition, m_CurTarget });
        }

        #endif

        #endregion

        #region Service methods
        
        protected override Vector3 GetVelocity()
        {
            if (CoDirectionalAvoidanceInterrupt)
            {
                SetAvoidanceInterrupt(false);

                return LastVelocity = Vector3.zero;
            }

            IVO triangleVO = null;

            if (m_Config.AvoidStaticObstacles)
                triangleVO = m_TrianglesVOProvider.GetVO();

            List<VOAgent> moversVOs = m_MoversVOsProvider.GetVOs();

            if (CoDirectionalAvoidanceInterrupt)
                return LastVelocity = Vector3.zero;

            if (moversVOs.Any())
            {
                List<Nav3D.LocalAvoidance.SupportingMath.Plane> moversPlanes = new List<Nav3D.LocalAvoidance.SupportingMath.Plane>(moversVOs.Count);

                moversPlanes.AddRange(moversVOs.Select(_VO => _VO.GetORCA()));

                if (m_VPreOptMovers == Vector3.zero)
                    m_VPreOptMovers = VPref;
                
                Vector3 moversAvoidingVelocity = LPSolver.Instance.SolveMax(moversPlanes, new Sphere(Vector3.zero, m_Config.MaxSpeed), m_VPreOptMovers);
                
                if (moversAvoidingVelocity.sqrMagnitude > m_Config.SqrMaxSpeed)
                    moversAvoidingVelocity = Vector3.ClampMagnitude(moversAvoidingVelocity, m_Config.MaxSpeed);

                m_VPreOptMovers = moversAvoidingVelocity;
            }
            else
            {
                m_VPreOptMovers = VPref;
            }

            if (triangleVO != null)
            {
                if (m_VPreOptStatic == Vector3.zero)
                    m_VPreOptStatic = VPref;

                Vector3 triangleAvoidingVelocity = LPSolver.Instance.SolveMax(
                        new List<LocalAvoidance.SupportingMath.Plane> { triangleVO.GetORCA() },
                        new Sphere(Vector3.zero, m_Config.MaxSpeed),
                        m_VPreOptStatic
                    );

                if (triangleAvoidingVelocity.sqrMagnitude > m_Config.SqrMaxSpeed)
                    triangleAvoidingVelocity = Vector3.ClampMagnitude(triangleAvoidingVelocity, m_Config.MaxSpeed);

                m_VPreOptStatic = triangleAvoidingVelocity;
            }
            else
            {
                m_VPreOptStatic = VPref;
            }
            
            Vector3 resultVelocity;

            //no movers and static obstacles near - move with pref velocity
            if (!moversVOs.Any() && triangleVO == null)
                resultVelocity = VPref;
            //no movers near - move with obstacles avoidance velocity
            else if (!moversVOs.Any())
                resultVelocity = m_VPreOptStatic;
            //no obstacles near - move with movers avoidance velocity
            else if (triangleVO == null)
                resultVelocity = m_VPreOptMovers;
            //has both movers and obstacles near - mix avoidance velocities according to weights
            else
            {
                resultVelocity = UtilsMath.WeightedVector3Sum2(
                        m_VPreOptMovers,
                        m_VPreOptStatic,
                        m_Config.AgentsAvoidanceVelocityWeight2,
                        m_Config.ObstaclesAvoidanceVelocityWeight2
                    );
            }

            #if UNITY_EDITOR
            m_LocalAvoidanceVelocity = resultVelocity;
            #endif
            
            LastVelocity = resultVelocity;
            
            //Check if any target point passed
            CheckTargetPassed();
            
            return resultVelocity;
        }

        void CheckTargetPassed()
        {
            if ((m_CurTarget - m_CurrentPosition).sqrMagnitude > Mathf.Max(m_Config.TargetReachDistanceSqr, UtilsMath.Sqr(m_Config.Speed)))
                return;

            m_OnTargetPassed?.Invoke(m_CurTarget);

            m_CurTargetIndex++;

            if (m_CurTargetIndex == m_Targets.Length)
            {
                m_OnLastTargetReached?.Invoke();
                return;
            }

            m_CurTarget = m_Targets.Length > m_CurTargetIndex ? m_Targets[m_CurTargetIndex] : m_CurTarget;
        }

        void InitLocalAvoidanceSolvers()
        {
            m_MoversVOsProvider = new MoversVOsProvider(this, m_Config);

            if (m_Config.AvoidStaticObstacles)
                m_TrianglesVOProvider = new StaticTrianglesVOProvider(this, m_Config.Radius, m_Config.MaxSpeed, m_Config.ORCATau, false);
        }

        #endregion
    }
}