using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Nav3D.Obstacles.Serialization
{
    class ForkSerializable : NodeSerializable
    {
        #region Properties

        byte  ChildrenMap { get; }
        int[] ChildrenIDs { get; }

        protected override NodeSerializableType NodeType => NodeSerializableType.FORK;

        #endregion

        #region Constructors

        public ForkSerializable(int _ID, Vector3Int _Index, float _Size, byte _GridLayer, bool _Occupied, int[] _ChildrenIDs, byte _ChildrenMap)
            : base(_ID, _Index, _Size, _GridLayer, _Occupied)
        {
            ChildrenIDs = _ChildrenIDs;
            ChildrenMap = _ChildrenMap;
        }

        public ForkSerializable(BinaryReader _Reader)
            : base(_Reader)
        {
            int childIDsCount = _Reader.ReadInt32();

            ChildrenIDs = new int[childIDsCount];

            for (int i = 0; i < childIDsCount; i++)
            {
                ChildrenIDs[i] = _Reader.ReadInt32();
            }

            ChildrenMap = _Reader.ReadByte();
        }

        #endregion

        #region Public methods

        public override void WriteIntoBinary(BinaryWriter _Writer)
        {
            base.WriteIntoBinary(_Writer);

            _Writer.Write(ChildrenIDs.Length);

            foreach (int childID in ChildrenIDs)
            {
                _Writer.Write(childID);
            }

            _Writer.Write(ChildrenMap);
        }

        public override Node GetDeserializedInstance()
        {
            return new Fork(ID, new Vector3Int(IndexX, IndexY, IndexZ), Size, GridLayer, Occupied > 0);
        }

        public (ForkChildOctIndex OctIndex, int ID)[] GetChildrenData()
        {
            List<(ForkChildOctIndex OctIndex, int ID)> result = new List<(ForkChildOctIndex OctIndex, int ID)>(8);

            int  childIndex = 0;
            byte mask       = 1;

            for (int i = 0; i < 8; i++)
            {
                if ((ChildrenMap & mask) == 0)
                {
                    mask <<= 1;
                    continue;
                }

                result.Add(((ForkChildOctIndex)mask, ChildrenIDs[childIndex]));
                childIndex++;
                mask <<= 1;
            }

            return result.ToArray();
        }

        #endregion
    }
}