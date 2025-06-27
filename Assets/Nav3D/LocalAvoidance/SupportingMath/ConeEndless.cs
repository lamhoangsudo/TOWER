using Nav3D.Common;
using System;
using UnityEngine;

namespace Nav3D.LocalAvoidance.SupportingMath
{
    public class ConeEndless : SpatialShape
    {
        #region Attributes

        protected Vector3 m_Point;
        protected Vector3 m_Direction;
        protected float m_Alpha;
        protected float m_TgAlpha;

        float m_DirectionMagnitudeDebug;
        float m_SphereRadiusDebug;
        #endregion

        #region Properties

        public float Angle => m_Alpha;

        #endregion

        #region Construction

        public ConeEndless(Vector3 _Point, Sphere _Sphere)
        {
            m_Point = _Point;
            m_Direction = _Sphere.Center - _Point;
            m_Alpha = Mathf.Asin(/*_Sphere.Radius / m_Direction.magnitude*/Mathf.Clamp(_Sphere.Radius / m_Direction.magnitude, -1f + Mathf.Epsilon, 1f - Mathf.Epsilon));
            if (float.IsNaN(m_Alpha))
            {
                Debug.LogError($"m_Alpha is NaN: _Sphere.Radius: {_Sphere.Radius}, m_Direction.magnitude: {m_Direction.magnitude}");
            }
            m_TgAlpha = Mathf.Tan(m_Alpha);

            m_DirectionMagnitudeDebug = m_Direction.magnitude;
            m_SphereRadiusDebug = _Sphere.Radius;

            m_Direction.Normalize();
        }

        public ConeEndless(Vector3 _Point, Vector3 _Direcrion, float _TgAlpha)
        {
            if (_TgAlpha < 0)
                throw new ArgumentException("Tanget must be positive");

            m_Point = _Point;
            m_Direction = _Direcrion;
            m_TgAlpha = _TgAlpha;
        }

        #endregion

        #region SpatialShape methods

        public override Vector3 GetClosestPoint(Vector3 _Point)
        {
            Vector3 topToPoint = _Point - m_Point;

            if (topToPoint == Vector3.zero)
                return m_Point;

            double radAngle = (double)m_Alpha - Vector3.Angle(m_Direction, topToPoint) * Mathf.Deg2Rad;
            double generatrixLength = topToPoint.magnitude * Math.Cos(radAngle);
            Vector3 normal = Vector3.Cross(m_Direction, topToPoint).normalized;


            Vector3 surfacePoint = m_Point + (Quaternion.AngleAxis(m_Alpha * Mathf.Rad2Deg, normal) * m_Direction).normalized * (float)generatrixLength;

            return surfacePoint;
        }

        public virtual Vector3 GetClosestSurfaceNormal(Vector3 _Point)
        {
            Vector3 onDirProj = new Straight(m_Direction, m_Point).GetClosestPoint(_Point);

            //OP'
            float onDirProjLength = (onDirProj - m_Point).magnitude;
            //OP
            float toPivotLength = (_Point - m_Point).magnitude;
            //OP''
            float onDirFinalProjLength = (Mathf.Cos(Mathf.Acos(onDirProjLength / toPivotLength) - m_Alpha) * toPivotLength) / Mathf.Cos(m_Alpha);

            Vector3 onDirFinalProj = m_Point + m_Direction * onDirFinalProjLength;

            return (_Point - onDirFinalProj).normalized;
        }

        public virtual bool IsPointInside(Vector3 _Point)
        {
            Vector3 toPointVector = _Point - m_Point;

            return Vector3.Angle(toPointVector, m_Direction) < Mathf.Atan(m_TgAlpha) * Mathf.Rad2Deg;
        }

#if UNITY_EDITOR
        public override void Visualize()
        {
            const int SAMPLES = 16;

            Vector3 preAxisPoint = m_Point;
            float height = 5f;
            float axisStep = height / SAMPLES;

            Vector3 stepVector = m_Direction * (axisStep);
            //axis-ortho vector
            Vector3 orthoVector = UtilsMath.GetRandomOrthogonal(m_Direction).normalized;
            float axisOffset = 0;
            float angleStep = 360f / SAMPLES;

            using (Common.Debug.UtilsGizmos.ColorPermanence)
            {
                Gizmos.color = Color.cyan;

                Gizmos.DrawLine(m_Point, m_Point + m_Direction * height);

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