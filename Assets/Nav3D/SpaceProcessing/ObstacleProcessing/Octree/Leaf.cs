using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Nav3D.Common;
using Nav3D.Obstacles.Serialization;

namespace Nav3D.Obstacles
{
    public class Leaf : Node
    {
        #region Attributes

        readonly HashSet<Leaf> m_FreeAdjacents = new HashSet<Leaf>();
        
        Bounds? m_NavigationBounds;

        #endregion

        #region Properties

        public bool          Occupied      => m_Occupied;
        public HashSet<Leaf> FreeAdjacents => m_FreeAdjacents;

        public Bounds NavigationBounds => m_NavigationBounds ??= m_Octree.ObstacleInfo.Bounds.Embrace(Bounds) ? Bounds : m_Octree.ObstacleInfo.Bounds.Intersection(Bounds);
        
        public List<Vector3Int> FacesDirections { get; } = new List<Vector3Int>
        {
            Vector3Int.left,
            Vector3Int.right,
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.forward,
            Vector3Int.back
        };

        #endregion

        #region Constructors

        public Leaf(
            Octree      _Octree,
            SpatialGrid _SpatialGrid,
            float       _Size,
            Vector3Int  _Index,
            int         _GridLayer,
            bool        _Occupied,
            Func<int>   _GetID)
            :
            base(
                _Octree,
                _Size,
                _Index,
                _GridLayer,
                _Occupied,
                _GetID)
        {
            _SpatialGrid.AddLeaf(this);
            _SpatialGrid.SubtractLeafFromProgress(_GridLayer, true);
        }

        public Leaf(int _ID, Vector3Int _Index, float _Size, int _GridLayer, bool _Occupied)
            : base(_ID, _Index, _Size, _GridLayer, _Occupied)
        {
        }

        #endregion

        #region Public methods

        public override Leaf GetClosestOrEmbracingLeaf(Vector3 _Point)
        {
            return this;
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
            base.FillGizmosDrawData(
                _GizmosDrawData,
                _DrawOccupiedLeaves,
                _DrawFreeLeaves,
                _DrawGraph,
                _DrawOccupiedLeavesAll,
                _DrawOccupiedLeavesLayerNumber,
                _DrawFreeLeavesAll,
                _DrawFreeLeavesLayerNumber,
                _DrawGraphNodesAll,
                _DrawGraphLayerNumber);

            if (m_Occupied || !_DrawGraph)
                return;

            if (_DrawGraphNodesAll == 0 || _DrawGraphLayerNumber == m_GridLayer)
            {
                _GizmosDrawData.Add(new Common.Debug.GizmosSphereSolid(NavigationBounds.center, NavigationBounds.GetMinSize() * 0.05f, Color.magenta));

                foreach (Leaf leaf in m_FreeAdjacents)
                {
                    Vector3 contactPoint = GetAdjacentContactPointInternal(leaf);
                    _GizmosDrawData.Add(new Common.Debug.GizmosLine(NavigationBounds.center, contactPoint, Color.magenta));
                    _GizmosDrawData.Add(new Common.Debug.GizmosLine(contactPoint, leaf.NavigationBounds.center, Color.magenta));
                }
            }
        }

        #endif

        public void AddFreeAdjacent(Leaf _Adjacent)
        {
            m_FreeAdjacents.Add(_Adjacent);
        }

        public Vector3 GetAdjacentContactPoint(Leaf _OtherLeaf)
        {
            return GetAdjacentContactPointInternal(_OtherLeaf);
        }

        public void RemoveFaceConsideration(Vector3Int _Face)
        {
            FacesDirections.Remove(_Face);
        }

        public void SetFreeAdjacents(Dictionary<int, Node> _NodeMap, int[] _FreeAdjacentIDs)
        {
            foreach (int id in _FreeAdjacentIDs)
                if (_NodeMap.TryGetValue(id, out Node node))
                    m_FreeAdjacents.Add((Leaf)node);
        }

