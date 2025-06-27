using System;

namespace Nav3D.Common
{
    public interface IExecutable
    {
        #region Public methods

        public void   Execute(Action _OnResolve);
        public string GetExecutingStatus();

        #endregion
    }
}