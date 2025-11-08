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
    public AnimationCurve slideCurve;
    public AnimationCurve rotationCurve;
    public float sfxPitch;
    public float sfxVolume;
    public GameObject audioGatlingEffect;
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
            using BlobBuilder builder = new(Allocator.Temp);
            ref BarrelAnimatorCurveBlobDatabase rootBarrelAnimatorCurveBlobDatabase = ref builder.ConstructRoot<BarrelAnimatorCurveBlobDatabase>();
            ref PointShotEntityBlobDatabase rootPointShotEntityBlobDatabase = ref builder.ConstructRoot<PointShotEntityBlobDatabase>();
            BlobBuilderArray<float> slideArray = builder.Allocate(ref rootBarrelAnimatorCurveBlobDatabase.slideCurve, sampleCount);
            BlobBuilderArray<float> rotationArray = builder.Allocate(ref rootBarrelAnimatorCurveBlobDatabase.rotationCurve, sampleCount);
            BlobBuilderArray<PointShotEntityBlobData> pointShotArray = builder.Allocate(ref rootPointShotEntityBlobDatabase.pointShotEntityBlobDataArray, authoring.pointShoot.Length);
            for (int i = 0; i < sampleCount; i++)
            {
                slideArray[i] = slideSamples[i];
                rotationArray[i] = rotationSamples[i];
            }
            for (int i = 0; i < authoring.pointShoot.Length; i++)
            {
                pointShotArray[i] = new PointShotEntityBlobData
                {
                    pointShoot = GetEntity(authoring.pointShoot[i], TransformUsageFlags.Dynamic)
                };
            }
            rootBarrelAnimatorCurveBlobDatabase.sampleCount = sampleCount;
            BlobAssetReference<BarrelAnimatorCurveBlobDatabase> barrelAnimatorCurveAsset = builder.CreateBlobAssetReference<BarrelAnimatorCurveBlobDatabase>(Allocator.Persistent);
            BlobAssetReference<PointShotEntityBlobDatabase> pointShotAsset = builder.CreateBlobAssetReference<PointShotEntityBlobDatabase>(Allocator.Persistent);
            AddBlobAsset(ref barrelAnimatorCurveAsset, out var _);
            AddBlobAsset(ref pointShotAsset, out var _);
            AddComponent(entity, new BarrelAnimator
            {
                barrelBaseEntity = GetEntity(authoring.barrelBaseEntity, TransformUsageFlags.Dynamic),
                animationDuration = authoring.animationDuration,
                baseSlideDistance = authoring.baseSlideDistance,
                muzzleFlashEntity = GetEntity(authoring.muzzleFlashEntity, TransformUsageFlags.Dynamic),
                tipSlideAmountDistance = authoring.tipSlideAmountDistance,
                tipRotateDegrees = authoring.tipRotateDegrees,
                curveBlob = barrelAnimatorCurveAsset,
                pointShotBlob = pointShotAsset,
                flashSpawned = false,
                sfxPitch = authoring.sfxPitch,
                sfxVolume = authoring.sfxVolume,
                random = new Unity.Mathematics.Random((uint)entity.Index),
                audioGatlingEffect = GetEntity(authoring.audioGatlingEffect, TransformUsageFlags.Dynamic),
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
        }
    }
}
public struct BarrelAnimator : IComponentData
{
    public Entity barrelBaseEntity;
    public Entity muzzleFlashEntity;
    public Entity audioGatlingEffect;

    public float animationDuration;
    public float baseSlideDistance;
    public float tipSlideAmountDistance;
    public float tipRotateDegrees;
    public float gatlingRotationSpeed;
    public float curentGatlingRotation;
    public float gatlingRotationSpeedChange;
    public float accumulatedGatlingAngle;

    public float lastFireTime;
    public bool animationPlaying;

    public float tipRotationAtFire;

    public BlobAssetReference<BarrelAnimatorCurveBlobDatabase> curveBlob;
    public BlobAssetReference<PointShotEntityBlobDatabase> pointShotBlob;

    public bool flashSpawned;
    public float sfxPitch;
    public float sfxVolume;
    public Unity.Mathematics.Random random;
}
public struct BarrelAnimatorCurveBlobDatabase
{
    public BlobArray<float> slideCurve;
    public BlobArray<float> rotationCurve;
    public int sampleCount;
}
public struct PointShotEntityBlobDatabase
{
    public BlobArray<PointShotEntityBlobData> pointShotEntityBlobDataArray;
}
public struct PointShotEntityBlobData
{
    public Entity pointShoot;
}
[InternalBufferCapacity(10)]
public struct BarrelTipEntityBuffer : IBufferElementData
{
    public Entity barrelTipEntity;
    public float3 tipInitialPosition;
    public float3 tipInitialRotation;
}


