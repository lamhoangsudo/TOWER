using System.Collections.Generic;
using System.IO;

namespace Nav3D.Obstacles.Serialization
{
    public class ObstacleInfoGroupedSerializable : ObstacleInfoBaseSerializable
    {
        #region Properties

        public int[] ObstacleInfosIDs { get; set; }
        public override ObstacleInfoSerializableType ObstacleType => ObstacleInfoSerializableType.GROUPED;

        #endregion

        #region Constructors

        public ObstacleInfoGroupedSerializable(ObstacleInfoGrouped _ObstacleInfo, int[] _SingleInfosIDs, int _ID) : base(_ObstacleInfo.Bounds, _ObstacleInfo.Triangles, _ID)
        {
            ObstacleInfosIDs = _SingleInfosIDs;
        }

        public ObstacleInfoGroupedSerializable(BinaryReader _Reader) : base(_Reader)
        {
            int obstacleInfosCount = _Reader.ReadInt32();

            ObstacleInfosIDs = new int[obstacleInfosCount];

            for (int i = 0; i < obstacleInfosCount; i++)
            {
                ObstacleInfosIDs[i] = _Reader.ReadInt32();
            }
        }

        #endregion

        #region Public methods

        public override void WriteIntoBinary(BinaryWriter _Writer)
        {
            base.WriteIntoBinary(_Writer);

            _Writer.Write(ObstacleInfosIDs.Length);

            foreach (int ID in ObstacleInfosIDs)
            {
                _Writer.Write(ID);
            }
        }

        public ObstacleInfoGrouped GetDeserializedInstance(Dictionary<int, ObstacleInfoBase> _ObstacleInfosMap)
        {
            List<ObstacleInfoSingle> obstacleInfos = new List<ObstacleInfoSingle>(ObstacleInfosIDs.Length);

            foreach (int id in ObstacleInfosIDs)
                if (_ObstacleInfosMap.TryGetValue(id, out ObstacleInfoBase obstacleInfo))
                {
                    obstacleInfos.Add(obstacleInfo as ObstacleInfoSingle);
                }

            return new ObstacleInfoGrouped(obstacleInfos, GetDeserializedTriangles(), GetDeserializedBounds());
        }

        #endregion
    }
}