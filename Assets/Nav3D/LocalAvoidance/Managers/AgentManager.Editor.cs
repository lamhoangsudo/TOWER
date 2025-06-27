using UnityEngine;

namespace Nav3D.LocalAvoidance
{
    #if UNITY_EDITOR
    
    public partial class AgentManager
    {
        #region Serialized fields

        [SerializeField] bool m_DrawAgentStorage;
        
        #endregion
        
        #region Unity events

        void OnDrawGizmos()
        {
            using (Common.Debug.UtilsGizmos.ColorPermanence)
            {
                if (m_DrawAgentStorage)
                    m_AgentsStorage.Draw();
            }
        }
        
        #endregion
    }
    
    #endif
}