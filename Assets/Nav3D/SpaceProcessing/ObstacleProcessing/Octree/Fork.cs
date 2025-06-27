using Nav3D.Obstacles.Serialization;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.Linq;
using Nav3D.Common;
using UnityEngine;

namespace Nav3D.Obstacles
{
    public class Fork : Node
    {
        #region Attributes

        readonly Dictionary<ForkChildOctIndex, Node> m_ChildrenMap = new Dictionary<ForkChildOctIndex, Node>();

        #endregion

        #region Properties

        public Dictionary<ForkChildOctIndex, Node> ChildrenMap => m_ChildrenMap;

        #endregion

        #region Constructors

        public Fork(
            Octree            _Octree,
            SpatialGrid       _SpatialGrid,
            TriangleStorage   _ObstacleTriangles,
            float             _Size,
            Vector3Int        _Index,
            int               _GridLayer,
            bool              _Occupied,
            Func<int>         _GetID,
            int               _MaxGridLayer,
            bool              _NeedCheckMaxLayer,
            CancellationToken _CancellationToken,
            int               _ParallelFactor)
            :
            base(
                _Octree,
                _Size,
                _Index,
                _GridLayer,
                _Occupied,
                _GetID)
        {
            if (_CancellationToken.IsCancellationRequested)
                return;

            ProduceChildren(_MaxGridLayer, _GetID, _SpatialGrid, _ObstacleTriangles, _CancellationToken, _NeedCheckMaxLayer, _ParallelFactor);
        }

        public Fork(int _ID, Vector3Int _Index, float _Size, int _GridLayer, bool _Occupied)
            : base(_ID, _Index, _Size, _GridLayer, _Occupied)
        {
        }

        #endregion

        #region Public methods

        public override Leaf GetClosestOrEmbracingLeaf(Vector3 _Point)
        {
            Vector3 center = Bounds.center;

            if (_Point.z >= center.z)
            {
                if (_Point.y >= center.y)
                {
                    if (_Point.x >= center.x)
                        return m_ChildrenMap.TryGetValue(ForkChildOctIndex.I111, out Node child) ? child.GetClosestOrEmbracingLeaf(_Point) : null;
                    else
                        return m_ChildrenMap.TryGetValue(ForkChildOctIndex.I011, out Node child) ? child.GetClosestOrEmbracingLeaf(_Point) : null;
                }
                else
                {
                    if (_Point.x >= center.x)
                        return m_ChildrenMap.TryGetValue(ForkChildOctIndex.I101, out Node child) ? child.GetClosestOrEmbracingLeaf(_Point) : null;
                    else
                        return m_ChildrenMap.TryGetValue(ForkChildOctIndex.I001, out Node child) ? child.GetClosestOrEmbracingLeaf(_Point) : null;
                }
            }
            else
            {
                if (_Point.y >= center.y)
                {
                    if (_Point.x >= center.x)
                        return m_ChildrenMap.TryGetValue(ForkChildOctIndex.I110, out Node child) ? child.GetClosestOrEmbracingLeaf(_Point) : null;
                    else
                        return m_ChildrenMap.TryGetValue(ForkChildOctIndex.I010, out Node child) ? child.GetClosestOrEmbracingLeaf(_Point) : null;
                }
                else
                {
                    if (_Point.x >= center.x)
                        return m_ChildrenMap.TryGetValue(ForkChildOctIndex.I100, out Node child) ? child.GetClosestOrEmbracingLeaf(_Point) : null;
                    else
                        return m_ChildrenMap.TryGetValue(ForkChildOctIndex.I000, out Node child) ? child.GetClosestOrEmbracingLeaf(_Point) : null;
                }
            }
        }

        #if UNITY_EDITOR

