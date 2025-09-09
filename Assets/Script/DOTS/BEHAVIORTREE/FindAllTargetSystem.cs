using Opsive.BehaviorDesigner.Runtime.Components;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.GraphDesigner.Runtime;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
[UpdateAfter(typeof(EnableBakeBehaviorTreeSystem))]
public struct FindAllTargetSystemNode : ITreeLogicNode, IAuthoringTask, IConditional
{
    // Required ILogicNode properties.
    [field: Tooltip("The index of the node.")]
    [field: SerializeField]
    public ushort Index { get; set; }
    [field: Tooltip("The parent index of the node. ushort.MaxValue indicates no parent.")]
    [field: SerializeField]
    public ushort ParentIndex { get; set; }
    [field: Tooltip("The sibling index of the node. ushort.MaxValue indicates no sibling.")]
    [field: SerializeField]
    public ushort SiblingIndex { get; set; }
    public ushort RuntimeIndex { get; set; }
    public ComponentType Flag => typeof(FindAllTargetSystemNodeTask);
    public Type SystemType => typeof(FindAllTargetSystem);
    public int AddBufferElement(World world, Entity entity, GameObject gameObject)
    {
        DynamicBuffer<FindAllTargetSystemNodeData> buffer;
        if (world.EntityManager.HasBuffer<FindAllTargetSystemNodeData>(entity))
        {
            buffer = world.EntityManager.GetBuffer<FindAllTargetSystemNodeData>(entity);
        }
        else
        {
            buffer = world.EntityManager.AddBuffer<FindAllTargetSystemNodeData>(entity);
        }

        buffer.Add(new FindAllTargetSystemNodeData()
        {
            Index = RuntimeIndex,
        });
        return buffer.Length - 1;
    }

    public void ClearBufferElement(World world, Entity entity)
    {
        DynamicBuffer<FindAllTargetSystemNodeData> buffer;
        if (world.EntityManager.HasBuffer<FindAllTargetSystemNodeData>(entity))
        {
            buffer = world.EntityManager.GetBuffer<FindAllTargetSystemNodeData>(entity);
            buffer.Clear();
        }
    }
}
public struct FindAllTargetSystemNodeData : IBufferElementData
{
    public ushort Index;
}
public struct FindAllTargetSystemNodeTask : IComponentData, IEnableableComponent { }
[DisableAutoCreation]
public partial struct FindAllTargetSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<TaskComponent, FindAllTargetSystemNodeData, RadarRangeRay, LocalTransform, FindAllTargetSystemNodeTask, EvaluateFlag>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        NativeList<DistanceHit> hits = new(Allocator.Temp);
        foreach (
            (DynamicBuffer<TaskComponent> taskComponents,
            DynamicBuffer<FindAllTargetSystemNodeData> findTargetSystemNodeDatas,
            RefRO<RadarRangeRay> radarTurretRangeRay,
            DynamicBuffer<TargetEntityBuffer> targetEntityBuffers,
            RefRO<LocalTransform> localTransform
            )
            in
            SystemAPI.Query<
                DynamicBuffer<TaskComponent>,
                DynamicBuffer<FindAllTargetSystemNodeData>,
                RefRO<RadarRangeRay>,
                DynamicBuffer<TargetEntityBuffer>,
                RefRO<LocalTransform>>()
                .WithAll<FindAllTargetSystemNodeTask, EvaluateFlag>())
        {
            for (int i = 0; i < findTargetSystemNodeDatas.Length; i++)
            {
                TaskComponent taskComponent = taskComponents[findTargetSystemNodeDatas[i].Index];
                if (taskComponent.Status != TaskStatus.Queued) continue;
                collisionWorld.OverlapSphere(
                    localTransform.ValueRO.Position,
                    radarTurretRangeRay.ValueRO.radarScanRange,
                    ref hits,
                    new CollisionFilter
                    {
                        BelongsTo = ~0u,
                        CollidesWith = 1u << 3,
                        GroupIndex = 0,
                    });
                if (hits.Length > 0)
                {
                    targetEntityBuffers.Clear();
                    for (int j = 0; j < hits.Length; j++)
                    {
                        targetEntityBuffers.Add(new TargetEntityBuffer
                        {
                            targetEntity = hits[j].Entity,
                            distance = hits[j].Distance,
                            targetPosition = hits[j].Position,
                        });
                    }
                    taskComponent.Status = TaskStatus.Success;
                }
                else
                {
                    taskComponent.Status = TaskStatus.Failure;
                }
                var __newtaskComponents__ = taskComponents;
                __newtaskComponents__[findTargetSystemNodeDatas[i].Index] = taskComponent;
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}