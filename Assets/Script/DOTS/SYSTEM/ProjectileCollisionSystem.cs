using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
/// <summary>
/// Handles projectile collision events.
/// When hitting something, the projectile is destroyed,
/// and prints a damage log for testing.
/// </summary>
partial struct ProjectileCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
public partial struct ProjectileTriggerJob
{
    
}