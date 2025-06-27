using System;
using System.Text;
using System.Threading;
using Nav3D.API;

namespace Nav3D.Obstacles.Serialization
{
    public class ObstacleSerializingProgress
    {
        #region Constants

        public static ObstacleSerializingProgress INITIAL
        {
            get
            {
                ObstacleSerializingProgress progress = new ObstacleSerializingProgress();
                progress.SetStatus(ObstacleSerializingStatus.PREPARING_OBSTACLE_INFOS);

                return progress;
            }
        }

        const string SERIALIZING_GENERAL = "Serializing in progress..";
        const string TITLE = "Obstacle serializing progress";
        const string PREPARING_OBSTACLE_INFOS = "Information about obstacles is being prepared..";
        readonly string PACKING_BACKED_OBSTACLES_DATA = "Packing data: {0} / {1}";
        const string SERIALIZING = "Serializing data..";
        const string COMPRESSING = "Compressing packed data..";

        const string RESULT_STATS_HEADER = "Obstacle serializing stats:";
        const string RESULT_STATS_IN_PROGRESS_INFO = "Obstacle serializing still in progress. Please stand by.";
        readonly string RESULT_STATS_PREPARING_INFOS_INFO = "Preparing obstacle infos takes: {0}";
        readonly string RESULT_STATS_BACKING_INFO = "Backing obstacles takes: {0}";
        readonly string RESULT_STATS_PACKING_INFO = "Packing data takes: {0}";
        readonly string RESULT_STATS_SERIALIZING_INFO = "Serializing takes: {0}";
        readonly string RESULT_STATS_COMPRESSING_INFO = "Compressing takes: {0}";
        readonly string RESULT_STATS_TOTAL_PROCESSING_INFO = "Total processing takes: {0}";

        #endregion

        #region Nested types

        public enum ObstacleSerializingStatus
        {
            PREPARING_OBSTACLE_INFOS = 0,
            BAKING_OBSTACLES = 1,
            PACKING_BAKED_DATA = 2,
            SERIALIZING_DATA = 3,
            COMPRESSING_DATA = 4,
            FINISHED = 5
        }

        #endregion

        #region Attributes

        ObstacleSerializingStatus m_Status;

        //Serializing start time.
        DateTime m_Start;
        //Serializing completion time.
        DateTime m_Finish;
        //Obstacle infos preparation duration.
        TimeSpan m_PreparingInfosDuration;
        //Obstacles graph construction duration.
        TimeSpan m_BakingObstaclesDuration;
        //Packing obstacles baked graphs into serializable data structure duration.
        TimeSpan m_PackingBakedDataDuration;
        //Serializing packed data duration.
        TimeSpan m_SerializingDuration;
        //Compressing serialized data duration.
        TimeSpan m_CompressingDuration;
        //Total serialization duration
        TimeSpan m_TotalProcessingDuration;

        CancellationTokenSource m_CTS = new CancellationTokenSource();

        int m_TotalNodesToPackCount;
        int m_PackedNodesCount;

        #endregion

        #region Properties

        public ObstacleAdditionProgress ObstacleAdditionProgress { get; private set; }
        public ObstacleSerializingResult Result { get; private set; }
        public CancellationToken CancellationToken => m_CTS.Token;

        public float Progress { get; private set; }

        #endregion

        #region Public methods

        public void SetObstacleAdditionProgress(ObstacleAdditionProgress _ObstacleAdditionProgress)
        {
            ObstacleAdditionProgress = _ObstacleAdditionProgress;
        }

        public void SetNodesPackingProgress(int _Counter, int _TotalCount)
        {
            m_PackedNodesCount = _Counter;
            m_TotalNodesToPackCount = _TotalCount;

            Progress = m_PackedNodesCount / (float)m_TotalNodesToPackCount;
        }

