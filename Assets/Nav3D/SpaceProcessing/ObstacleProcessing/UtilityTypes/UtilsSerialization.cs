using System.Collections.Generic;
using System.Linq;
using Nav3D.Common;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System;
using ObstacleSerializingStatus = Nav3D.Obstacles.Serialization.ObstacleSerializingProgress.ObstacleSerializingStatus;
using ObstacleDeserializingStatus = Nav3D.Obstacles.Serialization.ObstacleDeserializingProgress.ObstacleDeserializingStatus;

namespace Nav3D.Obstacles.Serialization
{
    public static class UtilsSerialization
    {
        #region Public methods

        public static void SerializeObstacleData(ObstacleDataSerializable _ObstacleData, string _FilePath, ObstacleSerializingProgress _Progress)
        {
            try
            {
                _Progress.SetStatus(ObstacleSerializingStatus.SERIALIZING_DATA);

                using MemoryStream serializedStream = new MemoryStream();
                _ObstacleData.WriteIntoStream(serializedStream);

                _Progress.SetStatus(ObstacleSerializingStatus.COMPRESSING_DATA);

                using FileStream fileStream = new FileStream(_FilePath, FileMode.OpenOrCreate);
                fileStream.SetLength(0);

                using DeflateStream compressor = new DeflateStream(fileStream, System.IO.Compression.CompressionLevel.Optimal);
                serializedStream.Position = 0;

                serializedStream.CopyTo(compressor);
            }
            catch (Exception _Exception)
            {
                Debug.LogException(_Exception);
            }
        }

        public static ObstacleDataSerializable DeserializeObstacleData(byte[] _Binary, ObstacleDeserializingProgress _Progress)
        {
            try
            {
                _Progress.SetStatus(ObstacleDeserializingStatus.DECOMPRESSION);

                using MemoryStream compressedStream = new MemoryStream(_Binary);
                compressedStream.Position = 0;
                using MemoryStream decompressedStream = new MemoryStream();
                using DeflateStream decompressor = new DeflateStream(compressedStream, CompressionMode.Decompress);

                decompressor.CopyTo(decompressedStream);

                _Progress.SetProgress(1);
                _Progress.SetStatus(ObstacleDeserializingStatus.DESERIALIZING);

                decompressedStream.Position = 0;

                return new ObstacleDataSerializable(decompressedStream, _Progress);
            }
            catch (Exception _Exception)
            {
                Debug.LogException(_Exception);
            }

            return null;
        }

        public static byte[] TrianglesToBytes(IEnumerable<Triangle> _Triangles)
        {
            //the length is triangles count * 9 (float per triangle) * 4 (float bytes)
            List<byte> bytes = new List<byte>(_Triangles.Count() * 36);

            foreach (Triangle triangle in _Triangles)
            {
                bytes.AddRange(BitConverter.GetBytes(triangle.V1.x));
                bytes.AddRange(BitConverter.GetBytes(triangle.V1.y));
                bytes.AddRange(BitConverter.GetBytes(triangle.V1.z));
                
                bytes.AddRange(BitConverter.GetBytes(triangle.V2.x));
                bytes.AddRange(BitConverter.GetBytes(triangle.V2.y));
                bytes.AddRange(BitConverter.GetBytes(triangle.V2.z));
                
                bytes.AddRange(BitConverter.GetBytes(triangle.V3.x));
                bytes.AddRange(BitConverter.GetBytes(triangle.V3.y));
                bytes.AddRange(BitConverter.GetBytes(triangle.V3.z));
            }

            return bytes.ToArray();
        }

        public static List<Triangle> ToTriangles(byte[] _Bytes, int _StartIndex, int _Count)
        {
            int length = _Count / 36;
            List<Triangle> triangles = new List<Triangle>(length);

            for (int i = _StartIndex; i < length; i++)
            {
                int baseIndex = i * 36;

                triangles.Add(new Triangle(
                    new Vector3(BitConverter.ToSingle(_Bytes, baseIndex), BitConverter.ToSingle(_Bytes, baseIndex + 4), BitConverter.ToSingle(_Bytes, baseIndex + 8)),
                    new Vector3(BitConverter.ToSingle(_Bytes, baseIndex + 12), BitConverter.ToSingle(_Bytes, baseIndex + 16), BitConverter.ToSingle(_Bytes, baseIndex + 20)),
                    new Vector3(BitConverter.ToSingle(_Bytes, baseIndex + 24), BitConverter.ToSingle(_Bytes, baseIndex + 28), BitConverter.ToSingle(_Bytes, baseIndex + 32))
                    ));
            }

            return triangles;
        }

        #endregion
    }
}