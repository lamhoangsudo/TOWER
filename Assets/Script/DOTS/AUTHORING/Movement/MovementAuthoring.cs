using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class MovementAuthoring : MonoBehaviour
{
    public float moveSpeed;
    public float rotateSpeed;
    public float pitchMin;
    public float pitchMax;
    public class MovementAuthoringBaker : Baker<MovementAuthoring>
    {
        public override void Bake(MovementAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Movement()
            {
                moveSpeed = authoring.moveSpeed,
            });
            AddComponent(entity, new Rotation()
            {
                rotationSpeed = authoring.rotateSpeed,
                pitchMin = authoring.pitchMin,
                pitchMax = authoring.pitchMax,
            });
        }
    }
}
public struct Movement : IComponentData
{
    public float3 moveVector;
    public float moveSpeed;
}
public struct Rotation : IComponentData
{
    public float rotationSpeed;
    public float yaw;
    public float pitch;
    public float pitchMax;
    public float pitchMin;
}


