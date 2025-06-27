namespace Nav3D.API
{
    /// <summary>
    /// Nav3DAgent motion navigation mode.
    /// </summary>
    public enum MotionNavigationType
    {
        //Use both pathfinding and local avoidance
        COMBINED = 0,

        //Use only pathfinding
        GLOBAL = 1,

        //Use only local avoidance
        LOCAL = 2
    }
}