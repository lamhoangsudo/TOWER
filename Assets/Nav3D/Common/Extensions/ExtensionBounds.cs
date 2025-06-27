using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Plane = Nav3D.LocalAvoidance.SupportingMath.Plane;
using Straight = Nav3D.LocalAvoidance.SupportingMath.Straight;

namespace Nav3D.Common
{
    public static class ExtensionBounds
    {
        #region Constants

        const string UNION_NO_PARAMS_ERROR = "There is no any parameters for the Bounds.Union() method.";

        #endregion

        #region Static methods

        public static string ToStringExt(this Bounds _Bounds)
        {
            return $"Center: {_Bounds.center.ToStringExt()}, Size: {_Bounds.size.ToStringExt()}";
        }
        
        public static void Draw(this Bounds _Bounds)
        {
            Gizmos.DrawWireCube(_Bounds.center, _Bounds.size);
        }
        
        public static void DrawSolid(this Bounds _Bounds)
        {
            Gizmos.DrawCube(_Bounds.center, _Bounds.size);
        }

        public static float GetMaxSize(this Bounds _Bounds)
        {
            return Mathf.Max(_Bounds.size.x, _Bounds.size.y, _Bounds.size.z);
        }
        
        public static float GetMinSize(this Bounds _Bounds)
        {
            return Mathf.Min(_Bounds.size.x, _Bounds.size.y, _Bounds.size.z);
        }

        public static bool DoesNotIntersects(this Bounds _Bounds, Bounds _Other)
        {
            Vector3 boundsMin = _Bounds.min;
            Vector3 boundsMax = _Bounds.max;

            Vector3 otherMin = _Other.min;
            Vector3 otherMax = _Other.max;

            return boundsMin.x > otherMax.x ||
                   boundsMax.x < otherMin.x ||
                   boundsMin.y > otherMax.y ||
                   boundsMax.y < otherMin.y ||
                   boundsMin.z > otherMax.z ||
                   boundsMax.z < otherMin.z;
        }

        public static Bounds TrianglesBounds(List<Triangle> _Triangles)
        {
            Bounds targetBounds = _Triangles.First().Bounds;

            if (_Triangles.Count == 1)
                return targetBounds;

            for (int i = 1; i < _Triangles.Count; i++)
            {
                targetBounds = targetBounds.Union(_Triangles[i].Bounds);
            }

            return targetBounds;
        }

        public static Bounds PointBounds(Vector3[] _Points)
        {
            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float zMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;
            float zMax = float.MinValue;

            for (int i = 0; i < _Points.Length; i++)
            {
                Vector3 point = _Points[i];

                xMin = Mathf.Min(xMin, point.x);
                yMin = Mathf.Min(yMin, point.y);
                zMin = Mathf.Min(zMin, point.z);
                xMax = Mathf.Max(xMax, point.x);
                yMax = Mathf.Max(yMax, point.y);
                zMax = Mathf.Max(zMax, point.z);
            }

            float sizeX = xMax - xMin;
            float sizeY = yMax - yMin;
            float sizeZ = zMax - zMin;

            Vector3 center = new Vector3(xMin + sizeX * 0.5f, yMin + sizeY * 0.5f, zMin + sizeZ * 0.5f);

            Vector3 size = new Vector3(sizeX, sizeY, sizeZ);

            return new Bounds(center, size);
        }

        public static bool Embrace(this Bounds _Bounds, Bounds _OtherBounds)
        {
            return _OtherBounds.GetCornerPoints().All(_Point => _Bounds.Contains(_Point));
        }

        public static Bounds MinMax(Vector3 _A, Vector3 _B)
        {
            float xMin = Mathf.Min(_A.x, _B.x);
            float yMin = Mathf.Min(_A.y, _B.y);
            float zMin = Mathf.Min(_A.z, _B.z);
            float xMax = Mathf.Max(_A.x, _B.x);
            float yMax = Mathf.Max(_A.y, _B.y);
            float zMax = Mathf.Max(_A.z, _B.z);

            float sizeX = xMax - xMin;
            float sizeY = yMax - yMin;
            float sizeZ = zMax - zMin;

            Vector3 center = new Vector3(xMin + sizeX * 0.5f, yMin + sizeY * 0.5f, zMin + sizeZ * 0.5f);

            Vector3 size = new Vector3(sizeX, sizeY, sizeZ);

            return new Bounds(center, size);
        }
        
