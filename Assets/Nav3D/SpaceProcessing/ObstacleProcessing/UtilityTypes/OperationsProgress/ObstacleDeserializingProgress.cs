using System;
using System.Text;
using Nav3D.API;

namespace Nav3D.Obstacles.Serialization
{
    public class ObstacleDeserializingProgress : IObstacleAdditionProgress
    {
        #region Constants

        public static ObstacleDeserializingProgress FAILED => new ObstacleDeserializingProgress { m_Status = ObstacleDeserializingStatus.FAILED };

        const string DECOMPRESSION_INFO = "Decompressing obstacle data file..";
        const string DESERIALIZING_INFO = "Deserializing obstacle data..";
        const string UNPACKING_INFO = "Unpacking data..";
        const string FINISHED_INFO = "Deserializing finished!";
        const string FAILED_INFO = "Deserializing failed";

        readonly string UNKNOWN_DESERIALIZING_STATUS_TYPE = $"Unknown {nameof(ObstacleDeserializingStatus)} value: {{0}}";

        const string RESULT_STATS_HEADER = "Obstacle deserializing stats:";
        const string RESULT_STATS_IN_PROGRESS_INFO = "Obstacle deserializing still in progress. Please stand by.";
        readonly string RESULT_STATS_DECOMPRESSING_INFO = "Decompressing file takes: {0}";
        readonly string RESULT_STATS_DESERIALIZING_INFO = "Deserializing takes: {0}";
        readonly string RESULT_STATS_UNPACKING_INFO = "Unpacking data takes: {0}";
        readonly string RESULT_STATS_TOTAL_PROCESSING_INFO = "Total processing takes: {0}";

        #endregion

        #region Nested types

        public enum ObstacleDeserializingStatus
        {
            DECOMPRESSION = 0,
            DESERIALIZING = 1,
            UNPACKING = 2,
            FINISHED = 3,
            FAILED = 4
        }

        #endregion

        #region Attributes

        ObstacleDeserializingStatus m_Status;

        //Processing start time.
        DateTime m_Start;
        //Processing completion time.
        DateTime m_Finish;
        //Decompressing data file duration.
        TimeSpan m_DecompressingDuration;
        //Deserializing data duration.
        TimeSpan m_DeserializingDuration;
        //Unpacking data duration.
        TimeSpan m_UnpackingDataDuration;
        //Total deserialization duration
        TimeSpan m_TotalProcessingDuration;

        #endregion

        #region Properties

        public float Progress { get; private set; }
        public bool Finished => m_Status == ObstacleDeserializingStatus.FINISHED;

        #endregion

        #region Public methods

        public void Fail()
        {
            SetStatus(ObstacleDeserializingStatus.FAILED);
        }

        public void SetStatus(ObstacleDeserializingStatus _Status)
        {
            m_Status = _Status;

            switch (_Status)
            {
                case ObstacleDeserializingStatus.DECOMPRESSION:
                    m_Start = DateTime.UtcNow;
                    break;
                case ObstacleDeserializingStatus.DESERIALIZING:
                    m_DecompressingDuration = DateTime.UtcNow - m_Start;
                    break;
                case ObstacleDeserializingStatus.UNPACKING:
                    m_DeserializingDuration = DateTime.UtcNow - m_Start - m_DecompressingDuration;
                    break;
                case ObstacleDeserializingStatus.FINISHED:
                    m_Finish = DateTime.UtcNow;
                    m_UnpackingDataDuration = m_Finish - m_Start - m_DecompressingDuration - m_DeserializingDuration;
                    m_TotalProcessingDuration = m_Finish - m_Start;
                    break;
            }
        }

        public void SetProgress(float _Progrress)
        {
            Progress = _Progrress;
        }

        public string GetStatusInfo()
        {
            switch (m_Status)
            {
                case ObstacleDeserializingStatus.DECOMPRESSION:
                    return DECOMPRESSION_INFO;
                case ObstacleDeserializingStatus.DESERIALIZING:
                    return DESERIALIZING_INFO;
                case ObstacleDeserializingStatus.UNPACKING:
                    return UNPACKING_INFO;
                case ObstacleDeserializingStatus.FINISHED:
                    return FINISHED_INFO;
                case ObstacleDeserializingStatus.FAILED:
                    return FAILED_INFO;
            }

            throw new Exception(string.Format(UNKNOWN_DESERIALIZING_STATUS_TYPE, m_Status));
        }

        public string GetResultStats(bool _InMilliseconds = false)
        {
            if (m_Status == ObstacleDeserializingStatus.FAILED)
                return FAILED_INFO;

            if (m_Status != ObstacleDeserializingStatus.FINISHED)
                return RESULT_STATS_IN_PROGRESS_INFO;

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(RESULT_STATS_HEADER);
            stringBuilder.AppendLine(string.Format(RESULT_STATS_DECOMPRESSING_INFO, _InMilliseconds ? m_DecompressingDuration.TotalMilliseconds.ToString() : m_DecompressingDuration.ToString()));
            stringBuilder.AppendLine(string.Format(RESULT_STATS_DESERIALIZING_INFO, _InMilliseconds ? m_DeserializingDuration.TotalMilliseconds.ToString() : m_DeserializingDuration.ToString()));
            stringBuilder.AppendLine(string.Format(RESULT_STATS_UNPACKING_INFO, _InMilliseconds ? m_UnpackingDataDuration.TotalMilliseconds.ToString() : m_UnpackingDataDuration.ToString()));
            stringBuilder.AppendLine(string.Format(RESULT_STATS_TOTAL_PROCESSING_INFO, _InMilliseconds ? m_TotalProcessingDuration.TotalMilliseconds.ToString() : m_TotalProcessingDuration.ToString()));

            return stringBuilder.ToString();
        }

        #endregion
    }
}