using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Core barrel animation: base slide + tip slide/rotation.
/// Chạy cho Individual, Simultaneous, MissileLauncher patterns.
/// Gatling tip rotation xử lý bởi BarrelGatlingSpinSystem.
/// </summary>
[BurstCompile]
[UpdateAfter(typeof(WeaponSystem))]
[UpdateAfter(typeof(GatlingWeaponSystem))]
partial struct BarrelSlideAnimSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<BarrelAnimation, Weapon, BarrelTipEntityBuffer, WeaponFireTime>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        BarrelSlideAnimJob job = new()
        {
            ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
            localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: false),
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
public partial struct BarrelSlideAnimJob : IJobEntity
{
    public float ElapsedTime;
    [ReadOnly] public ComponentLookup<LocalTransform> localTransformLookup;
    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(
        [ChunkIndexInQuery] int sortkey,
        ref BarrelAnimation barrelAnimation,
        ref BarrelVFX barrelVFX,
        in Weapon weapon,
        DynamicBuffer<BarrelTipEntityBuffer> tipBuffers,
        in WeaponFireTime weaponFireTime)
    {
        // Gatling animation (tip spin) xử lý bởi BarrelGatlingSpinSystem
        if (weapon.firingPattern == Enum.WeaponFiringPattern.Gatling) return;
        if (!barrelAnimation.animationPlaying) return;

        // Calculate progress + curve values
        float elapsed = ElapsedTime - barrelAnimation.lastFireTime;
        float progress = math.clamp(elapsed / barrelAnimation.animationDuration, 0f, 1f);
        ref BarrelAnimatorCurveBlobDatabase blob = ref barrelAnimation.curveBlob.Value;
        int sampleCount = blob.sampleCount;
        float sampleT = progress * (sampleCount - 1);
        int idx0 = (int)math.floor(sampleT);
        int idx1 = math.min(idx0 + 1, sampleCount - 1);
        float frac = sampleT - idx0;
        float slideValue = math.lerp(blob.slideCurve[idx0], blob.slideCurve[idx1], frac);
        float rotationValue = math.lerp(blob.rotationCurve[idx0], blob.rotationCurve[idx1], frac);

        // Base slide
        if (barrelAnimation.barrelBaseEntity != Entity.Null)
        {
            LocalTransform baseTransform = localTransformLookup[barrelAnimation.barrelBaseEntity];
            baseTransform.Position = new float3(0f, 0f, -slideValue * barrelAnimation.baseSlideDistance);
            ecb.SetComponent(sortkey, barrelAnimation.barrelBaseEntity, baseTransform);
        }

        // Tip slide + rotation
        switch (weapon.firingPattern)
        {
            case Enum.WeaponFiringPattern.Individual:
            case Enum.WeaponFiringPattern.MissileLauncher:
                AnimateSingleTip(sortkey, ref barrelAnimation, tipBuffers, weaponFireTime.barrelTipIndex, slideValue, rotationValue);
                break;

            case Enum.WeaponFiringPattern.Simultaneous:
                for (int i = 0; i < tipBuffers.Length; i++)
                {
                    AnimateSingleTip(sortkey, ref barrelAnimation, tipBuffers, i, slideValue, rotationValue);
                }
                break;
        }

        // End animation
        if (progress >= 1f)
        {
            barrelAnimation.animationPlaying = false;
            barrelVFX.flashSpawned = false; // Reset để lần bắn tiếp có thể trigger effects
        }
    }

    private void AnimateSingleTip(
        int sortkey,
        ref BarrelAnimation barrelAnimation,
        DynamicBuffer<BarrelTipEntityBuffer> tipBuffers,
        int tipIndex,
        float slideValue,
        float rotationValue)
    {
        BarrelTipEntityBuffer tip = tipBuffers[tipIndex];
        LocalTransform tipTransform = localTransformLookup[tip.barrelTipEntity];

        // Cache initial position/rotation on first use
        if (tip.tipInitialPosition.Equals(float3.zero) && tip.tipInitialRotation.Equals(float3.zero))
        {
            tip.tipInitialPosition = tipTransform.Position;
            tip.tipInitialRotation = math.Euler(tipTransform.Rotation);
            tipBuffers.ElementAt(tipIndex) = tip;
        }

        // Tip slide
        float tipY = tip.tipInitialPosition.y + slideValue * barrelAnimation.tipSlideAmountDistance;
        tipTransform.Position = new float3(tip.tipInitialPosition.x, tipY, tip.tipInitialPosition.z);

        // Tip rotation
        if (barrelAnimation.tipRotateDegrees != 0f)
        {
            float tipRotY = math.lerp(
                barrelAnimation.tipRotationAtFire,
                barrelAnimation.tipRotationAtFire + barrelAnimation.tipRotateDegrees,
                rotationValue);
            tipTransform.Rotation = quaternion.Euler(
                math.radians(tip.tipInitialRotation.x),
                math.radians(tipRotY),
                math.radians(tip.tipInitialRotation.z));
        }

        ecb.SetComponent(sortkey, tip.barrelTipEntity, tipTransform);
    }
}
