using Nav3D.API;
using Nav3D.Common;
using Nav3D.Pathfinding;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nav3D.Obstacles.Serialization;
using ConstructionStatus = Nav3D.Obstacles.GraphConstructionProgress.ConstructionStatus;

namespace Nav3D.Obstacles
{
    public partial class Octree
    {
        #region Constants

        public static float BUCKET_STEP_OVER_GAP = 0.00001f;

        readonly string EMPTY_LEAVES_ERROR = $"[{nameof(Octree)}]:{nameof(FindPathInternal)}: There is no embracing leaf: A: {{0}}, ALeaf is null: {{1}}, B: {{2}}, BLeaf is null: {{3}}";

        #endregion

        #region Constructors

        public Octree(
            ObstacleInfoBase          _ObstacleInfo,
            float                     _MinBucketSize,
            CancellationToken         _CancellationToken,
            GraphConstructionProgress _ConstructionProgress = null)
        {
            ObstacleInfo         = _ObstacleInfo;
            ConstructionProgress = _ConstructionProgress;

            BuildTree(_MinBucketSize, _CancellationToken);
        }

        public Octree(
            ObstacleInfoBase _ObstacleInfo,
            Node[]           _Roots,
            int              _LayersCount,
            float            _MinBucketSizeBase,
            float            _MinBucketSizeReal,
            int              _NodesCount)
        {
            ObstacleInfo = _ObstacleInfo;
            m_Roots      = _Roots;

            LayersCount         = _LayersCount;
            m_MinBucketSizeBase = _MinBucketSizeBase;
            m_MinBucketSizeReal = _MinBucketSizeReal;

            FillBucketSizesCache();

            NodeCount = _NodesCount;

            ConstructionProgress = GraphConstructionProgress.COMPLETED;
        }

        #endregion

        #region Attributes

        Node[] m_Roots;

        //min bucket size given at the octree init;
        float m_MinBucketSizeBase;

        //factual min bucket size taking into account particular regions min bucket size
        float m_MinBucketSizeReal;

        volatile int m_CurNodeID = -1;

        float[] m_BucketSizes;

        #endregion

        #region Properties

        public ObstacleInfoBase ObstacleInfo { get; private set; }
        public int              LayersCount  { get; private set; }
        public int              NodeCount    { get; private set; }

        public GraphConstructionProgress ConstructionProgress { get; private set; }

        int MaxLayer => LayersCount - 1;

        #endregion

        #region Public methods : Runtime operating

        public OctreePathfindingResult FindPath(
                Vector3           _PointA,
                Vector3           _PointB,
                bool              _TryRepositionStartToFreeLeaf,
                bool              _TryRepositionTargetToFreeLeaf,
                CancellationToken _CancellationTokenExternal,
                CancellationToken _CancellationTokenTimeout
            )
        {
            return FindPathInternal(_PointA, _PointB, _TryRepositionStartToFreeLeaf, _TryRepositionTargetToFreeLeaf, _CancellationTokenExternal, _CancellationTokenTimeout);
        }

        public bool SegmentIntersectOccupiedLeaf(Segment3 _Segment3)
        {
            return m_Roots.Any(_Root => SegmentIntersectOccupiedLeaf(_Root, _Segment3));
        }

        public bool PointInsideOccupiedLeaf(Vector3 _Point)
        {
            return m_Roots.Any(_Root => PointInsideOccupiedLeaf(_Root, _Point));
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
            m_Roots.ForEach(
                _Node => _Node.FillGizmosDrawData(
                    _GizmosDrawData,
                    _DrawOccupiedLeaves,
                    _DrawFreeLeaves,
                    _DrawGraph,
                    _DrawOccupiedLeavesAll,
                    _DrawOccupiedLeavesLayerNumber,
                    _DrawFreeLeavesAll,
                    _DrawFreeLeavesLayerNumber,
                    _DrawGraphNodesAll,
                    _DrawGraphLayerNumber)
            );

            if (_DrawRoots)
                m_Roots.ForEach(_RootNode =>
                {
                    GUIStyle style = new GUIStyle
                    {
                        normal =
                        {
                            textColor = Color.cyan
                        }
                    };

                    _GizmosDrawData.Add(new Common.Debug.GizmosWireCube(_RootNode.Bounds.center, _RootNode.Bounds.size, Color.cyan));
                    _GizmosDrawData.Add(new Common.Debug.GizmosLabel(_RootNode.Bounds.center, $"[ID: {_RootNode.ID}]", style));
                });

            _GizmosDrawData.Add(new Common.Debug.GizmosWireCube(ObstacleInfo.Bounds.center, ObstacleInfo.Bounds.size, Color.white));
        }

        #endif

        #endregion

        #region Public methods : Construction

        public float GetBucketSizeOnLayer(int _Layer)
        {
            return m_BucketSizes![_Layer];
        }

