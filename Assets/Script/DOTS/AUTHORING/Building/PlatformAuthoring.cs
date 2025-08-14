using System.Collections.Generic;
using System.Drawing;
using Unity.Entities;
using UnityEngine;

public class PlatformAuthoring : MonoBehaviour
{
    public Enum.PlatformSizeType platformSizeType;
    public class PlatformAuthoringBaker : Baker<PlatformAuthoring>
    {
        public override void Bake(PlatformAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Platform()
            {
                PlatformSizeType = authoring.platformSizeType,
            });
        }
    }
}
public struct Platform : IComponentData
{
    public Enum.PlatformSizeType PlatformSizeType;
}


