using Nav3D.Common;
using Nav3D.API;
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using GizmosDrawData = Nav3D.Common.Debug.GizmosDrawData;
#endif

namespace Nav3D.Obstacles
{
    public class ObstacleManager : MonoBehaviour
    {
        #region Attributes

        ObstacleStorage m_ObstaclesStorage;

        #endregion

        #region Properties

        public static ObstacleManager Instance => Singleton<ObstacleManager>.Instance;

        public static bool  Doomed           { get; private set; }
        public        bool  Inited           { get; private set; }
        public        float MinBucketSize    { get; private set; }
        public        float MinBucketSizeSqr { get; private set; }

        #endregion

        #region Public methods

        public void Initialize(float _MinBucketSize)
        {
            MinBucketSize    = _MinBucketSize;
            MinBucketSizeSqr = MinBucketSize * MinBucketSize;

            m_ObstaclesStorage = new ObstacleStorage(MinBucketSize);

            Inited = true;
        }

        public void Uninitialize(bool _NeedDestroy = true)
        {
            m_ObstaclesStorage?.Uninitialize();

            if (!_NeedDestroy)
                return;

            UtilsCommon.SmartDestroy(this);
        }

        public string GetStorageContentDescription()
        {
            return m_ObstaclesStorage.GetStorageContentDescription();
        }

        public ObstacleAdditionProgress AddObstacles(List<ObstacleInfoBase> _ObstacleInfos, Action _OnFinish = null, bool _EditMode = false)
        {
            if (!Inited)
                return null;

            ObstacleAdditionProgress additionProgress = ObstacleAdditionProgress.INITIAL;

            m_ObstaclesStorage.AddObstacles(_ObstacleInfos, additionProgress, _OnFinish, _EditMode);

            return additionProgress;
        }

        public void AddBakedObstacles(Dictionary<ObstacleInfoBase, Obstacle> _ObstaclesData, Action _OnFinish)
        {
            if (!Inited)
                return;

            m_ObstaclesStorage.AddBakedObstacles(_ObstaclesData, _OnFinish);
        }

        public void RemoveObstacles(int _ObstacleControllerID)
        {
            if (!Inited)
                return;

            m_ObstaclesStorage.RemoveObstacles(_ObstacleControllerID);
        }

        #if UNITY_EDITOR

        public void FillGizmosDrawData(
            GizmosDrawData _GizmosDrawData,
            int            _ObstacleControllerID,
            bool           _DrawOccupiedLeaves,
            bool           _DrawFreeLeaves,
            bool           _DrawGraph,
            bool           _DrawRoots,
            int            _DrawOccupiedLeavesAll,
            int            _DrawOccupiedLeavesLayerNumber,
            int            _DrawFreeLeavesAll,
            int            _DrawFreeLeavesLayerNumber,
            int            _DrawGraphNodesAll,
            int            _DrawGraphLayerNumber)
        {
            if (!Inited)
                return;

            m_ObstaclesStorage.FillGizmosDrawData(
                _GizmosDrawData,
                _ObstacleControllerID,
                _DrawOccupiedLeaves,
                _DrawFreeLeaves,
                _DrawGraph,
                _DrawRoots,
                _DrawOccupiedLeavesAll,
                _DrawOccupiedLeavesLayerNumber,
                _DrawFreeLeavesAll,
                _DrawFreeLeavesLayerNumber,
                _DrawGraphNodesAll,
                _DrawGraphLayerNumber);
        }

        #endif

        public int GetObstacleOctreeLayersMaxCount(int _ObstacleControllerID)
        {
            if (!Inited)
                return 0;

            return m_ObstaclesStorage.GetObstacleOctreeLayersMaxCount(_ObstacleControllerID);
        }

        public int GetObstacleNodesCount(int _ObstacleControllerID)
        {
            if (!Inited)
                return 0;

            return m_ObstaclesStorage.GetObstacleNodesCount(_ObstacleControllerID);
        }

        public void UpdateBoundsCrossingObstacles(Bounds _Bounds)
        {
            if (!Inited)
                return;

            m_ObstaclesStorage.UpdateObstaclesCrossingTheBounds(_Bounds);
        }

        public bool BoundsCrossStaticObstacles(Bounds _Bounds)
        {
            if (!Inited)
                return false;

            return m_ObstaclesStorage.BoundsCrossStaticObstacles(_Bounds);
        }
        
        public bool BoundsCrossObstacles(Bounds _Bounds)
        {
            if (!Inited)
                return false;

            return m_ObstaclesStorage.BoundsCrossObstacles(_Bounds);
        }

        public bool IsPointInsideObstacle(Vector3 _Point)
        {
            if (!Inited)
                return false;

            return m_ObstaclesStorage.IsAnyObstacleIntersects(_Point);
        }

        public List<Triangle> GetIntersectedObstaclesTriangles(Bounds _Bounds)
        {
            return m_ObstaclesStorage.GetIntersectedObstaclesTriangles(_Bounds);
        }

        public List<Obstacle> GetObstaclesCrossingTheLine(Vector3 _PointA, Vector3 _PointB)
        {
            return m_ObstaclesStorage.GetObstaclesCrossingTheLine(_PointA, _PointB);
        }

        public List<Obstacle> GetObstaclesCrossingTheBounds(Bounds _Bounds)
        {
            return m_ObstaclesStorage.GetObstaclesCrossingTheBounds(_Bounds);
        }

        public Dictionary<ObstacleInfoBase, Obstacle> GetObstacleDatas(int _ObstacleControllerID)
        {
            return m_ObstaclesStorage.GetObstacleDatas(_ObstacleControllerID);
        }

        public Dictionary<ObstacleInfoBase, Obstacle> GetObstacleDatas()
        {
            return m_ObstaclesStorage.GetObstacleDatas();
        }

        public void RecreateObstacleTriangleStorages(float _BucketSize)
        {
            m_ObstaclesStorage.RecreateObstacleTriangleStorages(_BucketSize);
        }

        #endregion

        #region Unity events

        void Awake()
        {
            Doomed = false;
        }

        void OnDestroy()
        {
            Doomed = true;

            Uninitialize(false);
        }

        #endregion
    }
}