        public Vector3Int GetBucketIndexOnLayer(int _Layer, Leaf _Leaf)
        {
            return UtilsSpatialGrid.GetBucketIndex(_Leaf.Bounds.center, GetBucketSizeOnLayer(_Layer));
        }

        public float GetCrossingRegionsMinRes(Bounds _Bounds)
        {
            float particularMinBucketSize;

            if (ObstacleParticularResolutionManager.Instance.TryGetCrossingRegions(_Bounds, out HashSet<Nav3DParticularResolutionRegion> regions))
            {
                particularMinBucketSize = regions.Min(_Region => _Region.MinBucketSize);
            }
            else
            {
                return m_MinBucketSizeBase;
            }

            return GetClosestBucketSize(particularMinBucketSize, m_MinBucketSizeBase);
        }

        public bool TryGetEmbracingRegionsMinRes(Bounds _Bounds, out float _BucketSize, out int _RegionsCount)
        {
            float particularMinBucketSize;

            if (ObstacleParticularResolutionManager.Instance.TryGetEmbracingRegions(_Bounds, out HashSet<Nav3DParticularResolutionRegion> regions))
            {
                particularMinBucketSize = regions.Min(_Region => _Region.MinBucketSize);
                _RegionsCount           = regions.Count;
            }
            else
            {
                _BucketSize   = m_MinBucketSizeBase;
                _RegionsCount = 0;
                return false;
            }

            _BucketSize = GetClosestBucketSize(particularMinBucketSize, m_MinBucketSizeBase);
            return true;
        }

        public int GetLayersCount(float _MinBucketSize, out float _BoundsSize)
        {
            return GetLayersCount(ObstacleInfo.Bounds.GetMaxSize(), _MinBucketSize, out _BoundsSize);
        }

        public OctreeSerializable GetSerializableInstance(ObstacleSerializingProgress _Progress, int _ID)
        {
            NodeSerializable[] nodesSerializable = GetNodesSerializable(_Progress);

            return new OctreeSerializable(
                nodesSerializable,
                m_Roots.Select(_Root => _Root.ID).ToList(),
                LayersCount,
                m_MinBucketSizeBase,
                m_MinBucketSizeReal,
                _ID
            );
        }

        #endregion

        #region Service methods

        int GetNextNodeID()
        {
            return Interlocked.Increment(ref m_CurNodeID);
        }

        NodeSerializable[] GetNodesSerializable(ObstacleSerializingProgress _Progress)
        {
            //After the octree is built the m_CurNodeID stores the last created node number, so the total nodes count is m_CurNodeID + 1
            List<NodeSerializable> nodes = new List<NodeSerializable>(m_CurNodeID + 1);

            foreach (Node root in m_Roots)
            {
                root.GetSerializableInstances(nodes, _Progress);
            }

            return nodes.ToArray();
        }

        void BuildTree(float _MinBucketSize, CancellationToken _CancellationToken)
        {
            m_MinBucketSizeBase = _MinBucketSize;

            ConstructionProgress.SetStatus(ConstructionStatus.FILLING_TRIANGLE_STORAGE);

            TriangleStorage triangleStorage = new TriangleStorage(ObstacleInfo.Triangles, ConstructionProgress);

            ConstructionProgress.SetStatus(ConstructionStatus.TREE_CONSTRUCTION_PREPARATION);

            float particularMinBucketSize = _MinBucketSize;

            int baseLayersCount;

            int particularLayersCount = baseLayersCount = GetLayersCount(m_MinBucketSizeBase, out float boundsSize);

            bool hasCrossingParticularResRegions = ObstacleParticularResolutionManager.Instance.HasCrossingBoundables(ObstacleInfo.Bounds);

            if (hasCrossingParticularResRegions)
            {
                particularMinBucketSize = GetCrossingRegionsMinRes(ObstacleInfo.Bounds);

                if (particularMinBucketSize != m_MinBucketSizeBase)
                    particularLayersCount = GetLayersCount(particularMinBucketSize, out _);
            }

            //More levels => higher detail
            if (hasCrossingParticularResRegions && particularLayersCount > baseLayersCount)
            {
                LayersCount         = particularLayersCount;
                m_MinBucketSizeReal = particularMinBucketSize;
            }
            else
            {
                LayersCount         = baseLayersCount;
                m_MinBucketSizeReal = m_MinBucketSizeBase;
            }

            FillBucketSizesCache();

            SpatialGrid spatialGrid = new SpatialGrid(LayersCount, m_MinBucketSizeReal, ConstructionProgress);

            int parallelFactor = (int)(Mathf.Log(
                                              Mathf.ClosestPowerOfTwo((int)Mathf.Ceil(Mathf.Ceil(Mathf.Pow(2, baseLayersCount) *
                                                                                                 m_MinBucketSizeBase /
                                                                                                 m_MinBucketSizeBase) / 10f)), 2)
                                      ) - 1;

            ConstructionProgress.SetStatus(ConstructionStatus.TREE_CONSTRUCTION);

            BuildRoots(boundsSize, GetRootsIndices(boundsSize), triangleStorage, spatialGrid, baseLayersCount, parallelFactor, _CancellationToken);

            ConstructionProgress.SetStatus(ConstructionStatus.GRAPH_CONNECTIONS_BUILDING);

            spatialGrid.FormLeavesConnections(ConstructionProgress);

            NodeCount = m_CurNodeID + 1;

            ConstructionProgress.SetStatus(ConstructionStatus.FINISHED);
        }