        ///                Bounds A
        ///  _____________ /
        /// |      _______|_____
        /// |     |       |     |-- Bounds B
        /// |     |Inter- |     |
        /// |     |section|     |
        /// |_____|_______|     |
        ///       |_____________|
        public static Bounds Intersection(this Bounds _BoundsA, Bounds _BoundsB)
        {
            Vector3 AMin = new Vector3(
                Mathf.Min(_BoundsA.min.x, _BoundsA.max.x),
                Mathf.Min(_BoundsA.min.y, _BoundsA.max.y),
                Mathf.Min(_BoundsA.min.z, _BoundsA.max.z)
            );
            Vector3 AMax = new Vector3(
                Mathf.Max(_BoundsA.min.x, _BoundsA.max.x),
                Mathf.Max(_BoundsA.min.y, _BoundsA.max.y),
                Mathf.Max(_BoundsA.min.z, _BoundsA.max.z)
            );

            Vector3 BMin = new Vector3(
                Mathf.Min(_BoundsB.min.x, _BoundsB.max.x),
                Mathf.Min(_BoundsB.min.y, _BoundsB.max.y),
                Mathf.Min(_BoundsB.min.z, _BoundsB.max.z)
            );
            Vector3 BMax = new Vector3(
                Mathf.Max(_BoundsB.min.x, _BoundsB.max.x),
                Mathf.Max(_BoundsB.min.y, _BoundsB.max.y),
                Mathf.Max(_BoundsB.min.z, _BoundsB.max.z)
            );

            //check for intersection at all
            if (!_BoundsA.Intersects(_BoundsB))
                return new Bounds(Vector3.zero, Vector3.zero);

            //check if B nested inside A
            if (_BoundsA.Contains(BMin) && _BoundsA.Contains(BMax))
                return _BoundsB;
            //check if A nested inside B
            if (_BoundsB.Contains(AMin) && _BoundsB.Contains(AMax))
                return _BoundsA;

            //check special cases
            if (_BoundsB.Contains(AMin))
            {
                Vector3 min = AMin;
                Vector3 max;

                max.x = Mathf.Min(BMax.x, AMax.x);
                max.y = Mathf.Min(BMax.y, AMax.y);
                max.z = Mathf.Min(BMax.z, AMax.z);

                Vector3 center = min + (max - min) * 0.5f;

                return new Bounds(center, new Vector3(max.x - min.x, max.y - min.y, max.z - min.z));
            }

            if (_BoundsB.Contains(AMax))
            {
                Vector3 min;
                Vector3 max = AMax;

                min.x = Mathf.Max(AMin.x, BMin.x);
                min.y = Mathf.Max(AMin.y, BMin.y);
                min.z = Mathf.Max(AMin.z, BMin.z);

                Vector3 center = min + (max - min) * 0.5f;

                return new Bounds(center, new Vector3(max.x - min.x, max.y - min.y, max.z - min.z));
            }

            if (_BoundsA.Contains(BMin))
            {
                Vector3 min = BMin;
                Vector3 max;

                max.x = Mathf.Min(AMax.x, BMax.x);
                max.y = Mathf.Min(AMax.y, BMax.y);
                max.z = Mathf.Min(AMax.z, BMax.z);

                Vector3 center = min + (max - min) * 0.5f;

                return new Bounds(center, new Vector3(max.x - min.x, max.y - min.y, max.z - min.z));
            }

            if (_BoundsA.Contains(BMax))
            {
                Vector3 min;
                Vector3 max = BMax;

                min.x = Mathf.Max(AMin.x, BMin.x);
                min.y = Mathf.Max(AMin.y, BMin.y);
                min.z = Mathf.Max(AMin.z, BMin.z);

                Vector3 center = min + (max - min) * 0.5f;

                return new Bounds(center, new Vector3(max.x - min.x, max.y - min.y, max.z - min.z));
            }

            // Rare case
            //              B
            //    ________ /    A
            //  _|________|____/
            // | |        |    |
            // |_|________|____|
            //   |________|
            {
                Vector3 min;
                min.x = Mathf.Max(AMin.x, BMin.x);
                min.y = Mathf.Max(AMin.y, BMin.y);
                min.z = Mathf.Max(AMin.z, BMin.z);

                Vector3 max;
                max.x = Mathf.Min(AMax.x, BMax.x);
                max.y = Mathf.Min(AMax.y, BMax.y);
                max.z = Mathf.Min(AMax.z, BMax.z);

                Vector3 center = min + (max - min) * 0.5f;

                return new Bounds(center, new Vector3(max.x - min.x, max.y - min.y, max.z - min.z));
            }

        }

