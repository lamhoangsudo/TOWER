using Nav3D.LocalAvoidance;
using System;
using UnityEngine;

namespace Nav3D.Common
{
    public class Nav3DSphereShellMover : IMovable
    {
        #region Attributes

        readonly float m_Radius;

        Vector3 m_PrevPosition;
        Vector3 m_CurrentPosition;

        Vector3 m_CurrentVelocity;
        Vector3 m_LastNonZeroVelocity;

        #endregion

        #region Events

        public event Action<IMovable, Vector3> OnPositionChanged;

        #endregion

        #region Properties

        public bool Avoiding        => false;
        public bool NeedToBeAvoided => true;

        public bool IsNeighborMoversDirty    => false;
        public bool IsNeighborObstaclesDirty => false;

        #endregion

        #region Constructors

        public Nav3DSphereShellMover(Vector3 _CurrentPosition, float _Radius)
        {
            m_PrevPosition    = _CurrentPosition;
            m_CurrentPosition = _CurrentPosition;
            m_Radius          = _Radius;
        }

        #endregion

        #region Public methods

        public void Initialize()
        {
            AgentManager.Instance.RegisterAgentMover(this);
        }

        public void Uninitialize()
        {
            if (!AgentManager.Doomed)
                AgentManager.Instance.RegisterAgentMover(this);
        }

        public void SetCurrentPosition(Vector3 _Position)
        {
            m_PrevPosition    = m_CurrentPosition;
            m_CurrentPosition = _Position;
            
            m_CurrentVelocity = m_CurrentPosition - m_PrevPosition;

            if (m_CurrentVelocity != Vector3.zero)
                m_LastNonZeroVelocity = m_CurrentVelocity;
            
            OnPositionChanged?.Invoke(this, m_CurrentPosition);
        }

        public Vector3 GetPosition()            => m_CurrentPosition;
        public Vector3 GetLastFrameVelocity()   => m_CurrentVelocity;
        public Vector3 GetLastNonZeroVelocity() => m_LastNonZeroVelocity;

        public float GetRadius()                        => m_Radius;
        public float GetMaxSpeed()                      => m_CurrentVelocity.magnitude;
        public float GetORCATau()                       => 1;
        public float GetStaticObstaclesDangerDistance() => 0;

        public void SetNeighborMovablesDirty(bool _Dirty)
        {
        }
        public void SetNeighborObstaclesDirty(bool _Dirty)
        {
        }

        #if UNITY_EDITOR

        public void DrawCurrentVelocity()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(m_CurrentPosition, m_CurrentPosition + m_CurrentVelocity);
        }
        
        #endif
        
        #endregion
    }
}