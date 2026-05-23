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
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            // === Bake AnimationCurve → BlobAsset ===
            const int sampleCount = 50;
            float[] slideSamples = new float[sampleCount];
            float[] rotationSamples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);
                slideSamples[i] = authoring.slideCurve != null ? authoring.slideCurve.Evaluate(t) : 0f;
                rotationSamples[i] = authoring.rotationCurve != null ? authoring.rotationCurve.Evaluate(t) : 0f;
            }

            // Curve BlobAsset
            BlobBuilder curveBuilder = new(Allocator.Temp);
            ref BarrelAnimatorCurveBlobDatabase curveRoot = ref curveBuilder.ConstructRoot<BarrelAnimatorCurveBlobDatabase>();
            BlobBuilderArray<float> slideArray = curveBuilder.Allocate(ref curveRoot.slideCurve, sampleCount);
            BlobBuilderArray<float> rotationArray = curveBuilder.Allocate(ref curveRoot.rotationCurve, sampleCount);
            for (int i = 0; i < sampleCount; i++)
            {
                slideArray[i] = slideSamples[i];
                rotationArray[i] = rotationSamples[i];
            }
            curveRoot.sampleCount = sampleCount;
            BlobAssetReference<BarrelAnimatorCurveBlobDatabase> curveAsset = curveBuilder.CreateBlobAssetReference<BarrelAnimatorCurveBlobDatabase>(Allocator.Persistent);
            AddBlobAsset(ref curveAsset, out var _);
            curveBuilder.Dispose();

            // PointShot BlobAsset
            BlobBuilder pointShotBuilder = new(Allocator.Temp);
            ref PointShotEntityBlobDatabase pointShotRoot = ref pointShotBuilder.ConstructRoot<PointShotEntityBlobDatabase>();
            BlobBuilderArray<PointShotEntityBlobData> pointShotArray = pointShotBuilder.Allocate(ref pointShotRoot.pointShotEntityBlobDataArray, authoring.pointShoot.Length);
            for (int i = 0; i < authoring.pointShoot.Length; i++)
            {
                pointShotArray[i] = new PointShotEntityBlobData
                {
                    pointShoot = GetEntity(authoring.pointShoot[i], TransformUsageFlags.Dynamic)
                };
            }
            BlobAssetReference<PointShotEntityBlobDatabase> pointShotAsset = pointShotBuilder.CreateBlobAssetReference<PointShotEntityBlobDatabase>(Allocator.Persistent);
            AddBlobAsset(ref pointShotAsset, out var _);
            pointShotBuilder.Dispose();

            // === Component 1: BarrelAnimation (core animation) ===
            AddComponent(entity, new BarrelAnimation
            {
                barrelBaseEntity = GetEntity(authoring.barrelBaseEntity, TransformUsageFlags.Dynamic),
                animationDuration = authoring.animationDuration,
                baseSlideDistance = authoring.baseSlideDistance,
                tipSlideAmountDistance = authoring.tipSlideAmountDistance,
                tipRotateDegrees = authoring.tipRotateDegrees,
                lastFireTime = 0f,
                animationPlaying = false,
                tipRotationAtFire = 0f,
                curveBlob = curveAsset,
            });

            // === Component 2: BarrelVFX (muzzle flash + point shots) ===
            AddComponent(entity, new BarrelVFX
            {
                muzzleFlashEntity = GetEntity(authoring.muzzleFlashEntity, TransformUsageFlags.Dynamic),
                flashSpawned = false,
                pointShotBlob = pointShotAsset,
            });

            // === Component 3: BarrelSFX (sound) ===
            AddComponent(entity, new BarrelSFX
            {
                sfxPitch = authoring.sfxPitch,
                sfxVolume = authoring.sfxVolume,
                random = new Unity.Mathematics.Random((uint)entity.Index),
            });

            // === Component 4: GatlingSpin (optional — chỉ khi có audioGatlingEffect) ===
            if (authoring.audioGatlingEffect != null)
            {
                AddComponent(entity, new GatlingSpin
                {
                    gatlingRotationSpeed = 0f,
                    currentGatlingRotation = 0f,
                    gatlingRotationSpeedChange = 0f,
                    accumulatedGatlingAngle = 0f,
                    audioGatlingEffect = GetEntity(authoring.audioGatlingEffect, TransformUsageFlags.Dynamic),
                });
            }

            // === Buffer: BarrelTipEntityBuffer ===
            DynamicBuffer<BarrelTipEntityBuffer> buffer = AddBuffer<BarrelTipEntityBuffer>(entity);
            int tipCount = authoring.barrelTipEntity.Length;
            for (int i = 0; i < tipCount; i++)
            {
                Entity barrelTipEntity = GetEntity(authoring.barrelTipEntity[i], TransformUsageFlags.Dynamic);
                buffer.Add(new BarrelTipEntityBuffer
                {
                    barrelTipEntity = barrelTipEntity,
                    tipInitialPosition = float3.zero,
                    tipInitialRotation = float3.zero
                });
            }
        }
    }
}

// === Shared structs (giữ nguyên vì nhiều systems dùng) ===

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
