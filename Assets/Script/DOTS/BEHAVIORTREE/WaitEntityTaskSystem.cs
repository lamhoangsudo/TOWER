using Opsive.BehaviorDesigner.Runtime.Components;
using Opsive.BehaviorDesigner.Runtime.Utility;
using Opsive.GraphDesigner.Runtime;
using Opsive.GraphDesigner.Runtime.Variables;
using Opsive.Shared.Utility;
using Unity.Entities;
using Unity.Burst;
using UnityEngine;
using System;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
[NodeIcon("b4b59e888607422409f1efa599af34ae", "e1cb9cb566a90fb4489bf31465b99747")]
public struct WaitEntity : ITreeLogicNode, ITaskComponentData, IAction, IPausableTask, ISavableTask
{
    [Tooltip("The index of the node.")]
    [SerializeField] ushort m_Index;
    [Tooltip("The parent index of the node. ushort.MaxValue indicates no parent.")]
    [SerializeField] ushort m_ParentIndex;
    [Tooltip("The sibling index of the node. ushort.MaxValue indicates no sibling.")]
    [SerializeField] ushort m_SiblingIndex;
    private ushort m_ComponentIndex;
    public ushort Index { get => m_Index; set => m_Index = value; }
    public ushort ParentIndex { get => m_ParentIndex; set => m_ParentIndex = value; }
    public ushort SiblingIndex { get => m_SiblingIndex; set => m_SiblingIndex = value; }
    public ushort RuntimeIndex { get; set; }
    public ComponentType Tag { get => typeof(WaitEntityTag); }
    public Type SystemType { get => typeof(WaitEntityTaskSystem); }
    public void ClearBufferElement(World world, Entity entity)
    {
        DynamicBuffer<WaitEntityComponent> buffer;
        if (world.EntityManager.HasBuffer<WaitEntityComponent>(entity))
        {
            buffer = world.EntityManager.GetBuffer<WaitEntityComponent>(entity);
            buffer.Clear();
        }
    }
    public MemberVisibility GetSaveReflectionType(int index) { return MemberVisibility.None; }
    public void Pause(World world, Entity entity)
    {
        var waitComponents = world.EntityManager.GetBuffer<WaitEntityComponent>(entity);
        var waitComponent = waitComponents[m_ComponentIndex];
        waitComponent.PauseTime = Time.time;
        var waitComponentBuffer = waitComponents;
        waitComponentBuffer[m_ComponentIndex] = waitComponent;
    }
    public void Resume(World world, Entity entity)
    {
        var waitComponents = world.EntityManager.GetBuffer<WaitEntityComponent>(entity);
        var waitComponent = waitComponents[m_ComponentIndex];
        waitComponent.StartTime += (Time.time - waitComponent.PauseTime);
        waitComponent.PauseTime = 0;
        var waitComponentBuffer = waitComponents;
        waitComponentBuffer[m_ComponentIndex] = waitComponent;
    }
    public object Save(World world, Entity entity)
    {
        var waitComponents = world.EntityManager.GetBuffer<WaitEntityComponent>(entity);
        var waitComponent = waitComponents[m_ComponentIndex];

        // Save the unique data.
        return new object[] { waitComponent.WaitDuration, Time.time - waitComponent.StartTime };
    }
    public void Load(object saveData, World world, Entity entity)
    {
        var waitComponents = world.EntityManager.GetBuffer<WaitEntityComponent>(entity);
        var waitComponent = waitComponents[m_ComponentIndex];

        // saveData is the wait duration and the elapsed amount of time.
        var data = (object[])saveData;
        waitComponent.WaitDuration = (double)data[0];
        waitComponent.StartTime = Time.time - (double)data[1];
        waitComponents[m_ComponentIndex] = waitComponent;
    }

    public int AddBufferElement(World world, Entity entity, GameObject gameObject)
    {
        DynamicBuffer<WaitEntityComponent> buffer;
        if (world.EntityManager.HasBuffer<WaitEntityComponent>(entity))
        {
            buffer = world.EntityManager.GetBuffer<WaitEntityComponent>(entity);
        }
        else
        {
            buffer = world.EntityManager.AddBuffer<WaitEntityComponent>(entity);
        }
        buffer.Add(new WaitEntityComponent()
        {
            Index = RuntimeIndex,
        });
        return buffer.Length - 1;
    }
}
public struct WaitEntityComponent : IBufferElementData
{
    [Tooltip("The index of the node.")]
    public ushort Index;
    [Tooltip("The amount of time the task should wait.")]
    public double WaitDuration;
    [Tooltip("The real time the task started to wait.")]
    public double StartTime;
    [Tooltip("The seed of the random number generator.")]
    public uint Seed;
    [Tooltip("The random number generator for the task.")]
    public Unity.Mathematics.Random RandomNumberGenerator;
    [Tooltip("The time that the game was paused.")]
    public double PauseTime;
}
public struct WaitEntityTag : IComponentData, IEnableableComponent { }
[DisableAutoCreation]
public partial struct WaitEntityTaskSystem : ISystem
{
    private EntityQuery query;
    [BurstCompile]
    private void OnCreate(ref SystemState state)
    {
        query = SystemAPI.QueryBuilder().WithAll<TaskComponent, WaitEntityComponent, WaitEntityTag, RadarRangeRay, EvaluateFlag>().Build();
        state.RequireForUpdate(query);
    }
    [BurstCompile]
    private void OnUpdate(ref SystemState state)
    {
        WaitEntityJob waitEntityJob = new()
        {
            ElapsedTime = SystemAPI.Time.ElapsedTime
        };
        state.Dependency = waitEntityJob.ScheduleParallel(query, state.Dependency);
    }
    [BurstCompile]
    private partial struct WaitEntityJob : IJobEntity
    {
        [Tooltip("The current ElapsedTime.")]
        public double ElapsedTime;
        [BurstCompile]
        public void Execute(Entity entity, ref DynamicBuffer<TaskComponent> taskComponents, ref DynamicBuffer<WaitEntityComponent> waitComponents, in RadarRangeRay radarRangeRay)
        {
            for (int i = 0; i < waitComponents.Length; i++)
            {
                var waitComponent = waitComponents[i];
                var taskComponent = taskComponents[waitComponent.Index];
                if (taskComponent.Status == TaskStatus.Queued)
                {
                    taskComponent.Status = TaskStatus.Running;
                    waitComponent.StartTime = ElapsedTime;
                    waitComponent.WaitDuration = radarRangeRay.radarScanTimeMax;
                    waitComponents[i] = waitComponent;
                }
                if (taskComponent.Status == TaskStatus.Running)
                {
                    if (waitComponent.StartTime + waitComponent.WaitDuration <= ElapsedTime)
                    {
                        taskComponent.Status = TaskStatus.Success;
                    }
                }
                taskComponents[waitComponent.Index] = taskComponent;
            }
        }
    }
}
