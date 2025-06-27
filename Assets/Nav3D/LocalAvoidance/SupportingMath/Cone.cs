using Nav3D.Common;
using UnityEngine;

namespace Nav3D.LocalAvoidance.SupportingMath
{
    public class Cone : ConeEndless
    {
        #region Attributes

        protected float m_Height;
        protected float m_Radius;

        #endregion

        #region Construction

        public Cone(Vector3 _Point, Sphere _Sphere) : base(_Point, _Sphere)
        {
            float distance = Vector3.Magnitude(_Sphere.Center - _Point);

            m_Height = distance + _Sphere.Radius;
            m_Radius = m_TgAlpha * m_Height;
        }

        #endregion

        #region SpatialShape methods

        public override bool IsPointInside(Vector3 _Point)
        {
            Vector3 toPointVector = _Point - m_Point;

            return Vector3.Angle(toPointVector, m_Direction) < Mathf.Atan(m_TgAlpha) * Mathf.Rad2Deg &&
                Vector3.Project(toPointVector, m_Direction).magnitude < m_Height;
        }

#if UNITY_EDITOR
        public override void Visualize()
        {
            base.Visualize();

            const int SAMPLES = 12;

            Vector3 preAxisPoint = m_Point;
            float axisStep = m_Height / SAMPLES;

            Vector3 stepVector = m_Direction * (axisStep);
            //axis-ortho vector
            Vector3 orthoVector = UtilsMath.GetRandomOrthogonal(m_Direction).normalized;
            float axisOffset = 0;
            float angleStep = 360f / SAMPLES;

            using (Common.Debug.UtilsGizmos.ColorPermanence)
            {
                Gizmos.color = Color.cyan;

                Gizmos.DrawLine(m_Point, m_Point + m_Direction * m_Height);

                //steps among axis
                for (int i = 0; i < SAMPLES; i++)
                {
                    Vector3 currAxisPoint = preAxisPoint + stepVector;
                    axisOffset += axisStep;

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

                    preAxisPoint = currAxisPoint;
                }
            }
        }
#endif

#endregion
    }
}