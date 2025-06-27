using UnityEngine;

#if UNITY_EDITOR
namespace Nav3D.Common.Debug
{
    public class GizmosSphereSolid : IDrawable
    {
        #region Attributes

        Vector3 m_Center;
        float m_Radius;
        Color m_Color;

        #endregion

        #region Constructors

        public GizmosSphereSolid(Vector3 _Center, float _Radius, Color _Color)
        {
            m_Center = _Center;
            m_Radius = _Radius;
            m_Color = _Color;
        }

        #endregion

        #region Public methods

        public void Draw()
        {
            Gizmos.color = m_Color;
            Gizmos.DrawSphere(m_Center, m_Radius);
        }

        #endregion
    }
}
#endif