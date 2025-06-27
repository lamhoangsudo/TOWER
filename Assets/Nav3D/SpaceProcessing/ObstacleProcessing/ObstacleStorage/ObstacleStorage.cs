using System;
using UnityEngine;
using System.Text;
using System.Linq;
using Nav3D.Pathfinding;
using Nav3D.LocalAvoidance;
using Nav3D.Common;
using Nav3D.API;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEditor;

namespace Nav3D.Obstacles
{
    class ObstacleStorage
    {
        #region Attributes

        readonly float m_MinBucketSize;

        //Many-to-many relations between Obstacle Controller IDs and ObstacleInfoBase instances
        readonly Dictionary<int, List<ObstacleInfoBase>>          m_IDToInfo        = new Dictionary<int, List<ObstacleInfoBase>>();
        readonly ConcurrentDictionary<ObstacleInfoBase, Obstacle> m_InfoToObstacle  = new ConcurrentDictionary<ObstacleInfoBase, Obstacle>();
        readonly BoundablesSpatialHashMap<Obstacle>               m_ObstacleStorage = new BoundablesSpatialHashMap<Obstacle>();
        readonly SequentialTasksExecutor                          m_TasksExecutor   = new SequentialTasksExecutor();

        #endregion

        #region Constructors

        public ObstacleStorage(float _MinBucketSize)
        {
            m_MinBucketSize = _MinBucketSize;
        }

        #endregion

        #region Public methods

        public void AddObstacles(
            List<ObstacleInfoBase>   _ObstacleInfos,
            ObstacleAdditionProgress _AdditionProgress,
            Action                   _OnFinish = null,
            bool                     _EditMode = false)
        {
            m_TasksExecutor.AddTask(() => ConstructObstacles(_ObstacleInfos, _AdditionProgress, _OnFinish, _EditMode));
        }

        public void AddBakedObstacles(Dictionary<ObstacleInfoBase, Obstacle> _ObstaclesDatas, Action _OnFinish)
        {
            m_TasksExecutor.AddTask(() => RegisterBakedObstacles(_ObstaclesDatas, _OnFinish));
        }

        public void RemoveObstacles(int _ObstacleControllerID, Action _OnFinish = null)
        {
            m_TasksExecutor.AddTask(() => DeconstructObstacles(_ObstacleControllerID, _OnFinish));
        }

        public void UpdateObstaclesCrossingTheBounds(Bounds _Bounds)
        {
            m_TasksExecutor.AddTask(() =>
            {
                if (!m_ObstacleStorage.TryGetCrossingBoundables(_Bounds, out HashSet<Obstacle> crossedObstacles))
                    return;

                //Except obstacles loaded from binary
                crossedObstacles.RemoveWhere(_Obstacle => _Obstacle.IsStatic);

                List<ObstacleInfoBase> infosToUpdate = crossedObstacles.Select(_Obstacle => _Obstacle.ObstacleInfo).ToList();

                infosToUpdate.ForEach(_Info => RemoveObstacleByInfo(_Info));

                ConstructObstacles(infosToUpdate);
            });
        }

        public bool BoundsCrossStaticObstacles(Bounds _Bounds)
        {
            if (!m_ObstacleStorage.TryGetCrossingBoundables(_Bounds, out HashSet<Obstacle> crossedObstacles))
                return false;

            return crossedObstacles.Any(_Obstacle => _Obstacle.IsStatic);
        }
        
        public bool BoundsCrossObstacles(Bounds _Bounds)
        {
            return m_ObstacleStorage.TryGetCrossingBoundables(_Bounds, out HashSet<Obstacle> crossedObstacles);
        }

        public bool IsAnyObstacleIntersects(Vector3 _Point)
        {
            if (!m_ObstacleStorage.TryGetCrossingBoundables(_Point, out HashSet<Obstacle> crossedObstacles))
                return false;

            return crossedObstacles.Any(_Obstacle => _Obstacle.PointInsideOccupiedLeaf(_Point));
        }

