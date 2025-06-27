using Nav3D.Agents;
using Nav3D.Common;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System;
using UnityEngine;

namespace Nav3D.LocalAvoidance
{
    public partial class AgentManager : MonoBehaviour
    {
        #region Nested types

        enum AgentOperationType
        {
            ADD,
            REMOVE
        }

        #endregion

        #region Attributes

        float m_DangerDistance = float.MinValue;
        float m_DangerDistanceSqr = float.MinValue;

        readonly HashSet<Nav3DAgentBase>                               m_Agents           = new HashSet<Nav3DAgentBase>();
        readonly ConcurrentQueue<(Nav3DAgentBase, AgentOperationType)> m_AgentsOperations = new ConcurrentQueue<(Nav3DAgentBase, AgentOperationType)>();

        SpatialHashMap<IMovable>   m_AgentsStorage;
        
        #endregion

        #region Properties

        public static AgentManager Instance => Singleton<AgentManager>.Instance;
        public static bool Doomed { get; private set; } = false;

        float DangerDistance
        {
            get => m_DangerDistance;
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (m_DangerDistance == value)
                    return;

                m_DangerDistance = value;
                m_DangerDistanceSqr = m_DangerDistance * m_DangerDistance;

                m_AgentsStorage.SetBucketSize(m_DangerDistance * 2);
            }
        }
        
        public float DangerDistanceSqr => m_DangerDistanceSqr;

        float BucketSize => m_DangerDistance * 2;
        
        public static DateTime Now { get; private set; }

        public float StorageBucketSize => m_AgentsStorage.BucketSize;

        #endregion

        #region Public methods

        public void Initialize(List<Nav3DAgentMover> _Agents)
        {
            m_AgentsStorage = new SpatialHashMap<IMovable>(BucketSize, _Agents);
        }

        public void Uninitialize(bool _NeedDestroy = true)
        {
            if (!_NeedDestroy)
                return;

            UtilsCommon.SmartDestroy(this);
        }

        public void RegisterAgent(Nav3DAgentBase _Agent)
        {
            m_AgentsOperations.Enqueue((_Agent, AgentOperationType.ADD));
        }

        public void UnregisterAgent(Nav3DAgentBase _Agent)
        {
            m_AgentsOperations.Enqueue((_Agent, AgentOperationType.REMOVE));
        }

        public void RegisterAgentMover(IMovable _Agent)
        {
            m_AgentsStorage.Insert(_Agent);

            UpdateCellSize((_Agent.GetRadius() + _Agent.GetMaxSpeed()) * 2f);
        }

        public void UnregisterAgentMover(IMovable _Agent)
        {
            m_AgentsStorage?.Remove(_Agent);
        }

        /// <summary>
        /// Determines agent bucket, returns all agents from bucket.
        /// </summary>
        /// <param name="_Agent">Agent.</param>
        /// <returns>Agents in bucket.</returns>
        public HashSet<IMovable> GetAgentBucketAgents(IMovable _Agent)
        {
            return m_AgentsStorage.GetElementBucketElements(_Agent);
        }

        /// <summary>
        /// Returns all agents from bucket.
        /// </summary>
        /// <param name="_InsidePoint">The point inside target bucket.</param>
        /// <returns>Agents in bucket.</returns>
        public HashSet<IMovable> GetBucketMovables(Vector3 _InsidePoint)
        {
            return m_AgentsStorage.GetBucketElements(_InsidePoint);
        }
        
        public IEnumerable<IMovable> GetMovablesInBounds(Bounds _Bounds)
        {
            return m_AgentsStorage.GetMovablesInBounds(_Bounds);
        }

        public bool HasNeighbors(IMovable _Movable)
        {
            return m_AgentsStorage.HasNeighbors(_Movable);
        }

        public bool HasNearestObstacles(IMovable _Movable, out Bounds _DangerBounds)
        {
            return m_AgentsStorage.HasNearestObstacles(_Movable, out _DangerBounds);
        }
        
        public void SetMovablesInBoundsObstacleDirty(Bounds _Bounds)
        {
            m_AgentsStorage.SetMovablesInBoundsObstacleDirty(_Bounds);
        }
        
        #endregion

        #region Service methods

        void UpdateCellSize(float _Value)
        {
            if (DangerDistance < _Value)
                DangerDistance = _Value;
        }

        #endregion

        #region Unity events

        void Awake()
        {
            Doomed = false;
        }

        void OnDestroy()
        {
            Doomed = true;

            Uninitialize(false);
        }

        void Update()
        {
            Now = DateTime.Now;
        }

        void FixedUpdate()
        {
            while (m_AgentsOperations.Any())
            {
                if (!m_AgentsOperations.TryDequeue(out (Nav3DAgentBase agent, AgentOperationType operation) agentOperation))
                    continue;

                if (agentOperation.operation == AgentOperationType.ADD)
                {
                    m_Agents.Add(agentOperation.agent);

                    continue;
                }

                m_Agents.Remove(agentOperation.agent);
            }

            foreach (Nav3DAgentBase agent in m_Agents)
            {
                agent.DoFixedUpdate();
            }
        }

        #endregion
    }
}