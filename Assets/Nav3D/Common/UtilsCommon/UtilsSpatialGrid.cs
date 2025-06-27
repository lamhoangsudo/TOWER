using Nav3D.Obstacles;
using UnityEngine;

namespace Nav3D.Common
{
    public static class UtilsSpatialGrid
    {
        #region Public methods

        public static float GetBucketSizePOTFactored(float _MinBucketSize, int _PowerOfTwo)
        {
            return _MinBucketSize * Mathf.Pow(2, _PowerOfTwo);
        }

        public static float GetBucketSizeOnGridLayer(float _MinBucketSize, int _Layer, int _LayersCount)
        {
            return GetBucketSizePOTFactored(_MinBucketSize, _LayersCount - _Layer - 1);
        }

        public static Vector3Int GetBucketIndex(Vector3 _Point, float _BucketSize)
        {
            return new Vector3Int(
                Mathf.FloorToInt(_Point.x / _BucketSize),
                Mathf.FloorToInt(_Point.y / _BucketSize),
                Mathf.FloorToInt(_Point.z / _BucketSize)
            );
        }

        public static Bounds GetBucketBounds(Vector3 _InsidePoint, float _BucketSize)
        {
            return GetBucketBounds(GetBucketIndex(_InsidePoint, _BucketSize), _BucketSize);
        }

        public static Bounds GetBucketBounds(Vector3Int _Index, float _BucketSize)
        {
            return new Bounds(
                new Vector3(
                    _Index.x * _BucketSize + 0.5f * _BucketSize,
                    _Index.y * _BucketSize + 0.5f * _BucketSize,
                    _Index.z * _BucketSize + 0.5f * _BucketSize
                ),
                new Vector3(_BucketSize, _BucketSize, _BucketSize)
            );
        }

        public static Vector3Int GetParentIndex(Vector3Int _Index)
        {
            return new Vector3Int(
                Mathf.FloorToInt(_Index.x * 0.5f),
                Mathf.FloorToInt(_Index.y * 0.5f),
                Mathf.FloorToInt(_Index.z * 0.5f)
            );
        }

        public static (ForkChildOctIndex OctIndex, Vector3Int GridIndex)[] GetChildrenIndices1(Vector3Int _BucketIndex)
        {
            Vector3Int childBaseIndex = new Vector3Int(_BucketIndex.x * 2, _BucketIndex.y * 2, _BucketIndex.z * 2);

            return new[]
            {
                (ForkChildOctIndex.I000, childBaseIndex),
                (ForkChildOctIndex.I001, new Vector3Int(childBaseIndex.x /*+0*/, childBaseIndex.y, childBaseIndex.z + 1)),
                (ForkChildOctIndex.I010, new Vector3Int(childBaseIndex.x /*+0*/, childBaseIndex.y + 1, childBaseIndex.z /*+0*/)),
                (ForkChildOctIndex.I011, new Vector3Int(childBaseIndex.x /*+0*/, childBaseIndex.y + 1, childBaseIndex.z + 1)),
                (ForkChildOctIndex.I100, new Vector3Int(childBaseIndex.x + 1, childBaseIndex.y /*+0*/, childBaseIndex.z /*+0*/)),
                (ForkChildOctIndex.I101, new Vector3Int(childBaseIndex.x + 1, childBaseIndex.y /*+0*/, childBaseIndex.z + 1)),
                (ForkChildOctIndex.I110, new Vector3Int(childBaseIndex.x + 1, childBaseIndex.y + 1, childBaseIndex.z /*+0*/)),
                (ForkChildOctIndex.I111, new Vector3Int(childBaseIndex.x + 1, childBaseIndex.y + 1, childBaseIndex.z + 1)),
            };
        }

        #endregion
    }
}