        ///                Bounds A
        ///  _____________ /                         ___________________ 
        /// |             |                         |                   |
        /// |      _______|_____                    |                   |
        /// |     |       |     |-- Bounds B   ==>  |                   |
        /// |_____|_______|     |                   |                   |
        ///       |             |                   |                   |
        ///       |_____________|                   |___________________|
        public static Bounds Union(this Bounds _BoundsA, Bounds _BoundsB)
        {
            _BoundsA.Encapsulate(_BoundsB.min);
            _BoundsA.Encapsulate(_BoundsB.max);

            return _BoundsA;
        }

        public static Bounds Union(params Bounds[] _Params)
        {
            if (!_Params.Any())
                throw new Exception(UNION_NO_PARAMS_ERROR);

            if (_Params.Length == 1)
                return _Params.First();

            Bounds result = _Params.First();

            for (int i = 1; i < _Params.Length; i++)
            {
                result = result.Union(_Params[i]);
            }

            return result;
        }

        public static Vector3[] GetCornerPoints(this Bounds _Bounds)
        {
            Vector3 center = _Bounds.center;
            Vector3 extents = _Bounds.extents;

            Vector3[] corners = new Vector3[8];

            corners[0] = center + Vector3.Scale(extents, new Vector3(-1, -1, -1));
            corners[1] = center + Vector3.Scale(extents, new Vector3(-1, -1, 1));
            corners[2] = center + Vector3.Scale(extents, new Vector3(-1, 1, -1));
            corners[3] = center + Vector3.Scale(extents, new Vector3(1, -1, -1));
            corners[4] = center + Vector3.Scale(extents, new Vector3(-1, 1, 1));
            corners[5] = center + Vector3.Scale(extents, new Vector3(1, -1, 1));
            corners[6] = center + Vector3.Scale(extents, new Vector3(1, 1, -1));
            corners[7] = center + Vector3.Scale(extents, new Vector3(1, 1, 1));

            return corners;
        }

        /// <summary>
        /// Returns the bounds which size components is a smallest multiple of multiplier greater to or equal to source bounds size components.
        /// </summary>
        public static Bounds CeilSizeToMultiple(this Bounds _Bounds, float _Multiple)
        {
            float sizeX = _Bounds.size.x;
            float sizeY = _Bounds.size.y;
            float sizeZ = _Bounds.size.z;

            int ceilX = (int)Mathf.Ceil(sizeX / _Multiple);
            int ceilY = (int)Mathf.Ceil(sizeY / _Multiple);
            int ceilZ = (int)Mathf.Ceil(sizeZ / _Multiple);

            sizeX = (ceilX + 2) * _Multiple;
            sizeY = (ceilY + 2) * _Multiple;
            sizeZ = (ceilZ + 2) * _Multiple;

            Vector3 size = new Vector3(sizeX, sizeY, sizeZ);

            return new Bounds(_Bounds.center, size);
        }

        public static Bounds Enlarge(this Bounds _Bounds, float _Enlargement)
        {
            return new Bounds(_Bounds.center, _Bounds.size + new Vector3(_Enlargement, _Enlargement, _Enlargement));
        }