        public int GetObstacleNodesCount(int _ObstacleControllerID)
        {
            if (!m_IDToInfo.TryGetValue(_ObstacleControllerID, out List<ObstacleInfoBase> infos))
                return 0;

            int result;

            lock (infos)
            {
                result = infos.Select(_Info => m_InfoToObstacle.TryGetValue(_Info, out Obstacle obstacle) ? obstacle.NodesCount : 0).Max();
            }

            return result;
        }

        public int GetObstacleOctreeLayersMaxCount(int _ObstacleControllerID)
        {
            if (!m_IDToInfo.TryGetValue(_ObstacleControllerID, out List<ObstacleInfoBase> infos))
                return 0;

            int result;

            lock (infos)
            {
                result = infos.Select(_Info => m_InfoToObstacle.TryGetValue(_Info, out Obstacle obstacle) ? obstacle.OctreeLayersCount : 0)
                              .Max();
            }

            return result;
        }

        public string GetStorageContentDescription()
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (KeyValuePair<int, List<ObstacleInfoBase>> kvp in m_IDToInfo)
            {
                string str = $"{kvp.Key}:\n{string.Join(", ", kvp.Value.Select(_Info => _Info.ToString()))}\n";
                stringBuilder.Append(str);
                stringBuilder.Append("----------------\n");
            }

            return stringBuilder.ToString();
        }

        public void Uninitialize()
        {
            m_IDToInfo.Clear();
            m_InfoToObstacle.ForEach(_Kvp => _Kvp.Value.Invalidate());
        }

        public List<Obstacle> GetObstaclesCrossingTheLine(Vector3 _PointA, Vector3 _PointB)
        {
            Bounds lineBounds = ExtensionBounds.MinMax(_PointA, _PointB);

            m_ObstacleStorage.TryGetCrossingBoundables(lineBounds, out HashSet<Obstacle> crossedObstacles);

            return crossedObstacles.Where(_Obstacle => _Obstacle.Bounds.IntersectSegment(new Segment3(_PointA, _PointB))).ToList();
        }

        public List<Obstacle> GetObstaclesCrossingTheBounds(Bounds _Bounds)
        {
            m_ObstacleStorage.TryGetCrossingBoundables(_Bounds, out HashSet<Obstacle> crossedObstacles);

            return crossedObstacles.ToList();
        }

        public List<Triangle> GetIntersectedObstaclesTriangles(Bounds _Bounds)
        {
            return GetIntersectedObstaclesTrianglesInternal(_Bounds);
        }

        public Dictionary<ObstacleInfoBase, Obstacle> GetObstacleDatas(int _ObstacleControllerID)
        {
            Dictionary<ObstacleInfoBase, Obstacle> datas = new Dictionary<ObstacleInfoBase, Obstacle>();

            if (!m_IDToInfo.TryGetValue(_ObstacleControllerID, out List<ObstacleInfoBase> infos))
                return datas;

            foreach (ObstacleInfoBase obstacleInfo in infos)
            {
                if (!m_InfoToObstacle.TryGetValue(obstacleInfo, out Obstacle obstacle))
                    continue;

                datas.Add(obstacleInfo, obstacle);
            }

            return datas;
        }

        public Dictionary<ObstacleInfoBase, Obstacle> GetObstacleDatas()
        {
            Dictionary<ObstacleInfoBase, Obstacle> datas = new Dictionary<ObstacleInfoBase, Obstacle>();

            foreach (KeyValuePair<ObstacleInfoBase, Obstacle> infoData in m_InfoToObstacle)
            {
                datas.Add(infoData.Key, infoData.Value);
            }

            return datas;
        }

        public void RecreateObstacleTriangleStorages(float _BucketSize)
        {
            foreach (KeyValuePair<ObstacleInfoBase, Obstacle> kvp in m_InfoToObstacle)
            {
                kvp.Value.RecreateTriangleStorage(_BucketSize);
            }
        }

