using System;
using UnityEngine;

namespace Nav3D.LocalAvoidance.SupportingMath
{
    /// <summary>
    /// Represents straight in space via direction vector and point belonging to line.
    /// </summary>
    public class Straight : SpatialShape, ILine
    {
        #region Attributes

        Vector3 m_Direction;
        Vector3 m_Point;

        #endregion

        #region Properties

        public Vector3 Direction => m_Direction.normalized;
        public Vector3 Point     => m_Point;

        #endregion

        #region Construction

        public Straight(Vector3 _Direction, Vector3 _Point)
        {
            m_Direction = _Direction;
            m_Point     = _Point;

            if (_Direction == Vector3.zero)
                Debug.LogWarning("Direction vector must be nonzero");
        }

        public Straight(Plane _PlaneA, Plane _PlaneB)
        {
            Straight straight = _PlaneA.Intersection(_PlaneB);

            m_Direction = straight.m_Direction;
            m_Point     = straight.m_Point;
        }

        #endregion

        #region SpatialShape methods

        public override Vector3 GetClosestPoint(Vector3 _Point)
        {
            return new Plane(m_Direction, _Point).GetClosestPoint(m_Point);
        }

        #if UNITY_EDITOR
        
        public override void Visualize()
        {
            using (Common.Debug.UtilsGizmos.ColorPermanence)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(m_Point, 0.1f);
                Gizmos.DrawLine(m_Point, m_Point + m_Direction);
                Gizmos.DrawLine(m_Point, m_Point - m_Direction);
            }
        }
        
        #endif

        #endregion

        #region ILine methods

        public Vector3[] Intersection(SpatialShape _Figure)
        {
            if (_Figure is Plane mathPlane)
                return mathPlane.Intersection(this);

            if (_Figure is Sphere mathSphere)
                return mathSphere.Intersection(this);

            throw new NotImplementedException("[Straight] Unknown intersection for type:" + _Figure.GetType().FullName);
        }

        public Vector3 ClosestPoint(Vector3 _Point) => GetClosestPoint(_Point);

        #endregion
    }
}