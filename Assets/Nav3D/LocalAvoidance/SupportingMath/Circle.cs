using Nav3D.Common;
using System;
using UnityEngine;

namespace Nav3D.LocalAvoidance.SupportingMath
{
    public class Circle : SpatialShape, ILine
    {
        #region Attributes

        Plane m_Plane;
        Sphere m_Sphere;
        Vector3? m_Center;
        float m_Radius;
        float m_SqrRadius;

        #endregion

        #region Properties

        public Plane GeneratrixPlane => m_Plane;
        public Sphere GeneratrixSphere => m_Sphere;
        public Vector3 Center => !m_Center.HasValue ? (m_Center = m_Plane.GetClosestPoint(m_Sphere.Center)).Value : m_Center.Value;
        public float Radius => m_Radius == 0 ? m_Radius = Mathf.Sqrt(m_Sphere.SqrRadius - (m_Sphere.Center - Center).sqrMagnitude) : m_Radius;
        public float SqrRadius => m_SqrRadius == 0 ? m_SqrRadius = Radius * Radius : m_SqrRadius;

        #endregion

        #region Constants

        const int VISUALISATION_SAMPLES_NUM = 16;

        #endregion

        #region Construction

        public Circle(Plane _Plane, Sphere _Sphere)
        {
            if (Mathf.Abs(_Plane.DistanceToPoint(_Sphere.Center)) >= _Sphere.Radius)
                throw new ArgumentException($"[{nameof(Circle)}] No intersection");

            m_Plane = _Plane;
            m_Sphere = _Sphere;
        }

        #endregion

        #region SpatialShape methods

        public override Vector3 GetClosestPoint(Vector3 _Point)
        {
            var onPlanePointProj = m_Plane.GetSurfaceClosestPoint(_Point);

            var onPlaneSphereCenterProj = m_Plane.GetSurfaceClosestPoint(m_Sphere.Center);

            float distToSolution = Mathf.Sqrt(m_Sphere.SqrRadius - onPlaneSphereCenterProj.Distance * onPlaneSphereCenterProj.Distance);
            Vector3 directionToSolution = (onPlanePointProj.Point - onPlaneSphereCenterProj.Point).normalized;

            return onPlaneSphereCenterProj.Point + directionToSolution * distToSolution;
        }

#if UNITY_EDITOR
        public override void Visualize()
        {
            using (Common.Debug.UtilsGizmos.ColorPermanence)
            {

                Vector3 onPlaneCenterProj = m_Plane.GetSurfaceClosestPoint(m_Sphere.Center).Point;
                Vector3 randomShiftedCenterProj = m_Plane.GetSurfaceClosestPoint(
                    m_Sphere.Center + UtilsMath.GetRandomVector()
                    ).Point;

                Gizmos.DrawSphere(onPlaneCenterProj, 0.05f);
                Gizmos.DrawSphere(randomShiftedCenterProj, 0.05f);

                Straight projLine = new Straight(randomShiftedCenterProj - onPlaneCenterProj, onPlaneCenterProj);
                Vector3 onLinePoint = projLine.GetClosestPoint(m_Sphere.Center);

                Vector3 fromCenterVector = onLinePoint - m_Sphere.Center;
                Vector3 lineVector = projLine.Direction * Mathf.Sqrt(m_Sphere.SqrRadius - fromCenterVector.sqrMagnitude);

                Vector3 onSpherePoint = m_Sphere.Center + (fromCenterVector + lineVector).normalized * m_Sphere.Radius;

                Gizmos.DrawSphere(onSpherePoint, 0.05f);

                float degreeStep = 360f / VISUALISATION_SAMPLES_NUM;
                float angle = degreeStep;

                Vector3 prePoint = onSpherePoint;

                for (int i = 0; i < VISUALISATION_SAMPLES_NUM; i++)
                {
                    Vector3 curPoint = m_Sphere.Center + Quaternion.AngleAxis(angle, m_Plane.Normal) * (onSpherePoint - m_Sphere.Center);
                    angle += degreeStep;

                    Gizmos.DrawLine(prePoint, curPoint);

                    prePoint = curPoint;
                }
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

            throw new NotImplementedException("[Circle] Unknown intersection for type:" + _Figure.GetType().FullName);
        }

        public Vector3 ClosestPoint(Vector3 _Point) => GetClosestPoint(_Point);

        #endregion
    }
}