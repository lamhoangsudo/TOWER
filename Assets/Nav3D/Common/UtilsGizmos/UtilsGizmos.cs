using System;
using UnityEngine;

#if UNITY_EDITOR
namespace Nav3D.Common.Debug
{
    public static class UtilsGizmos
    {
        #region Nested types

        class ColorInvariant : IDisposable
        {
            #region Atributes

            Color m_GizmosColorBuffer;

            #endregion

            #region Construction

            public ColorInvariant()
            {
                m_GizmosColorBuffer = Gizmos.color;
            }

            #endregion

            #region IDisposable methods

            public void Dispose() => Gizmos.color = m_GizmosColorBuffer;

            #endregion
        }

        #endregion

        #region Properties

        public static IDisposable ColorPermanence => new ColorInvariant();

        #endregion

        #region Public methods

        public static void DrawPoint(Vector3 _Coordinate)
        {
            using (ColorPermanence)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawSphere(_Coordinate, 0.05f);
            }
        }

        public static void DrawPoint(Vector3 _Coordinate, Color _Color)
        {
            using (ColorPermanence)
            {
                Gizmos.color = _Color;
                Gizmos.DrawSphere(_Coordinate, 0.05f);
            }
        }

        #endregion
    }
}
#endif