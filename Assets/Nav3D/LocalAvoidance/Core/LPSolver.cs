using Nav3D.Common;
using Nav3D.LocalAvoidance.SupportingMath;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nav3D.LocalAvoidance
{
    using Plane = SupportingMath.Plane;

    /// <summary>
    /// LP-solver class.
    /// Based on algorithm, described in original paper:
    /// M. de Berg, O. Cheong, M. van Kreveld, M. Overmars. Computational Geometry: Algorithms and Applications.
    /// </summary>
    public class LPSolver
    {
        #region Attributes

        static LPSolver m_Instance;

        #endregion
        
        #region Properties

        public static LPSolver Instance => m_Instance ??= new LPSolver();

        #endregion
        
        #region Nested types

        public abstract class Intersection
        {
            #region Factory

            public static Intersection Create(SpatialShape _Shape, ILine _Line)
            {
                IntersectionType shapeAndLineIntersection = _Shape.CheckIntersection(_Line);

                switch (shapeAndLineIntersection)
                {
                    case IntersectionType.INTERSECTION:
                        return new IntersectionPointSet(_Shape, _Line);
                    case IntersectionType.BELONGING:
                        return new IntersectionContinuous(_Shape, _Line);
                    default:
                        return null;
                }
            }

            #endregion

            #region Public methods

            public abstract Vector3? GetClosestFitPoint(Vector3 _Point, Constraint[] _Constraints, int _StartIndex, int _EndIndex);

#if UNITY_EDITOR
            public abstract void Visualize();
#endif

#endregion
        }

        class IntersectionContinuous : Intersection
        {
            #region Attributes

            ILine m_IntersectionOutline;

            #endregion

            #region Construction

            public IntersectionContinuous(SpatialShape _Shape, ILine _Line)
            {
                m_IntersectionOutline = _Line;
            }

            #endregion

            #region Intersection methods

            public override Vector3? GetClosestFitPoint(Vector3 _Point, Constraint[] _Constraints, int _StartIndex, int _EndIndex)
            {
                Vector3 optimalCandidate = m_IntersectionOutline.ClosestPoint(_Point);

                if (IsPointFitToRangeSet(_Constraints, _StartIndex, _EndIndex, optimalCandidate))
                    return optimalCandidate;

                List<Vector3> intersectionPoints = new List<Vector3>();

                _Constraints.ForEach(_Constraint =>
                {
                    if (_Constraint.MainShape.CheckIntersection(m_IntersectionOutline) == IntersectionType.INTERSECTION)
                        intersectionPoints.AddRange(m_IntersectionOutline.Intersection(_Constraint.MainShape));
                });

                return intersectionPoints.
                    Where(_IntersectionPoint => IsPointFitToRangeSet(_Constraints, _StartIndex, _EndIndex, _IntersectionPoint)).
                    OrderBy(_FitPoint => Vector3.SqrMagnitude(_FitPoint - _Point)).FirstOrDefault();
            }
#if UNITY_EDITOR
            public override void Visualize() => m_IntersectionOutline.Visualize();
#endif

#endregion
        }

        class IntersectionPointSet : Intersection
        {
            #region Attributes

            Vector3[] m_PointSet;

            #endregion

            #region Construction

            public IntersectionPointSet(SpatialShape _Shape, ILine _Line)
            {
                m_PointSet = _Line.Intersection(_Shape);
            }

            #endregion

            #region Intersection methods

            public override Vector3? GetClosestFitPoint(Vector3 _Point, Constraint[] _Constraints, int _StartIndex, int _EndIndex) =>
                m_PointSet.
                Where(_IntersectionPoint => IsPointFitToRangeSet(_Constraints, _StartIndex, _EndIndex, _IntersectionPoint)).
                OrderBy(_FitPoint => Vector3.SqrMagnitude(_FitPoint - _Point)).FirstOrDefault();

#if UNITY_EDITOR
            public override void Visualize() => m_PointSet.ForEach(_Point => Common.Debug.UtilsGizmos.DrawPoint(_Point, Color.red));
#endif

#endregion
        }

        public abstract class Constraint
        {
            #region Properties

            public abstract SpatialShape MainShape { get; }

            #endregion

            #region Public methods

            public abstract bool IsPointFitTo(Vector3 _Point);

            public abstract Vector3 FindBestBoundaryValue(Vector3 _Point);

            public abstract SpatialShape Intersection(Constraint _Other);

            public Intersection Intersection(ILine _Line) => LPSolver.Intersection.Create(MainShape, _Line);

            public abstract bool IsIntersect(Constraint _Other);

            public abstract bool IsIntersect(ILine _MathLine);

            #endregion
        }

        protected class ConstraintPlanar : Constraint
        {
            #region Attributes

            Plane m_Plane;

            #endregion

            #region Properties

            public override SpatialShape MainShape => m_Plane;
            public Plane Plane => m_Plane;

            #endregion

            #region Construction

            public ConstraintPlanar(Plane _ConstraintPlane)
            {
                m_Plane = _ConstraintPlane;
            }

            #endregion

            #region Constraint methods

            public override bool IsPointFitTo(Vector3 _Point)
            {
                return m_Plane.IsBelongsHalfPlane(_Point);
            }

            public override Vector3 FindBestBoundaryValue(Vector3 _Point)
            {
                return m_Plane.GetSurfaceClosestPoint(_Point).Point;
            }

            public override SpatialShape Intersection(Constraint _Other)
            {
                if (_Other is ConstraintPlanar planarConstraint)
                {
                    if (IsIntersect(planarConstraint))
                    {
                        return m_Plane.Intersection(planarConstraint.m_Plane);
                    }
                    else
                    {
                        Debug.LogError(m_Plane.Normal + "   " + planarConstraint.m_Plane.Normal);
                    }
                }

                if (_Other is ConstraintSpherical sphericalConstraint && IsIntersect(sphericalConstraint))
                    return new Circle(m_Plane, sphericalConstraint.Sphere);

                throw new NotImplementedException("[ConstraintLinear] Unknown intersection for type:" + _Other.GetType().FullName);
            }

            public override bool IsIntersect(Constraint _Other)
            {
                if (_Other is ConstraintPlanar linearConstraint)
                    return !Plane.IsPlanesParallel(m_Plane, linearConstraint.m_Plane);

                if (_Other is ConstraintSpherical sphericalConstraint)
                    return sphericalConstraint.Sphere.IsIntersect(m_Plane);

                throw new NotImplementedException("[ConstraintLinear] Unknown intersection for type:" + _Other.GetType().FullName);
            }

            public override bool IsIntersect(ILine _MathLine)
            {
                if (_MathLine is Circle circle)
                    return !Plane.IsPlanesParallel(Plane, circle.GeneratrixPlane);

                if (_MathLine is Straight straight)
                    return !Mathf.Approximately(0, Vector3.Dot(Plane.Normal, straight.Direction));

                throw new NotImplementedException("[ConstraintLinear] Unknown intersection for type:" + _MathLine.GetType().FullName);
            }

            #endregion
        }

        protected class ConstraintSpherical : Constraint
        {
            #region Attributes

            Sphere m_Sphere;

            #endregion

            #region Properties

            public override SpatialShape MainShape => m_Sphere;
            public Sphere Sphere => m_Sphere;

            #endregion

            #region Construction

            public ConstraintSpherical(Sphere _ConstraintSphere)
            {
                m_Sphere = _ConstraintSphere;
            }

            #endregion

            #region Constraint methods

            public override bool IsPointFitTo(Vector3 _Point)
            {
                return m_Sphere.IsPointInside(_Point);
            }

            public override Vector3 FindBestBoundaryValue(Vector3 _Point)
            {
                return m_Sphere.GetClosestPoint(_Point);
            }

            public override SpatialShape Intersection(Constraint _Other)
            {
                if (_Other is ConstraintPlanar planarConstraint)
                    return new Circle(planarConstraint.Plane, m_Sphere);

                throw new NotImplementedException("[ConstraintLinear] Unknown intersection for type:" + _Other.GetType().FullName);
            }

            public override bool IsIntersect(Constraint _Other)
            {
                if (_Other is ConstraintPlanar linearConstraint)
                    return m_Sphere.IsIntersect(linearConstraint.Plane);

                if (_Other is ConstraintSpherical sphericalConstraint)
                    return m_Sphere.IsIntersect(sphericalConstraint.Sphere);

                throw new NotImplementedException("[ConstraintSpherical] Unknown intersection for type:" + _Other.GetType().FullName);
            }

            public override bool IsIntersect(ILine _MathLine)
            {
                if (_MathLine is Straight straight)
                    return (straight.GetClosestPoint(Sphere.Center) - Sphere.Center).sqrMagnitude <= Sphere.SqrRadius;

                if (_MathLine is Circle circle)
                    return Sphere.IsIntersect(circle);

                throw new NotImplementedException("[ConstraintLinear] Unknown intersection for type:" + _MathLine.GetType().FullName);
            }

            #endregion
        }

        #endregion

        #region Public methods

        public Vector3 SolveMax(
            List<Plane> _PlaneConstraints,
            Sphere _SphereConstraint,
            Vector3 _Goal
            )
        {
            List<Constraint> constraints = new List<Constraint>(_PlaneConstraints.Count + 1);
            List<ConstraintPlanar> planarConstraints = new List<ConstraintPlanar>(_PlaneConstraints.Count);

            foreach (Plane plane in _PlaneConstraints)
            {
                ConstraintPlanar constraintPlanar = new ConstraintPlanar(plane);
                constraints.Add(constraintPlanar);
                planarConstraints.Add(constraintPlanar);
            }

            ConstraintSpherical constraintSpherical = new ConstraintSpherical(_SphereConstraint);

            if (_SphereConstraint != null)
                constraints.Add(constraintSpherical);

            // Try to find "clear" solution
            Vector3? primarySolution = SolveMax(
                constraints.ToArray(),
                _Goal
                );

            if (primarySolution.HasValue)
            {
                return GetPrimarySolution(primarySolution.Value);
            }
            // If there is no solution - we dealing with "Densely Packed Conditions" problem
            // So it is necessary to solve the problem with translating constraints (approach, described at section 5.3. in original paper).
            // "Reciprocal n-body Collision Avoidance"
            // Jur van den Berg, Stephen J. Guy, Ming Lin, and Dinesh Manocha
            else
            {
                return GetExtendedSolution(constraintSpherical, planarConstraints.ToArray(), _Goal);
            }
        }

        protected virtual Vector3 GetPrimarySolution(Vector3 _PrimarySolution)
        {
            return _PrimarySolution;
        }

        protected virtual Vector3 GetExtendedSolution(ConstraintSpherical _ConstraintSpherical, ConstraintPlanar[] _PlanarConstraints, Vector3 _GoalValue)
        {
            return SolveExtendedProblem(_ConstraintSpherical, _PlanarConstraints, _GoalValue);
        }

        public static bool IsPointFit(Plane[] _PlaneConstraints, Sphere _SphereConstraint, Vector3 _Point)
        {
            List<Constraint> constraints = _PlaneConstraints.Select(_Plane => new ConstraintPlanar(_Plane) as Constraint).ToList();

            if (_SphereConstraint != null)
                constraints.Add(new ConstraintSpherical(_SphereConstraint));

            return IsPointFitToRangeSet(constraints.ToArray(), 0, constraints.Count - 1, _Point);
        }

        #endregion

        #region Service methods

        static Vector3? SolveMax(Constraint[] _Constraints, Vector3 _GoalValue)
        {
            Vector3 currentValue = _GoalValue;

            //first constraint
            {
                Constraint firstConstraint = _Constraints[0];
                if (!firstConstraint.IsPointFitTo(currentValue))
                {
                    currentValue = firstConstraint.FindBestBoundaryValue(_GoalValue);
                }
            }

            //rest constraints
            {
                for (int i = 1; i < _Constraints.Length; i++)
                {
                    Constraint currentConstraint = _Constraints[i];

                    //point does not fit the constraint
                    if (!currentConstraint.IsPointFitTo(currentValue))
                    {
                        Vector3 bestCandidate = currentConstraint.FindBestBoundaryValue(_GoalValue);

                        if (IsPointFitToRangeSet(_Constraints, 0, i - 1, bestCandidate))
                            currentValue = bestCandidate;
                        else
                        {
                            Vector3? solution = SolveForSubset(_Constraints, 0, i - 1, currentConstraint, _GoalValue);

                            if (solution.HasValue)
                                currentValue = solution.Value;
                            else
                                return null;
                        }
                    }
                }
            }

            if (!IsPointFitToRangeSet(_Constraints, 0, _Constraints.Length - 1, currentValue))
                return null;

            return currentValue;
        }

        static (ConstraintPlanar Constraint, float Distance)[] ConstructDistanceSortedTupleArray(
            ConstraintPlanar[] _PlanarConstraints, ConstraintSpherical _SphericalConstraint) =>
            _PlanarConstraints
                .Select(_Constraint => (Constraint: _Constraint,
                    Distance: _Constraint.Plane.DistanceToPoint(_SphericalConstraint.Sphere.Center)))
                .OrderBy(_Tuple => _Tuple.Distance)
                .ToArray();

        Vector3 SolveExtendedProblem(ConstraintSpherical _SphericalConstraint, ConstraintPlanar[] _PlanarConstraints,
            Vector3 _GoalValue)
        {
            float sphericalRadius = _SphericalConstraint.Sphere.Radius;
            (ConstraintPlanar Constraint, float Distance)[] constraintPlanarsData =
                ConstructDistanceSortedTupleArray(_PlanarConstraints, _SphericalConstraint);

            float minDistance = constraintPlanarsData.First().Distance;

            Vector3? solutionCandidate;

            List<Constraint> translatedConstraints = new List<Constraint>();
            translatedConstraints.AddRange(_PlanarConstraints);
            translatedConstraints.Add(_SphericalConstraint);

            Constraint[] translatedConstraintsArray = translatedConstraints.ToArray();

            //first iteration
            if (minDistance < -sphericalRadius)
            {
                float delta = minDistance + sphericalRadius;
                _PlanarConstraints.ForEach(_PlanarConstraint =>
                {
                    _PlanarConstraint.Plane.Translate(_PlanarConstraint.Plane.Normal * (delta - 0.0001f));
                });

                solutionCandidate = SolveMax(translatedConstraintsArray, _GoalValue);

                if (solutionCandidate.HasValue)
                {
                    return solutionCandidate.Value;
                }
            }

            //rest iterations
            do
            {
                constraintPlanarsData = ConstructDistanceSortedTupleArray(_PlanarConstraints, _SphericalConstraint);

                IEnumerable<(ConstraintPlanar Constraint, float Distance)> filteredTuples =
                    constraintPlanarsData.Where(_Tuple => _Tuple.Distance < 0);

                float maxNegativeDistance = filteredTuples.Any()
                    ? filteredTuples.Max(_TupleFiltered => _TupleFiltered.Distance) - 0.0001f
                    : -constraintPlanarsData.Min(_Tuple => _Tuple.Distance);

                _PlanarConstraints.ForEach(_PlanarConstraint =>
                {
                    _PlanarConstraint.Plane.Translate(_PlanarConstraint.Plane.Normal * maxNegativeDistance);
                });

                solutionCandidate = SolveMax(translatedConstraintsArray, _GoalValue);
            } while (!solutionCandidate.HasValue);

            return solutionCandidate.Value;
        }

        static Vector3? SolveForSubset(Constraint[] _Constraints, int _StartIndex, int _EndIndex, Constraint _SolverConstraint, Vector3 _GoalValue)
        {
            float curSqrDistance = float.PositiveInfinity;
            Vector3 curSolution = _GoalValue;

            bool solutionExist = false;

            for (int i = _StartIndex; i <= _EndIndex; i++)
            {
                Constraint currentConstraint = _Constraints[i];

                if (!_SolverConstraint.IsIntersect(currentConstraint))
                    continue;

                SpatialShape intersection = _SolverConstraint.Intersection(currentConstraint);
                Vector3 solutionCandidate = intersection.GetClosestPoint(_GoalValue);

                //check closest point on line
                if (IsPointFitToRangeSet(_Constraints, _StartIndex, _EndIndex, solutionCandidate))
                {
                    solutionExist = true;

                    float sqrDistanceToGoal = (_GoalValue - solutionCandidate).sqrMagnitude;
                    if (sqrDistanceToGoal < curSqrDistance)
                    {
                        curSqrDistance = sqrDistanceToGoal;
                        curSolution = solutionCandidate;
                    }
                    continue;
                }

                //check intersection points with other constraints
                if (intersection is ILine intersectionLine)
                {
                    Vector3? intersectionBestPoint = GetBestPointForIntersectionLine(intersectionLine, _Constraints, 0, _Constraints.Length - 1, _GoalValue);
                    if (intersectionBestPoint.HasValue)
                    {
                        solutionExist = true;

                        float sqrDistanceToGoal = (_GoalValue - intersectionBestPoint.Value).sqrMagnitude;
                        if (sqrDistanceToGoal < curSqrDistance)
                        {
                            curSqrDistance = sqrDistanceToGoal;
                            curSolution = intersectionBestPoint.Value;
                        }
                    }
                }
            }

            return solutionExist ? (Vector3?)curSolution : null;
        }

        static Vector3? GetBestPointForIntersectionLine(ILine _IntersectionLine, Constraint[] _Constraints, int _StartIndex, int _EndIndex, Vector3 _Goal)
        {
            float curSqrDistance = float.PositiveInfinity;
            Vector3? curSolution = null;

            for (int i = _StartIndex; i <= _EndIndex; i++)
            {
                Constraint currentConstraint = _Constraints[i];

                if (!currentConstraint.IsIntersect(_IntersectionLine))
                    continue;

                Intersection constraintAndLineIntersection = Intersection.Create(currentConstraint.MainShape, _IntersectionLine);

                if (constraintAndLineIntersection == null)
                    continue;

                Vector3? point = constraintAndLineIntersection.GetClosestFitPoint(_Goal, _Constraints, 0, _Constraints.Length - 1);

                if (!point.HasValue)
                    continue;

                float sqrDistToCurPoint = (_Goal - point.Value).sqrMagnitude;
                if (sqrDistToCurPoint <= curSqrDistance)
                {
                    curSqrDistance = sqrDistToCurPoint;
                    curSolution = point.Value;
                }
            }

            return curSolution;
        }

        static bool IsPointFitToRangeSet(Constraint[] _Constraints, int _StartIndex, int _EndIndex, Vector3 _Point)
        {
            for (int i = _StartIndex; i <= _EndIndex; i++)
            {
                if (!_Constraints[i].IsPointFitTo(_Point))
                    return false;
            }

            return true;
        }

        #endregion
    }
}
