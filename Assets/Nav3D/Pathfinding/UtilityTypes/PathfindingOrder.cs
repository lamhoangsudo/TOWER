using System;
using System.Threading;
using System.Threading.Tasks;
using Nav3D.API;
using UnityEngine;
using Nav3D.Common;
using PathfindingMethod =
    System.Func<
        UnityEngine.Vector3[],
        bool,
        bool,
        bool,
        bool,
        bool,
        int,
        System.Threading.CancellationToken,
        System.Threading.CancellationToken,
        System.Action<string>,
        Nav3D.Common.Log,
        Nav3D.API.PathfindingResult>;

namespace Nav3D.Pathfinding
{
    public class PathfindingOrder : IExecutable
    {
        #region Constants

        readonly string LOG_CTOR                 = $"{nameof(PathfindingOrder)}.ctor (ID:{{0}}): Points {{1}}, Loop: {{2}}";
        readonly string LOG_EXECUTION_STARTED    = $"{nameof(PathfindingOrder)}.{nameof(Execute)}: for points {{0}}";
        readonly string LOG_PATHFINDING_STARTED  = $"{nameof(PathfindingOrder)}: Pathfinding task started: for points {{0}}";
        readonly string LOG_PATHFINDING_FINISHED = $"{nameof(PathfindingOrder)}: Pathfinding task finished: for points {{0}}";

        const string FAIL_TIMEOUT               = "Pathfinding for points {0} failed;\nReason = TIMEOUT: {1} ms was taken, time limit is: {2} ms";
        const string FAIL_NO_PATH               = "Pathfinding for points {0} failed;\nReason = PATH DOES NOT EXIST: {1} ms was taken";
        const string FAIL_CANCELLED             = "Pathfinding for points {0} failed;\nReason = CANCELLED BY EXTERNAL CONTROLLER: {1} ms was taken";
        const string FAIL_START_INSIDE_OBSTACLE = "Pathfinding for points {0} failed;\nReason = START POINT IS INSIDE THE OBSTACLE: {1} ms was taken";

        const string FAIL_TARGET_INSIDE_OBSTACLE =
            "Pathfinding for points {0} failed;\nReason = ONE OF THE POINTS IS INSIDE THE OBSTACLE; Failed point info {1}: {2} ms was taken";

        const string FAIL_UNKNOWN = "Pathfinding for points {0} canceled;\nReason = UNKNOWN: {1} ms was taken";

        #endregion

        #region Attributes

        readonly string m_ID;

        readonly Vector3[] m_Points;

        readonly bool m_Loop;
        readonly bool m_SkipUnpassableTargets;
        readonly bool m_TryRepositionStartIfOccupied;
        readonly bool m_TryRepositionTargetIfOccupied;
        readonly bool m_Smooth;
        readonly int  m_PerMinBucketSmoothSamples;

        readonly CancellationToken m_CancellationTokenOuter;
        readonly int               m_Timeout;

        readonly PathfindingMethod m_PathfindingMethod;

        readonly Action<PathfindingResult> m_OnSuccess;
        readonly Action<PathfindingError>  m_OnFail;

        string       m_Status;
        readonly Log m_Log;

        #endregion

        #region Properties

        public bool Resolved { get; private set; }

        #endregion

        #region Constructors

        public PathfindingOrder(
                string                    _ID,
                Vector3[]                 _Points,
                bool                      _Loop,
                bool                      _SkipUnpassableTargets,
                bool                      _TryRepositionStartIfOccupied,
                bool                      _TryRepositionTargetIfOccupied,
                bool                      _Smooth,
                int                       _PerMinBucketSmoothSamples,
                CancellationToken         _CancellationToken,
                int                       _Timeout,
                PathfindingMethod         _PathfindingMethod,
                Action<PathfindingResult> _OnSuccess,
                Action<PathfindingError>  _OnFail,
                Log                       _Log = null
            )
        {
            m_ID                            = _ID;
            m_Points                        = _Points;
            m_Loop                          = _Loop;
            m_SkipUnpassableTargets         = _SkipUnpassableTargets;
            m_TryRepositionStartIfOccupied  = _TryRepositionStartIfOccupied;
            m_TryRepositionTargetIfOccupied = _TryRepositionTargetIfOccupied;
            m_Smooth                        = _Smooth;
            m_PerMinBucketSmoothSamples     = _PerMinBucketSmoothSamples;

            m_CancellationTokenOuter = _CancellationToken;
            m_Timeout                = _Timeout;

            m_PathfindingMethod = _PathfindingMethod;

            m_OnSuccess = _OnSuccess;
            m_OnFail    = _OnFail;

            (m_Log = _Log)?.WriteFormat(LOG_CTOR, GetHashCode(), UtilsCommon.GetPointsString(m_Points), m_Loop);
        }

