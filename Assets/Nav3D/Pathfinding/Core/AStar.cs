using Nav3D.API;
using Nav3D.Obstacles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Nav3D.Pathfinding
{
    public class AStar
    {
        #region Nested types

        struct PricedLeaf : IComparable<PricedLeaf>
        {
            #region Properties

            public Leaf Leaf { get; private set; }
            public float GCost { get; private set; }
            public float HCost { get; private set; }

            #endregion

            #region Constructors

            public PricedLeaf(Leaf _Leaf, float _GCost, float _HCost)
            {
                Leaf = _Leaf;
                GCost = _GCost;
                HCost = _HCost;
            }

            #endregion

            #region Public methods

            public int CompareTo(PricedLeaf _Other)
            {
                float dif = (GCost - _Other.GCost);

                if (Mathf.Abs(dif) < 0.000001f)
                    return 0;

                return (dif > 0) ? 1 : -1;
            }

            #endregion
        }

        #endregion

        #region Attributes

        Vector3 m_Start;
        Vector3 m_Goal;

        Vector3 m_Direction;

        MinHeap<PricedLeaf> m_OpenList;

        #endregion

        #region Properties

        public Leaf StartLeaf { get; set; }
        public Leaf GoalLeaf { get; set; }

        public CancellationToken CancellationTokenTimeout { get; set; }
        public CancellationToken CancellationTokenControl { get; set; }

        #endregion

        #region Constructors

        public AStar()
        {
        }

        #endregion

        #region Public methods

        public PathResolverResult GetPath()
        {
            m_Direction = m_Goal - m_Start;
            m_Start     = StartLeaf.NavigationBounds.center;
            m_Goal      = GoalLeaf.NavigationBounds.center;

            m_OpenList = new MinHeap<PricedLeaf>();
            Dictionary<Leaf, float> closedList = new Dictionary<Leaf, float>();

            float h = 0;
            AddToOpenList(StartLeaf, h);

            while (m_OpenList.Count != 0)
            {
                if (CancellationTokenTimeout.IsCancellationRequested)
                    return new PathResolverResult(null, PathfindingResultCode.TIMEOUT);

                if (CancellationTokenControl.IsCancellationRequested)
                    return new PathResolverResult(null, PathfindingResultCode.CANCELLED);

                PricedLeaf minLeaf = m_OpenList.PopMin();

                if (closedList.ContainsKey(minLeaf.Leaf))
                    continue;

                if (minLeaf.Leaf.Equals(GoalLeaf))
                {
                    List<Leaf> path = ConstructPath(closedList);

                    if (CancellationTokenTimeout.IsCancellationRequested)
                        return new PathResolverResult(null, PathfindingResultCode.TIMEOUT);

                    if (CancellationTokenControl.IsCancellationRequested)
                        return new PathResolverResult(null, PathfindingResultCode.CANCELLED);

                    return new PathResolverResult(path, PathfindingResultCode.SUCCEEDED);
                }

                closedList.Add(minLeaf.Leaf, minLeaf.HCost);

                foreach (Leaf leaf in minLeaf.Leaf.FreeAdjacents)
                {
                    if (closedList.ContainsKey(leaf))
                        continue;

                    float hCost = HCost(minLeaf.Leaf, leaf, minLeaf.HCost);

                    AddToOpenList(leaf, hCost);
                }
            }

            //path does not exist
            return new PathResolverResult(null, PathfindingResultCode.PATH_DOES_NOT_EXIST);
        }

        public virtual void AddToOpenList(Leaf _Leaf, float _HCost)
        {
            m_OpenList.Add(new PricedLeaf(_Leaf, GCost(_Leaf, _HCost), _HCost));
        }

        public virtual List<Leaf> GetHistory()
        {
            return new List<Leaf> { };
        }

        #endregion

        #region Service methods

        List<Leaf> ConstructPath(Dictionary<Leaf, float> _ClosedList)
        {
            Leaf minLeaf = GoalLeaf;
            float minPrice = float.MaxValue;

            foreach (Leaf leaf in GoalLeaf.FreeAdjacents)
            {
                if (leaf == StartLeaf)
                    return new List<Leaf>() { StartLeaf, GoalLeaf };

                if (_ClosedList.TryGetValue(leaf, out float leafPrice) && leafPrice <= minPrice)
                {
                    minPrice = leafPrice;
                    minLeaf = leaf;
                }
            }

            if (Mathf.Abs(minPrice - float.MaxValue) < 0.000001f)
                minPrice = 0;

            List<Leaf> path = new List<Leaf>((int)Mathf.Round(minPrice) + 1) { GoalLeaf, minLeaf };

            while (minPrice > 0)
            {
                if (CancellationTokenTimeout.IsCancellationRequested || CancellationTokenControl.IsCancellationRequested)
                    return new List<Leaf>();

                foreach (Leaf leaf in path.Last().FreeAdjacents)
                {
                    if (_ClosedList.TryGetValue(leaf, out float leafPrice) && leafPrice < minPrice)
                    {
                        minPrice = leafPrice;
                        minLeaf = leaf;
                    }
                }

                path.Add(minLeaf);
            }

            path.Reverse();

            return path;
        }

        //Cost to goal
        float GCost(Leaf _Leaf, float _AccruedCost)
        {
            Vector3 leafCenter = _Leaf.NavigationBounds.center;

            const float w1 = 1;
            const float w2 = 1;
            const float w3 = 1;

            return
                //dist to goal
                w1 * ManhattanDistance(m_Goal, leafCenter) +
                //accumulated weight
                w2 * _AccruedCost +
                //penalty for deviation from straight from start to goal
                w3 * ManhattanDistance(Vector3.Project(leafCenter, m_Direction), leafCenter);
        }

        //Cost to start
        float HCost(Leaf _LeafA, Leaf _LeafB, float _AccruedCost)
        {
            return _AccruedCost + ManhattanDistance(_LeafA.NavigationBounds.center, _LeafB.NavigationBounds.center);
        }

        float ManhattanDistance(Vector3 _A, Vector3 _B)
        {
            return Mathf.Abs(_A.x - _B.x) + Mathf.Abs(_A.y - _B.y) + Mathf.Abs(_A.z - _B.z);
        }

        #endregion
    }
}
