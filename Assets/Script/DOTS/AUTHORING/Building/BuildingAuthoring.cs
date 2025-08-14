using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BuildingAuthoring : MonoBehaviour
{
    public List<SnapPointAuthoring> snapPointsAuthorings;
    public ModelBuildingSo buildingSo;
    public class BuildingAuthoringBaker : Baker<BuildingAuthoring>
    {
        public override void Bake(BuildingAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Building
            {
                buildingID = authoring.buildingSo.buildingID,
                snapMaxDistance = authoring.buildingSo.snapMaxDistance,
            });
            DynamicBuffer<SnapPointBuffer> snapPointBuffers = AddBuffer<SnapPointBuffer>(entity);
            for (int i = 0; i < authoring.snapPointsAuthorings.Count; i++)
            {
                snapPointBuffers.Add(new SnapPointBuffer
                {
                    snapPointEntity = GetEntity(authoring.snapPointsAuthorings[i].gameObject, TransformUsageFlags.Dynamic),
                });
            }
            AddComponent(entity, new BuildingTrackMousePosition
            {

            });
            SetComponentEnabled<BuildingTrackMousePosition>(entity, false);
        }
    }
}
public struct Building : IComponentData
{
    public Enum.BuildingID buildingID;
    public float snapMaxDistance;
}
public struct BuildingTrackMousePosition : IComponentData, IEnableableComponent
{
    public float3 buildPosition;
    public Enum.Direction snapDirection;
    public Enum.Direction SnapGhostDirection;
    public quaternion targetQuaternion;
}

