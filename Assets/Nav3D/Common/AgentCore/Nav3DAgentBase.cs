using UnityEngine;

namespace Nav3D.Agents
{
    public abstract class Nav3DAgentBase : MonoBehaviour
    {
        #region Attributes

        protected Transform m_CachedTransform;
        
        #endregion
        
        #region Public methods

        public abstract void DoFixedUpdate();

        #endregion
    }
}