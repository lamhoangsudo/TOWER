using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BuildingGhostAuthoring : MonoBehaviour
{
    private const string TAG = "SnapPointDirection";
    public ModelBuildingSo buildingSo;
    public class BuildingGhostAuthoringBaker : Baker<BuildingGhostAuthoring>
    {
        public override void Bake(BuildingGhostAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BuildingGhost
            {
                buildingEntity = GetEntity(authoring.buildingSo.buildingPrefab, TransformUsageFlags.Dynamic),
                timeBuildMax = authoring.buildingSo.timeBuildMax,
                timeBuild = authoring.buildingSo.timeBuildMax,
            });
            AddComponent(entity, new IsBuilding
            {

            });
            SetComponentEnabled<IsBuilding>(entity, false);
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
                        directionVector = child.forward,
                    });
                }
            }
        }
    }
}
public struct BuildingGhost : IComponentData
{
    public Entity buildingEntity;
    public float timeBuild;
    public float timeBuildMax;
}
public struct IsBuilding : IComponentData, IEnableableComponent
{
    public Entity buildingEntity;
}

