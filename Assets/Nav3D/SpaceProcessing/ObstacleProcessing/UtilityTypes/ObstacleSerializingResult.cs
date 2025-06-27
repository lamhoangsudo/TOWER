using System;

namespace Nav3D.Obstacles.Serialization
{
    public struct ObstacleSerializingResult
    {
        #region Constructors

        public ObstacleSerializingResult(
            DateTime _Start,
            DateTime _Finish,
            TimeSpan _PreparingInfosDuration,
            TimeSpan _BakingObstaclesDuration,
            TimeSpan _PackingBakedDataDuration,
            TimeSpan _SerializingDuration,
            TimeSpan _CompressingDuration
            )
        {
            Start = _Start;
            Finish = _Finish;
            PreparingInfosDuration = _PreparingInfosDuration;
            BakingObstaclesDuration = _BakingObstaclesDuration;
            PackingBakedDataDuration = _PackingBakedDataDuration;
            SerializingDuration = _SerializingDuration;
            CompressingDuration = _CompressingDuration;
            TotalDuration = Finish - Start;
        }

        #endregion

        #region Properties

        //Serializing start time.
        public DateTime Start { get; private set; }
        //Serializing completion time.
        public DateTime Finish { get; private set; }
        //Obstacle infos preparation duration.
        public TimeSpan PreparingInfosDuration { get; private set; }
        //Obstacles graph construction duration.
        public TimeSpan BakingObstaclesDuration { get; private set; }
        //Packing obstacles baked graphs into serializable data structure duration.
        public TimeSpan PackingBakedDataDuration { get; private set; }
        //Serializing compressed data duration.
        public TimeSpan SerializingDuration { get; private set; }
        //Compressing serialized data duration.
        public TimeSpan CompressingDuration { get; private set; }
        //Total serialization operations duration.
        public TimeSpan TotalDuration { get; private set; }

        #endregion
    }
}
