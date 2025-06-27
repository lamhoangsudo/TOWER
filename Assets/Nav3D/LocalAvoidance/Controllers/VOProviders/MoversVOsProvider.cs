using Nav3D.API;
using Nav3D.Common;
using Nav3D.LocalAvoidance;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Plane = Nav3D.LocalAvoidance.SupportingMath.Plane;

namespace Nav3D.Agents
{
    public class MoversVOsProvider
    {
        #region Nested types

        class RivalsStorage
        {
            #region Attributes

            readonly Dictionary<Nav3DAgentMover, VOAgent> m_Rivals = new Dictionary<Nav3DAgentMover, VOAgent>();

            #endregion

            #region Properties

            bool IsEmpty { get; set; }

            #endregion

            #region Construction

            public RivalsStorage() { }

            #endregion

            #region Public methods

            public void Add(Nav3DAgentMover _RivalAgent, VOAgent _RivalVO)
            {
                m_Rivals[_RivalAgent] = _RivalVO;

                IsEmpty = false;
            }

            public void Clear()
            {
                if (IsEmpty)
                    return;

                m_Rivals.Clear();
                IsEmpty = true;
            }

            public bool TryGetValidVO(Nav3DAgentMover _Agent, out VOAgent _VO)
            {
                m_Rivals.TryGetValue(_Agent, out VOAgent vo);
                _VO = vo;

                return _VO != null;
            }

            #endregion
        }

        #endregion

        #region Attributes

        readonly RivalsStorage m_Rivals = new RivalsStorage();

        List<IMovable> m_NearestMovers;

        readonly Nav3DAgentMover       m_Mover;
        readonly Nav3DAgentConfig m_Config;

        readonly float m_VelocityRadius;

        #endregion

        #region Constructors

        public MoversVOsProvider(Nav3DAgentMover _Mover, Nav3DAgentConfig _Config)
        {
            m_Mover        = _Mover;
            m_Config  = _Config;
            m_VelocityRadius = m_Config.Radius + m_Config.MaxSpeed;
        }

        #endregion

        #region Public methods

