namespace Nav3D.API
{
    public interface IObstacleAdditionProgress
    {
        #region Properties

        public float Progress { get; }
        public bool Finished { get; }

        #endregion

        #region Public methods

        public string GetStatusInfo();
        public string GetResultStats(bool _InMilliseconds = false);

        #endregion
    }
}
