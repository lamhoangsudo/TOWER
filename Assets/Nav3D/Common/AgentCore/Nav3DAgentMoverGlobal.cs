using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nav3D.API;
using Nav3D.Pathfinding;
using Nav3D.Common;
using Nav3D.Obstacles;
using UnityEngine;

namespace Nav3D.Agents
{
    public class Nav3DAgentMoverGlobal : Nav3DAgentMover
    {
        #region Constants

        readonly string LOG_BEGIN_MOVE_TO_POINT  = $"{nameof(Nav3DAgentMoverGlobal)}.{nameof(BeginMoveTo)}: {{0}}";
        readonly string LOG_BEGIN_MOVE_TO_POINTS = $"{nameof(Nav3DAgentMoverGlobal)}.{nameof(BeginMoveToPoints)}: {{0}}";
        readonly string LOG_PATH_INIT            = $"{nameof(Nav3DAgentMoverGlobal)}.{nameof(BeginPathfinding)}";
        readonly string LOG_RESTART_FOLLOWING    = $"{nameof(Nav3DAgentMoverGlobal)}.{nameof(ReinitPathFollowingData)}";
        readonly string LOG_REINIT_PATH          = $"{nameof(Nav3DAgentMoverGlobal)}.{nameof(ReinitPath)}";
        readonly string LOG_PATH_UPDATE_SUCCESS  = $"{nameof(Nav3DAgentMoverGlobal)}.{nameof(OnPathfindingSucceed)}";
        readonly string LOG_PATH_UPDATE_FAIL     = $"{nameof(Nav3DAgentMoverGlobal)}.{nameof(OnPathfindingFailed)}: {nameof(Path)}.UpdatePath: Fail: {{0}}";

        #endregion

        #region Attributes

        Vector3[] m_LastRequestedPathfindingPoints;
        Vector3[] m_LastSucceededPathfindingPoints;

        bool m_Loop;
        bool m_SkipUnpassableTargets;
        bool m_TryRepositionStartIfOccupied;

        protected Nav3DPath      m_Path;
        protected PathFollowData m_CurrFollowData;

        protected Vector3 m_PathNextPoint;

        protected Action<PathfindingError> m_OnPathfindingFail;

        #endregion

        #region Properties

        public override bool Avoiding => false;

        public override Vector3[] CurrentPath => m_Path?.Trajectory;

        #endregion

        #region Constructors

        public Nav3DAgentMoverGlobal(Vector3 _Position, Nav3DAgentConfig _Config, Nav3DAgent _Agent, Log _Log)
            : base(_Position, _Config, _Agent, _Log)
        {
        }

        #endregion

        #region Public methods

        public override void BeginMoveTo(Vector3 _Point, bool _TryRepositionStartIfOccupied, Action<Vector3[]> _OnPathUpdated, Action<PathfindingError> _OnPathfindingFail = null)
        {
            m_Log?.WriteFormat(LOG_BEGIN_MOVE_TO_POINT, _Point.ToStringExt());

            m_Loop                         = false;
            m_SkipUnpassableTargets          = false;
            m_TryRepositionStartIfOccupied = _TryRepositionStartIfOccupied;
            
            m_Targets = new[] { _Point };

            m_OnPathUpdated = _OnPathUpdated;

            BeginPathfinding(m_Targets, false, false, _OnPathfindingFail);
        }

        public override void BeginMoveToPoints(
                Vector3[]                _Points,
                bool                     _Loop,
                bool                     _SkipUnpassableTargets,
                bool                     _TryRepositionStartIfOccupied,
                Action<Vector3[]>        _OnPathUpdated,
                Action<Vector3>          _OnTargetPassed     = null,
                Action                   _OnLastPointReached = null,
                Action<PathfindingError> _OnPathfindingFail  = null
            )
        {
            m_Log?.WriteFormat(LOG_BEGIN_MOVE_TO_POINTS, UtilsCommon.GetPointsString(_Points));

            m_OnTargetPassed      = _OnTargetPassed;
            m_OnLastTargetReached = _OnLastPointReached;
            m_Targets             = _Points;

            m_Loop                         = _Loop;
            m_SkipUnpassableTargets        = _SkipUnpassableTargets;
            m_TryRepositionStartIfOccupied = _TryRepositionStartIfOccupied;

            m_OnPathUpdated = _OnPathUpdated;

            BeginPathfinding(m_Targets, _Loop, _SkipUnpassableTargets, _OnPathfindingFail);
        }

