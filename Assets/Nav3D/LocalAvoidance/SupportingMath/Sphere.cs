using Nav3D.Common;
using System;
using UnityEngine;

namespace Nav3D.LocalAvoidance.SupportingMath
{
    public class Sphere : SpatialShape
    {
        #region Attributes

        Vector3 m_Center;
        float m_Radius;
        float m_SqrRadius;

        #endregion

        #region Properties

        public Vector3 Center => m_Center;
        public float Radius
        {
            get => m_Radius;
            set
            {
                if (value <= 0)
                    return;

                m_Radius = value;
                m_SqrRadius = 0;
            }
        }
        public float SqrRadius => m_SqrRadius == 0 ? m_SqrRadius = m_Radius * m_Radius : m_SqrRadius;
        public Sphere ZeroFlipped => new Sphere(-m_Center, m_Radius);

        #endregion

        #region Construction

        public Sphere(Vector3 _Center, float _Radius)
        {
            m_Center = _Center;
            m_Radius = _Radius;
        }

        #endregion

        #region Public methods

        public Vector3 GetClosestSurfaceNormal(Vector3 _Point)
        {
            return (_Point - m_Center).normalized;
        }

        public override Vector3 GetClosestPoint(Vector3 _Point)
        {
            if (m_Center == _Point)
                throw new ArgumentException("Point must be unequal to the sphere center");

            return m_Center + (_Point - m_Center).normalized * m_Radius;
        }

        public bool IsPointInside(Vector3 _Point)
        {
            return Vector3.SqrMagnitude(m_Center - _Point) <= SqrRadius + 0.1f;
        }

        public Vector3[] Intersection(ILine _Line)
        {
            if (_Line is Straight straight)
            {
                Vector3 onStraightPoint = straight.GetClosestPoint(m_Center);
                float heightSqr = (onStraightPoint - m_Center).sqrMagnitude;
                float offset = Mathf.Sqrt(SqrRadius - heightSqr);
                return new Vector3[] {
                    onStraightPoint + straight.Direction * offset,
                    onStraightPoint - straight.Direction * offset
                };
            }

            if (_Line is Circle circle)
            {
                Circle circleProj = new Circle(circle.GeneratrixPlane, this);
                Vector3 radiusVector = circleProj.Center - circle.Center;
                float centersDelta = radiusVector.magnitude;
                float h = 2 * UtilsMath.TriangleHeronSquare(circle.Radius, circleProj.Radius, centersDelta) / centersDelta;
                float radiusShift = Mathf.Sqrt(circle.SqrRadius - h * h);

                Vector3 pivotPoint = circle.Center + radiusVector.normalized * radiusShift;
                Vector3 orthoVector = Vector3.Cross(circle.GeneratrixPlane.Normal, radiusVector).normalized;

                return new Vector3[]
                {
                    pivotPoint + orthoVector * h,
                    pivotPoint - orthoVector * h
                };
            }

            throw new NotImplementedException("[Sphere] Unknown intersection for type:" + _Line.GetType().FullName);
        }

        #endregion

        #region SpatialShape methods

#if UNITY_EDITOR
        public override void Visualize()
        {
            using (Common.Debug.UtilsGizmos.ColorPermanence)
            {

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(m_Center, m_Radius);

            }
        }
#endif

        public override IntersectionType CheckIntersection(ILine _Line)
        {
            if (_Line is Straight straight)
            {
                Vector3 closestOnStraightPoint = straight.GetClosestPoint(m_Center);
                float sqrDistToCenter = Vector3.SqrMagnitude(m_Center - closestOnStraightPoint);

                return sqrDistToCenter < SqrRadius ? IntersectionType.INTERSECTION : IntersectionType.NONINTERSECTION;
            }

            if (_Line is Circle circle)
            {
                if (this == circle.GeneratrixSphere)
                    return IntersectionType.BELONGING;

                if (IsIntersect(circle.GeneratrixPlane))
                {
                    Circle circleProj = new Circle(circle.GeneratrixPlane, this);
                    float biggerRadius, smallerRadius;

                    if (circle.Radius >= circleProj.Radius)
                    {
                        biggerRadius = circle.Radius;
                        smallerRadius = circleProj.Radius;
                    }
                    else
                    {
                        biggerRadius = circleProj.Radius;
                        smallerRadius = circle.Radius;
                    }
                    float radiusDelta = biggerRadius - smallerRadius;
                    float radiusSum = biggerRadius + smallerRadius;
                    float positionDelta = Vector3.Magnitude(circle.Center - circleProj.Center);

                    return positionDelta <= radiusSum && positionDelta >= radiusDelta ?
                        IntersectionType.INTERSECTION :
                        IntersectionType.NONINTERSECTION;
                }
                else
                    return IntersectionType.NONINTERSECTION;
            }

            throw new NotImplementedException($"[Sphere] Unknown intersection for type:{_Line.GetType().FullName}");
        }

        public override bool IsIntersect(SpatialShape _Other)
        {
            if (_Other is Sphere sphere)
            {
                return Vector3.Distance(m_Center, sphere.m_Center) < m_Radius + sphere.Radius;
            }

            if (_Other is Plane plane)
            {
                return Mathf.Abs(plane.DistanceToPoint(m_Center)) < m_Radius;
            }

            if (_Other is Circle circle)
            {
                return Vector3.SqrMagnitude(m_Center - circle.GeneratrixSphere.m_Center) <= SqrRadius + circle.GeneratrixSphere.SqrRadius &&
                    Mathf.Abs(circle.GeneratrixPlane.DistanceToPoint(m_Center)) <= m_Radius;
            }

            throw new NotImplementedException("[Sphere] Unknown intersection for type:" + _Other.GetType().FullName);
        }

        #endregion
    }
}