        void BuildRoots(
            float             _RootSize,
            Vector3Int[]      _RootsIndices,
            TriangleStorage   _TriangleStorage,
            SpatialGrid       _SpatialGrid,
            int               _BaseLayersCount,
            int               _ParallelFactor,
            CancellationToken _CancellationToken)
        {
            try
            {
                ConstructionProgress.SetTotalRootsCount(GetTreeMaxPower(_RootsIndices.Length, MaxLayer));

                int maxLayer = _BaseLayersCount - 1;

                List<Task>              taskSet = new List<Task>(_RootsIndices.Length);
                ConcurrentHashSet<Node> roots   = new ConcurrentHashSet<Node>();

                foreach (Vector3Int rootIndex in _RootsIndices)
                {
                    Bounds rootBounds = UtilsSpatialGrid.GetBucketBounds(rootIndex, _RootSize);

                    taskSet.Add(Task.Run(() =>
                    {
                        bool needCheckMaxLayer = ObstacleParticularResolutionManager.Instance.HasCrossingBoundables(rootBounds);

                        if (needCheckMaxLayer && TryGetEmbracingRegionsMinRes(rootBounds, out float bucketSize, out int regionsCount))
                        {
                            //if whole fork is inside all regions then stop checking for children
                            if (ObstacleParticularResolutionManager.Instance.GetCrossingBoundablesCount(rootBounds) == regionsCount)
                            {
                                maxLayer          = GetLayersCount(bucketSize, out _) - 1;
                                needCheckMaxLayer = false;
                            }
                        }

                        Node root = Node.Create(
                            this,
                            _SpatialGrid,
                            _TriangleStorage,
                            _RootSize,
                            rootIndex,
                            0,
                            true,
                            GetNextNodeID,
                            maxLayer,
                            needCheckMaxLayer,
                            _CancellationToken,
                            _ParallelFactor
                        );

                        roots.TryAdd(root);
                    }, _CancellationToken));
                }

                Task.WaitAll(taskSet.ToArray());

                m_Roots = roots.ToArray();
            }
            catch (System.Exception _Exception)
            {
                Debug.LogException(_Exception);
            }
        }

        int GetTreeMaxPower(int _RootsCount, int _MaxLayer)
        {
            //Octree dimension is 8, layers are numbered starting from 0
            return _RootsCount * (int)Mathf.Pow(8, _MaxLayer);
        }

        Leaf GetClosestOrEmbracingLeaf(Vector3 _Point)
        {
            return m_Roots.MinBy(_Root => (_Root.Bounds.center - _Point).sqrMagnitude).GetClosestOrEmbracingLeaf(_Point);
        }

        Leaf GetNeighborFreeLeaf(Leaf _Leaf, Vector3 _InsidePoint)
        {
            Vector3 faceCenter = _Leaf.Bounds.GetClosestFaceCenterPlusNormal(_InsidePoint, out Vector3 normal);

            return GetClosestOrEmbracingLeaf(faceCenter + normal * m_MinBucketSizeReal * 0.5f);
        }

