using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ProjecTileAuthoring : MonoBehaviour
{
    public float speed = 50f;
    public float lifetime = 5f;
    public float damage = 10f;
    public Vector3 direction = Vector3.forward;
}

public class ProjecTileAuthoringBaker : Baker<ProjecTileAuthoring>
{
    public override void Bake(ProjecTileAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ProjecTile
        {
            speed = authoring.speed,
            lifetime = authoring.lifetime,
            damage = authoring.damage,
            direction = math.normalize(authoring.direction),
            isHit = false // Initialize as not hit
        });
    }
}
public struct ProjecTile : IComponentData
{
    public float speed;
    public float lifetime;
    public float damage;
    public float3 direction;
    public bool isHit; // Indicates if the projectile has hit something
}
