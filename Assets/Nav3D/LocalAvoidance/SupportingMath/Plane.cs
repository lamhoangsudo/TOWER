using Nav3D.Common;
using System;
using UnityEngine;

namespace Nav3D.LocalAvoidance.SupportingMath
{
    /// <summary>
    /// Represents plane in space.
    /// A(x-x0) + B(y-y0) + C(z-z0) = 0
    /// or
    /// Ax + By + Cz + D = 0
    /// </summary>
    public class Plane : SpatialShape
    {
        #region Attributes

        float   m_A;
        float   m_B;
        float   m_C;
        float   m_Distance;
        Vector3 m_Normal;

        Vector3 m_BelongingPoint;

        #endregion

        #region Properties

        public float Distance => m_Distance;

        public Vector3 Normal => m_Normal;

        public Vector3 BelongingPoint => m_BelongingPoint;

        #endregion

        #region Construction

        public Plane(Vector3 _Normal, Vector3 _Point)
        {
            m_Normal   = _Normal.normalized;
            m_Distance = -Vector3.Dot(m_Normal, _Point);

            m_BelongingPoint = _Point;
        }

        #endregion

        #region Public methods

        public float DistanceToPoint(Vector3 _Point)
        {
            return Vector3.Dot(m_Normal, _Point) + m_Distance;
        }

        public void Translate(Vector3 _Translation)
        {
            m_BelongingPoint += _Translation;
            m_Distance       =  -Vector3.Dot(m_Normal, m_BelongingPoint);
        }

        public (Vector3 Point, float Distance) GetSurfaceClosestPoint(Vector3 _Point)
        {
            float distance = DistanceToPoint(_Point);
            return (_Point - m_Normal * distance, distance);
        }

        public bool GetSide(Vector3 _Point)
        {
            return (double)Vector3.Dot(m_Normal, _Point) + (double)m_Distance > 0.0;
        }

        public bool IsBelongsHalfPlane(Vector3 _Point)
        {
            return (double)Vector3.Dot(m_Normal, _Point) + (double)m_Distance > -(0.0 + UtilsMath.PLANE_BOUNDARY_THRESHOLD);
        }

        public bool IsPointBelongs(Vector3 _Point)
        {
            return Mathf.Approximately(DistanceToPoint(_Point), 0);
        }

        public Vector3 ProjPoint;

        public Straight Intersection(Plane _Other)
        {
            Vector3 crossProduct = Vector3.Cross(Normal, _Other.Normal);
            float   dotProduct   = Vector3.Dot(Normal, _Other.Normal);
            float   alpha        = Mathf.Acos(Mathf.Abs(dotProduct));

            Vector3 b         = BelongingPoint;
            Vector3 bProj     = _Other.GetClosestPoint(b);
            Vector3 bProjProj = GetClosestPoint(bProj);

            Vector3 direction = (bProjProj - b).normalized;
            float   a         = Vector3.Distance(b, bProj);
            float   l         = a / Mathf.Sin(alpha);

            Vector3 p = b + direction * l;

            return new Straight(crossProduct, p);
        }
        
        public Vector3[] Intersection(ILine _Line)
        {
            if (_Line is Straight straight)
            {
                return IntersectionWithLine(straight.Point, straight.Direction, out Vector3 point) ? new[] { point } : new[] { Vector3.zero };
            }

            if (_Line is Circle circle)
            {
                Straight                        planesSecantStraight = Intersection(circle.GeneratrixPlane);
                (Vector3 Point, float Distance) onStraightPointData  = GetSurfaceClosestPoint(circle.GeneratrixSphere.Center);
                Vector3                         onStraightPoint      = onStraightPointData.Point;
                float                           offset               = Mathf.Sqrt(circle.GeneratrixSphere.SqrRadius - onStraightPointData.Distance);

                return new[]
                {
                    onStraightPoint + planesSecantStraight.Direction * offset,
                    onStraightPoint - planesSecantStraight.Direction * offset
                };
            }

            throw new NotImplementedException("[Plane] Unknown intersection for type:" + _Line.GetType().FullName);
        }

        public static bool IsPlanesParallel(Plane _PlaneA, Plane _PlaneB)
        {
            return (Vector3.Cross(_PlaneA.Normal, _PlaneB.Normal) == Vector3.zero);
        }

        #endregion

        #region SpatialShape methods

        public override Vector3 GetClosestPoint(Vector3 _Point)
        {
            return GetSurfaceClosestPoint(_Point).Point;
        }

        public override IntersectionType CheckIntersection(ILine _Line)
        {
            if (_Line is Straight straight)
            {
                if (Mathf.Approximately(0, Vector3.Dot(Normal, straight.Direction)))
                    return Mathf.Approximately(0, DistanceToPoint(straight.Point)) ? IntersectionType.BELONGING : IntersectionType.NONINTERSECTION;
                else
                    return IntersectionType.INTERSECTION;
            }

            if (_Line is Circle circle)
            {
                if (circle.GeneratrixPlane.IsPointBelongs(m_BelongingPoint) && IsPlanesParallel(this, circle.GeneratrixPlane))
                    return IntersectionType.BELONGING;

                Straight intersectionStraight   = Intersection(circle.GeneratrixPlane);
                Vector3  closestOnStraightPoint = intersectionStraight.GetClosestPoint(circle.GeneratrixSphere.Center);

                return circle.GeneratrixSphere.IsPointInside(closestOnStraightPoint) ? IntersectionType.INTERSECTION : IntersectionType.NONINTERSECTION;
            }

            throw new NotImplementedException($"[Plane] Unknown intersection for type:{_Line.GetType().FullName}");
        }

        #if UNITY_EDITOR
        public override void Visualize()
        {
            using (Common.Debug.UtilsGizmos.ColorPermanence)
            {
                Vector3 orthoVector  = UtilsMath.GetRandomOrthogonal(Normal);
                Vector3 orthoVector1 = Vector3.Cross(orthoVector, Normal);

                Gizmos.color = Color.blue;

                Gizmos.DrawLine(BelongingPoint, BelongingPoint + orthoVector.normalized  * 0.2f);
                Gizmos.DrawLine(BelongingPoint, BelongingPoint - orthoVector.normalized  * 0.2f);
                Gizmos.DrawLine(BelongingPoint, BelongingPoint + orthoVector1.normalized * 0.2f);
                Gizmos.DrawLine(BelongingPoint, BelongingPoint - orthoVector1.normalized * 0.2f);

                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(BelongingPoint, BelongingPoint + Normal);
            }
        }
        #endif

        #endregion

        #region Service methods

        bool IntersectionWithSegment(Vector3 _Start, Vector3 _End, out Vector3 _Point)
        {
            Vector3 direction = _End - _Start;

            float denominator = Vector3.Dot(direction, Normal);

            if (Math.Abs(denominator) < float.Epsilon)
            {
                _Point = Vector3.zero;
                return false;
            }

            float t = (-Vector3.Dot(_Start, Normal) - m_Distance) / denominator;

            if (t < 0 || t > 1)
            {
                _Point = Vector3.zero;
                return false;
            }

            _Point = _Start + direction * t;
            return true;
        }

        bool IntersectionWithLine(Vector3 _Origin, Vector3 _Direction, out Vector3 _Point)
        {
            float denominator = Vector3.Dot(_Direction, Normal);

            if (Mathf.Abs(denominator) < float.Epsilon)
            {
                _Point = Vector3.zero;
                return false;
            }

            float t = (-Vector3.Dot(_Origin, Normal) - m_Distance) / denominator;

            _Point = _Origin + _Direction * t;
            return true;
        }

        #endregion
    }
}