using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TargetAuthoring : MonoBehaviour
{
    public bool test;
    public class TargetAuthoringBaker : Baker<TargetAuthoring>
    {
        public override void Bake(TargetAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Target
            {
                time = 1f,
                TargetPosition = authoring.transform.position,
                RandomGenerator = new Unity.Mathematics.Random((uint)entity.Index),
                test = authoring.test,
            });
        }
    }
}
public struct Target : IComponentData
{
    public float3 TargetPosition;
    public float time;
    public Unity.Mathematics.Random RandomGenerator;
    public bool test;
}


