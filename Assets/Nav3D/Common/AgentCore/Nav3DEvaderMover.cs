using Nav3D.Common;
using Nav3D.LocalAvoidance;
using System;
using UnityEngine;

namespace Nav3D.Agents
{
    public class Nav3DEvaderMover : IMovable
    {
        #region Attributes

        readonly float m_Radius;
        readonly float m_MaxSpeed;

        // ReSharper disable once InconsistentNaming
        readonly float m_ORCATau;
        readonly float m_SpeedDecayFactor;
        readonly float m_VelocityRadius;

        Vector3 m_CurrentPosition;
        Vector3 m_LastVelocity;
        Vector3 m_LastNonZeroVelocity;

        bool m_IsNeighborMovablesDirty  = true;
        bool m_IsNeighborObstaclesDirty = true;

        EvaderLocalAvoidanceSolver m_LocalAvoidanceSolver;

        #endregion

        #region Events

        public event Action<IMovable, Vector3> OnPositionChanged;

        #endregion

        #region Properties

        public bool Avoiding => true;
        //other agents should not avoid this kind of mover 
        public bool NeedToBeAvoided          => false;
        public bool IsNeighborMoversDirty    => m_IsNeighborMovablesDirty;
        public bool IsNeighborObstaclesDirty => m_IsNeighborObstaclesDirty;

        #endregion

        #region Construction

        // ReSharper disable once InconsistentNaming
        public Nav3DEvaderMover(Vector3 _CurrentPosition, float _Radius, float _MaxSpeed, float _ORCATau, float _SpeedDecayFactor)
        {
            m_CurrentPosition = _CurrentPosition;

            m_Radius           = _Radius;
            m_MaxSpeed         = _MaxSpeed;
            m_ORCATau          = _ORCATau;
            m_SpeedDecayFactor = _SpeedDecayFactor;

            m_VelocityRadius = m_Radius + m_MaxSpeed;
        }

        #endregion

        #region Public methods

        public void SetCurrentPosition(Vector3 _Position)
        {
            m_CurrentPosition = _Position;

            OnPositionChanged?.Invoke(this, m_CurrentPosition);
        }

        public Vector3 GetVelocity()
        {
            m_LastVelocity = m_LocalAvoidanceSolver.ResolveVelocity();

            if (m_LastVelocity != Vector3.zero)
                m_LastNonZeroVelocity = m_LastVelocity;

            return m_LastVelocity;
        }

        public void Initialize()
        {
            m_LocalAvoidanceSolver = new EvaderLocalAvoidanceSolver(this, m_Radius, m_MaxSpeed, m_ORCATau, m_SpeedDecayFactor);

            AgentManager.Instance.RegisterAgentMover(this);
        }

        public void Uninitialize()
        {
            if (!AgentManager.Doomed)
                AgentManager.Instance.UnregisterAgentMover(this);
        }

        #if UNITY_EDITOR

        public void DrawNearestMovers()
        {
            m_LocalAvoidanceSolver.DrawNearestMovers();
        }
        
        public void DrawNearestTriangles()
        {
            m_LocalAvoidanceSolver.DrawNearestTriangles();
        }
        
        public void DrawTrianglesUpdateInfo()
        {
            m_LocalAvoidanceSolver?.DrawTrianglesUpdateInfo();
        }

        #endif

        #endregion

        #region IMovable

        public Vector3 GetPosition()
        {
            return m_CurrentPosition;
        }
        
        public Vector3 GetLastFrameVelocity()
        {
            return m_LastVelocity;
        }
        
        public Vector3 GetLastNonZeroVelocity()
        {
            return m_LastNonZeroVelocity;
        }
        
        public float GetRadius()
        {
            return m_Radius;
        }
        
        public float GetMaxSpeed()
        {
            return m_MaxSpeed;
        }

        public float GetORCATau()
        {
            return m_ORCATau;
        }

        public float GetStaticObstaclesDangerDistance()
        {
            return m_VelocityRadius;
        }

        public void SetNeighborMovablesDirty(bool _Dirty)
        {
            m_IsNeighborMovablesDirty = _Dirty;
        }

        public void SetNeighborObstaclesDirty(bool _Dirty)
        {
            m_IsNeighborObstaclesDirty = _Dirty;
        }

        #endregion
    }
}