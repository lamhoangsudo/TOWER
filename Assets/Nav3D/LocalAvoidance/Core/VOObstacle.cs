using Nav3D.Common;
using Nav3D.LocalAvoidance.SupportingMath;
using UnityEngine;
using Plane = Nav3D.LocalAvoidance.SupportingMath.Plane;

namespace Nav3D.LocalAvoidance
{
    public class VOObstacle : IVO
    {
        #region Attributes

        readonly IMovable m_Solver;
        readonly Triangle m_Triangle;

        ConeArced m_VoCone;

        readonly float m_Tau;

        #endregion
        
        #region Constructors
        
        public VOObstacle(IMovable _SolverAgent, Triangle _Triangle, float _Tau = 2f)
        {
            m_Solver   = _SolverAgent;
            m_Triangle = _Triangle;
            m_Tau      = _Tau;
        }
        
        #endregion

        #region Public methods

        public Plane GetORCA()
        {
            ComputeVO();

            Vector3 lastFrameVelocity = m_Solver.GetLastFrameVelocity();

            Vector3 u = GetU(lastFrameVelocity);

            Vector3 planePivotPoint = lastFrameVelocity + u;

            return new Plane(m_Triangle.Normal, planePivotPoint);
        }

        #endregion

        #region Service methods

        Vector3 GetU(Vector3 _InnerPoint)
        {
            Vector3 onConePoint = m_VoCone.GetClosestPoint(_InnerPoint);

            Vector3 u = onConePoint - _InnerPoint;

            return u;
        }

        void ComputeVO()
        {
            Vector3 solverPos = m_Solver.GetPosition();

            //closest predicted point (where will closest point relative to the future agent position)
            Vector3 closestPTrianglePoint = m_Triangle.Plane.ClosestPointOnPlane(solverPos + m_Solver.GetLastFrameVelocity() * m_Tau);

            Vector3 deltaPos  = closestPTrianglePoint - solverPos;
            float   radiusSum = m_Solver.GetRadius() * 2f;

            if (deltaPos.sqrMagnitude < UtilsMath.Sqr(radiusSum))
            {
                deltaPos = solverPos + deltaPos.normalized * (radiusSum + 0.00001f);
            }

            Vector3 conePivot = Vector3.zero;

            Sphere minkowskiSum = new Sphere(deltaPos, radiusSum);

            m_VoCone = new ConeArced(conePivot, minkowskiSum, m_Tau);
        }

        #endregion
    }
}