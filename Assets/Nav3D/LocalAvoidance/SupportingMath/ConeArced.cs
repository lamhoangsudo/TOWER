using Nav3D.Common;
using System;
using UnityEngine;

namespace Nav3D.LocalAvoidance.SupportingMath
{
    public class ConeArced : Cone
    {
        #region Attributes

        readonly Sphere m_BaseSphere;
        Sphere m_SecantSphere;
        readonly double m_SmallBaseRadius;
        readonly Vector3 m_SmallBasePoint;
        readonly double m_TopToBaseGeneratrixLength;
        readonly double m_TopToBaseGeneratrixSqrLength;
        float m_Tau;

        #endregion

        #region Properties

        public float Tau
        {
            get => m_Tau;
            set
            {
                m_Tau = value;
                m_SecantSphere = new Sphere(
                    m_Point + (m_BaseSphere.Center - m_Point) / m_Tau,
                    m_BaseSphere.Radius / m_Tau
                );
            }
        }
        public ConeArced ZeroFlipped => new ConeArced(
            -m_Point,
            m_BaseSphere.ZeroFlipped,
            m_Tau
        );
        public Sphere SecantSphere => m_SecantSphere;

        #endregion

        #region Construction

        public ConeArced(Vector3 _Point, Sphere _Sphere, float _Tau) : base(_Point, _Sphere)
        {
            m_BaseSphere = _Sphere;
            m_SecantSphere = new Sphere(_Point + (_Sphere.Center - _Point) / _Tau, _Sphere.Radius / _Tau);
            float betaAngle = Mathf.Deg2Rad * 90 - m_Alpha;
            m_SmallBaseRadius = m_SecantSphere.Radius * Math.Sin(betaAngle);
            m_SmallBasePoint = m_SecantSphere.Center + (_Point - m_SecantSphere.Center).normalized * m_SecantSphere.Radius * Mathf.Cos(betaAngle);

            m_TopToBaseGeneratrixLength = m_SmallBaseRadius / Math.Sin(m_Alpha);
            m_TopToBaseGeneratrixSqrLength = m_TopToBaseGeneratrixLength * m_TopToBaseGeneratrixLength;

            m_Tau = _Tau;
        }

        #endregion

        #region SpatialShape methods

        public override Vector3 GetClosestPoint(Vector3 _InnerPoint)
        {
            Vector3 onConePoint = base.GetClosestPoint(_InnerPoint);

            if (float.IsNaN(onConePoint.x))
                onConePoint = _InnerPoint;

            if ((m_Point - onConePoint).sqrMagnitude <= m_TopToBaseGeneratrixSqrLength)
                return m_SecantSphere.GetClosestPoint(_InnerPoint);
            else
                return onConePoint;
        }

        public override Vector3 GetClosestSurfaceNormal(Vector3 _InnerPoint)
        {
            Vector3 onConePoint = base.GetClosestPoint(_InnerPoint);

            if ((m_Point - onConePoint).sqrMagnitude < m_TopToBaseGeneratrixSqrLength)
                return m_SecantSphere.GetClosestSurfaceNormal(_InnerPoint);
            else
                return base.GetClosestSurfaceNormal(_InnerPoint);
        }

        public override bool IsPointInside(Vector3 _Point)
        {
            Vector3 onConePoint = base.GetClosestPoint(_Point);

            if ((m_Point - onConePoint).sqrMagnitude < m_TopToBaseGeneratrixSqrLength)
                return Vector3.Distance(m_SecantSphere.Center, _Point) < m_SecantSphere.Radius;
            else
                return base.IsPointInside(_Point);
        }

#if UNITY_EDITOR
        public override void Visualize()
        {
            m_SecantSphere.Visualize();
            m_BaseSphere.Visualize();

            const int SAMPLES = 16;

            Vector3 preAxisPoint = m_SmallBasePoint;
            float partialConeHeight = m_Height - Vector3.Magnitude(m_Point - m_SmallBasePoint);
            float axisStep = partialConeHeight / SAMPLES;

            Vector3 stepVector = m_Direction * (axisStep);
            //axis-ortho vector
            Vector3 orthoVector = UtilsMath.GetRandomOrthogonal(m_Direction).normalized;
            float axisOffset = Vector3.Magnitude(m_Point - m_SmallBasePoint);
            float angleStep = 360f / SAMPLES;

            using (Common.Debug.UtilsGizmos.ColorPermanence)
            {
                Gizmos.color = Color.cyan;

                Gizmos.DrawLine(m_SmallBasePoint, m_SmallBasePoint + m_Direction * partialConeHeight);

                //steps among axis
                for (int i = 0; i <= SAMPLES; i++)
                {
                    Vector3 currAxisPoint = preAxisPoint;


                    Vector3 radiusPoint = orthoVector * axisOffset * m_TgAlpha;
                    float angle = 0;
                    Vector3 preCirclePoint = currAxisPoint + Quaternion.AngleAxis(angle, stepVector) * radiusPoint;

                    //angle steps
                    for (int j = 0; j < SAMPLES; j++)
                    {
                        angle += angleStep;
                        Vector3 currCirclePoint = currAxisPoint + Quaternion.AngleAxis(angle, stepVector) * radiusPoint;
                        Gizmos.DrawLine(preCirclePoint, currCirclePoint);

                        preCirclePoint = currCirclePoint;
                    }
                    axisOffset += axisStep;
                    preAxisPoint += stepVector;
                }
            }
        }
#endif

#endregion
    }
}