        public void SetStatus(ObstacleSerializingStatus _Status)
        {
            m_Status = _Status;

            switch (_Status)
            {
                case ObstacleSerializingStatus.PREPARING_OBSTACLE_INFOS:
                    m_Start = DateTime.UtcNow;
                    break;
                case ObstacleSerializingStatus.BAKING_OBSTACLES:
                    m_PreparingInfosDuration = DateTime.UtcNow - m_Start;
                    break;
                case ObstacleSerializingStatus.PACKING_BAKED_DATA:
                    m_BakingObstaclesDuration = DateTime.UtcNow - m_Start - m_PreparingInfosDuration;
                    break;
                case ObstacleSerializingStatus.SERIALIZING_DATA:
                    m_PackingBakedDataDuration = DateTime.UtcNow - m_Start - m_PreparingInfosDuration - m_BakingObstaclesDuration;
                    break;
                case ObstacleSerializingStatus.COMPRESSING_DATA:
                    m_SerializingDuration = DateTime.UtcNow - m_Start - m_PreparingInfosDuration - m_BakingObstaclesDuration - m_PackingBakedDataDuration;
                    break;
                case ObstacleSerializingStatus.FINISHED:
                    m_Finish = DateTime.UtcNow;
                    m_CompressingDuration = m_Finish - m_Start - m_PreparingInfosDuration - m_BakingObstaclesDuration - m_PackingBakedDataDuration - m_SerializingDuration;
                    m_TotalProcessingDuration = m_Finish - m_Start;
                    Result = new ObstacleSerializingResult(
                        m_Start,
                        m_Finish,
                        m_PreparingInfosDuration,
                        m_BakingObstaclesDuration,
                        m_PackingBakedDataDuration,
                        m_SerializingDuration,
                        m_CompressingDuration);
                    break;
            }
        }

        public float GetProgress()
        {
            switch (m_Status)
            {
                case ObstacleSerializingStatus.BAKING_OBSTACLES:
                    return ObstacleAdditionProgress?.GetProgress() ?? Progress;
                default:
                    return Progress;
            }
        }

        public string GetTitle()
        {
            return TITLE;
        }

        public string GetInfo()
        {
            switch (m_Status)
            {
                case ObstacleSerializingStatus.PREPARING_OBSTACLE_INFOS:
                    return PREPARING_OBSTACLE_INFOS;
                case ObstacleSerializingStatus.BAKING_OBSTACLES:
                    return ObstacleAdditionProgress?.GetStatusInfo() ?? SERIALIZING_GENERAL;
                case ObstacleSerializingStatus.PACKING_BAKED_DATA:
                    return string.Format(PACKING_BACKED_OBSTACLES_DATA, m_PackedNodesCount, m_TotalNodesToPackCount);
                case ObstacleSerializingStatus.COMPRESSING_DATA:
                    return COMPRESSING;
                case ObstacleSerializingStatus.SERIALIZING_DATA:
                    return SERIALIZING;
            }

            return SERIALIZING_GENERAL;
        }

        public string GetResultStats(bool _InMilliseconds = false)
        {
            if (m_Status != ObstacleSerializingStatus.FINISHED)
                return RESULT_STATS_IN_PROGRESS_INFO;

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(RESULT_STATS_HEADER);
            stringBuilder.AppendLine(string.Format(RESULT_STATS_PREPARING_INFOS_INFO, _InMilliseconds ? m_PreparingInfosDuration.Milliseconds.ToString() : m_PreparingInfosDuration.ToString()));
            stringBuilder.AppendLine(string.Format(RESULT_STATS_BACKING_INFO, _InMilliseconds ? m_BakingObstaclesDuration.Milliseconds.ToString() : m_BakingObstaclesDuration.ToString()));
            stringBuilder.AppendLine(string.Format(RESULT_STATS_PACKING_INFO, _InMilliseconds ? m_PackingBakedDataDuration.Milliseconds.ToString() : m_PackingBakedDataDuration.ToString()));
            stringBuilder.AppendLine(string.Format(RESULT_STATS_SERIALIZING_INFO, _InMilliseconds ? m_SerializingDuration.Milliseconds.ToString() : m_SerializingDuration.ToString()));
            stringBuilder.AppendLine(string.Format(RESULT_STATS_COMPRESSING_INFO, _InMilliseconds ? m_CompressingDuration.Milliseconds.ToString() : m_CompressingDuration.ToString()));
            stringBuilder.AppendLine(string.Format(RESULT_STATS_TOTAL_PROCESSING_INFO, _InMilliseconds ? m_TotalProcessingDuration.Milliseconds.ToString() : m_TotalProcessingDuration.ToString()));

            return stringBuilder.ToString();
        }

        public void CancelSerialization()
        {
            if (m_Status == ObstacleSerializingStatus.FINISHED)
                return;

            m_CTS.Cancel();

            ObstacleAdditionProgress?.CancelAddition();
        }

        #endregion
    }
}
