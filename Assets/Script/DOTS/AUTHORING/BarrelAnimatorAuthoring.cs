using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// ECS data for controlling the barrel animation,
/// including spin and recoil behavior.
/// </summary>
public class BarrelAnimatorAuthoring : MonoBehaviour
{
    [Header("References")]
    [Tooltip("(Optional). The barrel base is the model the barrel tip is attached to. Used for animation")]
    public GameObject barrelBaseObject;
    [Tooltip("Reference to barrel tip Gameobject. Used for animation")]
    public GameObject barrelTipObject;
    [Tooltip("Reference to muzzle flash effect. Is enabled when fire animation starts playing")]
    public GameObject muzzleFlashObject;
    public float rotationSpeed = 300f;
    public float recoilDistance = 0.2f;
    public float recoilSpeed = 5f;
    public class BarrelAnimatorAuthoringBaker : Baker<BarrelAnimatorAuthoring>
    {
        public override void Bake(BarrelAnimatorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new BarrelAnimator
            {
                rotationSpeed = authoring.rotationSpeed,
                recoilDistance = authoring.recoilDistance,
                recoilSpeed = authoring.recoilSpeed,
                initialPosition = authoring.transform.localPosition,
                isFiring = false,
                barrelBaseEntity = GetEntity(authoring.barrelBaseObject, TransformUsageFlags.Dynamic),
                barrelTipEntity = GetEntity(authoring.barrelTipObject, TransformUsageFlags.Dynamic),
                muzzleFlashEntity = GetEntity(authoring.muzzleFlashObject, TransformUsageFlags.Dynamic)
            });
        }
    }
}
public struct BarrelAnimator : IComponentData
{
    public float rotationSpeed;     // barrel rotation speed in degrees per second
    public float recoilDistance;    // how far the barrel recoils when firing
    public float recoilSpeed;       // how fast the barrel returns after recoil
    public float3 initialPosition;  // initial local position of the barrel
    public bool isFiring;           // whether the weapon is currently firing
    public Entity barrelBaseEntity; // reference to the barrel base entity
    public Entity barrelTipEntity;  // reference to the barrel tip entity
    public Entity muzzleFlashEntity;// reference to the muzzle flash entity
}


