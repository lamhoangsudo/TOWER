using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

partial struct WeaponControllerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (
            var (weapon, entity) in SystemAPI.Query<RefRW<WeaponController>>().WithEntityAccess()
        )
        {
            // cooldown ticking
            weapon.ValueRW.cooldownTimer -= deltaTime;

            if (!weapon.ValueRO.isFiring || weapon.ValueRO.cooldownTimer > 0f)
            {
                continue;
            }

            // fire!
            weapon.ValueRW.cooldownTimer = 1f / weapon.ValueRO.fireRate;

            // get barrel buffer (fire from first barrel for now)
            DynamicBuffer<BarrelEntity> barrelBuffer = SystemAPI.GetBuffer<BarrelEntity>(entity);

            if (barrelBuffer.Length > 0)
            {
                Entity barrel = barrelBuffer[0].barrelEntity;

                if (SystemAPI.Exists(barrel) && SystemAPI.HasComponent<LocalTransform>(barrel))
                {
                    LocalTransform barrelTransform = SystemAPI.GetComponentRO<LocalTransform>(barrel).ValueRO;

                    Entity newProjectile = state.EntityManager.Instantiate(weapon.ValueRO.projectilePrefab);

                    state.EntityManager.SetComponentData(newProjectile, new LocalTransform
                    {
                        Position = barrelTransform.Position,
                        Rotation = barrelTransform.Rotation,
                        Scale = 1f
                    });
                }
            }

            // TODO v2: handle loadedMissilesEntity buffer
            //   - hide missile mesh when fired
            //   - activate/deactivate racks
            //   - missile prefab reuse
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
