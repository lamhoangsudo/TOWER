using System.Collections.Generic;
using Nav3D.Common;
using Nav3D.Obstacles.Serialization;
using UnityEngine;

namespace Nav3D.Obstacles
{
    public abstract class ObstacleInfoSingle : ObstacleInfoBase
    {
        #region Attributes

        int m_ObstacleControllerID;

        #endregion

        #region Properties

        public int ObstacleControllerID => m_ObstacleControllerID;
        public override List<int> IDs => new List<int> { m_ObstacleControllerID };

        #endregion

        #region Constructors

        public ObstacleInfoSingle(int _ObstacleControllerID)
        {
            m_ObstacleControllerID = _ObstacleControllerID;
        }

        #endregion

        #region Public methods

        public override void ReplaceID(int _OldID, int _NewID)
        {
            if (m_ObstacleControllerID != _OldID)
                return;

            m_ObstacleControllerID = _NewID;
        }

        public ObstacleInfoBaseSerializable GetSerializableInstance(int _ID)
        {
            return new ObstacleInfoSingleSerializable(this, _ID);
        }

        #endregion

        #region Service methods

        protected abstract List<Triangle> GetTriangles(Vector3 _Position);

        protected void ObtainTriangles(Vector3 _Position)
        {
            Triangles = GetTriangles(_Position);

            ComputeBounds();
        }

        #endregion
    }
}