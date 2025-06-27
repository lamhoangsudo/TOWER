using Nav3D.Common;
using System;
using UnityEngine;

namespace Nav3D.LocalAvoidance.SupportingMath
{
    public abstract class SpatialShape
    {
        #region Service methods

        #if UNITY_EDITOR

        public virtual void Visualize()
        {
            throw new NotImplementedException($"[{nameof(SpatialShape)}] Visualize() is not implemented");
        }

        #endif

        public virtual Vector3 GetClosestPoint(Vector3 _Point)
        {
            throw new NotImplementedException($"[{nameof(SpatialShape)}] GetClosestPoint() is not implemented");
        }

        public virtual bool IsIntersect(SpatialShape _Other)
        {
            throw new NotImplementedException($"[{nameof(SpatialShape)}] IsIntersect() is not implemented");
        }

        public virtual IntersectionType CheckIntersection(ILine _Line)
        {
            throw new NotImplementedException($"[{nameof(SpatialShape)}] CheckIntersection() is not implemented");
        }

        #endregion
    }
}