using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BarrelAnimatorAuthoring : MonoBehaviour
{
    public GameObject barrelBaseEntity;
    public GameObject[] barrelTipEntity;
    public GameObject muzzleFlashEntity;
    public GameObject[] pointShoot;
    public float animationDuration;
    public float baseSlideDistance;
    public float tipSlideAmountDistance;
    public float tipRotateDegrees;

    public float lastFireTime;
    public bool animationPlaying;

    public float tipRotationAtFire;

    public AnimationCurve slideCurve;
    public AnimationCurve rotationCurve;
    public float sfxPitch;
    public float sfxVolume;
    public class BarrelAnimatorAuthoringBaker : Baker<BarrelAnimatorAuthoring>
    {
        public override void Bake(BarrelAnimatorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // Sample AnimationCurve to array
            const int sampleCount = 50;
            float[] slideSamples = new float[sampleCount];
            float[] rotationSamples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);
                slideSamples[i] = authoring.slideCurve != null ? authoring.slideCurve.Evaluate(t) : 0f;
                rotationSamples[i] = authoring.rotationCurve != null ? authoring.rotationCurve.Evaluate(t) : 0f;
            }

            // Build blob asset
            using BlobBuilder builder = new(Allocator.Temp);
            ref BarrelAnimatorCurveBlob root = ref builder.ConstructRoot<BarrelAnimatorCurveBlob>();
            BlobBuilderArray<float> slideArray = builder.Allocate(ref root.slideCurve, sampleCount);
            BlobBuilderArray<float> rotationArray = builder.Allocate(ref root.rotationCurve, sampleCount);
            for (int i = 0; i < sampleCount; i++)
            {
                slideArray[i] = slideSamples[i];
                rotationArray[i] = rotationSamples[i];
            }
            root.sampleCount = sampleCount;

            BlobAssetReference<BarrelAnimatorCurveBlob> blobAsset = builder.CreateBlobAssetReference<BarrelAnimatorCurveBlob>(Allocator.Persistent);
            AddBlobAsset(ref blobAsset, out var hash);
            AddComponent(entity, new BarrelAnimator
            {
                barrelBaseEntity = GetEntity(authoring.barrelBaseEntity, TransformUsageFlags.Dynamic),
                muzzleFlashEntity = GetEntity(authoring.muzzleFlashEntity, TransformUsageFlags.Dynamic),
                animationDuration = authoring.animationDuration,
                baseSlideDistance = authoring.baseSlideDistance,
                tipSlideAmountDistance = authoring.tipSlideAmountDistance,
                tipRotateDegrees = authoring.tipRotateDegrees,
                lastFireTime = authoring.lastFireTime,
                animationPlaying = authoring.animationPlaying,
                tipRotationAtFire = authoring.tipRotationAtFire,
                curveBlob = blobAsset,
                flashSpawned = false,
                sfxPitch = authoring.sfxPitch,
                sfxVolume = authoring.sfxVolume,
                random = new Unity.Mathematics.Random((uint)entity.Index)
            });
            DynamicBuffer<BarrelTipEntityBuffer> buffer = AddBuffer<BarrelTipEntityBuffer>(entity);
            int tipCount = authoring.barrelTipEntity.Length;
            for (int i = 0; i < tipCount; i++)
            {
                Entity barrelTipEntity = GetEntity(authoring.barrelTipEntity[i], TransformUsageFlags.Dynamic);
                buffer.Add(new BarrelTipEntityBuffer { 
                    barrelTipEntity = barrelTipEntity,
                    tipInitialPosition = float3.zero,
                    tipInitialRotation = float3.zero
                });
            }
            DynamicBuffer<PointShotEntityBuffer> pointShootBuffer = AddBuffer<PointShotEntityBuffer>(entity);
            int pointShootCount = authoring.pointShoot.Length;
            for (int i = 0; i < pointShootCount; i++)
            {
                Entity pointShootEntity = GetEntity(authoring.pointShoot[i], TransformUsageFlags.Dynamic);
                pointShootBuffer.Add(new PointShotEntityBuffer { pointShoot = pointShootEntity });
            }
        }
    }
}
public struct BarrelAnimator : IComponentData
{
    public Entity barrelBaseEntity;
    public Entity muzzleFlashEntity;

    public float animationDuration;
    public float baseSlideDistance;
    public float tipSlideAmountDistance;
    public float tipRotateDegrees;

    public float lastFireTime;
    public bool animationPlaying;

    public float tipRotationAtFire;

    public BlobAssetReference<BarrelAnimatorCurveBlob> curveBlob;

    public bool flashSpawned;
    public float sfxPitch;
    public float sfxVolume;
    public Unity.Mathematics.Random random;
    public int barrelTipIndex;
    public int pointShootIndex;
}
public struct BarrelAnimatorCurveBlob
{
    public BlobArray<float> slideCurve;
    public BlobArray<float> rotationCurve;
    public int sampleCount;
}
[InternalBufferCapacity(10)]
public struct BarrelTipEntityBuffer : IBufferElementData
{
    public Entity barrelTipEntity;
    public float3 tipInitialPosition;
    public float3 tipInitialRotation;
}
[InternalBufferCapacity(10)]
public struct PointShotEntityBuffer : IBufferElementData
{
    public Entity pointShoot;
}


