using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Nav3D.Obstacles;

namespace Nav3D.Common
{
    class SpatialHashMap<T> where T : IMovable
    {
        #region Attributes

        float m_BucketSize;

        Dictionary<Vector3Int, HashSet<T>> m_Buckets = new Dictionary<Vector3Int, HashSet<T>>();
        Dictionary<Vector3Int, HashSet<T>> m_BucketsEmbracing = new Dictionary<Vector3Int, HashSet<T>>();

        Dictionary<T, Vector3Int> m_BucketLinks = new Dictionary<T, Vector3Int>();

        #endregion

        #region Properties

        public float BucketSize => m_BucketSize;

        #endregion

        #region Constructors

        public SpatialHashMap(float _BucketSize, IEnumerable<T> _InitialSet)
        {
            Initialize(_BucketSize, _InitialSet);
        }

        #endregion

        #region Public methods

        public void Insert(T _Element)
        {
            if (m_BucketLinks.ContainsKey(_Element))
                return;

            Vector3Int bucket = UtilsSpatialGrid.GetBucketIndex(_Element.GetPosition(), m_BucketSize);

            AddToBucket(bucket, _Element);

            m_BucketLinks.Add(_Element, bucket);

            _Element.OnPositionChanged += UpdatePosition;
        }

        public void Remove(T _Element)
        {
            if (!m_BucketLinks.TryGetValue(_Element, out Vector3Int bucket))
                return;

            RemoveFromOldBuckets(_Element);

            m_BucketLinks.Remove(_Element);

            _Element.OnPositionChanged -= UpdatePosition;
        }

        public List<T> GetMovablesInBounds(Bounds _Bounds)
        {
            List<T> movables = new List<T>();

            float minX = Mathf.Min(_Bounds.min.x, _Bounds.max.x);
            float maxX = Mathf.Max(_Bounds.min.x, _Bounds.max.x);
            float minY = Mathf.Min(_Bounds.min.y, _Bounds.max.y);
            float maxY = Mathf.Max(_Bounds.min.y, _Bounds.max.y);
            float minZ = Mathf.Min(_Bounds.min.z, _Bounds.max.z);
            float maxZ = Mathf.Max(_Bounds.min.z, _Bounds.max.z);

            Vector3Int minIndices = UtilsSpatialGrid.GetBucketIndex(new Vector3(minX, minY, minZ), m_BucketSize);
            Vector3Int maxIndices = UtilsSpatialGrid.GetBucketIndex(new Vector3(maxX, maxY, maxZ), m_BucketSize);

            for (int x = minIndices.x; x <= maxIndices.x; x++)
            {
                for (int y = minIndices.y; y < maxIndices.y; y++)
                {
                    for (int z = minIndices.z; z < maxIndices.z; z++)
                    {
                        if (m_Buckets.TryGetValue(new Vector3Int(x, y, z), out HashSet<T> bucketMovables))
                            movables.AddRange(bucketMovables);
                    }
                }
            }

            return movables;
        }

        public bool HasNeighbors(T _Element)
        {
            if (!m_BucketLinks.TryGetValue(_Element, out Vector3Int bucket))
                return false;

            //exclude self element
            return m_BucketsEmbracing[bucket].Count > 1;
        }


        /// <summary>
        /// Determines element bucket, returns all bucket elements.
        /// Returns original collection link (not copy!!!)
        /// </summary>
        /// <param name="_Element">Element.</param>
        /// <returns>Elements in bucket.</returns>
        public HashSet<T> GetElementBucketElements(T _Element)
        {
            if (!m_BucketLinks.TryGetValue(_Element, out Vector3Int bucket))
                return new HashSet<T>();

            return m_BucketsEmbracing[bucket];
        }

        public HashSet<T> GetBucketElements(Vector3 _InsidePoint)
        {
            Vector3Int bucket = UtilsSpatialGrid.GetBucketIndex(_InsidePoint, m_BucketSize);

            if (m_BucketsEmbracing.TryGetValue(bucket, out HashSet<T> bucketElements))
                return bucketElements;

            return new HashSet<T>();
        }

