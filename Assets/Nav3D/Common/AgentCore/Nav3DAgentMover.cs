using Nav3D.API;
using Nav3D.Common;
using Nav3D.LocalAvoidance;
using System;
using UnityEngine;

namespace Nav3D.Agents
{
    public abstract class Nav3DAgentMover : IMovable
    {
        #region Attributes

        bool m_IsNeighborMovablesDirty  = true;
        bool m_IsNeighborObstaclesDirty = true;

        protected MoversVOsProvider m_MoversVOsProvider;

        // ReSharper disable once InconsistentNaming
        protected StaticTrianglesVOProvider m_TrianglesVOProvider;

        Vector3           m_AccumulatedVelocity;
        protected Vector3 m_CurrentPosition;
        Vector3           m_LastVelocity;
        Vector3           m_LastNonZeroVelocity;

        protected Nav3DAgentConfig m_Config;

        protected Vector3[] m_Targets;

        protected Action<Vector3> m_OnTargetPassed;
        protected Action          m_OnLastTargetReached;

        protected readonly Log m_Log;

        protected Action<Vector3[]> m_OnPathUpdated;

        #endregion

        #region Events

        public event Action<IMovable, Vector3> OnPositionChanged;

        #endregion

        #region Properties

        public Nav3DAgent Agent { get; private set; }

        public abstract bool Avoiding                        { get; }
        //We assume that we should avoid only active movers that performing some task.
        public          bool NeedToBeAvoided                 => Agent.Operates && !CoDirectionalAvoidanceInterrupt;
        public          bool CoDirectionalAvoidanceInterrupt { get; private set; }

        public bool IsNeighborMoversDirty    => m_IsNeighborMovablesDirty;
        public bool IsNeighborObstaclesDirty => m_IsNeighborObstaclesDirty;

        public Vector3 CurrentPosition => m_CurrentPosition;

        public float UpdateStaticObstaclesSqrDistanceThreshold => m_Config.VelocityRadiusSqr;

        protected Vector3 LastVelocity
        {
            get => m_LastVelocity;
            set
            {
                m_LastVelocity = value;

                if (value != Vector3.zero)
                    m_LastNonZeroVelocity = value;
            }
        }

        public abstract Vector3[] CurrentPath { get; }

        #endregion

        #region Constructors

        protected Nav3DAgentMover(Vector3 _Position, Nav3DAgentConfig _Config, Nav3DAgent _Agent, Log _Log)
        {
            Agent             = _Agent;
            m_CurrentPosition = _Position;

            m_Log = _Log;

            ApplyConfig(_Config);
        }

        #endregion

        #region Public methods

        public abstract void BeginMoveTo(Vector3 _Point, bool _TryRepositionStartIfOccupied, Action<Vector3[]> _OnPathUpdated, Action<PathfindingError> _OnPathfindingFail = null);

        public abstract void BeginMoveToPoints(
                Vector3[]                _Points,
                bool                     _Loop,
                bool                     _SkipUnpassableTargets,
                bool                     _TryRepositionStartIfOccupied,
                Action<Vector3[]>        _OnPathUpdated,
                Action<Vector3>          _OnTargetPassed     = null,
                Action                   _OnLastPointReached = null,
                Action<PathfindingError> _OnPathfindingFail  = null
            );

        public Vector3 GetFrameVelocity()
        {
            Vector3 velocity = GetVelocity();

            m_AccumulatedVelocity = Vector3.Slerp(m_AccumulatedVelocity, velocity, 0.5f);

            return velocity;
        }

        public virtual void Uninitialize()
        {
            //check for case when scene cleanup pass occurs
            if (!AgentManager.Doomed)
                AgentManager.Instance.UnregisterAgentMover(this);
        }

        public virtual void DisposePath()
        {
        }

        public virtual void SetCurrentPosition(Vector3 _Position)
        {
            m_CurrentPosition = _Position;

            OnPositionChanged?.Invoke(this, m_CurrentPosition);
        }

        public virtual bool IsDeviatedFromPathLastFrame()
        {
            return false;
        }

        public Vector3 GetLastNonZeroVelocity()
        {
            return m_LastNonZeroVelocity;
        }

        // ReSharper disable once InconsistentNaming
        public virtual void UpdateRivalVO(Nav3DAgentMover _RivalMover, VOAgent _RivalVO)
        {
        }

        public bool IsCollidingWithOther(Nav3DAgentMover _Other)
        {
            Vector3 agentsDeltaPos = _Other.GetPosition() - m_CurrentPosition;
            float   radiusSum      = m_Config.Radius      + _Other.GetRadius();

            return agentsDeltaPos.magnitude < radiusSum;
        }

        public void SetAvoidanceInterrupt(bool _Value)
        {
            CoDirectionalAvoidanceInterrupt = _Value;
        }

        public virtual void ReinitPathFollowingData()
        {
        }

        public virtual void ReinitPath()
        {
        }

        public virtual string GetInnerStatusString()
        {
            return string.Empty;
        }

        public virtual string GetLastPathfindingStatsString()
        {
            return string.Empty;
        }

        #if UNITY_EDITOR

        public virtual void Draw(bool _DrawRadius, bool _DrawPath, bool _DrawVelocities)
        {
            if (_DrawRadius)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(m_CurrentPosition, m_Config.Radius);
            }
        }

        public void DrawNearestMovers()
        {
            m_MoversVOsProvider?.Draw();
        }

        public void DrawNearestTriangles()
        {
            m_TrianglesVOProvider?.DrawNearestTriangles();
        }

        public void DrawTrianglesUpdateInfo()
        {
            m_TrianglesVOProvider?.DrawTrianglesUpdateInfo();
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

        public float GetRadius()
        {
            return m_Config.Radius;
        }

        public float GetMaxSpeed()
        {
            return m_Config.FactualMaxSpeed;
        }

        public float GetORCATau()
        {
            return m_Config.ORCATau;
        }

        public float GetStaticObstaclesDangerDistance()
        {
            return m_Config.VelocityRadius;
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

        #region Service methods

        void ApplyConfig(Nav3DAgentConfig _Config)
        {
            m_Config = _Config;

            if (!AgentManager.Doomed)
                AgentManager.Instance.RegisterAgentMover(this);
        }

        protected abstract Vector3 GetVelocity();

        #endregion
    }
}