using UnityEngine;
using System;
using System.IO;

namespace Nav3D.Obstacles.Serialization
{
    public abstract class NodeSerializable
    {
        #region Constants

        static readonly string UNKNOWN_NODE_TYPE = $"Unknown {nameof(NodeSerializableType)} value: {{0}}";

        #endregion

        #region Nested type

        protected enum NodeSerializableType
        {
            FORK = 0,
            LEAF = 1
        }

        #endregion

        #region Factory methods

        public static NodeSerializable ReadFromBytes(BinaryReader _Reader)
        {
            NodeSerializableType nodeType = (NodeSerializableType)_Reader.ReadInt32();

            return nodeType switch
                   {
                       NodeSerializableType.FORK => new ForkSerializable(_Reader),
                       NodeSerializableType.LEAF => new LeafSerializable(_Reader),
                       _                         => throw new Exception(string.Format(UNKNOWN_NODE_TYPE, nodeType))
                   };
        }

        #endregion

        #region Properties

        public    int ID     { get; }
        protected int IndexX { get; }
        protected int IndexY { get; }

        protected int IndexZ { get; }

        protected float Size      { get; }
        protected byte  GridLayer { get; }
        protected byte  Occupied  { get; }

        protected abstract NodeSerializableType NodeType { get; }

        #endregion

        #region Constructors

        protected NodeSerializable(int _ID, Vector3Int _Index, float _Size, byte _GridLayer, bool _Occupied)
        {
            ID        = _ID;
            IndexX    = _Index.x;
            IndexY    = _Index.y;
            IndexZ    = _Index.z;
            Size      = _Size;
            GridLayer = _GridLayer;
            Occupied  = (byte)(_Occupied ? 1 : 0);
        }

        protected NodeSerializable(BinaryReader _Reader)
        {
            ID        = _Reader.ReadInt32();
            IndexX    = _Reader.ReadInt32();
            IndexY    = _Reader.ReadInt32();
            IndexZ    = _Reader.ReadInt32();
            Size      = _Reader.ReadSingle();
            GridLayer = _Reader.ReadByte();
            Occupied  = _Reader.ReadByte();
        }

        #endregion

        #region Public methods

        public virtual void WriteIntoBinary(BinaryWriter _Writer)
        {
            _Writer.Write((int)NodeType);
            _Writer.Write(ID);
            _Writer.Write(IndexX);
            _Writer.Write(IndexY);
            _Writer.Write(IndexZ);
            _Writer.Write(Size);
            _Writer.Write(GridLayer);
            _Writer.Write(Occupied);
        }

        public abstract Node GetDeserializedInstance();

        #endregion
    }
}
