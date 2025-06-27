using Nav3D.Common;
using Nav3D.LocalAvoidance;
using Nav3D.LocalAvoidance.SupportingMath;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Plane = Nav3D.LocalAvoidance.SupportingMath.Plane;

namespace Nav3D.Agents
{
    public class EvaderLocalAvoidanceSolver
    {
        #region Attributes

        readonly StaticTrianglesVOProvider m_TrianglesVOProvider;

        readonly HashSet<IMovable> m_NearestMovers = new HashSet<IMovable>();

        readonly Nav3DEvaderMover m_Mover;

        readonly float m_Radius;
        readonly float m_MaxSpeed;
        readonly float m_ORCATau;
        readonly float m_SpeedDecayFactor;

        #endregion

        #region Constructors

        public EvaderLocalAvoidanceSolver(Nav3DEvaderMover _Mover, float _Radius, float _MaxSpeed, float _ORCATau, float _SpeedDecayFactor)
        {
            m_Radius           = _Radius;
            m_MaxSpeed         = _MaxSpeed;
            m_ORCATau          = _ORCATau;
            m_SpeedDecayFactor = _SpeedDecayFactor;
            m_Mover            = _Mover;

            m_TrianglesVOProvider = new StaticTrianglesVOProvider(_Mover, _Radius, _MaxSpeed, _ORCATau, true);
        }

        #endregion

        #region Public methods

        public Vector3 ResolveVelocity()
        {
            Vector3 position = m_Mover.GetPosition();

            TryUpdateNeighborMovers();

            Vector3 avoidingSumVector = Vector3.zero;

            List<IVO> nearestVOs = new List<IVO>();

            foreach (IMovable otherMovable in m_NearestMovers)
            {
                //skip self mover
                if (otherMovable == m_Mover)
                    continue;

                float   otherRadius = otherMovable.GetRadius();
                float   radiusSum   = m_Radius + otherRadius;
                Vector3 inVector    = position - otherMovable.GetPosition();
                float   dist        = inVector.magnitude;

                //check if there is no collision between movables 
                if (dist > radiusSum)
                    continue;

                avoidingSumVector += inVector * (radiusSum / dist);

                VOAgent vOAgent = new VOAgent(m_Mover, otherMovable, m_ORCATau);
                vOAgent.ComputeVO();
                nearestVOs.Add(vOAgent);
            }

            IVO trianglesVO = m_TrianglesVOProvider.GetVO();

            if (trianglesVO != null)
            {
                Plane ORCAPlane = trianglesVO.GetORCA();
                float distance  = m_Radius - ORCAPlane.DistanceToPoint(position);
                avoidingSumVector += ORCAPlane.Normal * (Mathf.Sign(distance) > 0 ? distance : -distance);
                nearestVOs.Add(trianglesVO);
            }

            Vector3 newVelocity = Vector3.zero;

            if (nearestVOs.Any())
            {
                int         VOCount = nearestVOs.Count;
                List<Plane> ORCAs   = new List<Plane>(nearestVOs.Select(_VO => _VO.GetORCA()));

                newVelocity = LPSolver.Instance.SolveMax(
                        ORCAs,
                        new Sphere(Vector3.zero, m_MaxSpeed),
                        (avoidingSumVector / VOCount).normalized * m_MaxSpeed * (m_MaxSpeed / m_Radius)
                    );
            }
            else
            {
                Vector3 lastVelocity = m_Mover.GetLastFrameVelocity();

                if (lastVelocity != Vector3.zero)
                    newVelocity = Vector3.Lerp(lastVelocity, Vector3.zero, m_SpeedDecayFactor);
            }

            return newVelocity;
        }

        #if UNITY_EDITOR

        public void DrawNearestMovers()
        {
            Gizmos.color = Color.red;

            foreach (IMovable mover in m_NearestMovers)
            {
                Gizmos.DrawLine(m_Mover.GetPosition(), mover.GetPosition());
                Gizmos.DrawWireSphere(mover.GetPosition(), mover.GetRadius());
            }
        }

        public void DrawNearestTriangles()
        {
            m_TrianglesVOProvider.DrawNearestTriangles();
        }

        public void DrawTrianglesUpdateInfo()
        {
            m_TrianglesVOProvider.DrawTrianglesUpdateInfo();
        }

        #endif

        #endregion

        #region Service methods

        void TryUpdateNeighborMovers()
        {
            //update neighbor movers list if necessary
            if (!m_Mover.IsNeighborMoversDirty)
                return;

            m_NearestMovers.Clear();
            m_NearestMovers.AddRange(AgentManager.Instance.GetBucketMovables(m_Mover.GetPosition()));

            //reset flag
            m_Mover.SetNeighborMovablesDirty(false);
        }

        #endregion
    }
}