        OctreePathfindingResult FindPathInternal(
                Vector3           _PointA,
                Vector3           _PointB,
                bool              _TryRepositionStartToFreeLeaf,
                bool              _TryRepositionGoalToFreeLeaf,
                CancellationToken _CancellationTokenExternal,
                CancellationToken _CancellationTokenTimeout
            )
        {
            Leaf startNode = GetClosestOrEmbracingLeaf(_PointA);
            Leaf goalNode  = GetClosestOrEmbracingLeaf(_PointB);

            if (startNode == null || goalNode == null)
            {
                Debug.LogError(string.Format(EMPTY_LEAVES_ERROR, _PointA.ToStringExt(), startNode == null, _PointB.ToStringExt(), goalNode == null));

                return new OctreePathfindingResult(new List<Vector3> { _PointA, _PointB }, PathfindingResultCode.SUCCEEDED);
            }

            if (startNode.Occupied)
            {
                if (_TryRepositionStartToFreeLeaf)
                {
                    startNode = GetNeighborFreeLeaf(startNode, _PointA);

                    if (startNode.Occupied)
                        return new OctreePathfindingResult(null, PathfindingResultCode.START_POINT_INSIDE_OBSTACLE);
                }
                else
                    return new OctreePathfindingResult(null, PathfindingResultCode.START_POINT_INSIDE_OBSTACLE);
            }

            if (goalNode.Occupied)
            {
                if (_TryRepositionGoalToFreeLeaf)
                {
                    goalNode = GetNeighborFreeLeaf(goalNode, _PointB);

                    if (goalNode.Occupied)
                        return new OctreePathfindingResult(null, PathfindingResultCode.TARGET_POINT_INSIDE_OBSTACLE);
                }
                else
                    return new OctreePathfindingResult(null, PathfindingResultCode.TARGET_POINT_INSIDE_OBSTACLE);
            }

            //A* execution. Obtaining adjacent leaves sequence.
            AStar pathResolver = new AStar
            {
                StartLeaf                = startNode,
                GoalLeaf                 = goalNode,
                CancellationTokenControl = _CancellationTokenExternal,
                CancellationTokenTimeout = _CancellationTokenTimeout
            };

            PathResolverResult result = pathResolver.GetPath();

            if (result.ResultCode != PathfindingResultCode.SUCCEEDED)
                return new OctreePathfindingResult(null, result.ResultCode);

            List<Leaf>    leaves = result.Path;
            List<Vector3> points = new List<Vector3> { _PointA };

            //construct path by contact points at adjacent leaves.
            if (leaves.Any())
            {
                for (int i = 0; i < leaves.Count - 1; i++)
                {
                    Leaf curLeaf  = leaves[i];
                    Leaf nextLeaf = leaves[i + 1];

                    points.Add(curLeaf.NavigationBounds.center);
                    points.Add(curLeaf.GetAdjacentContactPoint(nextLeaf));
                }

                points.Add(leaves.Last().NavigationBounds.center);
            }

            points.Add(_PointB);

            return new OctreePathfindingResult(points, PathfindingResultCode.SUCCEEDED);
        }

        bool SegmentIntersectOccupiedLeaf(Node _CurrentNode, Segment3 _Segment3)
        {
            if (!_CurrentNode.Bounds.IntersectSegment(_Segment3))
                return false;

            if (_CurrentNode is Leaf leaf)
                return leaf.Occupied;

            return (_CurrentNode as Fork).ChildrenMap.Any(_ChildData => SegmentIntersectOccupiedLeaf(_ChildData.Value, _Segment3));
        }

        bool PointInsideOccupiedLeaf(Node _CurrentNode, Vector3 _Point)
        {
            if (_CurrentNode.Bounds.Contains(_Point))
            {
                if (_CurrentNode is Leaf leaf)
                    return leaf.Occupied;

                foreach (KeyValuePair<ForkChildOctIndex, Node> childData in (_CurrentNode as Fork).ChildrenMap)
                {
                    if (PointInsideOccupiedLeaf(childData.Value, _Point))
                        return true;
                }
            }

            return false;
        }

        Vector3Int[] GetRootsIndices(float _BoundSize)
        {
            return ObstacleInfo.Bounds.GetCornerPoints().Select(_Corner => UtilsSpatialGrid.GetBucketIndex(_Corner, _BoundSize)).Distinct().ToArray();
        }

        //determines the number of the layer, that has the greatest bucket size less
        //than the smallest bucket size among all resolution regions that embraces the given bounds
        float GetClosestBucketSize(float _Resolution, float _MinBucketSize)
        {
            float minBucketSize = _MinBucketSize;
            float multiplier;

            if (_Resolution < minBucketSize)
            {
                multiplier = 0.5f;

                do
                {
                    minBucketSize *= multiplier;
                } while (minBucketSize > _Resolution);
            }
            else if (_Resolution > minBucketSize)
            {
                multiplier = 2f;

                while (minBucketSize < _Resolution)
                {
                    minBucketSize *= multiplier;
                }
            }

            return minBucketSize;
        }

        int GetLayersCount(float _MaxBucketSize, float _MinBucketSize, out float _BoundsSize)
        {
            int   currentLayerNum   = 1;
            float currentBucketSize = _MinBucketSize;

            while (currentBucketSize < _MaxBucketSize && currentLayerNum < 10)
            {
                currentBucketSize *= 2f;
                currentLayerNum++;
            }

            _BoundsSize = currentBucketSize;

            return currentLayerNum;
        }

        void FillBucketSizesCache()
        {
            m_BucketSizes = new float[LayersCount];

            for (int layerNum = 0; layerNum < LayersCount; layerNum++)
            {
                m_BucketSizes[layerNum] = m_MinBucketSizeReal * Mathf.Pow(2, LayersCount - layerNum - 1);
            }
        }

        #endregion
    }
}