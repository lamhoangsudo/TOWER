using System;
using System.Threading;
using Nav3D.API;

namespace Nav3D.Obstacles
{
    public class GraphConstructionProgress
    {
        #region Constants

        public static  GraphConstructionProgress INITIAL => new GraphConstructionProgress { Status = ConstructionStatus.WAITING_FOR_CONSTRUCTION };
        public static  GraphConstructionProgress COMPLETED => new GraphConstructionProgress { Status = ConstructionStatus.FINISHED };

        const string PROCESSING_GENERAL_INFO = "Processing..";
        const string TREE_CONSTRUCTION_PREPARATION_INFO = "Preparing to build a tree..";
        readonly string TIRIANGLE_STORAGE_PROGRESS_INFO = "{0} of {1} triangles registered.";
        readonly string TREE_CONSTRUCTION = "Octree construction: {0}";
        readonly string GRAPH_CONNECTIONS_BUILDING_INFO = "Forming leaves connections. {0} of {1} layers processed.";

        #endregion

        #region Nested types

        public enum ConstructionStatus
        {
            WAITING_FOR_CONSTRUCTION = 0,
            FILLING_TRIANGLE_STORAGE = 1,
            TREE_CONSTRUCTION_PREPARATION = 2,
            TREE_CONSTRUCTION = 3,
            GRAPH_CONNECTIONS_BUILDING = 4,
            FINISHED = 5
        }

        #endregion

        #region Attributes

        int m_LeavesTotalCount;
        int m_LeavesCounterCurrent;

        int m_TrianglesCounterCurrent;
        int m_TrianglesCounterTotal;

        int m_LayersProcessedTotalCount;
        int m_LayersProcessedCounterCurrent;

        //Processing start time.
        DateTime m_Start;
        //Processing completion time.
        DateTime m_Finish;
        //Triangle storage building time.
        TimeSpan m_TriangleStorageBuildDuration;
        //Octree build preparations time;
        TimeSpan m_OctreeBulidPreparationsDuration;
        //Octree building time.
        TimeSpan m_OctreeBulidDuration;
        //Navigation graph building time by octree.
        TimeSpan m_GraphBuildDuration;

        CancellationTokenSource m_CTS = new CancellationTokenSource();

        #endregion

        #region Properties

        public ConstructionStatus Status { get; private set; }
        public float Progress { get; private set; }
        public ObstacleAdditionResult Result { get; private set; }

        public CancellationToken CancellationToken => m_CTS.Token;

        #endregion

        #region Public methods

        public void SetStatus(ConstructionStatus _Status)
        {
            Status = _Status;

            switch (_Status)
            {
                case ConstructionStatus.FILLING_TRIANGLE_STORAGE:
                    m_Start = DateTime.UtcNow;
                    break;
                case ConstructionStatus.TREE_CONSTRUCTION_PREPARATION:
                    m_TriangleStorageBuildDuration = DateTime.UtcNow - m_Start;
                    break;
                case ConstructionStatus.TREE_CONSTRUCTION:
                    m_OctreeBulidPreparationsDuration = DateTime.UtcNow - m_Start - m_TriangleStorageBuildDuration;
                    break;
                case ConstructionStatus.GRAPH_CONNECTIONS_BUILDING:
                    m_OctreeBulidDuration = DateTime.UtcNow - m_Start - m_TriangleStorageBuildDuration - m_OctreeBulidPreparationsDuration;
                    break;
                case ConstructionStatus.FINISHED:
                    m_Finish = DateTime.UtcNow;
                    m_GraphBuildDuration = m_Finish - m_Start - m_TriangleStorageBuildDuration - m_OctreeBulidPreparationsDuration - m_OctreeBulidDuration;
                    Result = new ObstacleAdditionResult(m_Start, m_Finish, m_TriangleStorageBuildDuration, m_OctreeBulidPreparationsDuration, m_OctreeBulidDuration, m_GraphBuildDuration);
                    break;
            }
        }

        public string GetInfo()
        {
            switch (Status)
            {
                case ConstructionStatus.FILLING_TRIANGLE_STORAGE:
                    return string.Format(TIRIANGLE_STORAGE_PROGRESS_INFO, m_TrianglesCounterCurrent, m_TrianglesCounterTotal);
                case ConstructionStatus.TREE_CONSTRUCTION_PREPARATION:
                    return TREE_CONSTRUCTION_PREPARATION_INFO;
                case ConstructionStatus.TREE_CONSTRUCTION:
                    return string.Format(TREE_CONSTRUCTION, string.Format("{0:P2}", Progress));
                case ConstructionStatus.GRAPH_CONNECTIONS_BUILDING:
                    return string.Format(GRAPH_CONNECTIONS_BUILDING_INFO, m_LayersProcessedCounterCurrent, m_LayersProcessedTotalCount);
            }

            return PROCESSING_GENERAL_INFO;
        }

        public void SetTrianglesStorageProgress(int _CurrentCounter, int _TotalCounter)
        {
            m_TrianglesCounterCurrent = _CurrentCounter;
            m_TrianglesCounterTotal = _TotalCounter;

            Progress = m_TrianglesCounterCurrent / (float)m_TrianglesCounterTotal;
        }

        public void SetLayersProcessignProgress(int _CurrentCounter, int _TotalCounter)
        {
            m_LayersProcessedCounterCurrent = _CurrentCounter;
            m_LayersProcessedTotalCount = _TotalCounter;

            Progress = m_LayersProcessedCounterCurrent / (float)m_LayersProcessedTotalCount;
        }

        public void SetTotalRootsCount(int _RootsCount)
        {
            m_LeavesTotalCount = _RootsCount;
        }

        public void AddTotalLeavesCount(int _AddValue)
        {
            Interlocked.Add(ref m_LeavesTotalCount, _AddValue);
            Progress = m_LeavesCounterCurrent / (float)m_LeavesTotalCount;
        }

        public void AddProcessedLeavesCount(int _AddValue)
        {
            Interlocked.Add(ref m_LeavesCounterCurrent, _AddValue);
            Progress = m_LeavesCounterCurrent / (float)m_LeavesTotalCount;
        }

        public void CancelConstruction()
        {
            m_CTS.Cancel();
        }

        #endregion
    }
}