        public bool HasNearestObstacles(T _Element, out Bounds _DangerBounds)
        {
            Vector3Int bucket = UtilsSpatialGrid.GetBucketIndex(_Element.GetPosition(), m_BucketSize);

            _DangerBounds = UtilsSpatialGrid.GetBucketBounds(bucket, m_BucketSize).Enlarge(_Element.GetStaticObstaclesDangerDistance() * 2);

            return ObstacleManager.Instance.BoundsCrossObstacles(_DangerBounds);
        }

        public void SetMovablesInBoundsObstacleDirty(Bounds _Bounds)
        {
            float minX = Mathf.Min(_Bounds.min.x, _Bounds.max.x);
            float maxX = Mathf.Max(_Bounds.min.x, _Bounds.max.x);
            float minY = Mathf.Min(_Bounds.min.y, _Bounds.max.y);
            float maxY = Mathf.Max(_Bounds.min.y, _Bounds.max.y);
            float minZ = Mathf.Min(_Bounds.min.z, _Bounds.max.z);
            float maxZ = Mathf.Max(_Bounds.min.z, _Bounds.max.z);

            Vector3Int minIndices = UtilsSpatialGrid.GetBucketIndex(new Vector3(minX, minY, minZ), m_BucketSize);
            Vector3Int maxIndices = UtilsSpatialGrid.GetBucketIndex(new Vector3(maxX, maxY, maxZ), m_BucketSize);

            for (int x = minIndices.x; x <= maxIndices.x; x++)
            {
                for (int y = minIndices.y; y < maxIndices.y; y++)
                {
                    for (int z = minIndices.z; z < maxIndices.z; z++)
                    {
                        if (m_BucketsEmbracing.TryGetValue(new Vector3Int(x, y, z), out HashSet<T> bucketMovables))
                            bucketMovables.ForEach(_Movable => _Movable.SetNeighborObstaclesDirty(true));
                    }
                }
            }
        }

        public void SetBucketSize(float _Size)
        {
            if (Mathf.Approximately(_Size, m_BucketSize) || _Size <= 0)
                return;

            ObstacleManager.Instance.RecreateObstacleTriangleStorages(_Size);

            Initialize(_Size, new List<T>(m_BucketLinks.Keys));
        }

        public void Draw()
        {
            foreach(KeyValuePair<T, Vector3Int> kvp in m_BucketLinks)
            {
                Vector3Int bucket = kvp.Value;

                Bounds bucketBounds          = UtilsSpatialGrid.GetBucketBounds(bucket, m_BucketSize);
                Bounds bucketEmbracingBounds = new Bounds(bucketBounds.center, new Vector3(m_BucketSize * 3f, m_BucketSize * 3f, m_BucketSize * 3f));
                
                //movable self bucket
                Gizmos.color = Color.green;
                bucketBounds.Draw();

                //movable locality bounds
                Gizmos.color = Color.magenta;
                bucketEmbracingBounds.Draw();
            }
        }

        #endregion
        
        #region Service methods

        void Initialize(float _BucketSize, IEnumerable<T> _InitialSet)
        {
            m_Buckets.Clear();
            m_BucketsEmbracing.Clear();
            m_BucketLinks.Clear();

            m_BucketSize = _BucketSize;

            foreach (T element in _InitialSet)
            {
                Insert(element);
            }
        }

        void AddToBucket(Vector3Int _Bucket, T _Element)
        {
            {
                if (m_Buckets.TryGetValue(_Bucket, out HashSet<T> bucketMovables))
                {
                    bucketMovables.ForEach(_Movable => _Movable.SetNeighborMovablesDirty(true));
                    bucketMovables.Add(_Element);
                }
                else
                    m_Buckets.GetOrAdd(_Bucket).Add(_Element);
            }

            foreach (Vector3Int neighbor in UtilsMath.NeighborBuckets)
            {
                Vector3Int bucketEmbracing = _Bucket + neighbor;

                if (m_BucketsEmbracing.TryGetValue(bucketEmbracing, out HashSet<T> bucketMovables))
                {
                    bucketMovables.ForEach(_Movable => _Movable.SetNeighborMovablesDirty(true));
                    bucketMovables.Add(_Element);
                }
                else
                    m_BucketsEmbracing.GetOrAdd(bucketEmbracing).Add(_Element);
            }

            _Element.SetNeighborObstaclesDirty(true);
            _Element.SetNeighborMovablesDirty(true);
        }

