using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class TurretFireTimeAuthoring : MonoBehaviour
{
    public Enum.TurretFiringPattern firingPattern;
    public int burstCountMax;
    public float burstDelayMax;
    public float cooldownMax;
    public WeaponAuthoring[] weaponAuthorings;

    public class TurretFireTimeAuthoringBaker : Baker<TurretFireTimeAuthoring>
    {
        public override void Bake(TurretFireTimeAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new TurretFireTime
            {
                firingPattern = authoring.firingPattern,
                burstCountMax = authoring.burstCountMax * authoring.weaponAuthorings.Length,
                burstDelayMax = authoring.burstDelayMax,
                cooldownMax = authoring.cooldownMax,
                indexWeapons = 0,
            });

            if (authoring.weaponAuthorings != null && authoring.weaponAuthorings.Length > 0)
            {
                BlobBuilder builder = new(Allocator.Temp);
                ref WeaponBlobDatabase root = ref builder.ConstructRoot<WeaponBlobDatabase>();
                BlobBuilderArray<WeaponBlobData> blobBuilderArray = builder.Allocate(ref root.weapons, authoring.weaponAuthorings.Length);
                for (int i = 0; i < blobBuilderArray.Length; i++)
                {
                    blobBuilderArray[i] = new WeaponBlobData
                    {
                        weapon = GetEntity(authoring.weaponAuthorings[i], TransformUsageFlags.Dynamic),
                    };
                }
                BlobAssetReference<WeaponBlobDatabase> blobAsset = builder.CreateBlobAssetReference<WeaponBlobDatabase>(Allocator.Persistent);
                AddBlobAsset(ref blobAsset, out var hash);
                builder.Dispose();

                AddComponent(entity, new Weapons
                {
                    weaponBlobReference = blobAsset,
                });
            }
        }
    }
}

/// <summary>
/// Turret-level firing schedule: cooldown, burst pattern, weapon index cycling.
/// Gắn trên turret entity. TurretFireSystem đọc component này.
/// </summary>
public struct TurretFireTime : IComponentData
{
    public Enum.TurretFiringPattern firingPattern;
    public int burstCountMax;
    public int burstCount;
    public float burstDelayMax;
    public float burstDelay;
    public float cooldownMax;
    public float cooldown;
    public int indexWeapons;
}

/// <summary>
/// Chứa BlobAsset reference đến danh sách weapon entities của turret.
/// </summary>
public struct Weapons : IComponentData
{
    public BlobAssetReference<WeaponBlobDatabase> weaponBlobReference;
}

public struct WeaponBlobDatabase
{
    public BlobArray<WeaponBlobData> weapons;
}

public struct WeaponBlobData
{
    public Entity weapon;
}
