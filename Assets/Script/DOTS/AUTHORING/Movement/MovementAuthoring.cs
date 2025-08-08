using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class MovementAuthoring : MonoBehaviour
{
    public float moveSpeed;
    public class MovementAuthoringBaker : Baker<MovementAuthoring>
    {
        public override void Bake(MovementAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Movement()
            {
                moveSpeed = authoring.moveSpeed,
            });
        }
    }
}
public struct Movement : IComponentData
{
    public float2 moveVector;
    public float moveSpeed;
}


