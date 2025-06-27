using System.Collections.Generic;
using Nav3D.Common;
using UnityEngine;
using System.Linq;
using Nav3D.Obstacles.Serialization;

namespace Nav3D.Obstacles
{
    public class ObstacleInfoGrouped : ObstacleInfoBase
    {
        #region Constants

        static readonly string EXCEPTION_UNSUPPORTED_OBSTACLE_INFO_TYPE = $"[{nameof(CombineObstacleInfos)}]: Unsupported obstacle types: A:{{0}}, B{{1}}";

        #endregion

        #region Factory

        public static ObstacleInfoGrouped CombineObstacleInfos(ObstacleInfoBase _ObstacleInfoA, ObstacleInfoBase _ObstacleInfoB)
        {
            if (_ObstacleInfoA is ObstacleInfoSingle obstacleASingle)
            {
                if (_ObstacleInfoB is ObstacleInfoGrouped obstacleBGrouped)
                    return new ObstacleInfoGrouped(obstacleBGrouped, obstacleASingle);
                else if (_ObstacleInfoB is ObstacleInfoSingle obstacleBSingle)
                    return new ObstacleInfoGrouped(obstacleASingle, obstacleBSingle);
            }
            else if (_ObstacleInfoA is ObstacleInfoGrouped obstacleAGrouped)
            {
                if (_ObstacleInfoB is ObstacleInfoGrouped obstacleBGrouped)
                    return new ObstacleInfoGrouped(obstacleAGrouped, obstacleBGrouped);
                else if (_ObstacleInfoB is ObstacleInfoSingle obstacleBSingle)
                    return new ObstacleInfoGrouped(obstacleAGrouped, obstacleBSingle);
            }

            throw new System.ArgumentException(
                string.Format(EXCEPTION_UNSUPPORTED_OBSTACLE_INFO_TYPE,
                _ObstacleInfoA.GetType().FullName,
                _ObstacleInfoB.GetType().FullName
                ));
        }

        #endregion

        #region Attributes

        public List<int> m_IDs;

        #endregion

        #region Properties

        //stores obstacle controller ids, keeping their order
        public override List<int> IDs => m_IDs;
        public Dictionary<int, List<ObstacleInfoSingle>> ObstacleInfos { get; private set; }

        #endregion

        #region Constructors

        public ObstacleInfoGrouped(List<ObstacleInfoSingle> _ObstacleInfos, List<Triangle> _Triangles, Bounds _Bounds)
        {
            m_IDs = _ObstacleInfos.Select(_Info => _Info.ObstacleControllerID).ToList();
            ObstacleInfos = new Dictionary<int, List<ObstacleInfoSingle>>();

            foreach (ObstacleInfoSingle obstacleInfo in _ObstacleInfos)
            {
                ObstacleInfos.GetOrAdd(obstacleInfo.ObstacleControllerID).Add(obstacleInfo);
            }

            Triangles = _Triangles;
            Bounds = _Bounds;
        }

        ObstacleInfoGrouped(ObstacleInfoSingle _ObstacleA, ObstacleInfoSingle _ObstacleB)
        {
            m_IDs = new List<int> { _ObstacleA.ObstacleControllerID, _ObstacleB.ObstacleControllerID };
            ObstacleInfos = new Dictionary<int, List<ObstacleInfoSingle>>();

            ObstacleInfos.GetOrAdd(_ObstacleA.ObstacleControllerID).Add(_ObstacleA);
            ObstacleInfos.GetOrAdd(_ObstacleB.ObstacleControllerID).Add(_ObstacleB);

            Triangles = new List<Triangle>(_ObstacleA.Triangles.Count + _ObstacleB.Triangles.Count);
            Triangles.AddRange(_ObstacleA.Triangles);
            Triangles.AddRange(_ObstacleB.Triangles);

            ComputeBounds();
        }

        ObstacleInfoGrouped(ObstacleInfoGrouped _ObstacleGrouped, ObstacleInfoSingle _ObstacleSingle)
        {
            m_IDs = new List<int>(_ObstacleGrouped.m_IDs.Count + 1);
            m_IDs.AddRange(_ObstacleGrouped.m_IDs);
            m_IDs.Add(_ObstacleSingle.ObstacleControllerID);

            ObstacleInfos = new Dictionary<int, List<ObstacleInfoSingle>>(_ObstacleGrouped.ObstacleInfos.Count + 1);
            ObstacleInfos.AddRange(_ObstacleGrouped.ObstacleInfos);
            ObstacleInfos.GetOrAdd(_ObstacleSingle.ObstacleControllerID).Add(_ObstacleSingle);

            Triangles = new List<Triangle>(_ObstacleGrouped.Triangles.Count + _ObstacleSingle.Triangles.Count);
            Triangles.AddRange(_ObstacleGrouped.Triangles);
            Triangles.AddRange(_ObstacleSingle.Triangles);

            ComputeBounds();
        }

        ObstacleInfoGrouped(ObstacleInfoGrouped _ObstacleGroupedA, ObstacleInfoGrouped _ObstacleGroupedB)
        {
            m_IDs = new List<int>(_ObstacleGroupedA.m_IDs.Count + _ObstacleGroupedB.m_IDs.Count);
            m_IDs.AddRange(_ObstacleGroupedA.m_IDs);
            m_IDs.AddRange(_ObstacleGroupedB.m_IDs);

            ObstacleInfos = new Dictionary<int, List<ObstacleInfoSingle>>(_ObstacleGroupedA.ObstacleInfos.Count + _ObstacleGroupedB.ObstacleInfos.Count);
            ObstacleInfos.AddRange(_ObstacleGroupedA.ObstacleInfos);
            _ObstacleGroupedB.ObstacleInfos.ForEach(_Kvp => ObstacleInfos.GetOrAdd(_Kvp.Key).AddRange(_Kvp.Value));

            Triangles = new List<Triangle>(_ObstacleGroupedA.Triangles.Count + _ObstacleGroupedB.Triangles.Count);
            Triangles.AddRange(_ObstacleGroupedA.Triangles);
            Triangles.AddRange(_ObstacleGroupedB.Triangles);

            ComputeBounds();
        }

        #endregion

        #region Public methods

        public override void ReplaceID(int _OldID, int _NewID)
        {
            for (int i = 0; i < m_IDs.Count; i++)
            {
                if (m_IDs[i] != _OldID)
                    continue;

                m_IDs[i] = _NewID;
            }

            if (ObstacleInfos.TryGetValue(_OldID, out List<ObstacleInfoSingle> infos))
            {
                infos.ForEach(_Info => _Info.ReplaceID(_OldID, _NewID));

                ObstacleInfos.Remove(_OldID);
                ObstacleInfos.Add(_NewID, infos);
            }
        }

        public List<ObstacleInfoSingle> GetInfosExceptByID(int _ObstacleControllerID)
        {
            List<ObstacleInfoSingle> result = new List<ObstacleInfoSingle>();

            foreach(KeyValuePair<int, List<ObstacleInfoSingle>> kvp in ObstacleInfos)
            {
                if (kvp.Key == _ObstacleControllerID)
                    continue;

                result.AddRange(kvp.Value);
            }

            return result;
        }

        public ObstacleInfoBaseSerializable GetSerializableInstance(int[] _SingleInfosIDs, int _ID)
        {
            return new ObstacleInfoGroupedSerializable(this, _SingleInfosIDs, _ID);
        }

        #endregion
    }
}