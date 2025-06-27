using System.IO;
using UnityEngine;

namespace Nav3D.Obstacles.Serialization
{
    class LeafSerializable : NodeSerializable
    {
        #region Properties

        public             int[]                FreeAdjacentIDs { get; }
        protected override NodeSerializableType NodeType        => NodeSerializableType.LEAF;

        #endregion

        #region Constructors

        public LeafSerializable(int _ID, Vector3Int _Index, float _Size, byte _GridLayer, bool _Occupied, int[] _FreeAdjacentIDs)
            : base(_ID, _Index, _Size, _GridLayer, _Occupied)
        {
            FreeAdjacentIDs = _FreeAdjacentIDs;
        }

        public LeafSerializable(BinaryReader _Reader)
            : base(_Reader)
        {
            int freeAdjacentIDsCount = _Reader.ReadInt32();

            FreeAdjacentIDs = new int[freeAdjacentIDsCount];

            for (int i = 0; i < freeAdjacentIDsCount; i++)
            {
                FreeAdjacentIDs[i] = _Reader.ReadInt32();
            }
        }

        #endregion

        #region Public methods

        public override void WriteIntoBinary(BinaryWriter _Writer)
        {
            base.WriteIntoBinary(_Writer);

            _Writer.Write(FreeAdjacentIDs.Length);

            foreach (int childID in FreeAdjacentIDs)
            {
                _Writer.Write(childID);
            }
        }

        public override Node GetDeserializedInstance()
        {
            return new Leaf(ID, new Vector3Int(IndexX, IndexY, IndexZ), Size, GridLayer, Occupied > 0);
        }

        #endregion
    }
}