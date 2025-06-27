using Nav3D.Pathfinding;

namespace Nav3D.API
{
    static class Nav3DPathfindingManager
    {
        #region Properties

        /// <summary>
        /// Determines the number of concurrently executing pathfinding tasks.
        /// By default, is Environment.ProcessorCount - 1
        /// </summary>
        public static int MaxPathfindingTasks
        {
            get
            {
                Nav3DManager.CheckInitedHard();

                return PathfindingManager.Instance.PathFindingTasksMaxCount;
            }
            set
            {
                Nav3DManager.CheckInitedHard();

                PathfindingManager.Instance.PathFindingTasksMaxCount = value;
            }
        }

        /// <summary>
        /// The number of pathfinding tasks that are currently running
        /// </summary>
        public static int CurrentPathfindingTasksCount
        {
            get
            {
                if (!Nav3DManager.Inited)
                    throw new Nav3DManager.Nav3DManagerNotInitializedException();

                return PathfindingManager.Instance.PathFindingTasksOperatingCount;
            }
        }

        #endregion
    }
}
