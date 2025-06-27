using UnityEngine;
#if UNITY_EDITOR
using Nav3D.Common.Debug;
#endif

namespace Nav3D.API
{
    #if UNITY_EDITOR

    public partial class Nav3DEvader
    {
        #region Serialized fields

        [SerializeField] bool m_DrawRadius;
        [SerializeField] bool m_DrawNearestAgents;
        [SerializeField] bool m_DrawNearestTriangles;
        [SerializeField] bool m_DrawTrianglesUpdateInfo;

        #endregion

        #region Service methods

        void DrawNearestMovers()
        {
            m_Mover?.DrawNearestMovers();
        }

        void DrawNearestTriangles()
        {
            m_Mover?.DrawNearestTriangles();
        }
        
        void DrawTrianglesUpdateInfo()
        {
            m_Mover?.DrawTrianglesUpdateInfo();
        }
        
        #endregion

        #region Unity events

        void OnDrawGizmos()
        {
            if (!enabled)
                return;

            Gizmos.color = Color.white;

            if (m_DrawRadius)
                Gizmos.DrawWireSphere(transform.position, m_Radius);

            if (!Application.isPlaying)
                return;

            using (UtilsGizmos.ColorPermanence)
            {
                //draw nearest agents
                if (m_DrawNearestAgents)
                    DrawNearestMovers();

                if (m_DrawNearestTriangles)
                    DrawNearestTriangles();
                
                if (m_DrawTrianglesUpdateInfo)
                    DrawTrianglesUpdateInfo();
            }
        }

        #endregion
    }

    #endif
}