        public static Vector3 GetClosestFaceCenterPlusNormal(this Bounds _Bounds, Vector3 _Point, out Vector3 _Normal)
        {
            float boundsMinX = Mathf.Min(_Bounds.min.x, _Bounds.max.x);
            float boundsMaxX = Mathf.Max(_Bounds.min.x, _Bounds.max.x);
            float boundsMinY = Mathf.Min(_Bounds.min.y, _Bounds.max.y);
            float boundsMaxY = Mathf.Max(_Bounds.min.y, _Bounds.max.y);
            float boundsMinZ = Mathf.Min(_Bounds.min.z, _Bounds.max.z);
            float boundsMaxZ = Mathf.Max(_Bounds.min.z, _Bounds.max.z);

            Vector3 center = _Bounds.center;

            float x,  y,  z;
            float dx, dy, dz;

            int xNormal, yNormal, zNormal;

            if (_Point.x >= boundsMaxX)
            {
                x       = boundsMaxX;
                dx      = _Point.x - boundsMaxX;
                xNormal = 1;
            }
            else if (_Point.x <= boundsMinX)
            {
                x       = boundsMinX;
                dx      = boundsMinX - _Point.x;
                xNormal = -1;
            }
            else
            {
                if (boundsMaxX - _Point.x <= _Point.x - boundsMinX)
                {
                    x       = boundsMaxX;
                    dx      = boundsMaxX - _Point.x;
                    xNormal = 1;
                }
                else
                {
                    x       = boundsMinX;
                    dx      = _Point.x - boundsMinX;
                    xNormal = -1;
                }
            }

            if (_Point.y >= boundsMaxY)
            {
                y       = boundsMaxY;
                dy      = _Point.y - boundsMaxY;
                yNormal = 1;
            }
            else if (_Point.y <= boundsMinY)
            {
                y       = boundsMinY;
                dy      = boundsMinY - _Point.y;
                yNormal = -1;
            }
            else
            {
                if (boundsMaxY - _Point.y <= _Point.y - boundsMinY)
                {
                    y       = boundsMaxY;
                    dy      = boundsMaxY - _Point.y;
                    yNormal = 1;
                }
                else
                {
                    y       = boundsMinY;
                    dy      = _Point.y - boundsMinY;
                    yNormal = -1;
                }
            }

            if (_Point.z >= boundsMaxZ)
            {
                z       = boundsMaxZ;
                dz      = _Point.z - boundsMaxZ;
                zNormal = 1;
            }
            else if (_Point.z <= boundsMinZ)
            {
                z       = boundsMinZ;
                dz      = boundsMinZ - _Point.z;
                zNormal = -1;
            }
            else
            {
                if (boundsMaxZ - _Point.z <= _Point.z - boundsMinZ)
                {
                    z       = boundsMaxZ;
                    dz      = boundsMaxZ - _Point.z;
                    zNormal = 1;
                }
                else
                {
                    z       = boundsMinZ;
                    dz      = _Point.z - boundsMinZ;
                    zNormal = -1;
                }
            }

            if (dx <= dy)
            {
                if (dx <= dz)
                {
                    _Normal = new Vector3(xNormal, 0, 0);
                    return new Vector3(x, center.y, center.z);
                }
                else
                {
                    _Normal = new Vector3(0, 0, zNormal);
                    return new Vector3(center.x, center.y, z);
                }
            }

            if (dy <= dz)
            {
                _Normal = new Vector3(0, yNormal, 0);
                return new Vector3(center.x, y, center.z);
            }
            else
            {
                _Normal = new Vector3(0, 0, zNormal);
                return new Vector3(center.x, center.y, z);
            }
        }
        
