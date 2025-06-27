using Nav3D.API;
using System.Collections.Generic;
using UnityEngine;

namespace Nav3D.Obstacles
{
    public class OctreePathfindingResult
    {
        #region Properties

        public List<Vector3> Path { get; private set; }
        public PathfindingResultCode ResultCode { get; private set; }
        public bool Failed => ResultCode != PathfindingResultCode.SUCCEEDED;

        #endregion

        #region Constructors

        public OctreePathfindingResult(List<Vector3> _Path, PathfindingResultCode _Result)
        {
            Path = _Path;
            ResultCode = _Result;
        }

        #endregion
    }
}