        public List<VOAgent> GetVOs()
        {
            TryUpdateNeighborMovers();
            
            int consideredMoversNumberLimit = m_Config.UseConsideredAgentsNumberLimit ? m_Config.ConsideredAgentsNumberLimit : m_NearestMovers.Count;

            List<VOAgent> nearestMoverVOs = new List<VOAgent>(consideredMoversNumberLimit);

            foreach (IMovable movable in m_NearestMovers)
            {
                if (!movable.NeedToBeAvoided)
                    continue;

                Vector3 otherMoverPos       = movable.GetPosition();
                float   otherVelocityRadius = movable.GetRadius() + movable.GetMaxSpeed();
                Vector3 moverPos            = m_Mover.GetPosition();

                Vector3 deltaPos          = otherMoverPos - moverPos;
                float   sqrProximity      = Vector3.SqrMagnitude(deltaPos);
                float   dangerDistanceSqr = UtilsMath.Sqr(otherVelocityRadius + m_VelocityRadius);

                //skip if proximity greater than next frame colliding distance
                if (sqrProximity > dangerDistanceSqr)
                    continue;

                float radiiSqrSum = UtilsMath.Sqr(m_Mover.GetRadius() + movable.GetRadius());

                //5 degrees angle threshold (in rads.)
                const float VELOCITY_CO_DIRECTIONAL_THRESHOLD = 0.349066f;
                Vector3     thisMoverLastVelocity             = m_Mover.GetLastNonZeroVelocity();
                Vector3     otherMoverLastVelocity            = movable.GetLastNonZeroVelocity();

                float velocitiesAngle = Mathf.Acos(Mathf.Clamp01(Vector3.Dot(thisMoverLastVelocity.normalized, otherMoverLastVelocity.normalized)));

                //agents intersects
                if (sqrProximity < radiiSqrSum)
                {
                    //the velocity vectors are coo-directed and the other agent is ahead
                    if (velocitiesAngle < VELOCITY_CO_DIRECTIONAL_THRESHOLD &&
                        new Plane(thisMoverLastVelocity, moverPos).GetSide(otherMoverPos))
                    {
                        m_Mover.SetAvoidanceInterrupt(true);

                        return new List<VOAgent>();
                    }
                }
                //agents does do not intersect
                else
                {
                    //skip if movement vectors are co-directional
                    if (velocitiesAngle < VELOCITY_CO_DIRECTIONAL_THRESHOLD)
                        continue;

                    //skip if velocity trajectories will not lead to collision in the next frame
                    if (movable.Avoiding && !UtilsMath.VelocityTrailIntersects(
                                otherMoverPos,
                                movable.GetLastFrameVelocity() * movable.GetORCATau(),
                                movable.GetRadius(),
                                moverPos,
                                m_Mover.GetLastFrameVelocity() * m_Mover.GetORCATau(),
                                m_Mover.GetRadius()
                            ))
                        continue;
                }

                // ReSharper disable once InconsistentNaming
                if (movable is Nav3DAgentMover agentMover)
                {
                    if (!m_Rivals.TryGetValidVO(agentMover, out VOAgent agentVO))
                    {
                        agentVO = CreateAgentVO(m_Mover, agentMover, m_Config.ORCATau);
                        agentVO.ComputeVO();

                        agentMover.UpdateRivalVO(m_Mover, agentVO.Flipped);
                    }

                    nearestMoverVOs.Add(agentVO);
                }

                if (movable is Nav3DSphereShellMover avoidingSphere)
                {
                    VOAgent agentVO = CreateAgentVO(m_Mover, avoidingSphere, m_Config.ORCATau);
                    agentVO.ComputeVO();
                    
                    nearestMoverVOs.Add(agentVO);
                }

                if (nearestMoverVOs.Count == consideredMoversNumberLimit)
                    break;
            }

            m_Rivals.Clear();
            return nearestMoverVOs;
        }

        public void UpdateRivalVO(Nav3DAgentMover _RivalMover, VOAgent _RivalVO)
        {
            m_Rivals.Add(_RivalMover, _RivalVO);
        }

        #if UNITY_EDITOR
        public void Draw()
        {
            Gizmos.color = Color.red;

            foreach (IMovable mover in m_NearestMovers)
            {
                Gizmos.DrawLine(m_Mover.GetPosition(), mover.GetPosition());
                Gizmos.DrawWireSphere(mover.GetPosition(), mover.GetRadius());
            }
        }
        #endif

        #endregion

        #region Service methods

        void TryUpdateNeighborMovers()
        {
            //update neighbor movers list if necessary
            if (!m_Mover.IsNeighborMoversDirty)
                return;

            HashSet<IMovable> nearestMovers = AgentManager.Instance.GetAgentBucketAgents(m_Mover);
            //there is possible the rare case when FillNearestMoversList executes earlier than agent was added in its bucket, so the _NearestMovers is empty
            m_NearestMovers ??= new List<IMovable>(Mathf.Max(nearestMovers.Count - 1, 0));

            float   minSqrDist    = float.MaxValue;
            Vector3 moverPosition = m_Mover.GetPosition();

            m_NearestMovers.Clear();
            m_NearestMovers.AddRange(
                    nearestMovers.Where(_Mover => _Mover.NeedToBeAvoided && _Mover != m_Mover).OrderBy(_Mover => (_Mover.GetPosition() - moverPosition).sqrMagnitude)
                );

            //reset flag
            m_Mover.SetNeighborMovablesDirty(false);
        }

        protected virtual VOAgent CreateAgentVO(IMovable _SolverAgent, IMovable _OtherAgent, float _Tau)
        {
            return new VOAgent(_SolverAgent, _OtherAgent, _Tau);
        }

        #endregion
    }
}
