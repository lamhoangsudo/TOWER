using Nav3D.API;
using System.Collections.Generic;

namespace Nav3D.Obstacles
{
    public struct PathResolverResult
    {
        #region Constructors

        public PathResolverResult(List<Leaf> _Path, PathfindingResultCode _Result)
        {
            Path = _Path;
            ResultCode = _Result;
        }

        #endregion

        #region Properties

        public List<Leaf> Path { get; private set; }
        public PathfindingResultCode ResultCode { get; private set; }

        #endregion
    }
}
