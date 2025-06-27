using UnityEngine;

namespace Nav3D.API
{
    public partial class Nav3DSphereShell
    {
        #region Serialized fields

        [SerializeField] bool m_DrawRadius;

        #endregion

        #region Unity events

        #if UNITY_EDITOR
        
        void OnDrawGizmos()
        {
            if (!enabled)
                return;

            Gizmos.color = Color.white;

            if (m_DrawRadius)
            {
                Gizmos.DrawWireSphere(transform.position, m_Radius);

                m_Mover?.DrawCurrentVelocity();
            }
        }

        #endif
        
        #endregion
    }
}