        #if UNITY_EDITOR
        public void FillGizmosDrawData(
            Common.Debug.GizmosDrawData _GizmosDrawData,
            int                         _ObstacleControllerID,
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
            if (!m_IDToInfo.TryGetValue(_ObstacleControllerID, out List<ObstacleInfoBase> infos))
                return;

            lock (infos)
            {
                foreach (ObstacleInfoBase obstacleInfo in infos)
                {
                    if (!m_InfoToObstacle.TryGetValue(obstacleInfo, out Obstacle obstacle))
                        continue;

                    obstacle.FillGizmosDrawData(
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
            }
        }
        #endif

        #endregion

        #region Service methods

        /// <summary>
        /// Registers obstacle information, then creates obstacle instances, and then requests a graph construction for the latter.
        /// </summary>
        /// <param name="_ObstaclesInfo">Information about all the obstacles to be handled.</param>
        /// <param name="_AdditionProgress">The progress of all handling stages.</param>
        /// <param name="_OnFinish">Action to do on handling finish.</param>
        /// <param name="_EditMode">Is it need to invoke _OnFinish using ThreadDispatcher or not.</param>
        void ConstructObstacles(
            List<ObstacleInfoBase>   _ObstaclesInfo,
            ObstacleAdditionProgress _AdditionProgress = null,
            Action                   _OnFinish         = null,
            bool                     _EditMode         = false)
        {
            _AdditionProgress?.SetStatus(ObstacleAdditionProgress.ObstacleAdditionStatus.OBSTACLES_CLUSTERIZATION);

            ClusterizeObstacles(_ObstaclesInfo, out List<ObstacleInfoBase> newInfos, out List<ObstacleInfoBase> infosToDelete, _AdditionProgress);

            BuildAndRegisterObstacles(newInfos, infosToDelete, _AdditionProgress, _OnFinish, _EditMode);
        }

        void DeconstructObstacles(int _ObstacleControllerID, Action _OnFinish = null)
        {
            if (!m_IDToInfo.TryGetValue(_ObstacleControllerID, out List<ObstacleInfoBase> obstacleInfos))
            {
                _OnFinish?.Invoke();

                return;
            }

            ExcludeInfosByID(_ObstacleControllerID, obstacleInfos, out List<ObstacleInfoBase> infosToRemove, out List<ObstacleInfoBase> infosToAdd);

            BuildAndRegisterObstacles(infosToAdd, infosToRemove, _OnFinish: _OnFinish);
        }

        void RegisterBakedObstacles(Dictionary<ObstacleInfoBase, Obstacle> _ObstaclesDatas, Action _OnFinish, bool _EditMode = false)
        {
            RegisterObstacleData(_ObstaclesDatas);

            if (_OnFinish != null)
            {
                if (!_EditMode)
                    ThreadDispatcher.BeginInvoke(() => _OnFinish.Invoke());
                else
                {
                    try
                    {
                        _OnFinish.Invoke();
                    }
                    catch (Exception _Exception)
                    {
                        Debug.LogException(_Exception);
                    }
                }
            }
        }

        void BuildAndRegisterObstacles(
            List<ObstacleInfoBase>   _NewInfos,
            List<ObstacleInfoBase>   _InfosToDelete,
            ObstacleAdditionProgress _AdditionProgress = null,
            Action                   _OnFinish         = null,
            bool                     _EditMode         = false)
        {
            if (!_NewInfos.Any() && !_InfosToDelete.Any())
            {
                _AdditionProgress?.SetStatus(ObstacleAdditionProgress.ObstacleAdditionStatus.FINISHED);

                return;
            }

            _AdditionProgress?.SetStatus(ObstacleAdditionProgress.ObstacleAdditionStatus.OBSTACLES_GRAPH_CONSTRUCTION);

            Dictionary<ObstacleInfoBase, Obstacle> obstaclesData =
                _NewInfos.ToDictionary(_Info => _Info, _Info => new Obstacle(_Info, m_MinBucketSize));

            int counter = 0;

            foreach (Obstacle obstacle in obstaclesData.Values)
            {
                _AdditionProgress?.CancellationToken.Register(() => obstacle.Invalidate());

                if (_AdditionProgress?.CancellationToken.IsCancellationRequested ?? false)
                    return;

                counter++;

                _AdditionProgress?.SetCurrentConstructionProgress(obstacle.ConstructionProgress);

                obstacle.ConstructGraph();

                _AdditionProgress?.SetProgress(counter / obstaclesData.Values.Count);
            }

            _AdditionProgress?.SetStatus(ObstacleAdditionProgress.ObstacleAdditionStatus.REMOVING_OBSOLETE_OBSTACLES);

            if (_AdditionProgress?.CancellationToken.IsCancellationRequested ?? false)
                return;

            foreach (ObstacleInfoBase infoToDelete in _InfosToDelete)
            {
                #if UNITY_EDITOR

                try
                {
                    HashSet<int> removedIDs = new HashSet<int>();
                    removedIDs.AddRange(infoToDelete.IDs);
                    ThreadDispatcher.BeginInvoke(
                            () =>
                            {
                                foreach (int id in removedIDs)
                                {
                                    (EditorUtility.InstanceIDToObject(id) as Nav3DObstacle)?.ClearGizmosCache();
                                }
                            }
                        );
                }
                catch (Exception _Exception)
                {
                    UnityEngine.Debug.LogError(_Exception.Message);
                }

                #endif

                RemoveObstacleByInfo(infoToDelete);
            }

            RegisterObstacleData(obstaclesData);

            _AdditionProgress?.SetStatus(ObstacleAdditionProgress.ObstacleAdditionStatus.FINISHED);

            if (_OnFinish != null)
            {
                if (!_EditMode)
                    ThreadDispatcher.BeginInvoke(() => _OnFinish.Invoke());
                else
                {
                    try
                    {
                        _OnFinish?.Invoke();
                    }
                    catch (Exception _Exception)
                    {
                        Debug.LogException(_Exception);
                    }
                }
            }
        }

        void RegisterObstacleData(Dictionary<ObstacleInfoBase, Obstacle> _ObstaclesData)
        {
            foreach (KeyValuePair<ObstacleInfoBase, Obstacle> dataPair in _ObstaclesData)
            {
                ObstacleInfoBase obstacleInfo = dataPair.Key;

                foreach (int obstacleControllerID in obstacleInfo.IDs.Distinct())
                {
                    List<ObstacleInfoBase> infos = m_IDToInfo.GetOrAdd(obstacleControllerID);
                    lock (infos)
                    {
                        infos.Add(obstacleInfo);
                    }
                }

                Obstacle obstacle = dataPair.Value;

                m_ObstacleStorage.Register(obstacle);
                m_InfoToObstacle.TryAdd(obstacle.ObstacleInfo, obstacle);

                PathfindingManager.Instance.UpdateAllBoundsCrossingPaths(obstacle.Bounds);
                AgentManager.Instance.SetMovablesInBoundsObstacleDirty(obstacle.Bounds);
            }
        }

        void RemoveObstacleByInfo(ObstacleInfoBase _ObstacleInfo)
        {
            if (m_InfoToObstacle.TryGetValue(_ObstacleInfo, out Obstacle obstacle))
            {
                m_InfoToObstacle.TryRemove(_ObstacleInfo, out _);
                m_ObstacleStorage.Unregister(obstacle);
                obstacle.Invalidate();
            }

            foreach (int id in _ObstacleInfo.IDs)
            {
                if (m_IDToInfo.TryGetValue(id, out List<ObstacleInfoBase> obstacleInfos))
                {
                    lock (obstacleInfos)
                    {
                        obstacleInfos.Remove(_ObstacleInfo);

                        if (!obstacleInfos.Any())
                            m_IDToInfo.Remove(id);
                    }
                }
            }

            AgentManager.Instance.SetMovablesInBoundsObstacleDirty(obstacle.Bounds);
            PathfindingManager.Instance.UpdateAllBoundsCrossingPaths(obstacle.Bounds);
        }

        /// <summary>
        /// Groups new obstacle infos with existing ones.
        /// </summary>
        /// <param name="_NewObstacleInfos">Input obstacle infos.</param>
        /// <param name="_ClusterizedObstacles">Resulting grouped obstacles.</param>
        /// <param name="_ObsoleteInfos">Obstacles that'd been crossed by someone of new obstacles infos.</param>
        void ClusterizeObstacles(
            List<ObstacleInfoBase>     _NewObstacleInfos,
            out List<ObstacleInfoBase> _ClusterizedObstacles,
            out List<ObstacleInfoBase> _ObsoleteInfos,
            ObstacleAdditionProgress   _AdditionProgress = null)
        {
            List<ObstacleInfoBase>    newObstacleInfos      = _NewObstacleInfos.Copy();
            List<ObstacleInfoBase>    crossingObstacleInfos = new List<ObstacleInfoBase>();
            HashSet<ObstacleInfoBase> obsoleteInfos         = new HashSet<ObstacleInfoBase>();

            bool hasIntersections;

            do
            {
                hasIntersections = false;

                int counter = 0;

                foreach (ObstacleInfoBase newObstacleInfo in newObstacleInfos)
                {
                    counter++;

                    if (!m_ObstacleStorage.TryGetCrossingBoundables(newObstacleInfo.Bounds, out HashSet<Obstacle> currentCrossingObstacles))
                        continue;

                    foreach (Obstacle obstacle in currentCrossingObstacles)
                    {
                        if (!obsoleteInfos.Add(obstacle.ObstacleInfo))
                            continue;

                        crossingObstacleInfos.Add(obstacle.ObstacleInfo);

                        hasIntersections = true;
                    }

                    _AdditionProgress?.SetProgress(counter / newObstacleInfos.Count);

                    if (_AdditionProgress?.CancellationToken.IsCancellationRequested ?? false)
                        break;
                }

                newObstacleInfos.AddRange(crossingObstacleInfos);
                crossingObstacleInfos.Clear();
                newObstacleInfos = ObstacleInfoBase.GroupInfos(newObstacleInfos);
            } while (hasIntersections && !(_AdditionProgress?.CancellationToken.IsCancellationRequested ?? false));

            _ClusterizedObstacles = newObstacleInfos;
            _ObsoleteInfos        = obsoleteInfos.ToList();
        }

        /// <summary>
        /// Excludes obstacle info by given ID from existing both grouped and single obstacle infos.
        /// </summary>
        /// <param name="_ObstacleControllerID">Exluding obstacles ID.</param>
        /// <param name="_Infos">Existing input obstacles.</param>
        /// <param name="_ExcludedInfos">Resulting excluded obstacles list.</param>
        /// <param name="_RemainingInfos">Resulting remaining obstacles (grouped and single).</param>
        void ExcludeInfosByID(
            int                        _ObstacleControllerID,
            List<ObstacleInfoBase>     _Infos,
            out List<ObstacleInfoBase> _ExcludedInfos,
            out List<ObstacleInfoBase> _RemainingInfos
        )
        {
            List<ObstacleInfoBase> singleObstaclesToAdd = new List<ObstacleInfoBase>();
            List<ObstacleInfoBase> obstaclesToRemove    = new List<ObstacleInfoBase>();

            foreach (ObstacleInfoBase obstacleInfo in _Infos)
            {
                if (obstacleInfo is ObstacleInfoSingle obstacleInfoSingle)
                {
                    //Mark info as necessary for deletion
                    obstaclesToRemove.Add(obstacleInfo);

                    //Mark info as necessary for adddition in case if it has ObstacleControllerID different from given one
                    if (obstacleInfoSingle.ObstacleControllerID != _ObstacleControllerID)
                        singleObstaclesToAdd.Add(obstacleInfoSingle);
                }
                else if (obstacleInfo is ObstacleInfoGrouped obstacleInfoGrouped)
                {
                    obstaclesToRemove.Add(obstacleInfo);

                    singleObstaclesToAdd.AddRange(obstacleInfoGrouped.GetInfosExceptByID(_ObstacleControllerID));
                }
            }

            _ExcludedInfos  = obstaclesToRemove;
            _RemainingInfos = ObstacleInfoBase.GroupInfos(singleObstaclesToAdd);
        }

        List<Triangle> GetIntersectedObstaclesTrianglesInternal(Bounds _Bounds)
        {
            List<Triangle> triangles = new List<Triangle>();

            if (!m_ObstacleStorage.TryGetCrossingBoundables(_Bounds, out HashSet<Obstacle> crossedObstacles))
                return triangles;

            foreach (Obstacle obstacle in crossedObstacles)
            {
                triangles.AddRange(obstacle.GetIntersectedOccupiedTriangles(_Bounds));
            }

            return triangles;
        }

        #endregion
    }
}
