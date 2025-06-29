using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TargetAuthoring : MonoBehaviour
{
    public class TargetAuthoringBaker : Baker<TargetAuthoring>
    {
        public override void Bake(TargetAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Target
            {
                time = 10f,
                RandomGenerator = new Unity.Mathematics.Random((uint)entity.Index),
            });
        }
    }
}
public struct Target : IComponentData
{
    public float3 TargetPosition;
    public float time;
    public Unity.Mathematics.Random RandomGenerator;
}


