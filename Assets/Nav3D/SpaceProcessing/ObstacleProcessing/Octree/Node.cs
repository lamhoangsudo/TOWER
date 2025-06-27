using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System;
using Nav3D.Common;
using Nav3D.Obstacles.Serialization;

namespace Nav3D.Obstacles
{
    public abstract class Node
    {
        #region Factory

        public static Node Create(
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
        {
            if (_Occupied && _GridLayer < _MaxGridLayer)
                return new Fork(
                    _Octree,
                    _SpatialGrid,
                    _ObstacleTriangles,
                    _Size,
                    _Index,
                    _GridLayer,
                    _Occupied,
                    _GetID,
                    _MaxGridLayer,
                    _NeedCheckMaxLayer,
                    _CancellationToken,
                    _ParallelFactor);

            return new Leaf(_Octree, _SpatialGrid, _Size, _Index, _GridLayer, _Occupied, _GetID);
        }

        #endregion

        #region Attributes

        protected          Octree     m_Octree;
        protected readonly float      m_Size;
        protected readonly int        m_GridLayer;
        protected          Vector3Int m_Index;
        protected readonly bool       m_Occupied;

        //The ID unique within Octree instance
        protected readonly int m_ID;

        Bounds? m_Bounds;

        #endregion

        #region Properties

        public int        GridLayer => m_GridLayer;
        public Vector3Int Index     => m_Index;
        public Bounds     Bounds    => m_Bounds ??= UtilsSpatialGrid.GetBucketBounds(m_Index, m_Size);
        public int        ID        => m_ID;

        #endregion

        #region Constructors

        protected Node(
            Octree     _Octree,
            float      _Size,
            Vector3Int _Index,
            int        _GridLayer,
            bool       _Occupied,
            Func<int>  _GetID)
        {
            m_Octree    = _Octree;
            m_Size      = _Size;
            m_Index     = _Index;
            m_GridLayer = _GridLayer;
            m_Occupied  = _Occupied;
            m_ID        = _GetID();
        }

        protected Node(int _ID, Vector3Int _Index, float _Size, int _GridLayer, bool _Occupied)
        {
            m_ID        = _ID;
            m_Index     = _Index;
            m_Size      = _Size;
            m_GridLayer = _GridLayer;
            m_Occupied  = _Occupied;
        }

        #endregion

        #region Public methods

        public abstract Leaf GetClosestOrEmbracingLeaf(Vector3 _Point);

        public virtual void SetOctreeReference(Octree _Octree)
        {
            m_Octree = _Octree;
        }

        public virtual void GetSerializableInstances(List<NodeSerializable> _Nodes, ObstacleSerializingProgress _Progress)
        {
            if (m_Octree.ConstructionProgress.CancellationToken.IsCancellationRequested)
                return;

            _Nodes.Add(GetSerializableInstance());

            _Progress.SetNodesPackingProgress(_Nodes.Count, m_Octree.NodeCount);
        }

        #if UNITY_EDITOR

        public virtual void FillGizmosDrawData(
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
            if (_DrawOccupiedLeaves && m_Occupied)
            {
                if (_DrawOccupiedLeavesAll == 0 || _DrawOccupiedLeavesLayerNumber == m_GridLayer)
                    _GizmosDrawData.Add(new Common.Debug.GizmosWireCube(Bounds.center, Bounds.size, Color.red));

                return;
            }

            if (_DrawFreeLeaves && !m_Occupied)
            {
                if (_DrawFreeLeavesAll == 0 || _DrawFreeLeavesLayerNumber == m_GridLayer)
                    _GizmosDrawData.Add(new Common.Debug.GizmosWireCube(Bounds.center, Bounds.size, Color.green));
            }
        }

        #endif

        #endregion

        #region Service methods

        protected bool IsOccupy(TriangleStorage _TriangleStorage, Bounds _Bounds)
        {
            return _TriangleStorage.IsIntersect(_Bounds);
        }

        protected abstract NodeSerializable GetSerializableInstance();

        #endregion
    }
}