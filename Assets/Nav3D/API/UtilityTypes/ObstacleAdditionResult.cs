using System;
using System.Linq;

namespace Nav3D.API
{
    public struct ObstacleAdditionResult
    {
        #region Constructors

        public ObstacleAdditionResult(
            DateTime _Start,
            DateTime _Finish,
            TimeSpan _TriangleStorageBuildingDuration,
            TimeSpan _OctreeBulidPreparationsDuration,
            TimeSpan _OctreeBulidDuration,
            TimeSpan _GraphBuildDuration)
        {
            Start = _Start;
            Finish = _Finish;
            TriangleStorageBuildDuration = _TriangleStorageBuildingDuration;
            OctreeBulidPreparationsDuration = _OctreeBulidPreparationsDuration;
            OctreeBulidDuration = _OctreeBulidDuration;
            GraphBuildDuration = _GraphBuildDuration;
            TotalProcessingDuration = _Finish - _Start;
        }

        #endregion

        #region Properties

        //Processing start time.
        public DateTime Start { get; private set; }
        //Processing completion time.
        public DateTime Finish { get; private set; }
        //Triangle storage building time.
        public TimeSpan TriangleStorageBuildDuration { get; private set; }
        //Octree build preparations time;
        public TimeSpan OctreeBulidPreparationsDuration { get; private set; }
        //Octree building time.
        public TimeSpan OctreeBulidDuration { get; private set; }
        //Navigation graph building time by octree.
        public TimeSpan GraphBuildDuration { get; private set; }
        //Total processing time for an obstacle.
        public TimeSpan TotalProcessingDuration { get; private set; }

        #endregion

        #region Public methods

        public static ObstacleAdditionResult GetCombinedResult(ObstacleAdditionResult[] _Results)
        {
            DateTime start = _Results.Min(_Result => _Result.Start);
            DateTime finish = _Results.Max(_Result => _Result.Finish);

            return new ObstacleAdditionResult(
                start,
                finish,
                TimeSpan.FromMilliseconds(_Results.Sum(_Result => _Result.TriangleStorageBuildDuration.TotalMilliseconds)),
                TimeSpan.FromMilliseconds(_Results.Sum(_Result => _Result.OctreeBulidPreparationsDuration.TotalMilliseconds)),
                TimeSpan.FromMilliseconds(_Results.Sum(_Result => _Result.OctreeBulidDuration.TotalMilliseconds)),
                TimeSpan.FromMilliseconds(_Results.Sum(_Result => _Result.GraphBuildDuration.TotalMilliseconds))
                );
        }

        #endregion
    }
}