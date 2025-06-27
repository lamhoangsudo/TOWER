using UnityEngine;
using Nav3D.Common;
using System.Linq;
using System.Collections.Generic;

namespace Nav3D.Obstacles
{
    public class TriangleStorage
    {
        #region Constants

        public static TriangleStorage EMPTY => new TriangleStorage() { m_SpatialStorage = new IntersectablesSpatialHashMap<Triangle>(1) };

        #endregion

        #region Attributes

        IntersectablesSpatialHashMap<Triangle> m_SpatialStorage;

        #endregion

        #region Constructors

        TriangleStorage() { }

        public TriangleStorage(List<Triangle> _Triangles, GraphConstructionProgress _Progress = null, float? _BucketSize = null)
        {
            float maxTriangleSize = _Triangles.Max(_Triangle => _Triangle.Bounds.GetMaxSize());
            float minTriangleSize = _Triangles.Average(_Triangle => _Triangle.Bounds.GetMinSize());
            float storageBucketSize = _BucketSize ?? maxTriangleSize;

            int maxElementsPerBucket = Mathf.CeilToInt(storageBucketSize / minTriangleSize);

            m_SpatialStorage = new IntersectablesSpatialHashMap<Triangle>(storageBucketSize, _Triangles.Count, _Triangles.Count, 8, maxElementsPerBucket);

            int counter = 0;
            int counterStep = _Triangles.Count < 100 ? 1 :
                              _Triangles.Count < 1000 ? 10 :
                              _Triangles.Count < 10000 ? 100 :
                              1000;

            for (int i = 0; i <= _Triangles.Count - 1; i++)
            {
                m_SpatialStorage.Register(_Triangles[i]);
                counter++;

                if (counter == counterStep)
                {
                    if (_Progress?.CancellationToken.IsCancellationRequested ?? false)
                        return;

                    _Progress?.SetTrianglesStorageProgress(m_SpatialStorage.Count, _Triangles.Count);
                    counter = 0;
                }
            }
        }

        #endregion

        #region Public methods

        public bool IsIntersect(Bounds _Bounds)
        {
            return m_SpatialStorage.IsIntersect(_Bounds);
        }

        public List<Triangle> GetIntersectedTriangles(Bounds _Bounds)
        {
            return m_SpatialStorage.GetIntersectedElements(_Bounds);
        }

        #endregion
    }
}