using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BarrelAnimatorAuthoring : MonoBehaviour
{
    public GameObject barrelBaseEntity;
    public GameObject barrelTipEntity;
    public GameObject muzzleFlashEntity;
    public GameObject pointShoot;
    public float animationDuration;
    public float baseSlideDistance;
    public float tipSlideAmountDistance;
    public float tipRotateDegrees;

    public float lastFireTime;
    public bool animationPlaying;

    public float3 tipInitialPosition;
    public float3 tipInitialRotation;
    public float tipRotationAtFire;

    public AnimationCurve slideCurve;
    public AnimationCurve rotationCurve;
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
                barrelTipEntity = GetEntity(authoring.barrelTipEntity, TransformUsageFlags.Dynamic),
                muzzleFlashEntity = GetEntity(authoring.muzzleFlashEntity, TransformUsageFlags.Dynamic),
                animationDuration = authoring.animationDuration,
                baseSlideDistance = authoring.baseSlideDistance,
                tipSlideAmountDistance = authoring.tipSlideAmountDistance,
                tipRotateDegrees = authoring.tipRotateDegrees,
                lastFireTime = authoring.lastFireTime,
                animationPlaying = authoring.animationPlaying,
                tipInitialPosition = authoring.tipInitialPosition,
                tipInitialRotation = authoring.tipInitialRotation,
                tipRotationAtFire = authoring.tipRotationAtFire,
                pointShootPosition = GetEntity(authoring.pointShoot, TransformUsageFlags.Dynamic),
                curveBlob = blobAsset
            });
        }
    }
}
public struct BarrelAnimator : IComponentData
{
    public Entity barrelBaseEntity;
    public Entity barrelTipEntity;
    public Entity muzzleFlashEntity;

    public float animationDuration;
    public float baseSlideDistance;
    public float tipSlideAmountDistance;
    public float tipRotateDegrees;

    public float lastFireTime;
    public bool animationPlaying;

    public float3 tipInitialPosition;
    public float3 tipInitialRotation;
    public float tipRotationAtFire;

    public Entity pointShootPosition;

    public BlobAssetReference<BarrelAnimatorCurveBlob> curveBlob;
}
public struct BarrelAnimatorCurveBlob
{
    public BlobArray<float> slideCurve;
    public BlobArray<float> rotationCurve;
    public int sampleCount;
}


