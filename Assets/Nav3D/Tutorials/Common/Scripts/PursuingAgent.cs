using Nav3D.API;
using UnityEngine;

namespace Nav3D.Tutorials
{
    public class PursuingAgent : MonoBehaviour
    {
        #region Serialized fields

        [SerializeField] Transform m_Target;

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
            m_Agent.FollowTarget(
                    m_Target,
                    _TargetReachDistance: 0.5f,
                    _FollowContinuously: true,
                    _OnReach: () =>
                    {
                        Destroy(m_Target.gameObject);
                        Destroy(gameObject);
                    }
                );
        }

        #endregion
    }
}