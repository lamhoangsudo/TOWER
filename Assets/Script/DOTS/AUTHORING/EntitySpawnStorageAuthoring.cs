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
            AddComponent(entity, new EntitySpawnStorageTag());
            DynamicBuffer<BuildingBuffer> buildingBuffers = AddBuffer<BuildingBuffer>(entity);
            for (int i = 0; i < authoring.buildingSOs.list.Count; i++)
            {
                buildingBuffers.Add(new BuildingBuffer
                {
                    buildingID = authoring.buildingSOs.list[i].buildingID,
                    snapMaxDistance = authoring.buildingSOs.list[i].snapMaxDistance,
                    entityModelBuilding = GetEntity(authoring.buildingSOs.list[i].buildingPrefab, TransformUsageFlags.Dynamic),
                    entityGhostBuilding = GetEntity(authoring.buildingSOs.list[i].buildingGhostPrefab, TransformUsageFlags.Dynamic),
                    snapPointTypeSearch = authoring.buildingSOs.list[i].snapPointTypeSearch,
                });
            }
        }
        
    }
}
public struct BuildingBuffer : IBufferElementData
{
    public Entity entityGhostBuilding;
    public Enum.BuildingID buildingID;
    public Entity entityModelBuilding;
    public float snapMaxDistance;
    public Enum.SnapPointType snapPointTypeSearch;
}
public struct EntitySpawnStorageTag : IComponentData
{
    
}



