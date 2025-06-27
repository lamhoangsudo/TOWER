using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Physics;
/// <summary>
/// Handles projecTileWrite collision events.
/// When hitting something, the projecTileWrite is destroyed,
/// and prints a damage log for testing.
/// </summary>
public partial struct ProjectileCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // get collision world
        SimulationSingleton simulation = SystemAPI.GetSingleton<SimulationSingleton>();
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        // prepare job
        ProjectileTriggerJob job = new()
        {
            projectileLookup = SystemAPI.GetComponentLookup<ProjecTile>(true),
            ecb = ecb.AsParallelWriter(),
        };

        // schedule
        state.Dependency = job.Schedule(simulation, state.Dependency);
        state.Dependency.Complete();

        // playback ECB
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
public partial struct ProjectileTriggerJob : ITriggerEventsJob
{
    [ReadOnly] public ComponentLookup<ProjecTile> projectileLookup;
    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(TriggerEvent triggerEvent)
    {
        Entity entityA = triggerEvent.EntityA;
        Entity entityB = triggerEvent.EntityB;

        // check which entity is the projecTileWrite
        bool aIsProjectile = projectileLookup.HasComponent(entityA);
        bool bIsProjectile = projectileLookup.HasComponent(entityB);

        // only care if one side is the projecTileWrite
        if (aIsProjectile || bIsProjectile)
        {
            Entity projectileEntity = aIsProjectile ? entityA : entityB;
            Entity targetEntity = aIsProjectile ? entityB : entityA;

            // get projecTileWrite data
            ProjecTile projecTileWrite = projectileLookup[projectileEntity];

            // here you could apply damage to targetEntity's health component
            // for now, we just log it:
            Debug.Log($"Projectile hit {targetEntity} with damage {projecTileWrite.damage}");
            projecTileWrite.isHit = true;
            ecb.SetComponent<ProjecTile>(projectileEntity.Index, projectileEntity, projecTileWrite);
            // destroy the projecTileWrite
            ecb.DestroyEntity(projectileEntity.Index, projectileEntity);
        }
    }
}