using System.Collections.Generic;
using System.Drawing;
using Unity.Entities;
using UnityEngine;

public class PlatformAuthoring : MonoBehaviour
{
    public Enum.PlatformSizeType platformSizeType;
    public List<GameObject> snapPoints;
    public class PlatformAuthoringBaker : Baker<PlatformAuthoring>
    {
        public override void Bake(PlatformAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Platform()
            {
                PlatformSizeType = authoring.platformSizeType,
            });
            DynamicBuffer<PlatformSnapPoint> platformSnapPoints = AddBuffer<PlatformSnapPoint>(entity);
            for (int i = 0; i < authoring.snapPoints.Count; i++)
            {
                platformSnapPoints.Add(new PlatformSnapPoint
                {
                    snapPointEntity = GetEntity(authoring.snapPoints[i], TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
public struct Platform : IComponentData
{
    public Enum.PlatformSizeType PlatformSizeType;
}
public struct PlatformSnapPoint : IBufferElementData
{
    public Entity snapPointEntity;
}


