using UnityEngine;
using Nav3D.LocalAvoidance;
using Nav3D.Common;
using Nav3D.Obstacles.Serialization;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nav3D.Obstacles
{
    public partial class Obstacle : IBoundable
    {
        #region Attributes

        readonly float            m_MinBucketSize;
        readonly ObstacleInfoBase m_ObstacleInfo;
        Octree                    m_Octree;
        CancellationTokenSource   m_CTS;

        TriangleStorage           m_TriangleStorage;
        GraphConstructionProgress m_TriangleStorageConstructionProgress;

        #endregion

        #region Properties

        public bool             Doomed            { get; private set; }
        public ObstacleInfoBase ObstacleInfo      => m_ObstacleInfo;
        public Bounds           Bounds            => m_ObstacleInfo.Bounds;
        public int              OctreeLayersCount => m_Octree.LayersCount;

        /// <summary>
        /// Indicates whether the obstacle is loaded from a file. For such obstacles, the ability to remove/add on the scene in runtime is disabled.
        /// </summary>
        public bool IsStatic { get; private set; }

        public GraphConstructionProgress ConstructionProgress { get; private set; }

        public int NodesCount => m_Octree?.NodeCount ?? 0;

        #endregion

        #region Constructors

        public Obstacle(ObstacleInfoBase _ObstacleInfo, float _MinBucketSize)
        {
            m_ObstacleInfo  = _ObstacleInfo;
            m_MinBucketSize = _MinBucketSize;

            ConstructionProgress = GraphConstructionProgress.INITIAL;

            TryCreateTriangleStorage();
        }

        public Obstacle(ObstacleInfoBase _ObstacleInfo, Octree _Octree, float _MinBucketSize)
        {
            m_ObstacleInfo  = _ObstacleInfo;
            m_Octree        = _Octree;
            m_MinBucketSize = _MinBucketSize;

            IsStatic = true;

            ConstructionProgress = GraphConstructionProgress.COMPLETED;

            TryCreateTriangleStorage();
        }

        #endregion

        #region Public methods

        public OctreePathfindingResult FindPath(
                Vector3           _PointA,
                Vector3           _PointB,
                bool              _TryRepositionStartToFreeLeaf,
                bool              _TryRepositionTargetToFreeLeaf,
                CancellationToken _CancellationTokenExternal,
                CancellationToken _CancellationTokenTimeout
            )
        {
            return m_Octree.FindPath(_PointA, _PointB, _TryRepositionStartToFreeLeaf, _TryRepositionTargetToFreeLeaf, _CancellationTokenExternal, _CancellationTokenTimeout);
        }

        public bool PointInsideOccupiedLeaf(Vector3 _Point)
        {
            return m_Octree.PointInsideOccupiedLeaf(_Point);
        }

        public bool SegmentIntersectOccupiedLeaf(Segment3 _Segment3)
        {
            return Bounds.IntersectSegment(_Segment3) && m_Octree.SegmentIntersectOccupiedLeaf(_Segment3);
        }

        public void ConstructGraph()
        {
            m_CTS    = new CancellationTokenSource();
            m_Octree = new Octree(m_ObstacleInfo, m_MinBucketSize, m_CTS.Token, ConstructionProgress);
        }

        public void Invalidate()
        {
            m_CTS?.Cancel();

            if (m_TriangleStorageConstructionProgress != null)
                m_TriangleStorageConstructionProgress.CancelConstruction();

            Doomed = true;
        }

        public List<Triangle> GetIntersectedOccupiedTriangles(Bounds _Bounds)
        {
            return m_TriangleStorage.GetIntersectedTriangles(_Bounds);
        }

        public void RecreateTriangleStorage(float _BucketSize)
        {
            if (m_TriangleStorageConstructionProgress != null)
                m_TriangleStorageConstructionProgress.CancelConstruction();

            m_TriangleStorageConstructionProgress = GraphConstructionProgress.INITIAL;

            Task.Run(() =>
            {
                m_TriangleStorage = new TriangleStorage(ObstacleInfo.Triangles, m_TriangleStorageConstructionProgress, _BucketSize);
            });
        }

        #if UNITY_EDITOR
        public void FillGizmosDrawData(
            Common.Debug.GizmosDrawData _GizmosDrawData,
            bool                        _DrawOccupiedLeaves,
            bool                        _DrawFreeLeaves,
            bool                        _DrawGraph,
            bool                        _DrawRoots,
            int                         _DrawOccupiedLeavesAll,
            int                         _DrawOccupiedLeavesLayerNumber,
            int                         _DrawFreeLeavesAll,
            int                         _DrawFreeLeavesLayerNumber,
            int                         _DrawGraphNodesAll,
            int                         _DrawGraphLayerNumber)
        {
            m_Octree.FillGizmosDrawData(
                _GizmosDrawData,
                _DrawOccupiedLeaves,
                _DrawFreeLeaves,
                _DrawGraph,
                _DrawRoots,
                _DrawOccupiedLeavesAll,
                _DrawOccupiedLeavesLayerNumber,
                _DrawFreeLeavesAll,
                _DrawFreeLeavesLayerNumber,
                _DrawGraphNodesAll,
                _DrawGraphLayerNumber
            );
        }
        #endif

        public OctreeSerializable GetSerializableOctreeInstance(ObstacleSerializingProgress _Progress, int _ID)
        {
            return m_Octree.GetSerializableInstance(_Progress, _ID);
        }

        #endregion

        #region Service methods

        void TryCreateTriangleStorage()
        {
            float bucketSize = AgentManager.Instance.StorageBucketSize;

            if (bucketSize <= 0)
            {
                m_TriangleStorage = TriangleStorage.EMPTY;
                return;
            }

            RecreateTriangleStorage(bucketSize);
        }

        #endregion
    }
}