        /// <summary>
        /// Intersection cases:
        /// 1) The segment penetrates one or two faces of the bounds;
        /// 2) The segment is inside bounds;
        /// </summary> 
        public static bool IntersectSegment(this Bounds _Bounds, Segment3 _Segment3)
        {
            if (_Bounds.Contains(_Segment3.Start) || _Bounds.Contains(_Segment3.End))
                return true;

            float segmentStartX = _Segment3.Start.x;
            float segmentStartY = _Segment3.Start.y;
            float segmentStartZ = _Segment3.Start.z;

            float segmentEndX = _Segment3.End.x;
            float segmentEndY = _Segment3.End.y;
            float segmentEndZ = _Segment3.End.z;
            
            float boundsMinX = Mathf.Min(_Bounds.min.x, _Bounds.max.x);
            float boundsMaxX = Mathf.Max(_Bounds.min.x, _Bounds.max.x);
            float boundsMinY = Mathf.Min(_Bounds.min.y, _Bounds.max.y);
            float boundsMaxY = Mathf.Max(_Bounds.min.y, _Bounds.max.y);
            float boundsMinZ = Mathf.Min(_Bounds.min.z, _Bounds.max.z);
            float boundsMaxZ = Mathf.Max(_Bounds.min.z, _Bounds.max.z);

            Plane xMinPlane = new Plane(Vector3.right, new Vector3(boundsMinX, 0, 0));
            Plane xMaxPlane = new Plane(Vector3.left, new Vector3(boundsMaxX, 0, 0));
            Plane yMinPlane = new Plane(Vector3.up, new Vector3(0, boundsMinY, 0));
            Plane yMaxPlane = new Plane(Vector3.down, new Vector3(0, boundsMaxY, 0));
            Plane zMinPlane = new Plane(Vector3.forward, new Vector3(0, 0, boundsMinZ));
            Plane zMaxPlane = new Plane(Vector3.back, new Vector3(0, 0, boundsMaxZ));

            //Straight segStraight = new Straight(_Segment3.DirectionMagn, _Segment3.Origin);

            if (segmentStartX <= boundsMinX && segmentEndX >= boundsMinX ||
                segmentStartX >= boundsMinX && segmentEndX <= boundsMinX)
            {
                if (_Segment3.IntersectionWithPlane(xMinPlane, out Vector3 intersectionPoint) && 
                    intersectionPoint.y <= boundsMaxY && intersectionPoint.y >= boundsMinY &&
                    intersectionPoint.z <= boundsMaxZ && intersectionPoint.z >= boundsMinZ)
                    return true;
            }

            if (segmentStartX <= boundsMaxX && segmentEndX >= boundsMaxX ||
                segmentStartX >= boundsMaxX && segmentEndX <= boundsMaxX)
            {
                if (_Segment3.IntersectionWithPlane(xMaxPlane, out Vector3 intersectionPoint) && 
                    intersectionPoint.y <= boundsMaxY && intersectionPoint.y >= boundsMinY &&
                    intersectionPoint.z <= boundsMaxZ && intersectionPoint.z >= boundsMinZ)
                    return true;
            }

            if (segmentStartY <= boundsMinY && segmentEndY >= boundsMinY ||
                segmentStartY >= boundsMinY && segmentEndY <= boundsMinY)
            {
                if (_Segment3.IntersectionWithPlane(yMinPlane, out Vector3 intersectionPoint) && 
                    intersectionPoint.x <= boundsMaxX && intersectionPoint.x >= boundsMinX &&
                    intersectionPoint.z <= boundsMaxZ && intersectionPoint.z >= boundsMinZ)
                    return true;
            }

            if (segmentStartY <= boundsMaxY && segmentEndY >= boundsMaxY ||
                segmentStartY >= boundsMaxY && segmentEndY <= boundsMaxY)
            {
                if (_Segment3.IntersectionWithPlane(yMaxPlane, out Vector3 intersectionPoint) && 
                    intersectionPoint.x <= boundsMaxX && intersectionPoint.x >= boundsMinX &&
                    intersectionPoint.z <= boundsMaxZ && intersectionPoint.z >= boundsMinZ)
                    return true;
            }

            if (segmentStartZ <= boundsMinZ && segmentEndZ >= boundsMinZ ||
                segmentStartZ >= boundsMinZ && segmentEndZ <= boundsMinZ)
            {
                if (_Segment3.IntersectionWithPlane(zMinPlane, out Vector3 intersectionPoint) && 
                    intersectionPoint.x <= boundsMaxX && intersectionPoint.x >= boundsMinX &&
                    intersectionPoint.y <= boundsMaxY && intersectionPoint.y >= boundsMinY)
                    return true;
            }

            if (segmentStartZ <= boundsMaxZ && segmentEndZ >= boundsMaxZ ||
                segmentStartZ >= boundsMaxZ && segmentEndZ <= boundsMaxZ)
            {
                if (_Segment3.IntersectionWithPlane(zMaxPlane, out Vector3 intersectionPoint) && 
                    intersectionPoint.x <= boundsMaxX && intersectionPoint.x >= boundsMinX &&
                    intersectionPoint.y <= boundsMaxY && intersectionPoint.y >= boundsMinY)
                    return true;
            }

            return false;
        }
        
