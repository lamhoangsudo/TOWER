using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System.Linq;

namespace Nav3D.Common
{
    class CurvesSpatialHashMap<T> where T : ICurve
    {
        #region Constants

        const string INVALID_BUCKET_SIZE = "Invalid bucket size: {0}. The bucket size must be greater than zero.";

        #endregion

        #region Attributes

        float m_BucketSize = 0;
        ConcurrentDictionary<T, HashSet<Vector3Int>> m_Curves = new ConcurrentDictionary<T, HashSet<Vector3Int>>();
        ConcurrentDictionary<Vector3Int, ConcurrentHashSet<T>> m_Buckets = new ConcurrentDictionary<Vector3Int, ConcurrentHashSet<T>>();

        #endregion

        #region Properties

        public int Count => m_Curves.Count;

        #endregion

        #region Constructors

        public CurvesSpatialHashMap(float _BucketSize)
        {
            m_BucketSize = _BucketSize;
        }

        #endregion

        #region Public methods

        public void SetBucketSize(float _BucketSize)
        {
            if (_BucketSize <= 0)
                throw new ArgumentException(string.Format(INVALID_BUCKET_SIZE, _BucketSize));

            List<T> curves = new List<T>(m_Curves.Keys);

            m_Curves.Clear();
            m_Buckets.Clear();

            m_BucketSize = _BucketSize;

            foreach (T curve in curves)
            {
                Register(curve);
            }
        }

        public void Register(T _Curve)
        {
            HashSet<Vector3Int> buckets = new HashSet<Vector3Int>();

            int bucketsMaxCount = 0;
            string bucketsInfo = string.Empty;

            foreach (Segment3 segment in _Curve.Segments)
            {
                List<Vector3Int> curBuckets = LineVoxelizer.GetVoxels(segment, m_BucketSize);

                if (curBuckets.Count > bucketsMaxCount)
                {
                    bucketsMaxCount = curBuckets.Count;
                    bucketsInfo = $"{segment.Start.ToStringExt()}, {segment.End.ToStringExt()}, {curBuckets.Count}";
                }

                buckets.AddRange(curBuckets);
            }

            m_Curves.TryAdd(_Curve, buckets);
            buckets.ForEach(_Bucket => m_Buckets.GetOrAdd(_Bucket, new ConcurrentHashSet<T>()).TryAdd(_Curve));
        }

        public void Update (T _Curve)
        {
            Unregister(_Curve);
            Register(_Curve);
        }

        public void Unregister(T _Curve)
        {
            if (!m_Curves.TryGetValue(_Curve, out HashSet<Vector3Int> buckets))
                return;

            foreach (Vector3Int bucket in buckets)
            {
                if (m_Buckets.TryGetValue(bucket, out ConcurrentHashSet<T> bucketSet))
                {
                    bucketSet.TryRemove(_Curve);

                    if (!bucketSet.Any())
                        m_Buckets.TryRemove(bucket, out _);
                }
            }

            bool removed = m_Curves.TryRemove(_Curve, out HashSet<Vector3Int> _);
        }

        public bool TryGetIntersectingCurves(Bounds _Bounds, out HashSet<T> _IntersectingCurves)
        {
            Vector3[] corners = _Bounds.GetCornerPoints();
            List<Vector3Int> cornerBuckets = corners.Select(_Corner => UtilsSpatialGrid.GetBucketIndex(_Corner, m_BucketSize)).ToList();

            HashSet<T> result = new HashSet<T>();

            int xMin = cornerBuckets.Min(_Bucket => _Bucket.x);
            int yMin = cornerBuckets.Min(_Bucket => _Bucket.y);
            int zMin = cornerBuckets.Min(_Bucket => _Bucket.z);
            int xMax = cornerBuckets.Max(_Bucket => _Bucket.x);
            int yMax = cornerBuckets.Max(_Bucket => _Bucket.y);
            int zMax = cornerBuckets.Max(_Bucket => _Bucket.z);

            for (int x = xMin; x <= xMax; x++)
                for (int y = yMin; y <= yMax; y++)
                    for (int z = zMin; z <= zMax; z++)
                    {
                        if (!m_Buckets.TryGetValue(new Vector3Int(x, y, z), out ConcurrentHashSet<T> curves))
                            continue;

                        foreach (T curve in curves.Where(_Curve => _Curve.Intersects(_Bounds)))
                        {
                            result.Add(curve);
                        }
                    }

            _IntersectingCurves = result;

            return _IntersectingCurves.Any();
        }

        public void Draw()
        {
            foreach(Vector3Int bucket in m_Buckets.Keys)
            {
                UtilsSpatialGrid.GetBucketBounds(bucket, m_BucketSize).Draw();
            }

            Gizmos.color = Color.red;

            foreach(KeyValuePair<T, HashSet<Vector3Int>> kvp in m_Curves)
            {
                foreach (Segment3 segment in kvp.Key.Segments)
                    Gizmos.DrawLine(segment.Start, segment.End);
            }
        }

        #endregion
    }
}
