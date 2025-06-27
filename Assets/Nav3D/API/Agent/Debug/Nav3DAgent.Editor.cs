using UnityEngine;
#if UNITY_EDITOR
using Nav3D.Common.Debug;
#endif
namespace Nav3D.API
{
    #if UNITY_EDITOR
    
    public partial class Nav3DAgent
    {
        #region Serialized fields

        [Space, Header("Draw agent operating properties")]
        [SerializeField] bool m_DrawRadius;

        [SerializeField] bool m_DrawVelocities;
        [SerializeField] bool m_DrawCurrentPath;

        [Space, Header("Show agent nearest avoidance targets")]
        [SerializeField] bool m_ShowAgents;

        [SerializeField] bool m_DrawNearestTriangles;
        [SerializeField] bool m_DrawTrianglesUpdateInfo;

        #endregion

        #region Public methods

        public void Draw(bool _DrawRadius, bool _DrawPath, bool _DrawVelocities)
        {
            using (UtilsGizmos.ColorPermanence)
            {
                m_Mover?.Draw(_DrawRadius, _DrawPath, _DrawVelocities);
            }
        }
        
        public void DrawNearestTriangles()
        {
            m_Mover?.DrawNearestTriangles();
        }
        
        public void DrawTrianglesUpdateInfo()
        {
            m_Mover?.DrawTrianglesUpdateInfo();
        }

        void DrawNearestMovers()
        {
            m_Mover?.DrawNearestMovers();
        }

        #endregion

        #region Unity events

        void OnDrawGizmos()
        {
            if (!enabled)
                return;

            if (!Application.isPlaying)
            {
                Gizmos.color = Color.white;

                if (m_DrawRadius && m_Config != null)
                    Gizmos.DrawWireSphere(transform.position, m_Config.Radius);
            }
            else
            {
                using (UtilsGizmos.ColorPermanence)
                {
                    //draw operating parameters
                    if (m_DrawRadius || m_DrawVelocities || m_DrawCurrentPath)
                        Draw(m_DrawRadius, m_DrawCurrentPath, m_DrawVelocities);

                    //draw nearest agents
                    if (m_ShowAgents)
                        DrawNearestMovers();

                    //draw nearest obstacle's triangles
                    if (m_DrawNearestTriangles)
                        DrawNearestTriangles();

                    //draw nearest triangles last update info
                    if (m_DrawTrianglesUpdateInfo)
                        DrawTrianglesUpdateInfo();
                }
            }
        }

        #endregion
    }
    
    #endif
}