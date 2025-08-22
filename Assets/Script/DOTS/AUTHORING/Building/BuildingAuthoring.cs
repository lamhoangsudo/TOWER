using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BuildingAuthoring : MonoBehaviour
{
    public ModelBuildingSo buildingSo;
    private const string TAG = "SnapPointDirection";
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
            AddComponent(entity, new BuildingTrackMousePosition
            {
                snapDirection = Enum.Direction.none,
                snapGhostDirection = Enum.Direction.none,
            });
            SetComponentEnabled<BuildingTrackMousePosition>(entity, false);
            AddComponent(entity, new IsCheckSnapPoint
            {

            });
            SetComponentEnabled<IsCheckSnapPoint>(entity, false);
            DynamicBuffer<SnapPointsDirectionBuffer> snapPointsDirectionBuffers = AddBuffer<SnapPointsDirectionBuffer>(entity);
            for (int i = 0; i < authoring.transform.childCount; i++)
            {
                if (authoring.transform.GetChild(i).CompareTag(TAG))
                {
                    Transform child = authoring.transform.GetChild(i);
                    snapPointsDirectionBuffers.Add(new SnapPointsDirectionBuffer
                    {
                        SnapPointsDirectionEntity = GetEntity(child.gameObject, TransformUsageFlags.Dynamic),
                        direction = UtilClass.GetChildDirection(child),
                    });
                }
            }
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
    public Enum.Direction snapGhostDirection;
    public quaternion targetQuaternion;
}
public struct IsCheckSnapPoint : IComponentData, IEnableableComponent
{

}

