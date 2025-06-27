using UnityEngine;

namespace Nav3D.Common
{
    interface ICurve
    {
        #region Properties

        public Segment3[] Segments { get; }

        #endregion

        #region Public methods

        bool Intersects(Bounds _Bounds);

        #endregion
    }
}