        /// <summary>
        /// Here we assume that intersection exist.
        /// -----------------------------------------------------
        ///          \  intersection 1
        ///       ____\/______ 
        ///      |     \      |
        ///      |      \     |
        ///      |       \    |
        ///      |________\___|
        ///               /\
        /// intersection 2  \
        /// </summary>
        public static List<Vector3> GetIntersection(this Bounds _Bounds, Segment3 _Segment3)
        {
            List<Vector3> intersections = new List<Vector3>();

            float boundsMinX = Mathf.Min(_Bounds.min.x, _Bounds.max.x);
            float boundsMaxX = Mathf.Max(_Bounds.min.x, _Bounds.max.x);
            float boundsMinY = Mathf.Min(_Bounds.min.y, _Bounds.max.y);
            float boundsMaxY = Mathf.Max(_Bounds.min.y, _Bounds.max.y);
            float boundsMinZ = Mathf.Min(_Bounds.min.z, _Bounds.max.z);
            float boundsMaxZ = Mathf.Max(_Bounds.min.z, _Bounds.max.z);

            Plane xMinPlane = new Plane(Vector3.right, new Vector3(boundsMinX, 0, 0));
            Plane xMaxPlane = new Plane(Vector3.left, new Vector3(boundsMaxX, 0, 0));
            Plane yMinPlane = new Plane(Vector3.up, new Vector3(0, boundsMinY, 0));
            Plane yMaxPlane = new Plane(Vector3.down, new Vector3(0, boundsMaxY, 0));
            Plane zMinPlane = new Plane(Vector3.forward, new Vector3(0, 0, boundsMinZ));
            Plane zMaxPlane = new Plane(Vector3.back, new Vector3(0, 0, boundsMaxZ));

            Straight rayStraight = new Straight(_Segment3.DirectionMagn, _Segment3.Origin);

            //ray start and end points are in opposite halfspaces of the plane 
            if (_Segment3.Start.x <= boundsMinX && _Segment3.End.x >= boundsMinX ||
               _Segment3.Start.x >= boundsMinX && _Segment3.End.x <= boundsMinX)
            {
                Vector3 intersectionPoint = xMinPlane.Intersection(rayStraight).First();

                if (intersectionPoint.y <= boundsMaxY && intersectionPoint.y >= boundsMinY &&
                    intersectionPoint.z <= boundsMaxZ && intersectionPoint.z >= boundsMinZ)
                {
                    intersections.Add(intersectionPoint);
                }
            }

            if (_Segment3.Start.x <= boundsMaxX && _Segment3.End.x >= boundsMaxX ||
               _Segment3.Start.x >= boundsMaxX && _Segment3.End.x <= boundsMaxX)
            {
                Vector3 intersectionPoint = xMaxPlane.Intersection(rayStraight).First();

                if (intersectionPoint.y <= boundsMaxY && intersectionPoint.y >= boundsMinY &&
                    intersectionPoint.z <= boundsMaxZ && intersectionPoint.z >= boundsMinZ)
                {
                    intersections.Add(intersectionPoint);
                }
            }

            if (_Segment3.Start.y <= boundsMinY && _Segment3.End.y >= boundsMinY ||
               _Segment3.Start.y >= boundsMinY && _Segment3.End.y <= boundsMinY)
            {
                Vector3 intersectionPoint = yMinPlane.Intersection(rayStraight).First();

                if (intersectionPoint.x <= boundsMaxX && intersectionPoint.x >= boundsMinX &&
                    intersectionPoint.z <= boundsMaxZ && intersectionPoint.z >= boundsMinZ)
                {
                    intersections.Add(intersectionPoint);
                }
            }

            if (_Segment3.Start.y <= boundsMaxY && _Segment3.End.y >= boundsMaxY ||
               _Segment3.Start.y >= boundsMaxY && _Segment3.End.y <= boundsMaxY)
            {
                Vector3 intersectionPoint = yMaxPlane.Intersection(rayStraight).First();

                if (intersectionPoint.x <= boundsMaxX && intersectionPoint.x >= boundsMinX &&
                    intersectionPoint.z <= boundsMaxZ && intersectionPoint.z >= boundsMinZ)
                {
                    intersections.Add(intersectionPoint);
                }
            }

            if (_Segment3.Start.z <= boundsMinZ && _Segment3.End.z >= boundsMinZ ||
               _Segment3.Start.z >= boundsMinZ && _Segment3.End.z <= boundsMinZ)
            {
                Vector3 intersectionPoint = zMinPlane.Intersection(rayStraight).First();

                if (intersectionPoint.x <= boundsMaxX && intersectionPoint.x >= boundsMinX &&
                    intersectionPoint.y <= boundsMaxY && intersectionPoint.y >= boundsMinY)
                {
                    intersections.Add(intersectionPoint);
                }
            }

            if (_Segment3.Start.z <= boundsMaxZ && _Segment3.End.z >= boundsMaxZ ||
               _Segment3.Start.z >= boundsMaxZ && _Segment3.End.z <= boundsMaxZ)
            {
                Vector3 intersectionPoint = zMaxPlane.Intersection(rayStraight).First();

                if (intersectionPoint.x <= boundsMaxX && intersectionPoint.x >= boundsMinX &&
                    intersectionPoint.y <= boundsMaxY && intersectionPoint.y >= boundsMinY)
                {
                    intersections.Add(intersectionPoint);
                }
            }

            return intersections;
        }

        #endregion
    }
}
