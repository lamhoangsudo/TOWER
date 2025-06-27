using Nav3D.Common;
using Nav3D.Obstacles;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nav3D.LocalAvoidance
{
    public class StaticTrianglesVOProvider
    {
        #region Constants

        readonly TimeSpan UPDATE_PERIOD = TimeSpan.FromMilliseconds(500);

        #endregion

        #region Attributes

        readonly List<Triangle> m_NearestTriangles = new List<Triangle>();
        Triangle                m_ConsideringTriangle;

        readonly IMovable m_Mover;

        readonly float m_Radius;
        readonly float m_MaxSpeed;
        readonly float m_ORCATau;
        readonly float m_VelocityRadiusProjected;

        Vector3  m_LastUpdatePosition;
        DateTime m_LastUpdateTime;

        //have to return closest triangle only if intersection occurs or if it is intersecting with velocity radius sphere
        readonly bool m_ReturnIntersecting;

        #endregion

        #region Constructors

        public StaticTrianglesVOProvider(IMovable _Mover, float _Radius, float _MaxSpeed, float _ORCATau, bool _ReturnIntersecting)
        {
            m_Mover              = _Mover;
            m_Radius             = _Radius;
            m_MaxSpeed           = _MaxSpeed;
            m_ORCATau            = _ORCATau;
            m_ReturnIntersecting = _ReturnIntersecting;

            m_VelocityRadiusProjected = m_Radius + m_MaxSpeed * m_ORCATau;
        }

        #endregion

        #region Public methods

        public IVO GetVO()
        {
            Vector3 position = m_Mover.GetPosition();

            IVO resultVO = null;

            TryUpdateNeighborTriangles();

            //update neighbor obstacles if position is changed significantly or time update period passed
            if (DateTime.Now - m_LastUpdateTime > UPDATE_PERIOD && (m_LastUpdatePosition - position).sqrMagnitude > UtilsMath.Sqr(m_MaxSpeed * 0.3f))
            {
                //it's time to update considered obstacles list
                UpdateConsideredTriangles();
            }

            if (m_ConsideringTriangle != null)
            {
                float distToTrianglePlane = m_ConsideringTriangle.Plane.GetDistanceToPoint(position);

                if (distToTrianglePlane > m_Radius)
                {
                    m_ConsideringTriangle = null;
                    //if we finished working with triangle, check if we collided with a new one
                    UpdateConsideredTriangles();
                }
                else
                {
                    resultVO = new VOObstacle(m_Mover, m_ConsideringTriangle, m_ORCATau);
                }
            }

            return resultVO;
        }

        #if UNITY_EDITOR

        public void DrawNearestTriangles()
        {
            AgentManager.Instance.HasNearestObstacles(m_Mover, out Bounds dangerBounds);

            Gizmos.color = Color.yellow;
            dangerBounds.Draw();

            foreach (Triangle triangle in m_NearestTriangles)
            {
                triangle.Visualize(true);
            }

            Gizmos.color = Color.magenta;
            m_ConsideringTriangle?.Visualize(true);
        }

        public void DrawTrianglesUpdateInfo()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(m_LastUpdatePosition, 0.025f);
            Gizmos.DrawWireSphere(m_Mover.GetPosition(), m_MaxSpeed);
        }

        #endif

        #endregion

        #region Service methods

        void TryUpdateNeighborTriangles()
        {
            //update neighbor obstacles list if necessary
            if (!m_Mover.IsNeighborObstaclesDirty)
                return;

            m_NearestTriangles.Clear();

            if (AgentManager.Instance.HasNearestObstacles(m_Mover, out Bounds dangerBounds))
            {
                m_NearestTriangles.AddRange(ObstacleManager.Instance.GetIntersectedObstaclesTriangles(dangerBounds));
            }

            UpdateConsideredTriangles();

            //reset flag
            m_Mover.SetNeighborObstaclesDirty(false);
        }

        void UpdateConsideredTriangles()
        {
            Vector3 moverPosition = m_Mover.GetPosition();

            if (!m_NearestTriangles.Any())
            {
                m_ConsideringTriangle = null;
                return;
            }

            Triangle closestTriangle = m_NearestTriangles.MinBy(_Triangle => (_Triangle.ClosestPointOnTriangle(moverPosition) - moverPosition).sqrMagnitude, out _);

            if (m_ReturnIntersecting)
                m_ConsideringTriangle = closestTriangle.Intersects(moverPosition, m_Radius) ? closestTriangle : null;
            else
                m_ConsideringTriangle = closestTriangle.Intersects(moverPosition, m_VelocityRadiusProjected) ? closestTriangle : null;

            m_LastUpdatePosition = moverPosition;
            m_LastUpdateTime     = DateTime.Now;
        }

        #endregion
    }
}