        void RemoveFromOldBuckets(T _Element)
        {
            if (!m_BucketLinks.TryGetValue(_Element, out Vector3Int bucket))
                return;

            {
                //remove element from old bucket
                if (m_Buckets.TryGetValue(bucket, out HashSet<T> bucketMovables))
                {
                    //remove from old bucket
                    bucketMovables.Remove(_Element);
                    bucketMovables.ForEach(_Movable => _Movable.SetNeighborMovablesDirty(true));

                    if (!bucketMovables.Any())
                        m_Buckets.Remove(bucket);
                }
            }

            foreach (Vector3Int neighbor in UtilsMath.NeighborBuckets)
            {
                Vector3Int bucketEmbracing = bucket + neighbor;

                if (!m_BucketsEmbracing.TryGetValue(bucketEmbracing, out HashSet<T> bucketMovables))
                    continue;

                bucketMovables.Remove(_Element);
                bucketMovables.ForEach(_Movable => _Movable.SetNeighborMovablesDirty(true));

                if (!bucketMovables.Any())
                    m_BucketsEmbracing.Remove(bucketEmbracing);
            }
        }

        //here we assume that new bucket is neighboring to the old one
        void UpdateBucket(Vector3Int _BucketOld, Vector3Int _BucketNew, T _Element)
        {
            {
                //remove element from old bucket
                if (m_Buckets.TryGetValue(_BucketOld, out HashSet<T> bucketOldMovables))
                {
                    //remove from old bucket
                    bucketOldMovables.Remove(_Element);

                    if (!bucketOldMovables.Any())
                        m_Buckets.Remove(_BucketOld);
                }
            }

            foreach (Vector3Int neighbor in UtilsMath.NeighborBuckets)
            {
                {
                    Vector3Int bucketEmbracing = _BucketOld + neighbor;

                    if (m_BucketsEmbracing.TryGetValue(bucketEmbracing, out HashSet<T> bucketMovables))
                    {
                        bucketMovables.Remove(_Element);

                        bool isBucketEmbracingNeighboring = _BucketNew.IsNeighborBucket(bucketEmbracing);

                        if (!isBucketEmbracingNeighboring)
                            bucketMovables.ForEach(_Movable => _Movable.SetNeighborMovablesDirty(true));

                        if (!bucketMovables.Any())
                            m_BucketsEmbracing.Remove(bucketEmbracing);
                    }
                }
            }

            //add element to new bucket
            m_Buckets.GetOrAdd(_BucketNew).Add(_Element);

            foreach (Vector3Int neighbor in UtilsMath.NeighborBuckets)
            {
                Vector3Int bucketEmbracing = _BucketNew + neighbor;

                if (m_BucketsEmbracing.TryGetValue(bucketEmbracing, out HashSet<T> bucketMovables))
                {
                    bucketMovables.Add(_Element);

                    bool isBucketEmbracingNeighboring = _BucketOld.IsNeighborBucket(bucketEmbracing);

                    if (!isBucketEmbracingNeighboring)
                        bucketMovables.ForEach(_Movable => _Movable.SetNeighborMovablesDirty(true));
                }
                else
                    m_BucketsEmbracing.GetOrAdd(bucketEmbracing).Add(_Element);
            }

            _Element.SetNeighborMovablesDirty(true);
            _Element.SetNeighborObstaclesDirty(true);
        }

        void UpdatePosition(IMovable _Movable, Vector3 _NewPosition)
        {
            T element = (T)_Movable;

            if (!m_BucketLinks.TryGetValue(element, out Vector3Int bucket))
                return;

            Vector3Int newBucket = UtilsSpatialGrid.GetBucketIndex(_NewPosition, m_BucketSize);

            if (bucket == newBucket)
                return;

            UpdateBucket(bucket, newBucket, element);

            //update bucket link
            m_BucketLinks[element] = newBucket;
        }

        #endregion
    }
}
