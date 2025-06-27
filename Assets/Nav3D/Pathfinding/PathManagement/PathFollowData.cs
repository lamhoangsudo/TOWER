using Nav3D.LocalAvoidance.SupportingMath;
using Nav3D.Common;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Nav3D.Pathfinding
{
    public class PathFollowData
    {
        #region Constants

        readonly string LOG_CTOR       = $"{nameof(PathFollowData)}.ctor: {{0}}";
        readonly string LOG_INVALIDATE = $"{nameof(PathFollowData)}.{nameof(Invalidate)}: {{0}}";

        #endregion

        #region Attributes

        readonly Vector3[]       m_Path;
        readonly int[]           m_TargetIndices;
        readonly Action<Vector3> m_OnTargetPassed;
        readonly Action          m_OnLastTargetReached;

        readonly float m_ReachDistance;

        readonly Log m_Log;

        int     m_NextIndex;
        Vector3 m_NextPoint;
        int     m_NextTargetInTargetsIndex;
        Vector3 m_NextTarget;

        #endregion

        #region Properties

        public bool IsValid { get; private set; } = true;

        #endregion

        #region Constructors

        /// <summary>
        /// PathFollowData ctor.
        /// </summary>
        /// <param name="_Trajectory">The given path to follow.</param>
        /// <param name="_TargetIndices">The indices of the targets among all trajectory points.</param>
        /// <param name="_FollowerPoint">The initial position of the agent.</param>
        /// <param name="_ReachDist">Required distance from agent to target point to mark last as reached.</param>
        /// <param name="_OnTargetPassed">The action to execute when the target point passed.</param>
        /// <param name="_OnLastTargetReached">The action to execute when the last target point reached.</param>
        /// <param name="_Log">Log.</param>
        public PathFollowData(
            Vector3[]       _Trajectory,
            int[]           _TargetIndices,
            Vector3         _FollowerPoint,
            float           _ReachDist,
            Action<Vector3> _OnTargetPassed,
            Action          _OnLastTargetReached,
            Log             _Log)
        {
            m_Log = _Log;
            m_Log?.WriteFormat(LOG_CTOR, GetHashCode());

            m_Path                = _Trajectory;
            m_TargetIndices       = _TargetIndices;
            m_OnTargetPassed      = _OnTargetPassed;
            m_OnLastTargetReached = _OnLastTargetReached;

            m_ReachDistance = _ReachDist;
            
            GetClosestPoint(_FollowerPoint, _ReachDist, out m_NextPoint, out m_NextIndex, out m_NextTargetInTargetsIndex);

            m_NextTarget = m_Path[m_TargetIndices[m_NextTargetInTargetsIndex]];
        }

        #endregion

        #region Public methods

        public string GetNextTargetInfo()
        {
            return
                $"Next target index: {m_NextTargetInTargetsIndex}, next target index on path: {m_TargetIndices[m_NextTargetInTargetsIndex]}, next target position: {m_Path[m_TargetIndices[m_NextTargetInTargetsIndex]]}";
        }

        public Vector3 GetMovePoint(float _Speed, float _Radius, Vector3 _CurrentFollowerPosition)
        {
            m_NextPoint = m_Path[m_NextIndex];
            Vector3 curToNext      = m_NextPoint - _CurrentFollowerPosition;
            float   toNextDistance = curToNext.magnitude;

            void CheckTargetPoints()
            {
                if (m_NextTargetInTargetsIndex >= m_TargetIndices.Length || m_TargetIndices[m_NextTargetInTargetsIndex] >= m_NextIndex)
                    return;

                m_OnTargetPassed?.Invoke(m_Path[m_TargetIndices[m_NextTargetInTargetsIndex]]);

                if (m_NextTargetInTargetsIndex >= m_TargetIndices.Length - 1)
                    return;

                m_NextTargetInTargetsIndex++;
                m_NextTarget = m_Path[m_TargetIndices[m_NextTargetInTargetsIndex]];
            }

            while (toNextDistance <= _Speed + m_ReachDistance && m_NextIndex < m_Path.Length - 1)
            {
                _Speed                   -= toNextDistance;
                _CurrentFollowerPosition =  m_NextPoint;

                CheckTargetPoints();

                m_NextIndex++;
                m_NextPoint = m_Path[m_NextIndex];

                curToNext      = m_NextPoint - _CurrentFollowerPosition;
                toNextDistance = curToNext.magnitude;

                if (m_NextIndex == m_Path.Length - 1)
                    CheckTargetPoints();
            }

            //check if the last target is passed
            if (m_NextTargetInTargetsIndex                             == m_TargetIndices.Length - 1 &&
                m_TargetIndices[m_NextTargetInTargetsIndex]            == m_NextIndex                &&
                (m_NextTarget - _CurrentFollowerPosition).sqrMagnitude <= m_ReachDistance)
            {
                m_OnTargetPassed?.Invoke(m_Path[m_TargetIndices[m_NextTargetInTargetsIndex]]);
                m_OnLastTargetReached?.Invoke();
            }

            return m_NextPoint;
        }

        public Vector3[] GetUnpassedTargets(bool _Loop)
        {
            List<Vector3> result = new List<Vector3>(m_TargetIndices.Length);

            for (int i = m_NextTargetInTargetsIndex; i < m_TargetIndices.Length; i++)
            {
                result.Add(m_Path[m_TargetIndices[i]]);
            }

            if (!_Loop)
                return result.ToArray();

            for (int i = 0; i < m_NextTargetInTargetsIndex; i++)
            {
                result.Add(m_Path[m_TargetIndices[i]]);
            }

            return result.ToArray();
        }

        public void GetDistToClosestOnPath(Vector3 _Point, out Vector3 _Closest, out float _Magnitude)
        {
            int nextIndex = m_NextIndex;
            int preIndex;

            if (m_NextIndex == m_Path.Length)
                nextIndex--;

            preIndex = nextIndex - 1;

            if (nextIndex == 0)
            {
                _Closest   = m_Path[nextIndex];
                _Magnitude = (_Closest - _Point).magnitude;

                return;
            }

            Vector3 next = m_Path[nextIndex];
            Vector3 pre  = m_Path[preIndex];

            //The case when the path contains only two equal points
            if (next == pre)
            {
                _Closest   = next;
                _Magnitude = 0;

                return;
            }

            Vector3 closestOnLine = new Straight(next - pre, pre).ClosestPoint(_Point);

            float preMagn     = (pre           - _Point).magnitude;
            float nextMagn    = (next          - _Point).magnitude;
            float closestMagn = (closestOnLine - _Point).magnitude;

            if (preMagn < nextMagn && preMagn < closestMagn)
            {
                _Closest   = pre;
                _Magnitude = preMagn;

                return;
            }

            if (nextMagn < preMagn && nextMagn < closestMagn)
            {
                _Closest   = next;
                _Magnitude = nextMagn;

                return;
            }

            _Closest   = closestOnLine;
            _Magnitude = closestMagn;
        }

        public void RestartFollowing(Vector3 _FollowerPoint, float _ReachDistSqr)
        {
            GetClosestPoint(_FollowerPoint, _ReachDistSqr, out m_NextPoint, out m_NextIndex, out m_NextTargetInTargetsIndex);
            m_NextTarget = m_Path[m_TargetIndices[m_NextTargetInTargetsIndex]];
        }

        public void Invalidate()
        {
            m_Log?.WriteFormat(LOG_INVALIDATE, GetHashCode());

            IsValid = false;
        }

        #endregion

        #region Service methods

        /// <summary>
        /// Finds closest point on the path to the given one.
        /// </summary>
        /// <param name="_Point">Given point</param>
        /// <param name="_ReachDistSqr">The distance to a point required to consider the point reached.</param>
        /// <param name="_ClosestPoint">Closest point of the path</param>
        /// <param name="_Index">Index of closest point via path point indices</param>
        /// <param name="_NextTargetInTargetsIndex">Index of the next target via targets indices</param>
        void GetClosestPoint(
            Vector3     _Point,
            float       _ReachDistSqr,
            out Vector3 _ClosestPoint,
            out int     _Index,
            out int     _NextTargetInTargetsIndex)
        {
            Vector3 min      = Vector3.positiveInfinity;
            int     minIndex = int.MaxValue;

            //if the start and the end of a path are in the neighborhood of the initial point, then we chase start point
            if ((m_Path[0] - _Point).sqrMagnitude < _ReachDistSqr && (m_Path[m_Path.Length - 1] - _Point).sqrMagnitude < _ReachDistSqr)
            {
                _ClosestPoint       = m_Path[0];
                _Index              = 0;
                _NextTargetInTargetsIndex = 0;

                return;
            }

            _NextTargetInTargetsIndex = 0;

            float minSqrMagn = float.MaxValue;

            for (int i = 0; i < m_Path.Length; i++)
            {
                Vector3 curPathPoint = m_Path[i];

                float curSqrMagn = (curPathPoint - _Point).sqrMagnitude;

                if (curSqrMagn < minSqrMagn)
                {
                    minIndex   = i;
                    min        = curPathPoint;
                    minSqrMagn = curSqrMagn;
                }
            }

            _NextTargetInTargetsIndex = m_TargetIndices.FindIndex(_TargetIndex => _TargetIndex >= minIndex);

            _ClosestPoint = min;
            _Index        = minIndex;
        }

        #endregion
    }
}