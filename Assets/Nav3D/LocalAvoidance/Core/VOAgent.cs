using Nav3D.Common;
using Nav3D.LocalAvoidance.SupportingMath;
using UnityEngine;

namespace Nav3D.LocalAvoidance
{
    using Plane = SupportingMath.Plane;

    /// Reciprocal n-body Collision Avoidance
    /// Jur van den Berg, Stephen J. Guy, Ming Lin, and Dinesh Manocha
    /// https://gamma.cs.unc.edu/ORCA/publications/ORCA.pdf
    public class VOAgent : SpatialShape, IVO
    {
        #region Debug attributes

        Vector3 m_DeltaVOpt;
        Vector3 m_DebugU;

        #endregion

        #region Attributes

        ConeArced                   m_VoCone;
        protected readonly IMovable m_SolverAgent;
        readonly           IMovable m_OtherAgent;

        readonly float m_Tau;

        #endregion

        #region Properties

        public VOAgent Flipped => new VOAgent(m_VoCone.ZeroFlipped, m_OtherAgent, m_SolverAgent);

        #endregion

        #region Construction

        public VOAgent(IMovable _SolverAgent, IMovable _OtherAgent, float _Tau = 2f)
        {
            m_SolverAgent = _SolverAgent;
            m_OtherAgent  = _OtherAgent;
            m_Tau         = _Tau;
        }

        public VOAgent(ConeArced _VOCone, IMovable _SolverAgent, IMovable _OtherAgent)
        {
            m_VoCone      = _VOCone;
            m_SolverAgent = _SolverAgent;
            m_OtherAgent  = _OtherAgent;
        }

        #endregion

        #region Public methods

        public virtual void ComputeVO()
        {
            Vector3 agentsDeltaPos = m_OtherAgent.GetPosition() - m_SolverAgent.GetPosition();
            float   radiusSum      = m_SolverAgent.GetRadius()  + m_OtherAgent.GetRadius();

            //correct other agent position if agents intersect
            if (agentsDeltaPos.sqrMagnitude < UtilsMath.Sqr(radiusSum))
            {
                agentsDeltaPos = CorrectDeltaPosWithPushingOut(agentsDeltaPos, radiusSum);
            }

            Vector3 conePivot = Vector3.zero;

            Sphere minkowskiSum = new Sphere(agentsDeltaPos, radiusSum);

            m_VoCone = new ConeArced(conePivot, minkowskiSum, m_Tau);
        }

        public Plane GetORCA()
        {
            Vector3 deltaABVOpt = GetDeltaVOpt();
            Vector3 u           = /*DEBUG HUNK===>*/m_DebugU /*<===*/ = GetU(deltaABVOpt);

            Vector3 n = m_VoCone.GetClosestSurfaceNormal(deltaABVOpt);
            
            Vector3 planePivotPoint =
                m_SolverAgent.GetLastFrameVelocity() +
                //here we decide will other agent take the half of responsibility for collision avoidance or we solve the problem ourselves. 
                (m_OtherAgent.Avoiding ? 0.5f : 1f) * u;

            return new Plane(n, planePivotPoint);
        }

        #endregion

        #region Service methods

        protected virtual Vector3 CorrectDeltaPosWithPushingOut(Vector3 _DeltaPos, float _RadiusSum)
        {
            //condition for "pushing out" behaviour in case if agents are inside each other
            return m_SolverAgent.GetPosition() + _DeltaPos.normalized * (_RadiusSum * 1.00001f);
        }

        Vector3 GetDeltaVOpt()
        {
            Vector3 delta = m_SolverAgent.GetLastFrameVelocity() - m_OtherAgent.GetLastFrameVelocity();

            return /*DEBUG HUNK===>*/m_DeltaVOpt = /*<===*/ delta;
        }

        Vector3 GetU(Vector3 _InnerPoint)
        {
            Vector3 onConePoint = m_VoCone.GetClosestPoint(_InnerPoint);

            Vector3 u = onConePoint - _InnerPoint;

            return u;
        }

        #endregion

        #region SpatialShape methods

        #if UNITY_EDITOR
        public override void Visualize()
        {
            using (Common.Debug.UtilsGizmos.ColorPermanence)
            {

                Plane planeORCA = GetORCA();

                //V-Aopt - V-Bopt
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(m_DeltaVOpt, 0.01f);
                //U
                Gizmos.DrawLine(m_DeltaVOpt, m_DeltaVOpt + m_DebugU);
                //ORCA
                planeORCA.Visualize();

                m_VoCone.Visualize();
            }
        }
        #endif

#endregion
    }
}