        public override void ReinitPathFollowingData()
        {
            m_Log?.Write(LOG_RESTART_FOLLOWING);
            
            m_CurrFollowData.RestartFollowing(CurrentPosition, m_Config.TargetReachDistance);
        }

        public override void ReinitPath()
        {
            m_Log?.Write(LOG_REINIT_PATH);

            RestartPathFollowing(_SkipCurrentPositionAddition: true);
        }

        public override void DisposePath()
        {
            if (m_Path == null)
                return;

            m_Path.Dispose();
            m_Path = null;
        }

        public override void Uninitialize()
        {
            base.Uninitialize();

            DisposePath();
        }

        public override string GetInnerStatusString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"-------MOVER STATUS-------");
            stringBuilder.AppendLine($"Mover type: {nameof(Nav3DAgentMoverGlobal)}");

            stringBuilder.AppendLine($"-------FOLLOW DATA STATUS-------");
            if (m_CurrFollowData == null)
            {
                stringBuilder.AppendLine("m_CurrFollowData: null");
            }
            else
            {
                stringBuilder.AppendLine($"m_CurrFollowData: {m_CurrFollowData.GetHashCode()}, IsValid: {m_CurrFollowData.IsValid}");
                stringBuilder.AppendLine(m_CurrFollowData.GetNextTargetInfo());
            }

            stringBuilder.AppendLine($"-------PATHFINDING STATUS-------");
            
            string pathfindingInfo =
                $"Last requested pathfinding points: {UtilsCommon.GetPointsString(m_LastRequestedPathfindingPoints)}\nLast succeeded pathfinding points: {UtilsCommon.GetPointsString(m_LastSucceededPathfindingPoints)}";
            stringBuilder.AppendLine(pathfindingInfo);
            
            string pathString = m_Path == null
                                    ? "m_Path: null"
                                    : $"m_Path:\n\t{m_Path.GetHashCode()},\n\tIsValid: {m_Path.IsValid},\n\tIsPathfindingInProgress: {m_Path.IsPathfindingInProgress}\n\tOrder status: {m_Path.GetPathfindingStatus()}";
            stringBuilder.AppendLine(pathString);

