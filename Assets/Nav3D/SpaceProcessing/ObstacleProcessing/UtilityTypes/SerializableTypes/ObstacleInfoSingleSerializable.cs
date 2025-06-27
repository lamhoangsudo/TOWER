using System;
using System.IO;

namespace Nav3D.Obstacles.Serialization
{
    class ObstacleInfoSingleSerializable : ObstacleInfoBaseSerializable
    {
        #region Constants

        const string UNKNOWN_DESERIALIZATION_OBSTACLE_INFO_TYPE = "Unknown obstacle info type: {0}";
        const string UNKNOWN_SERIALIZATION_OBSTACLE_INFO_TYPE = "Unknown obstacle info single type: {0}";

        #endregion

        #region Nested types

        public enum ObstacleInfoSingleType
        {
            MESH = 0,
            TERRAIN = 1
        }

        #endregion

        #region Properties

        public int ObstacleControllerID { get; set; }
        public ObstacleInfoSingleType ObstacleInfoType { get; set; }
        public override ObstacleInfoSerializableType ObstacleType => ObstacleInfoSerializableType.SINGLE;

        #endregion

        #region Construction

        public ObstacleInfoSingleSerializable(ObstacleInfoSingle _ObstacleInfo, int _ID) : base(_ObstacleInfo.Bounds, _ObstacleInfo.Triangles, _ID)
        {
            ObstacleControllerID = _ObstacleInfo.ObstacleControllerID;

            if (_ObstacleInfo is ObstacleInfoMesh)
            {
                ObstacleInfoType = ObstacleInfoSingleType.MESH;
            }
            else if (_ObstacleInfo is ObstacleInfoTerrain)
            {
                ObstacleInfoType = ObstacleInfoSingleType.TERRAIN;
            }
            else
            {
                throw new Exception(string.Format(UNKNOWN_SERIALIZATION_OBSTACLE_INFO_TYPE, _ObstacleInfo.GetType().FullName));
            }
        }

        public ObstacleInfoSingleSerializable(BinaryReader _Reader) : base(_Reader)
        {
            ObstacleControllerID = _Reader.ReadInt32();
            ObstacleInfoType = (ObstacleInfoSingleType)_Reader.ReadInt32();
        }

        #endregion

        #region Public methods

        public override void WriteIntoBinary(BinaryWriter _Writer)
        {
            base.WriteIntoBinary(_Writer);

            _Writer.Write(ObstacleControllerID);
            _Writer.Write((int)ObstacleInfoType);
        }

        public ObstacleInfoSingle GetDeserializedInstance()
        {
            if (ObstacleInfoType == ObstacleInfoSingleType.MESH)
            {
                return new ObstacleInfoMesh(ObstacleControllerID, GetDeserializedBounds(), GetDeserializedTriangles());
            }
            else if (ObstacleInfoType == ObstacleInfoSingleType.TERRAIN)
            {
                return new ObstacleInfoTerrain(ObstacleControllerID, GetDeserializedBounds(), GetDeserializedTriangles());
            }
            else
            {
                throw new Exception(string.Format(UNKNOWN_DESERIALIZATION_OBSTACLE_INFO_TYPE, ObstacleInfoType));
            }
        }

        #endregion
    }
}