using Nav3D.API;
using UnityEngine;

namespace Nav3D.Demo
{
    [RequireComponent(typeof(Nav3DAgent))]
    public class EnemyController : MonoBehaviour
    {
        #region Attributes

        Nav3DAgent m_Agent;
        Vector3 m_AttackPoint;

        #endregion

        #region Properties

        //does the enemy has any defender that pursuing it
        public bool IsVictim { get; private set; }

        #endregion

        #region Public methods

        public void Init(Vector3 _AttackPoint)
        {
            m_AttackPoint = _AttackPoint;

            MoveToTarget();
        }

        //marks this enemy as victim
        public void MarkAsVictim()
        {
            IsVictim = true;
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        #endregion

        #region Service methods

        //just start moving to the target and destroy self on reach
        void MoveToTarget()
        {
            m_Agent.MoveTo(m_AttackPoint, Destroy);
        }

        #endregion

        #region Unity events

        void Awake()
        {
            m_Agent = GetComponent<Nav3DAgent>();
        }

        #endregion
    }
}