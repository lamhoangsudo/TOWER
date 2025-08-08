using Unity.Entities;
using UnityEngine;

public class EntitySpawnStorageAuthoring : MonoBehaviour
{
    [SerializeField] private ListModelBuildingSo buildingSOs;
    public class EntitySpawnStorageAuthoringBaker : Baker<EntitySpawnStorageAuthoring>
    {
        public override void Bake(EntitySpawnStorageAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            DynamicBuffer<BuildingBuffer> buildingBuffers = AddBuffer<BuildingBuffer>(entity);
            for (int i = 0; i < authoring.buildingSOs.list.Count; i++)
            {
                buildingBuffers.Add(new BuildingBuffer
                {
                    buildingID = authoring.buildingSOs.list[i].buildingID,
                    entityModelBuilding = GetEntity(authoring.buildingSOs.list[i].buildingPrefab, TransformUsageFlags.Dynamic),
                    entityModelGhost = GetEntity(authoring.buildingSOs.list[i].buildingGhostPrefab, TransformUsageFlags.Dynamic),
                });
            }
        }
        
    }
}
public struct BuildingBuffer : IBufferElementData
{
    public Entity entityModelGhost;
    public Enum.BuildingID buildingID;
    public Entity entityModelBuilding;
}



