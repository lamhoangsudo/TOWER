using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Nav3D.Obstacles.Serialization
{
    public class ObstacleDataSerializable
    {
        #region Properties

        /// <summary>
        /// Characteristics of the structure of processed transforms. Contains position, rotation, 
        /// scale, as well as the name of the game object and the type of entity being processed.
        /// </summary>
        public string[] ImprintData { get; set; }
        public float MinBucketSize { get; set; }
        public ObstacleInfoBaseSerializable[] ObstacleInfos { get; set; }
        public OctreeSerializable[] Octrees { get; set; }

        #endregion

        #region Constructors

        public ObstacleDataSerializable(Dictionary<ObstacleInfoBase, Obstacle> _ObstacleDatas, string[] _ImprintData, ObstacleSerializingProgress _Progress)
        {
            ImprintData = _ImprintData;
            List<ObstacleInfoBaseSerializable> createdObstacleInfos = new List<ObstacleInfoBaseSerializable>();
            List<OctreeSerializable> createdOctrees = new List<OctreeSerializable>();

            int id = 0;

            foreach (KeyValuePair<ObstacleInfoBase, Obstacle> obstacleData in _ObstacleDatas)
            {
                if (_Progress.CancellationToken.IsCancellationRequested)
                    return;

                ObstacleInfoBase obstacleInfo = obstacleData.Key;

                int obstacleInfoID = id;
                id++;

                if (obstacleInfo is ObstacleInfoSingle obstacleInfoSingle)
                {
                    createdObstacleInfos.Add(obstacleInfoSingle.GetSerializableInstance(obstacleInfoID));
                }
                //here wee need to serialize single obstacles infos, that composes our obstacle's group info and then we serialize group info instance
                else if (obstacleInfo is ObstacleInfoGrouped obstacleInfoGrouped)
                {
                    List<int> singleInfosIDs = new List<int>();

                    foreach (KeyValuePair<int, List<ObstacleInfoSingle>> kvp in obstacleInfoGrouped.ObstacleInfos)
                    {
                        if (!kvp.Value.Any())
                            continue;

                        foreach (ObstacleInfoSingle singleInfo in kvp.Value)
                        {
                            singleInfosIDs.Add(id);
                            createdObstacleInfos.Add(singleInfo.GetSerializableInstance(id));
                            id++;
                        }
                    }

                    createdObstacleInfos.Add(obstacleInfoGrouped.GetSerializableInstance(singleInfosIDs.ToArray(), obstacleInfoID));
                }

                if (_Progress.CancellationToken.IsCancellationRequested)
                    return;

                createdOctrees.Add(obstacleData.Value.GetSerializableOctreeInstance(_Progress, obstacleInfoID));
            }

            ObstacleInfos = createdObstacleInfos.ToArray();
            Octrees = createdOctrees.ToArray();
        }

        public ObstacleDataSerializable(Stream _Stream, ObstacleDeserializingProgress _Progress)
        {
            _Stream.Position = 0;

            using BinaryReader reader = new BinaryReader(_Stream);

            int imprintLength = reader.ReadInt32();
            ImprintData = new string[imprintLength];

            for (int i = 0; i < imprintLength; i++)
            {
                ImprintData[i] = reader.ReadString();
            }

            MinBucketSize = reader.ReadSingle();

            int obstacleInfosCount = reader.ReadInt32();
            ObstacleInfos = new ObstacleInfoBaseSerializable[obstacleInfosCount];

            for (int i = 0; i < obstacleInfosCount; i++)
            {
                ObstacleInfos[i] = ObstacleInfoBaseSerializable.ReadFromBytes(reader);
                _Progress.SetProgress(i / (float)obstacleInfosCount);
            }

            int octreesCount = reader.ReadInt32();
            Octrees = new OctreeSerializable[octreesCount];

            for (int i = 0; i < octreesCount; i++)
            {
                Octrees[i] = new OctreeSerializable(reader, _Progress);
            }
        }

        #endregion

        #region Public methods

        public void WriteIntoStream(Stream _Stream)
        {
            _Stream.Position = 0;

            using BinaryWriter writer = new BinaryWriter(_Stream, System.Text.Encoding.UTF8, true);

            writer.Write(ImprintData.Length);

            for (int i = 0; i < ImprintData.Length; i++)
                writer.Write(ImprintData[i]);

            writer.Write(MinBucketSize);
            writer.Write(ObstacleInfos.Length);

            foreach (ObstacleInfoBaseSerializable obstacleInfo in ObstacleInfos)
            {
                obstacleInfo.WriteIntoBinary(writer);
            }

            writer.Write(Octrees.Length);

            foreach (OctreeSerializable octree in Octrees)
            {
                octree.WriteIntoBinary(writer);
            }
        }

        public Dictionary<ObstacleInfoBase, Obstacle> Unpack(ObstacleDeserializingProgress _Progress)
        {
            Dictionary<int, ObstacleInfoBaseSerializable> inputInfos = new Dictionary<int, ObstacleInfoBaseSerializable>(ObstacleInfos.Length);

            foreach (ObstacleInfoBaseSerializable info in ObstacleInfos)
                inputInfos.Add(info.ID, info);

            Dictionary<int, ObstacleInfoBase> infos = new Dictionary<int, ObstacleInfoBase>();

            //at first get single infos
            foreach (KeyValuePair<int, ObstacleInfoBaseSerializable> info in inputInfos)
            {
                if (!(info.Value is ObstacleInfoSingleSerializable obstacleInfoSingle))
                    continue;

                infos.Add(info.Key, obstacleInfoSingle.GetDeserializedInstance());

                _Progress.SetProgress(infos.Count / (float)inputInfos.Count);
            }

            //remove single infos from inputInfos
            foreach (int id in infos.Keys)
                inputInfos.Remove(id);

            int counter = 0;

            //then get remaining grouped infos, consisting of single ones
            foreach (KeyValuePair<int, ObstacleInfoBaseSerializable> info in inputInfos)
            {
                ObstacleInfoGroupedSerializable obstacleInfoGrouped = info.Value as ObstacleInfoGroupedSerializable;

                ObstacleInfoGrouped obstacleInfo = obstacleInfoGrouped.GetDeserializedInstance(infos);

                infos.Add(info.Key, obstacleInfo);

                _Progress.SetProgress(counter / (float)inputInfos.Count);

                counter++;
            }

            //for now we unpacked all serialized single and grouped infos
            //and we must get only those of them which IDs contains in ObstacleDatas keys

            Dictionary<ObstacleInfoBase, Obstacle> result = new Dictionary<ObstacleInfoBase, Obstacle>(Octrees.Length);

            foreach (OctreeSerializable octreeSerialazable in Octrees)
            {
                ObstacleInfoBase obstacleInfo = infos[octreeSerialazable.ID];

                Octree octree = octreeSerialazable.GetDeserializedInstance(obstacleInfo, _Progress);
                Obstacle obstacle = new Obstacle(obstacleInfo, octree, MinBucketSize);

                result.Add(obstacleInfo, obstacle);
            }

            return result;
        }

        #endregion
    }
}