            return stringBuilder.ToString();
        }

        public override string GetLastPathfindingStatsString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (m_Path == null)
            {
                stringBuilder.AppendLine($"{nameof(m_Path)}: null");

                return stringBuilder.ToString();
            }

            PathfindingResult pathfindingResult = m_Path.LastPathfindingResult;

            if (pathfindingResult == null)
            {
                stringBuilder.AppendLine($"{nameof(m_Path.LastPathfindingResult)}: null");

                return stringBuilder.ToString();
            }

            stringBuilder.Append(m_Path.LastPathfindingResult.GetLastPathfindingStatsString());

            return stringBuilder.ToString();
        }

        #if UNITY_EDITOR

        public override void Draw(bool _DrawRadius, bool _DrawPath, bool _DrawVelocities)
        {
            if (_DrawPath)
            {
                m_Path?.Draw();

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(m_CurrentPosition, m_PathNextPoint);
                Gizmos.DrawSphere(m_PathNextPoint, 0.025f);
            }

            base.Draw(_DrawRadius, _DrawPath, _DrawVelocities);
        }

        #endif

        #endregion

        #region Service methods

        protected void BeginPathfinding(
                Vector3[]                _Targets,
                bool                     _Loop,
                bool                     _SkipUnpassableTargets,
                Action<PathfindingError> _OnPathfindingFail,
                bool                     _SkipCurrentPositionAddition = false
            )
        {
            m_Log?.Write(LOG_PATH_INIT);

            m_OnPathfindingFail = _OnPathfindingFail;

            Vector3[] targets;

            if (_Targets.Length >= 2 && (_SkipCurrentPositionAddition || m_CurrentPosition == _Targets.First()))
            {
                targets = _Targets.Copy();
            }
            else
            {
                targets    = new Vector3[_Targets.Length + 1];
                targets[0] = m_CurrentPosition;
                _Targets.CopyTo(targets, 1);
            }

            //Create Nav3DPath instance
            if (m_Path == null)
            {
                string name = $"{Agent.name}({Agent.GetInstanceID()})";
                
                m_Path                              =  new Nav3DPath(name, m_Log);
                m_Path.TryRepositionStartIfOccupied =  m_TryRepositionStartIfOccupied;
                m_Path.TryRepositionTargetIfOccupied  =  m_Config.TryRepositionTargetIfOccupied;
                m_Path.OnPathfindingSuccess         += OnPathfindingSucceed;
                m_Path.OnPathfindingFail            += OnPathfindingFailed;
            }

            //Apply pathfinding settings
            m_Path.Timeout     = m_Config.PathfindingTimeout;
            m_Path.Smooth      = m_Config.SmoothPath;
            m_Path.SmoothRatio = m_Config.SmoothRatio;

            m_LastRequestedPathfindingPoints = targets;
            
            //Start pathfinding
            m_Path.Find(targets, _Loop, _SkipUnpassableTargets);
        }

        void OnPathfindingSucceed()
        {
            m_Log?.Write(LOG_PATH_UPDATE_SUCCESS);

            if (m_Path is { IsValid: true })
            {
                m_LastSucceededPathfindingPoints = m_Path.Trajectory;
                m_OnPathUpdated.Invoke(m_LastSucceededPathfindingPoints);

                if (m_Path.TryGetFollowData(OnTargetPointPassed, m_OnLastTargetReached, m_CurrentPosition, m_Config.TargetReachDistance, out PathFollowData followData))
                    m_CurrFollowData = followData;
            }
        }

        void OnPathfindingFailed(PathfindingError _Error)
        {
            m_Log?.WriteFormat(LOG_PATH_UPDATE_FAIL, _Error.Msg);

            //In case if the agent is inside the obstacle try to find path from previous outside point
            //The agent can may end up inside an obstacle if it performs a local avoidance maneuver or if it cuts a corner of the obstacle while following the path
            if (_Error.Reason == PathfindingResultCode.START_POINT_INSIDE_OBSTACLE && m_LastSucceededPathfindingPoints.Any())
            {
                Vector3 closestFreePoint = UtilsMath.GetClosestPointOnCurve(m_LastSucceededPathfindingPoints, m_CurrentPosition);

                if (!ObstacleManager.Instance.IsPointInsideObstacle(closestFreePoint))
                {
                    RestartPathFollowing(closestFreePoint, true);

                    return;
                }
            }

            m_OnPathfindingFail?.Invoke(_Error);
        }
        
        protected void RestartPathFollowing(Vector3? _StartPoint = null, bool _SkipCurrentPositionAddition = false)
        {
            //Remove all points, that is not presented in initial target list.
            List<Vector3> sourceTargets   = new List<Vector3>(m_Targets);
            List<Vector3> unpassedTargets = m_CurrFollowData.GetUnpassedTargets(m_Loop).ToList();

            for (int i = 0; i < unpassedTargets.Count; i++)
            {
                int index = sourceTargets.IndexOf(unpassedTargets[i]);

                if (index > -1)
                {
                    sourceTargets.RemoveAt(index);
                }
                else
                {
                    unpassedTargets.RemoveAt(i);
                }
            }

            if (_StartPoint.HasValue)
                unpassedTargets.Insert(0, _StartPoint.Value);

            if (unpassedTargets.Count == 1)
                unpassedTargets.Insert(0, m_CurrentPosition);
            
            BeginPathfinding(unpassedTargets.ToArray(), m_Loop, m_SkipUnpassableTargets, m_OnPathfindingFail, _SkipCurrentPositionAddition);
        }
        
        protected virtual void OnTargetPointPassed(Vector3 _Point)
        {
            m_OnTargetPassed?.Invoke(_Point);
        }

        protected override Vector3 GetVelocity()
        {
            if (m_Path == null || m_Path.IsValid == false || m_CurrFollowData == null || m_CurrFollowData.IsValid == false)
                return LastVelocity = Vector3.zero;

            m_PathNextPoint = m_CurrFollowData.GetMovePoint(m_Config.Speed, m_Config.Radius, m_CurrentPosition);

            return LastVelocity = (m_PathNextPoint - m_CurrentPosition).normalized * m_Config.Speed;
        }

        #endregion
    }
}