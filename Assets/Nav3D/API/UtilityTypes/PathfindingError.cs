namespace Nav3D.API
{
    /// <summary>
    /// Error data thrown in case of a pathfinding error.
    /// </summary>
    public struct PathfindingError
    {
        #region Properties

        public PathfindingResultCode Reason { get; }
        public string Msg { get; }

        #endregion

        #region Constructors

        public PathfindingError(PathfindingResultCode _Type, string _Msg)
        {
            Reason = _Type;
            Msg = _Msg;
        }

        #endregion
        
        #region Public methods

        public override string ToString()
        {
            return $"Pathfinding was failed with reason: {Reason}. Message: {Msg}";
        }
        
        #endregion
    }
}