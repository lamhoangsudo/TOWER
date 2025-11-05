using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class EntitySpawnStorageAuthoring : MonoBehaviour
{
    [SerializeField] private ListModelBuildingSo buildingSOs;
    public class EntitySpawnStorageAuthoringBaker : Baker<EntitySpawnStorageAuthoring>
    {
        public override void Bake(EntitySpawnStorageAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            BlobBuilder blobBuilder = new(Allocator.Temp);
            ref BuildingConfigBlobDatabase root = ref blobBuilder.ConstructRoot<BuildingConfigBlobDatabase>();
            BlobBuilderArray<BuildingConfigDataBlob> buildingConfigBlobArray = blobBuilder.Allocate(ref root.buildingConfigArray, authoring.buildingSOs.list.Count);
            for (int i = 0; i < authoring.buildingSOs.list.Count; ++i) 
            {
                buildingConfigBlobArray[i] = new BuildingConfigDataBlob
                {
                    buildingID = authoring.buildingSOs.list[i].buildingID,
                    snapMaxDistance = authoring.buildingSOs.list[i].snapMaxDistance,
                    entityModelBuilding = GetEntity(authoring.buildingSOs.list[i].buildingPrefab, TransformUsageFlags.Dynamic),
                    entityGhostBuilding = GetEntity(authoring.buildingSOs.list[i].buildingGhostPrefab, TransformUsageFlags.Dynamic),
                    snapPointTypeSearch = authoring.buildingSOs.list[i].snapPointTypeSearch,
                    buildingSizeCell = authoring.buildingSOs.list[i].size,
                };
            }
            BlobAssetReference<BuildingConfigBlobDatabase> blobAsset = blobBuilder.CreateBlobAssetReference<BuildingConfigBlobDatabase>(Allocator.Persistent);
            AddBlobAsset(ref blobAsset, out _);
            AddComponent(entity, new EntityBuildSpawnStorage
            {
                assetReference = blobAsset,
            });
        }
        
    }
}
public struct BuildingConfigBlobDatabase
{
    public BlobArray<BuildingConfigDataBlob> buildingConfigArray;
}
public struct BuildingConfigDataBlob
{
    public Entity entityGhostBuilding;
    public Enum.BuildingID buildingID;
    public Entity entityModelBuilding;
    public float snapMaxDistance;
    public Enum.SnapPointType snapPointTypeSearch;
    public int3 buildingSizeCell;
}
public struct EntityBuildSpawnStorage : IComponentData
{
    public BlobAssetReference<BuildingConfigBlobDatabase> assetReference;
}
