using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Gatling barrel tip continuous rotation + SFX.
/// Chỉ chạy trên entities có GatlingSpin component.
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(GatlingWeaponSystem))]
partial struct BarrelGatlingSpinSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<GatlingSpin, BarrelTipEntityBuffer>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        BarrelGatlingSpinJob job = new()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: false),
            sfxGatlingSpinLookup = SystemAPI.GetComponentLookup<SFX_GatlingSpin>(isReadOnly: false),
            ecb = ecb.AsParallelWriter(),
        };
        JobHandle jobHandle = job.ScheduleParallel(state.Dependency);
        jobHandle.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}

[BurstCompile]
public partial struct BarrelGatlingSpinJob : IJobEntity
{
    public float DeltaTime;
    [ReadOnly] public ComponentLookup<LocalTransform> localTransformLookup;
    [ReadOnly] public ComponentLookup<SFX_GatlingSpin> sfxGatlingSpinLookup;
    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(
        [ChunkIndexInQuery] int sortkey,
        ref GatlingSpin gatling,
        DynamicBuffer<BarrelTipEntityBuffer> tipBuffers)
    {
        if (tipBuffers.Length == 0) return;
        if (gatling.gatlingRotationSpeed <= 0f) return;

        // Accumulate rotation angle
        gatling.accumulatedGatlingAngle += gatling.currentGatlingRotation * DeltaTime;

        // Apply rotation to first tip (gatling always has 1 tip that spins)
        Entity tipEntity = tipBuffers[0].barrelTipEntity;
        LocalTransform tipTransform = localTransformLookup[tipEntity];
        tipTransform = tipTransform.WithRotation(
            quaternion.Euler(0f, math.radians(math.fmod(gatling.accumulatedGatlingAngle, 1800f)), 0f));
        ecb.SetComponent(sortkey, tipEntity, tipTransform);

        // Update gatling spin SFX
        if (gatling.audioGatlingEffect != Entity.Null && sfxGatlingSpinLookup.HasComponent(gatling.audioGatlingEffect))
        {
            float rotationFactor = gatling.currentGatlingRotation / gatling.gatlingRotationSpeed;
            SFX_GatlingSpin sfx = sfxGatlingSpinLookup[gatling.audioGatlingEffect];
            sfx.isPlaying = rotationFactor > 0.05f;
            sfx.gatlingRotationFactor = rotationFactor;
            ecb.SetComponent(sortkey, gatling.audioGatlingEffect, sfx);
        }
    }
}
