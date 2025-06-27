using UnityEngine;
#if UNITY_EDITOR
namespace Nav3D.Common.Debug
{
    public class GizmosWireCube : IDrawable
    {
        #region Attributes

        Vector3 m_Center;
        Vector3 m_Size;
        Color m_Color;

        #endregion

        #region Constructors

        public GizmosWireCube(Vector3 _Center, Vector3 _Size, Color _Color)
        {
            m_Center = _Center;
            m_Size = _Size;
            m_Color = _Color;
        }

        #endregion

        #region Public methods

        public void Draw()
        {
            Gizmos.color = m_Color;
            Gizmos.DrawWireCube(m_Center, m_Size);
        }

        #endregion
    }
}
#endif