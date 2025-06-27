namespace Nav3D.API
{
    public enum PathfindingResultCode
    {
        //Pathfinding finished successfully.
        SUCCEEDED,
        //There is no path between points.
        PATH_DOES_NOT_EXIST,
        //The pathfinding took longer than allowed and was aborted.
        TIMEOUT,
        //Pathfinding was canceled by the user or Nav3D internal logic.
        CANCELLED,
        //Pathfinding has been canceled because the start point of the path is inside an obstacle.
        START_POINT_INSIDE_OBSTACLE,
        //Pathfinding has been canceled because the target point of the path is inside an obstacle.
        TARGET_POINT_INSIDE_OBSTACLE,
        //Internal error.
        UNKNOWN
    }
}
