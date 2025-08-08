using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class RadarRangeRayAuthoring : MonoBehaviour
{
    public float radarScanTimeMax;
    public float radarScanRange;
    public LayerMask enemyLayer;
    public class RadarRangeRayAuthoringBaker : Baker<RadarRangeRayAuthoring>
    {
        public override void Bake(RadarRangeRayAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new RadarRangeRay
            {
                radarScanRange = authoring.radarScanRange,
                radarScanTimeMax = authoring.radarScanTimeMax,
                enemyLayer = (uint)authoring.enemyLayer.value,
            });
            AddBuffer<TargetEntityBuffer>(entity);
        }
    }
}
public struct RadarRangeRay : IComponentData
{
    public float radarScanTimeMax;
    public float radarScanRange;
    public uint enemyLayer;
}
public struct TargetEntityBuffer : IBufferElementData
{
    public Entity targetEntity;
    public float distance;
    public float3 targetPosition;
}


