using System;
using System.Collections.Generic;
using UnityEngine;
using Nav3D.API;
using Nav3D.Obstacles;

namespace Nav3D.Dev
{
    public class PathfindingResultDebug : PathfindingResult
    {
        #region Properties

        public List<Leaf> PathfindingHistory { get; private set; }
        public List<Leaf> PathNodes { get; private set; }

        #endregion

        #region Constructors

        public PathfindingResultDebug(
            Vector3[]  _Path,
            Vector3[]  _PathOptimized,
            Vector3[]  _PathSmoothed,
            TimeSpan   _PathfindingDuration,
            TimeSpan   _OptimizingDuration,
            TimeSpan   _SmoothingDuration,
            List<Leaf> _PathfindingHistory,
            List<Leaf> _PathNodes
        )
            : base(_Path,
                   _PathOptimized,
                   _PathSmoothed,
                   _PathfindingDuration,
                   _OptimizingDuration,
                   _SmoothingDuration,
                   new int[] { },
                   new List<(Vector3, PathfindingResultCode)>(),
                   new List<(Vector3, TimeSpan)>())
        {
            PathfindingHistory = _PathfindingHistory;
            PathNodes          = _PathNodes;
        }

        public PathfindingResultDebug(PathfindingResultCode _ResultCode)
            : base(
                   new List<(Vector3, PathfindingResultCode)>(),
                   new List<(Vector3, TimeSpan)>(),
                   _ResultCode)
        {
        }

        #endregion
    }
}