        public override void FillGizmosDrawData(
            Common.Debug.GizmosDrawData _GizmosDrawData,
            bool                        _DrawOccupiedLeaves,
            bool                        _DrawFreeLeaves,
            bool                        _DrawGraph,
            int                         _DrawOccupiedLeavesAll,
            int                         _DrawOccupiedLeavesLayerNumber,
            int                         _DrawFreeLeavesAll,
            int                         _DrawFreeLeavesLayerNumber,
            int                         _DrawGraphNodesAll,
            int                         _DrawGraphLayerNumber)
        {
            m_ChildrenMap.ForEach(
                _ChildData => _ChildData.Value.FillGizmosDrawData(
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
        }

        #endif

        public override void GetSerializableInstances(List<NodeSerializable> _Nodes, ObstacleSerializingProgress _Progress)
        {
            base.GetSerializableInstances(_Nodes, _Progress);

            if (_Progress.CancellationToken.IsCancellationRequested)
                return;

            _Progress.SetNodesPackingProgress(_Nodes.Count, m_Octree.NodeCount);

            foreach (KeyValuePair<ForkChildOctIndex, Node> childData in m_ChildrenMap)
                childData.Value.GetSerializableInstances(_Nodes, _Progress);
        }

        public void SetChildren(Dictionary<int, Node> _NodeMap, (ForkChildOctIndex OctIndex, int ID)[] _ChildrenData)
        {
            foreach ((ForkChildOctIndex OctIndex, int ID) childData in _ChildrenData)
                if (_NodeMap.TryGetValue(childData.ID, out Node node))
                    m_ChildrenMap.Add(childData.OctIndex, node);
        }

        #endregion

        #region Service methods

        void ProduceChildren(
            int               _MaxGridLayer,
            Func<int>         _GetID,
            SpatialGrid       _SpatialGrid,
            TriangleStorage   _ObstacleTriangles,
            CancellationToken _CancellationToken,
            bool              _NeedCheckMaxLayer,
            int               _ParallelFactor)
        {
            bool parallelChildrenProduce;

            if (parallelChildrenProduce = _ParallelFactor > 0)
                _ParallelFactor--;

            List<Task> taskSet = new List<Task>();

            float childrenSize = m_Octree.GetBucketSizeOnLayer(m_GridLayer + 1);

            ConcurrentDictionary<ForkChildOctIndex, Node> tmpChildrenMap = new ConcurrentDictionary<ForkChildOctIndex, Node>();
            
            foreach ((ForkChildOctIndex OctIndex, Vector3Int GridIndex) childIndexData in UtilsSpatialGrid.GetChildrenIndices1(m_Index))
            {
                Bounds childBounds = UtilsSpatialGrid.GetBucketBounds(childIndexData.GridIndex, childrenSize);

                //check if bucket is out of obstacle bounds
                if (!m_Octree.ObstacleInfo.Bounds.Intersects(childBounds))
                {
                    _SpatialGrid.SubtractLeafFromProgress(m_GridLayer + 1, false);
                    continue;
                }

                bool needCheckMaxLayer =
                    _NeedCheckMaxLayer && ObstacleParticularResolutionManager.Instance.HasCrossingBoundables(childBounds);
                int maxLayer = _MaxGridLayer;

                if (needCheckMaxLayer)
                {
                    if (m_Octree.TryGetEmbracingRegionsMinRes(childBounds, out float bucketSize, out int regionsCount))
                    {
                        //if whole node is inside all regions then stop checking for
                        if (ObstacleParticularResolutionManager.Instance.GetCrossingBoundablesCount(childBounds) == regionsCount)
                        {
                            maxLayer          = m_Octree.GetLayersCount(bucketSize, out _) - 1;
                            needCheckMaxLayer = false;
                        }
                    }
                }

                if (!parallelChildrenProduce)
                {
                    Node newNode = Create(
                        m_Octree,
                        _SpatialGrid,
                        _ObstacleTriangles,
                        childrenSize,
                        childIndexData.GridIndex,
                        m_GridLayer + 1,
                        IsOccupy(_ObstacleTriangles, childBounds),
                        _GetID,
                        maxLayer,
                        needCheckMaxLayer,
                        _CancellationToken,
                        _ParallelFactor);
                    
                    m_ChildrenMap.Add(childIndexData.OctIndex, newNode);
                }
                else
                {
                    taskSet.Add(Task.Run(() =>
                    {
                        Node newNode = Create(
                            m_Octree,
                            _SpatialGrid,
                            _ObstacleTriangles,
                            childrenSize,
                            childIndexData.GridIndex,
                            m_GridLayer + 1,
                            IsOccupy(_ObstacleTriangles, childBounds),
                            _GetID,
                            maxLayer,
                            needCheckMaxLayer,
                            _CancellationToken,
                            _ParallelFactor);

                        tmpChildrenMap.TryAdd(childIndexData.OctIndex, newNode);
                    }, _CancellationToken));
                }
            }

            if (parallelChildrenProduce)
            {
                Task.WaitAll(taskSet.ToArray(), _CancellationToken);
                m_ChildrenMap.AddRange(tmpChildrenMap);
            }
        }

        protected override NodeSerializable GetSerializableInstance()
        {
            byte map = 0;
            int[] IDs = m_ChildrenMap.OrderBy(_KVP => _KVP.Key).Select(_KVP =>
            {
                map += (byte)_KVP.Key;
                return _KVP.Value.ID;
            }).ToArray();

            return new ForkSerializable(m_ID, m_Index, m_Size, (byte)m_GridLayer, m_Occupied, IDs, map);
        }

        #endregion
    }
}