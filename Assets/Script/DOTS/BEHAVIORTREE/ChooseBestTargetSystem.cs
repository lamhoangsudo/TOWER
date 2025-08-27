using Opsive.BehaviorDesigner.Runtime.Components;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.GraphDesigner.Runtime;
using System;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;
public struct ChooseBestTargetSystemNode : ILogicNode, ITaskComponentData, IConditional
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

    public ComponentType Tag => typeof(ChooseBestTargetNodeTask);
    public Type SystemType => typeof(ChooseBestTargetSystem);
    public int AddBufferElement(World world, Entity entity, GameObject gameObject)
    {
        DynamicBuffer<ChooseBestTargetNodeData> buffer;
        if (world.EntityManager.HasBuffer<ChooseBestTargetNodeData>(entity))
        {
            buffer = world.EntityManager.GetBuffer<ChooseBestTargetNodeData>(entity);
        }
        else
        {
            buffer = world.EntityManager.AddBuffer<ChooseBestTargetNodeData>(entity);
        }

        buffer.Add(new ChooseBestTargetNodeData()
        {
            Index = RuntimeIndex,
        });
        return buffer.Length - 1;
    }

    public void ClearBufferElement(World world, Entity entity)
    {
        DynamicBuffer<ChooseBestTargetNodeData> buffer;
        if (world.EntityManager.HasBuffer<ChooseBestTargetNodeData>(entity))
        {
            buffer = world.EntityManager.GetBuffer<ChooseBestTargetNodeData>(entity);
            buffer.Clear();
        }
    }
}
public struct ChooseBestTargetNodeData : IBufferElementData
{
    public ushort Index;
}
public struct ChooseBestTargetNodeTask : IComponentData, IEnableableComponent { }
public partial struct ChooseBestTargetSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<TaskComponent, ChooseBestTargetNodeData, TargetEntityBuffer, Turret, ChooseBestTargetNodeTask>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
            DynamicBuffer<TaskComponent> taskComponents,
            DynamicBuffer<ChooseBestTargetNodeData> chooseBestTargetNodeDatas,
            DynamicBuffer<TargetEntityBuffer> targetEntityBuffers,
            RefRW<Turret> turret
            )
            in
            SystemAPI.Query<
                DynamicBuffer<TaskComponent>,
                DynamicBuffer<ChooseBestTargetNodeData>,
                DynamicBuffer<TargetEntityBuffer>,
                RefRW<Turret>>()
                .WithAll<ChooseBestTargetNodeTask>())
        {
            for (int i = 0; i < chooseBestTargetNodeDatas.Length; i++)
            {
                TaskComponent taskComponent = taskComponents[chooseBestTargetNodeDatas[i].Index];
                if (taskComponent.Status != TaskStatus.Queued) continue;
                if (targetEntityBuffers.IsEmpty)
                {
                    taskComponent.Status = TaskStatus.Failure;
                    var __newtaskComponents1__ = taskComponents;
                    __newtaskComponents1__[chooseBestTargetNodeDatas[i].Index] = taskComponent;
                    continue;
                }
                TargetEntityBuffer closetTarget = targetEntityBuffers[0];
                for (int j = 1; j < targetEntityBuffers.Length; j++)
                {
                    if(closetTarget.distance > targetEntityBuffers[j].distance)
                    {
                        closetTarget = targetEntityBuffers[j];
                    }
                }
                turret.ValueRW.target = closetTarget.targetEntity;
                taskComponent.Status = TaskStatus.Success;
                var __newtaskComponents__ = taskComponents;
                __newtaskComponents__[chooseBestTargetNodeDatas[i].Index] = taskComponent;
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
