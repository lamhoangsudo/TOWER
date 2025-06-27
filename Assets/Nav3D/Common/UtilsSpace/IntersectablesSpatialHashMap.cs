using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System.Linq;
using System;

namespace Nav3D.Common
{
    /// <summary>
    /// Spatial storage for objects described by a prismatic boundary (Bounds) and having the ability to state the intersection with another Bounds.
    /// The storage is a spatial grid of segments.Each stored object is located in the segments with which it intersects.
    /// There are connections: object -> intersecting buckets and bucket -> intersecting objects.
    /// The size of storage segments is set during initialization.
    /// </summary>
    /// <typeparam name="T">Stored object.</typeparam>
    public class IntersectablesSpatialHashMap<T> where T : IBoundable, IBoundsIntersectable
    {
        #region Attributes

        int m_ElementsCount;
        int m_BucketsCount;
        int m_BucketsPerElement;
        int m_ElementsPerBucket;

        float m_BucketSize = 0;
        ConcurrentDictionary<T, HashSet<Vector3Int>> m_Elements;
        ConcurrentDictionary<Vector3Int, HashSet<T>> m_Buckets;

        #endregion

        #region Properties

        public int Count => m_Elements.Count;

        public List<T> Elements => m_Elements.Select(_Element => _Element.Key).ToList();

        #endregion

        #region Constructors

        public IntersectablesSpatialHashMap(float _BucketSize, int _ElementsCount = 31, int _BucketsCount = 31, int _BucketsPerElement = 4, int _ElementsPerBucket = 10)
        {
            m_BucketSize = _BucketSize;
            m_ElementsCount = _ElementsCount;
            m_BucketsCount = _BucketsCount;
            m_BucketsPerElement = _BucketsPerElement;
            m_ElementsPerBucket = _ElementsPerBucket;

            m_Elements = new ConcurrentDictionary<T, HashSet<Vector3Int>>(Environment.ProcessorCount - 1, m_ElementsCount);
            m_Buckets = new ConcurrentDictionary<Vector3Int, HashSet<T>>(Environment.ProcessorCount - 1, m_BucketsCount);
        }

        #endregion

        #region Public methods

        public List<T> GetIntersectedElements(Bounds _Bounds)
        {
            List<T> result = new List<T>();

            Vector3[] corners = _Bounds.GetCornerPoints();

            Vector3Int minBucket = GetBucketFromPoint(corners[0]);
            Vector3Int maxBucket = GetBucketFromPoint(corners[7]);

            for (int x = minBucket.x; x <= maxBucket.x; x++)
                for (int y = minBucket.y; y <= maxBucket.y; y++)
                    for (int z = minBucket.z; z <= maxBucket.z; z++)
                    {
                        Vector3Int bucket = new Vector3Int(x, y, z);

                        if (!m_Buckets.TryGetValue(bucket, out HashSet<T> elements))
                            continue;

                        foreach (T element in elements)
                        {
                            if (element.Intersects(_Bounds))
                                result.Add(element);
                        }
                    }

            return result;
        }

        public bool IsIntersect(Bounds _Bounds)
        {
            Vector3[] corners = _Bounds.GetCornerPoints();

            Vector3Int minBucket = GetBucketFromPoint(corners[0]);
            Vector3Int maxBucket = GetBucketFromPoint(corners[7]);

            for (int x = minBucket.x; x <= maxBucket.x; x++)
                for (int y = minBucket.y; y <= maxBucket.y; y++)
                    for (int z = minBucket.z; z <= maxBucket.z; z++)
                    {
                        Vector3Int bucket = new Vector3Int(x, y, z);

                        if (!m_Buckets.TryGetValue(bucket, out HashSet<T> elements))
                            continue;

                        foreach (T element in elements)
                        {
                            if (element.Intersects(_Bounds))
                                return true;
                        }
                    }

            return false;
        }

        public void Register(T _Element)
        {
            Bounds bounds = _Element.Bounds;

            Vector3[] corners = bounds.GetCornerPoints();
            Vector3Int minBucket;
            Vector3Int maxBucket;

            Vector3 minCorner = corners[0];
            Vector3 maxCorner = corners[7];

            minBucket = GetBucketFromPoint(minCorner);
            maxBucket = GetBucketFromPoint(maxCorner);

            for (int x = minBucket.x; x <= maxBucket.x; x++)
                for (int y = minBucket.y; y <= maxBucket.y; y++)
                    for (int z = minBucket.z; z <= maxBucket.z; z++)
                    {
                        Vector3Int bucket = new Vector3Int(x, y, z);

                        if (!_Element.Intersects(GetBucketBounds(bucket)))
                            continue;

                        AddToBucket(_Element, bucket);
                    }
        }

        public void Unregister(T _Element)
        {
            if (!m_Elements.TryGetValue(_Element, out HashSet<Vector3Int> buckets))
                return;

            foreach (Vector3Int bucket in buckets)
            {
                if (m_Buckets.TryGetValue(bucket, out HashSet<T> bucketSet))
                {
                    bucketSet.Remove(_Element);

                    if (!bucketSet.Any())
                        m_Buckets.TryRemove(bucket, out _);
                }
            }

            m_Elements.TryRemove(_Element, out HashSet<Vector3Int> _);
        }

        #endregion

        #region Service methods

        void AddToBucket(T _Element, Vector3Int _Bucket)
        {
            HashSet<T> bucketElements = m_Buckets.GetOrAdd(_Bucket, new HashSet<T>(/*m_ElementsPerBucket*/));
            lock (bucketElements)
            {
                bucketElements.Add(_Element);
            }

            HashSet<Vector3Int> elementBuckets = m_Elements.GetOrAdd(_Element, new HashSet<Vector3Int>(/*m_BucketsPerElement*/));
            lock (elementBuckets)
            {
                elementBuckets.Add(_Bucket);
            }
        }

        Bounds GetBucketBounds(Vector3Int _Bucket)
        {
            Vector3 center = new Vector3(_Bucket.x * m_BucketSize, _Bucket.y * m_BucketSize, _Bucket.z * m_BucketSize);
            Vector3 size = new Vector3(m_BucketSize, m_BucketSize, m_BucketSize);

            return new Bounds(center, size);
        }

        Vector3Int GetBucketFromPoint(Vector3 _Point)
        {
            int xI = (int)((Mathf.Abs(_Point.x) / m_BucketSize + 0.5f) * Mathf.Sign(_Point.x));
            int yI = (int)((Mathf.Abs(_Point.y) / m_BucketSize + 0.5f) * Mathf.Sign(_Point.y));
            int zI = (int)((Mathf.Abs(_Point.z) / m_BucketSize + 0.5f) * Mathf.Sign(_Point.z));

            return new Vector3Int(xI, yI, zI);
        }

        #endregion
    }
}