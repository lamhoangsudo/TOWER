using UnityEngine;

#if UNITY_EDITOR
namespace Nav3D.Common.Debug
{
    public class GizmosLine : IDrawable
    {
        #region Attributes

        Vector3 m_Start;
        Vector3 m_End;
        Color m_Color;

        #endregion

        #region Constructors

        public GizmosLine(Vector3 _Start, Vector3 _End, Color _Color)
        {
            m_Start = _Start;
            m_End = _End;
            m_Color = _Color;
        }

        #endregion

        #region Public methods

        public void Draw()
        {
            Gizmos.color = m_Color;
            Gizmos.DrawLine(m_Start, m_End);
        }

        #endregion
    }
}
#endif