        public override void SetOctreeReference(Octree _Octree)
        {
            base.SetOctreeReference(_Octree);

            m_Index = m_Octree.GetBucketIndexOnLayer(m_GridLayer, this);
        }

        #endregion

        #region Service methods

        Vector3 GetAdjacentContactPointInternal(Leaf _OtherLeaf)
        {
            Vector3 navigationExtents = NavigationBounds.extents;
            Vector3 center            = NavigationBounds.center;

            if (m_GridLayer == _OtherLeaf.m_GridLayer)
            {
                if (m_Index.x != _OtherLeaf.m_Index.x)
                {
                    if (m_Index.x < _OtherLeaf.m_Index.x)
                        return center + new Vector3(navigationExtents.x, 0, 0);

                    return center + new Vector3(-navigationExtents.x, 0, 0);
                }

                if (m_Index.y != _OtherLeaf.m_Index.y)
                {
                    if (m_Index.y < _OtherLeaf.m_Index.y)
                        return center + new Vector3(0, navigationExtents.y, 0);

                    return center + new Vector3(0, -navigationExtents.y, 0);
                }

                if (m_Index.z != _OtherLeaf.m_Index.z)
                {
                    if (m_Index.z < _OtherLeaf.m_Index.z)
                        return center + new Vector3(0, 0, navigationExtents.z);

                    return center + new Vector3(0, 0, -navigationExtents.z);
                }

                return center;
            }

            Leaf smallerLeaf;
            Leaf biggerLeaf;

            Vector3Int smallerIndex;
            Vector3Int biggerIndex;

            int layersDelta = Mathf.Abs(m_GridLayer - _OtherLeaf.m_GridLayer);

            if (m_GridLayer < _OtherLeaf.m_GridLayer)
            {
                smallerLeaf = _OtherLeaf;
                biggerLeaf  = this;
            }
            else
            {
                smallerLeaf = this;
                biggerLeaf  = _OtherLeaf;
            }

            smallerIndex = smallerLeaf.m_Index;
            biggerIndex  = biggerLeaf.m_Index;

            //equalize layers for leaves
            for (int i = 0; i < layersDelta; i++)
                smallerIndex = UtilsSpatialGrid.GetParentIndex(smallerIndex);

            Vector3 smallerLeafNavigationExtents = smallerLeaf.NavigationBounds.extents;
            Vector3 smallerLeafCenter            = smallerLeaf.NavigationBounds.center;

            if (smallerIndex.x != biggerIndex.x)
            {
                if (smallerIndex.x < biggerIndex.x)
                    return smallerLeafCenter + new Vector3(smallerLeafNavigationExtents.x, 0, 0);

                return smallerLeafCenter + new Vector3(-smallerLeafNavigationExtents.x, 0, 0);
            }

            if (smallerIndex.y != biggerIndex.y)
            {
                if (smallerIndex.y < biggerIndex.y)
                    return smallerLeafCenter + new Vector3(0, smallerLeafNavigationExtents.y, 0);

                return smallerLeafCenter + new Vector3(0, -smallerLeafNavigationExtents.y, 0);
            }

            if (smallerIndex.z != biggerIndex.z)
            {
                if (smallerIndex.z < biggerIndex.z)
                    return smallerLeafCenter + new Vector3(0, 0, smallerLeafNavigationExtents.z);

                return smallerLeafCenter + new Vector3(0, 0, -smallerLeafNavigationExtents.z);
            }

            Debug.LogError(
                    $"{nameof(GetAdjacentContactPointInternal)}: Positive infinity. {smallerLeaf.m_GridLayer} {biggerLeaf.m_GridLayer}, {smallerIndex} {biggerIndex}"
                );

            return Vector3.positiveInfinity;
        }

        protected override NodeSerializable GetSerializableInstance()
        {
            return new LeafSerializable(m_ID, m_Index, m_Size, (byte) m_GridLayer, Occupied, m_FreeAdjacents.Select(_Adjacent => _Adjacent.ID).ToArray());
        }

        #endregion
    }
}