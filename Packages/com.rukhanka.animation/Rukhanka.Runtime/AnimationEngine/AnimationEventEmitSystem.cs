
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

[DisableAutoCreation]
[UpdateBefore(typeof(AnimationProcessSystem))]
public partial struct AnimationEventEmitSystem: ISystem
{
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle EmitAnimationEvents(ref SystemState ss, JobHandle dependsOn)
	{
		var dt = SystemAPI.Time.DeltaTime;
		
		var emitAnimationEventsJob = new EmitAnimationEventsJob()
		{
			deltaTime = dt
		};
		var jh = emitAnimationEventsJob.ScheduleParallel(dependsOn);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle MakeProcessedAnimationsSnapshot(ref SystemState ss, JobHandle dependsOn)
	{
		var makeProcessedAnimationsSnapshotJob = new MakeProcessedAnimationsSnapshotJob() { };
		var jh = makeProcessedAnimationsSnapshotJob.ScheduleParallel(dependsOn);
		return jh;
	}
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	public void OnUpdate(ref SystemState ss)
	{
		//	Emit animation events based on current and previously processed animations
		var emitAnimationEventsJH = EmitAnimationEvents(ref ss, ss.Dependency);
		
		//	Make a snapshot of current frame animations, to use it in next frame as previously processed jobs
		var makeProcessedAnimationsSnapshotJH = MakeProcessedAnimationsSnapshot(ref ss, emitAnimationEventsJH);

		var combinedJH = JobHandle.CombineDependencies(emitAnimationEventsJH, makeProcessedAnimationsSnapshotJH);
		ss.Dependency = combinedJH;
	}
}
}
