using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
[UpdateAfter(typeof(BuildTimeSystem))]
public partial struct SnapPointCheckAvaliableSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((DynamicBuffer<SnapPointsDirectionBuffer> snapPointsDirectionBuffers, EnabledRefRW<IsCheckSnapPoint> isCheckSnapPointEnabled, Entity entity) in SystemAPI.Query<DynamicBuffer<SnapPointsDirectionBuffer>, EnabledRefRW<IsCheckSnapPoint>>().WithEntityAccess())
        {
            if (isCheckSnapPointEnabled.ValueRO == false) continue;
            for (int i = 0; i < snapPointsDirectionBuffers.Length; i++)
            {
                DynamicBuffer<SnapPointBuffer> snapPointBuffers = SystemAPI.GetBuffer<SnapPointBuffer>(snapPointsDirectionBuffers[i].SnapPointsDirectionEntity);
                for (int j = 0; j < snapPointBuffers.Length; j++)
                {
                    LocalToWorld localToWorld = SystemAPI.GetComponent<LocalToWorld>(snapPointBuffers[j].snapPointEntity);
                    CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
                    SnapPointBuffer snapPointBuffer = snapPointBuffers[j];
                    RaycastInput raycastInput = new()
                    {
                        Start = localToWorld.Position,
                        End = localToWorld.Position + localToWorld.Forward * 0.1f,
                        Filter = new CollisionFilter
                        {
                            BelongsTo = ~0u,
                            CollidesWith = 1u << 8,
                            GroupIndex = 0,
                        }
                    };
                    bool check = true;
                    MyCollector.IgnoreEntityCollector ignoreEntityCollector = new(entity, 1.0f, Allocator.Temp);
                    if (collisionWorld.CastRay(raycastInput, ref ignoreEntityCollector))
                    {
                        if (ignoreEntityCollector.Hits.Length > 0)
                        {
                            check = false;
                        }
                    }
                    else
                    {
                        check = true;
                    }
                }
            }
            isCheckSnapPointEnabled.ValueRW = false;
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
