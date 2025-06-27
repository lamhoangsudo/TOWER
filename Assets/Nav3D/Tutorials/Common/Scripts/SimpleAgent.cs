using Nav3D.API;
using UnityEngine;

namespace Nav3D.Tutorials
{
    public class SimpleAgent : MonoBehaviour
    {
        #region Serialized fields

        [SerializeField] Transform m_TargetPoint;

        #endregion

        #region Attributes

        Nav3DAgent m_Agent;

        #endregion

        #region Unity events

        void Awake()
        {
            Nav3DManager.OnNav3DInit += Init;
        }

        #endregion

        #region Service methods

        void Init()
        {
            m_Agent = GetComponent<Nav3DAgent>();
            m_Agent.MoveTo(m_TargetPoint.position);
        }

        #endregion
    }
}