        #endregion

        #region Public methods

        public string GetExecutingStatus()
        {
            return $"{m_ID}: {m_Status}";
        }

        public void Execute(Action _OnResolve)
        {
            const string STATUS_EXECUTION_CALLED         = "Execute method called";
            const string STATUS_EXECUTING_TASK_STARTED   = "Pathfinding task started";
            const string STATUS_EXECUTING_TASK_FINISHED  = "Pathfinding task finished";
            const string STATUS_EXECUTING_TASK_CANCELLED = "Pathfinding task cancelled";

            m_Status = STATUS_EXECUTION_CALLED;
            m_Log?.WriteFormat(LOG_EXECUTION_STARTED, UtilsCommon.GetPointsString(m_Points));

            CancellationTokenSource timeoutCancellationTokenSource = new CancellationTokenSource();
            CancellationToken       cancellationTokenTimeout       = timeoutCancellationTokenSource.Token;

            void OnCancelActions()
            {
                m_Status = STATUS_EXECUTING_TASK_CANCELLED;
                _OnResolve.Invoke();
                Resolved = true;
            }

            if (m_CancellationTokenOuter.IsCancellationRequested)
            {
                OnCancelActions();

                return;
            }

            Task.Run(
                     () =>
                     {
                         m_Status = STATUS_EXECUTING_TASK_STARTED;

                         m_Log?.WriteFormat(LOG_PATHFINDING_STARTED, UtilsCommon.GetPointsString(m_Points));

                         timeoutCancellationTokenSource.CancelAfter(m_Timeout);

                         PathfindingResult result = m_PathfindingMethod.Invoke(
                                 m_Points,
                                 m_Loop,
                                 m_SkipUnpassableTargets,
                                 m_TryRepositionStartIfOccupied,
                                 m_TryRepositionTargetIfOccupied,
                                 m_Smooth,
                                 m_PerMinBucketSmoothSamples,
                                 m_CancellationTokenOuter,
                                 cancellationTokenTimeout,
                                 _Status => m_Status = _Status,
                                 m_Log
                             );

                         if (result.Failed)
                         {
                             string message = result.Result switch
                             {
                                 PathfindingResultCode.PATH_DOES_NOT_EXIST => string.Format(
                                         FAIL_NO_PATH,
                                         UtilsCommon.GetPointsString(m_Points),
                                         result.PathfindingDuration.TotalMilliseconds
                                     ),
                                 PathfindingResultCode.TIMEOUT => string.Format(
                                         FAIL_TIMEOUT,
                                         UtilsCommon.GetPointsString(m_Points),
                                         result.PathfindingDuration.TotalMilliseconds,
                                         m_Timeout
                                     ),
                                 PathfindingResultCode.CANCELLED => string.Format(
                                         FAIL_CANCELLED,
                                         UtilsCommon.GetPointsString(m_Points),
                                         result.PathfindingDuration.TotalMilliseconds
                                     ),
                                 PathfindingResultCode.START_POINT_INSIDE_OBSTACLE => string.Format(
                                         FAIL_START_INSIDE_OBSTACLE,
                                         UtilsCommon.GetPointsString(m_Points),
                                         result.PathfindingDuration.TotalMilliseconds
                                     ),
                                 PathfindingResultCode.TARGET_POINT_INSIDE_OBSTACLE => string.Format(
                                         FAIL_TARGET_INSIDE_OBSTACLE,
                                         UtilsCommon.GetPointsString(m_Points),
                                         result.GetFailedPointInfo(),
                                         result.PathfindingDuration.TotalMilliseconds
                                     ),
                                 PathfindingResultCode.UNKNOWN => string.Format(
                                         FAIL_UNKNOWN,
                                         UtilsCommon.GetPointsString(m_Points),
                                         result.PathfindingDuration.TotalMilliseconds
                                     )
                             };

                             _OnResolve.Invoke();
                             Resolved = true;

                             m_OnFail?.Invoke(new PathfindingError(result.Result, message));
                         }
                         else
                         {
                             _OnResolve.Invoke();
                             Resolved = true;

                             m_OnSuccess.Invoke(result);
                         }

                         m_Status = STATUS_EXECUTING_TASK_FINISHED;
                         m_Log?.WriteFormat(LOG_PATHFINDING_FINISHED, UtilsCommon.GetPointsString(m_Points));
                     },
                     m_CancellationTokenOuter
                 )
                .ContinueWith(_ => OnCancelActions(), TaskContinuationOptions.OnlyOnCanceled);
        }

        #endregion
    }
}