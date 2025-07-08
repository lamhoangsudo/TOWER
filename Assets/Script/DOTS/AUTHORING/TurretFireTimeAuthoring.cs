using Unity.Entities;
using Unity.VisualScripting;
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
                burstCountMax = authoring.burstCountMax * authoring.weaponAuthorings.Length,
                burstDelayMax = authoring.burstDelayMax,
                cooldownMax = authoring.cooldownMax,
            });
            if (authoring.weaponAuthorings.Length > 0 && !authoring.weaponAuthorings.IsUnityNull())
            {
                DynamicBuffer<WeaponBuffer> weaponBuffers = AddBuffer<WeaponBuffer>(entity);
                foreach (WeaponAuthoring weaponAuthoring in authoring.weaponAuthorings)
                {
                    weaponBuffers.Add(new WeaponBuffer
                    {
                        weaponBuffer = GetEntity(weaponAuthoring.gameObject, TransformUsageFlags.Dynamic)
                    });
                }
            }
        }
    }
}
public struct TurretFireTime : IComponentData
{
    public Enum.TurretFiringPattern firingPattern;
    public int burstCountMax;
    public int burstCount;
    public float burstDelayMax;
    public float burstDelay;
    public float cooldownMax;
    public float cooldown;
}
[InternalBufferCapacity(6)] 
public struct WeaponBuffer : IBufferElementData
{
    public Entity weaponBuffer;
}


