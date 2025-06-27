using UnityEngine;
using Nav3D.Common;
using System.Collections.Generic;
using System.Linq;
using Nav3D.API;

namespace Nav3D.Obstacles
{
    public abstract class ObstacleInfoBase
    {
        #region Constructors

        protected ObstacleInfoBase() { }

        #endregion

        #region Properties

        /// <summary>
        /// Common bounds embracing all obstacle generative geometries.
        /// </summary>
        public Bounds Bounds { get; protected set; }
        /// <summary>
        /// All triangles compositing obstacle geometries.
        /// </summary>
        public List<Triangle> Triangles { get; protected set; }
        public abstract List<int> IDs { get; }

        #endregion

        #region Public methods

        public abstract void ReplaceID(int _OldID, int _NewID);

        public override string ToString()
        {
            return $"\t{GetType().Name}: IDs: {string.Join(", ", IDs)}\n";
        }

        public static List<ObstacleInfoBase> GroupInfos(List<ObstacleInfoBase> _ProcessingInfo)
        {
            List<ObstacleInfoBase> processingInfos = _ProcessingInfo.Copy();

            bool hasIntersections;

            do
            {
                hasIntersections = false;

                for (int i = 0; i < processingInfos.Count; i++)
                {
                    ObstacleInfoBase currentObstacleInfo = processingInfos[i];

                    for (int j = i + 1; j < processingInfos.Count; j++)
                    {
                        ObstacleInfoBase otherObstacleInfo = processingInfos[j];

                        if (currentObstacleInfo.Intersects(otherObstacleInfo))
                        {
                            processingInfos[i] = currentObstacleInfo.CombineWith(otherObstacleInfo);
                            processingInfos.RemoveAt(j);

                            hasIntersections = true;

                            break;
                        }
                    }

                    if (hasIntersections)
                        break;
                }
            } while (hasIntersections);

            return processingInfos;
        }

        public bool Intersects(ObstacleInfoBase _Other)
        {
            return Bounds.Intersects(_Other.Bounds);
        }

        public ObstacleInfoBase CombineWith(ObstacleInfoBase _OtherObstacleInfo)
        {
            return ObstacleInfoGrouped.CombineObstacleInfos(this, _OtherObstacleInfo);
        }

        #endregion

        #region Service methods

        protected void ComputeBounds()
        {
            Bounds trianglesBounds  = ExtensionBounds.TrianglesBounds(Triangles);
            
            float  actualBucketSize = TryGetEmbracingRegionsMaxRes(trianglesBounds);
            //At fist upscale bounds size to multiple of actual bucket size, then add side offsets equal to actual bucket size
            Bounds bounds = trianglesBounds.CeilSizeToMultiple(actualBucketSize).Enlarge(actualBucketSize);
            
            Bounds = new Bounds(bounds.center, bounds.size + new Vector3(Octree.BUCKET_STEP_OVER_GAP, Octree.BUCKET_STEP_OVER_GAP, Octree.BUCKET_STEP_OVER_GAP));
        }

        float TryGetEmbracingRegionsMaxRes(Bounds _Bounds)
        {
            float particularMinBucketSize;

            if (ObstacleParticularResolutionManager.Instance.TryGetCrossingRegions(_Bounds, out HashSet<Nav3DParticularResolutionRegion> regions))
            {
                particularMinBucketSize = regions.Max(_Region => _Region.MinBucketSize);
            }
            else
            {
                return ObstacleManager.Instance.MinBucketSize;
            }

            return GetClosestBucketSize(particularMinBucketSize, ObstacleManager.Instance.MinBucketSize);
        }

        float GetClosestBucketSize(float _Resolution, float _MinBucketSize)
        {
            if (_Resolution <= _MinBucketSize)
            {
                return _MinBucketSize;
            }

            float minBucketSize = _MinBucketSize;
            float multiplier    = 2f;

            while (minBucketSize < _Resolution)
            {
                minBucketSize *= multiplier;
            }

            return minBucketSize;
        }

        #endregion
    }
}