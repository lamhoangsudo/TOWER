using System;
using Nav3D.API;
using Nav3D.LocalAvoidance;
using Nav3D.Common;
using Nav3D.LocalAvoidance.SupportingMath;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nav3D.Agents
{
    public class Nav3DAgentMoverCombined : Nav3DAgentMoverGlobal
    {
        #region Attributes

        DateTime m_LastPathUpdateFireTime;

        bool m_DeviatedFromPath;
        bool m_DeviatedFromPathLastFrame;

        Vector3 m_VPreOptMovers = Vector3.zero;
        Vector3 m_VPreOptStatic = Vector3.zero;

        #endregion

        #region Attributes : Debug

        Vector3 m_LocalAvoidanceVelocity;
        Vector3 m_BlendedVelocity;

        #endregion

        #region Properties

        public override bool Avoiding => !CoDirectionalAvoidanceInterrupt;

        Vector3 VPref => (m_PathNextPoint - m_CurrentPosition).normalized * m_Config.Speed;

        float VelocityDangerDistance => m_Config.VelocityRadius * 2f;

        #endregion

        #region Constructors

        public Nav3DAgentMoverCombined(Vector3 _Position, Nav3DAgentConfig _Config, Nav3DAgent _Agent, Log _Log)
            : base(_Position, _Config, _Agent, _Log)
        {
            InitLocalAvoidanceSolvers();
        }

        #endregion

        #region Public methods

        public override bool IsDeviatedFromPathLastFrame()
        {
            return m_DeviatedFromPathLastFrame;
        }

        public override void UpdateRivalVO(Nav3DAgentMover _RivalMover, VOAgent _RivalVO)
        {
            m_MoversVOsProvider?.UpdateRivalVO(_RivalMover, _RivalVO);
        }

        #if UNITY_EDITOR

        public override void Draw(bool _DrawRadius, bool _DrawPath, bool _DrawVelocities)
        {
            if (_DrawVelocities)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(m_CurrentPosition, m_CurrentPosition + VPref.normalized);

                Gizmos.color = Color.green;
                Gizmos.DrawLine(m_CurrentPosition, m_CurrentPosition + m_LocalAvoidanceVelocity.normalized);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(m_CurrentPosition, m_CurrentPosition + m_BlendedVelocity.normalized);
            }

            base.Draw(_DrawRadius, _DrawPath, _DrawVelocities);
        }

        #endif

        #endregion

        #region Service methods

        protected override Vector3 GetVelocity()
        {
            m_DeviatedFromPathLastFrame = false;

            if (m_Path           == null || m_Path.IsValid           == false ||
                m_CurrFollowData == null || m_CurrFollowData.IsValid == false)
                return LastVelocity = Vector3.zero;

            if (CoDirectionalAvoidanceInterrupt)
            {
                SetAvoidanceInterrupt(false);

                return LastVelocity = Vector3.zero;
            }

            //Update path if agent is moved away from current one far enough
            if (m_Config.AutoUpdatePath && (AgentManager.Now - m_LastPathUpdateFireTime).TotalMilliseconds >
                Mathf.Max(m_Config.PathAutoUpdateCooldown, m_Config.PathfindingTimeout))
            {
                m_CurrFollowData.GetDistToClosestOnPath(m_CurrentPosition, out _, out float magnitude);

                if (magnitude > VelocityDangerDistance)
                {
                    RestartPathFollowing();

                    m_LastPathUpdateFireTime    = AgentManager.Now;
                    m_DeviatedFromPath          = true;
                    m_DeviatedFromPathLastFrame = true;

                    return LastVelocity = Vector3.zero;
                }
            }

            //obtain desirable position on the path
            m_PathNextPoint = m_CurrFollowData.GetMovePoint(m_Config.Speed, m_Config.Radius, m_CurrentPosition);

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

            //desired velocity to follow the global path
            Vector3 pathVelocity = VPref;

            Vector3 resultVelocity;

            //no movers and static obstacles near - move with pref velocity
            if (!moversVOs.Any() && triangleVO == null)
            {
                m_LocalAvoidanceVelocity = Vector3.zero;
                resultVelocity           = Vector3.Slerp(pathVelocity, LastVelocity, 0.1f);
            }
            //no movers near - mix path and obstacles avoidance velocities according to weights
            else if (!moversVOs.Any())
            {
                m_LocalAvoidanceVelocity = m_VPreOptStatic;
                
                resultVelocity = UtilsMath.WeightedVector3Sum2(
                        pathVelocity,
                        m_VPreOptStatic,
                        m_Config.PathVelocityWeight1,
                        m_Config.ObstaclesAvoidanceVelocityWeight1
                    );
            }
            //no obstacles near - mix path and movers avoidance velocities according to weights
            else if (triangleVO == null)
            {
                m_LocalAvoidanceVelocity = m_VPreOptMovers;

                resultVelocity = UtilsMath.WeightedVector3Sum2(
                        pathVelocity,
                        m_VPreOptMovers,
                        m_Config.PathVelocityWeight2,
                        m_Config.AgentsAvoidanceVelocityWeight1
                    );
            }
            //has both movers and obstacles near - mix avoidance velocities and path velocity according to weights
            else
            {
                resultVelocity = UtilsMath.WeightedVector3Sum3(
                        pathVelocity,
                        m_VPreOptMovers,
                        m_VPreOptStatic,
                        m_Config.PathVelocityWeight,
                        m_Config.AgentsAvoidanceVelocityWeight,
                        m_Config.ObstaclesAvoidanceVelocityWeight,
                        out m_LocalAvoidanceVelocity
                    );
            }

            #if UNITY_EDITOR
            m_BlendedVelocity = resultVelocity;
            #endif

            LastVelocity = resultVelocity;

            return resultVelocity;
        }

        protected override void OnTargetPointPassed(Vector3 _Point)
        {
            base.OnTargetPointPassed(_Point);

            if (m_DeviatedFromPath)
            {
                m_DeviatedFromPath = false;
                RestartPathFollowing();
            }
        }

        protected virtual void InitLocalAvoidanceSolvers()
        {
            m_MoversVOsProvider = new MoversVOsProvider(this, m_Config);

            if (m_Config.AvoidStaticObstacles)
                m_TrianglesVOProvider = new StaticTrianglesVOProvider(this, m_Config.Radius, m_Config.MaxSpeed, m_Config.ORCATau, false);
        }

        #endregion
    }
}