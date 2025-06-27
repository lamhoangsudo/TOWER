using System.Collections.Generic;
using UnityEngine;

namespace Nav3D.Common
{
    /// <summary>
    /// Implementation of the algorithm described in the original paper Amanatides & Woo - “A Fast Voxel Traversal Algorithm For Ray Tracing”
    /// http://www.cse.yorku.ca/~amana/research/grid.pdf
    /// </summary>
    public static class LineVoxelizer
    {
        #region Public methods

        public static List<Vector3Int> GetVoxels(Segment3 _Line, float _VoxelSize)
        {
            float tMax = 1f;
            List<Vector3Int> result = new List<Vector3Int>();

            Bounds startBucketBounds = UtilsSpatialGrid.GetBucketBounds(_Line.Start, _VoxelSize);

            Vector3 startBucketMin = startBucketBounds.min;
            Vector3 startBucketMax = startBucketBounds.max;

            Vector3 rayStart = _Line.Start;
            Vector3 rayEnd = _Line.Start + _Line.DirectionMagn;

            Vector3Int rayStartBucket = UtilsSpatialGrid.GetBucketIndex(rayStart, _VoxelSize);
            Vector3Int rayEndBucket   = UtilsSpatialGrid.GetBucketIndex(rayEnd, _VoxelSize);

            //Avoid rare case when ray's points differ too small, but lies at different buckets
            //Example: Start:(-3.481658E-13, 2, -7.499998), End: (0, 2, -7.5), Voxel size = 1.5f
            if (rayStartBucket != rayEndBucket)
            {
                if (_Line.DirectionMagn.sqrMagnitude < 0.00000001f)
                    return new List<Vector3Int> { rayStartBucket };
            }

            int current_X_index = rayStartBucket.x;
            int end_X_index = rayEndBucket.x;

            int stepX;
            float tDeltaX;
            float tMaxX;
            if (_Line.DirectionMagn.x > 0f)
            {
                stepX = 1;
                tDeltaX = _VoxelSize / _Line.DirectionMagn.x;
                tMaxX = Mathf.Abs((startBucketMax.x - rayStart.x) / _Line.DirectionMagn.x);
            }
            else if (_Line.DirectionMagn.x < 0f)
            {
                stepX = -1;
                tDeltaX = _VoxelSize / -_Line.DirectionMagn.x;
                tMaxX = Mathf.Abs((rayStart.x - startBucketMin.x) / _Line.DirectionMagn.x);
            }
            else
            {
                stepX = 0;
                tDeltaX = tMax;
                tMaxX = tMax;
            }

            int current_Y_index = rayStartBucket.y;
            int end_Y_index = rayEndBucket.y;

            int stepY;
            float tDeltaY;
            float tMaxY;
            if (_Line.DirectionMagn.y > 0f)
            {
                stepY = 1;
                tDeltaY = _VoxelSize / _Line.DirectionMagn.y;
                tMaxY = Mathf.Abs((startBucketMax.y - rayStart.y) / _Line.DirectionMagn.y);
            }
            else if (_Line.DirectionMagn.y < 0f)
            {
                stepY = -1;
                tDeltaY = _VoxelSize / -_Line.DirectionMagn.y;
                tMaxY = Mathf.Abs((rayStart.y - startBucketMin.y) / _Line.DirectionMagn.y);
            }
            else
            {
                stepY = 0;
                tDeltaY = tMax;
                tMaxY = tMax;
            }

            int current_Z_index = rayStartBucket.z;
            int end_Z_index = rayEndBucket.z;

            int stepZ;
            float tDeltaZ;
            float tMaxZ;
            if (_Line.DirectionMagn.z > 0f)
            {
                stepZ = 1;
                tDeltaZ = _VoxelSize / _Line.DirectionMagn.z;
                tMaxZ = Mathf.Abs((startBucketMax.z - rayStart.z) / _Line.DirectionMagn.z);
            }
            else if (_Line.DirectionMagn.z < 0f)
            {
                stepZ = -1;
                tDeltaZ = _VoxelSize / -_Line.DirectionMagn.z;
                tMaxZ = Mathf.Abs((rayStart.z - startBucketMin.z) / _Line.DirectionMagn.z);
            }
            else
            {
                stepZ = 0;
                tDeltaZ = tMax;
                tMaxZ = tMax;
            }

            //add start bucket
            result.Add(new Vector3Int(current_X_index, current_Y_index, current_Z_index));

            bool xAchieved = false;
            bool yAchieved = false;
            bool zAchieved = false;

            while (current_X_index != end_X_index || current_Y_index != end_Y_index || current_Z_index != end_Z_index)
            {
                if (tMaxX < tMaxY && tMaxX < tMaxZ)
                {
                    if (xAchieved)
                        break;

                    // X-axis traversal.
                    current_X_index += stepX;
                    tMaxX += tDeltaX;

                    if (current_X_index == end_X_index)
                        xAchieved = true;
                }
                else if (tMaxY < tMaxZ)
                {
                    if (yAchieved)
                        break;

                    // Y-axis traversal.
                    current_Y_index += stepY;
                    tMaxY += tDeltaY;

                    if (current_Y_index == end_Y_index)
                        yAchieved = true;
                }
                else
                {
                    if (zAchieved)
                        break;

                    // Z-axis traversal.
                    current_Z_index += stepZ;
                    tMaxZ += tDeltaZ;

                    if (current_Z_index == end_Z_index)
                        zAchieved = true;
                }

                result.Add(new Vector3Int(current_X_index, current_Y_index, current_Z_index));
            }

            return result;
        }

        #endregion
    }
}