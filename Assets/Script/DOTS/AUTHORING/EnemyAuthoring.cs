using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class EnemyAuthoring : MonoBehaviour
{
    public bool test;
    public class TargetAuthoringBaker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Enemy
            {
                time = 1f,
                TargetPosition = authoring.transform.position,
                RandomGenerator = new Unity.Mathematics.Random((uint)entity.Index),
                test = authoring.test,
            });
        }
    }
}
public struct Enemy : IComponentData
{
    public float3 TargetPosition;
    public float time;
    public Unity.Mathematics.Random RandomGenerator;
    public bool test;
}


