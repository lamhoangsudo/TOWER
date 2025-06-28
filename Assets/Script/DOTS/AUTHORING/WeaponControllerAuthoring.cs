using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class WeaponControllerAuthoring : MonoBehaviour
{
    [Tooltip("Shots per second")]
    public float fireRate = 5f;
    [Tooltip("Projectile prefab")]
    public GameObject projectilePrefab;
    [Tooltip("References to the loaded missiles in the launcher. This is so that the script knows to hide them, how many there are etc")]
    public List<GameObject> loadedMissilesList;
    [Tooltip("References to the barrels in the launcher. This is so that the script knows how many there are, and which ones to fire from")]
    public List<GameObject> barrelsList;
    public class WeaponControllerAuthoringBaker : Baker<WeaponControllerAuthoring>
    {

        public override void Bake(WeaponControllerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new WeaponController
            {
                fireRate = authoring.fireRate,
                cooldownTimer = 0f,
                projectilePrefab = GetEntity(authoring.projectilePrefab, TransformUsageFlags.Dynamic),
                isFiring = false,
            });
            DynamicBuffer<BarrelEntity> barrelEntities = AddBuffer<BarrelEntity>(entity);
            foreach (GameObject barrel in authoring.barrelsList)
            {
                barrelEntities.Add(new BarrelEntity
                {
                    barrelEntity = GetEntity(barrel, TransformUsageFlags.Dynamic)
                });
            }
            DynamicBuffer<LoadedMissileEntity> loadedMissileEntities = AddBuffer<LoadedMissileEntity>(entity);
            foreach (GameObject loadedMissile in authoring.loadedMissilesList)
            {
                loadedMissileEntities.Add(new LoadedMissileEntity
                {
                    loadedMissileEntity = GetEntity(loadedMissile, TransformUsageFlags.Dynamic)
                });
            }
        }
    }

}
public struct WeaponController : IComponentData
{
    public float fireRate;                                  // shots per second
    public float cooldownTimer;                             // internal timer
    public Entity projectilePrefab;                         // projectile entity prefab
    public bool isFiring;                                   // flag if the weapon is currently firing
}
[InternalBufferCapacity(8)]
public struct BarrelEntity : IBufferElementData
{
    public Entity barrelEntity; // entity reference to the barrel
}
[InternalBufferCapacity(8)]
public struct LoadedMissileEntity : IBufferElementData
{
    public Entity loadedMissileEntity; // entity reference to the loaded missile
}

