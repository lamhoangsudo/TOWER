using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Nav3D.Common;

namespace Nav3D.API
{
    /// <summary>
    /// Pathfinding result container for internal usage.
    /// </summary>
    public class PathfindingResult
    {
        #region Attributes

        readonly List<(Vector3 Point, PathfindingResultCode Code)> m_TargetResultCodes;
        readonly List<(Vector3 Point, TimeSpan Duration)>          m_FragmentPathfindingDurations;

        #endregion

        #region Properties

        public TimeSpan PathfindingDuration { get; private set; }
        public TimeSpan OptimizingDuration  { get; private set; }
        public TimeSpan SmoothingDuration   { get; private set; }

        /// <summary>
        /// Original path obtained after A* have performed. 
        /// </summary>
        public Vector3[] RawPath { get; }
        /// <summary>
        /// The original path after optimization.
        /// </summary>
        public Vector3[] PathOptimized { get; }
        /// <summary>
        /// The optimized path after smoothing. If smoothing was disabled then the content is identical to PathOptimized. 
        /// </summary>
        public Vector3[] PathSmoothed { get; }
        /// <summary>
        /// The array that contains the indices of the target points in PathSmoothed array.
        /// </summary>
        public int[] TargetIndices { get; }

        /// <summary>
        /// Result code.
        /// </summary>
        public PathfindingResultCode Result { get; }

        /// <summary>
        /// Whether pathfinding was failed.
        /// </summary>
        public bool Failed => Result != PathfindingResultCode.SUCCEEDED;

        #endregion

        #region Constructors

        public PathfindingResult(
                Vector3[]                              _RawPath,
                Vector3[]                              _PathOptimized,
                Vector3[]                              _PathSmoothed,
                TimeSpan                               _PathfindingDuration,
                TimeSpan                               _OptimizingDuration,
                TimeSpan                               _SmoothingDuration,
                int[]                                  _TargetIndices,
                List<(Vector3, PathfindingResultCode)> _TargetResultCodes,
                List<(Vector3, TimeSpan)>              _FragmentPathfindingDurations
            )
        {
            RawPath       = _RawPath;
            PathSmoothed  = _PathSmoothed;
            PathOptimized = _PathOptimized;

            PathfindingDuration = _PathfindingDuration;
            OptimizingDuration  = _OptimizingDuration;
            SmoothingDuration   = _SmoothingDuration;

            TargetIndices                  = _TargetIndices;
            m_TargetResultCodes            = _TargetResultCodes;
            m_FragmentPathfindingDurations = _FragmentPathfindingDurations;

            Result = PathfindingResultCode.SUCCEEDED;
        }

        /// <summary>
        /// Error case constructor.
        /// </summary>
        public PathfindingResult(
                List<(Vector3, PathfindingResultCode)> _TargetResultCodes,
                List<(Vector3, TimeSpan)>              _FragmentPathfindingDurations,
                PathfindingResultCode                  _ResultCode
            )
        {
            m_TargetResultCodes            = _TargetResultCodes;
            m_FragmentPathfindingDurations = _FragmentPathfindingDurations;

            Result = _ResultCode;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Returns text info about what the point pathfinding process was failed on.
        /// Can be useful in case when targets count is greater than 2.
        /// </summary>
        public string GetFailedPointInfo()
        {
            int index = m_TargetResultCodes.FindIndex(_Pair => _Pair.Code != PathfindingResultCode.SUCCEEDED);

            if (index == -1)
                return string.Empty;

            return $"Target index: {index}, Position: {m_TargetResultCodes[index].Point.ToStringExt()}, Code: {m_TargetResultCodes[index].Code};";
        }

        /// <summary>
        /// Returns detailed text info about pathfinding process. 
        /// </summary>
        public string GetLastPathfindingStatsString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{nameof(PathfindingResultCode)}: {Result}");

            if (Result != PathfindingResultCode.SUCCEEDED)
            {
                return stringBuilder.ToString();
            }

            stringBuilder.AppendLine("Pathfinding stages duration in ms:");
            stringBuilder.AppendLine($"{nameof(PathfindingDuration)}: {PathfindingDuration.Milliseconds}");
            stringBuilder.AppendLine($"{nameof(OptimizingDuration)}: {OptimizingDuration.Milliseconds}");
            stringBuilder.AppendLine($"{nameof(SmoothingDuration)}: {SmoothingDuration.Milliseconds}");

            stringBuilder.AppendLine("Targets stats:");

            int fragmentsCount = 0;

            List<(Vector3, PathfindingResultCode)> codesList          = m_TargetResultCodes.Copy();
            List<(Vector3, TimeSpan)>              fragmentsDurations = m_FragmentPathfindingDurations.Copy();

            for (int i = 0; i < TargetIndices.Length; i++)
            {
                int     targetIndex = TargetIndices[i];
                Vector3 target      = PathSmoothed[targetIndex];

                int index = codesList.FindIndex(_Data => _Data.Item1 == target);

                if (index < 0)
                    break;

                (Vector3, PathfindingResultCode) codeItem = codesList[index];
                codesList.RemoveAt(index);

                TimeSpan duration = fragmentsDurations[index].Item2;
                fragmentsDurations.RemoveAt(index);

                stringBuilder
                   .AppendLine($"[{i}]: [{target.ToStringExt()}], CODE: {codeItem.Item2}, DURATION (ms): {duration.TotalMilliseconds}");
                fragmentsCount++;
            }

            return stringBuilder.ToString();
        }

        #endregion
    }
}