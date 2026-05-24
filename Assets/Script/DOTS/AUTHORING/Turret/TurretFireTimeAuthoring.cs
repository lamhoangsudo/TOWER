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

            // Dùng DynamicBuffer — entity references được remap đúng trong mọi trường hợp
            if (authoring.weaponAuthorings != null && authoring.weaponAuthorings.Length > 0)
            {
                DynamicBuffer<WeaponEntityBuffer> buffer = AddBuffer<WeaponEntityBuffer>(entity);
                for (int i = 0; i < authoring.weaponAuthorings.Length; i++)
                {
                    buffer.Add(new WeaponEntityBuffer
                    {
                        weaponEntity = GetEntity(authoring.weaponAuthorings[i], TransformUsageFlags.Dynamic),
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
    public int indexWeapons;
}

[InternalBufferCapacity(4)]
public struct WeaponEntityBuffer : IBufferElementData
{
    public Entity weaponEntity;
}
