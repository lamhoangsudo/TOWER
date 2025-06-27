using UnityEngine;
using Plane = Nav3D.LocalAvoidance.SupportingMath.Plane;

namespace Nav3D.Common
{
    /// <summary>
    /// Represents a segment in space.
    /// </summary>
    public struct Segment3
    {
        #region Properties

        public Vector3 Start           { get; }
        public Vector3 End             { get; }
        public Vector3 Origin          { get; }
        public Vector3 DirectionNormal { get; }
        public Vector3 DirectionMagn   { get; }

        #endregion

        #region Constructors

        public Segment3(Vector3 _Start, Vector3 _End)
        {
            Start           = _Start;
            End             = _End;
            Origin          = _Start;
            DirectionMagn   = _End - _Start;
            DirectionNormal = DirectionMagn.normalized;
        }

        #endregion

        #region Public methods

        public bool IntersectionWithPlane(Plane _Plane, out Vector3 _Point)
        {
            float denominator = Vector3.Dot(DirectionMagn, _Plane.Normal);

            if (Mathf.Abs(denominator) < float.Epsilon)
            {
                _Point = Vector3.zero;
                return false;
            }

            float t = (-Vector3.Dot(Origin, _Plane.Normal) - _Plane.Distance) / denominator;

            if (t < 0 || t > 1)
            {
                _Point = Vector3.zero;
                return false;
            }

            _Point = (Origin + DirectionMagn * t).RoundWith5Precision();
            return true;
        }

        public Vector3 GetClosestPoint(Vector3 _Point)
        {
            Vector3 dir        = End    - Start;
            Vector3 toPointDir = _Point - Start;

            float magnitudeSqr = dir.sqrMagnitude;
            if (magnitudeSqr == 0f)
                return Start; // Segment is a point

            float t = Vector3.Dot(toPointDir, dir) / magnitudeSqr;
            t = Mathf.Clamp01(t); // Clamp to [0, 1] so it stays on the segment

            return Start + t * dir;
        }
        
        #endregion
    }
}