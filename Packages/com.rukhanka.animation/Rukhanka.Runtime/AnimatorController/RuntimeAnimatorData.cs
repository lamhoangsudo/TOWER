using Unity.Collections;
#if RUKHANKA_WITH_NETCODE
using Unity.NetCode;
#endif

/////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{ 

public struct RuntimeAnimatorData
{
	public struct StateSnapshot
	{
		public int id;
		public float weight;
		public float normalizedTime;
	}
	
//-------------------------------------------------------------------------------------------------//

	public struct StateData
	{
		public int id;
		public float normalizedDuration;
		
		public static StateData MakeDefault() => new StateData() { id = -1, normalizedDuration = 0 };
	}
	
//-------------------------------------------------------------------------------------------------//

	public struct TransitionData
	{
		public int id;
		public float normalizedDuration;
		public float length;
		
		public static TransitionData MakeDefault() => new TransitionData() { id = -1, length = 0, normalizedDuration = 0 };
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	public StateData srcState;
	public StateData dstState;
	public TransitionData activeTransition;
#if RUKHANKA_WITH_NETCODE
	[GhostField(SendData = false)]
#endif
	public FixedList64Bytes<StateSnapshot> srcStateSnapshots;
	
/////////////////////////////////////////////////////////////////////////////////////////////////////

	public void ClearStateSnapshots() { srcStateSnapshots.Clear(); }
	
/////////////////////////////////////////////////////////////////////////////////////////////////////

	public void PushStateSnapshot(int stateID, float weight, float normalizedTime)
	{
		//	If we are out of free space prune snapshot with the lowest weight
		if (srcStateSnapshots.length == srcStateSnapshots.Capacity)
		{
			var minWeight = 1.0f;
			var minWeightIndex = 0;
			for (var i = 0; i < srcStateSnapshots.length; ++i)
			{
				var w = srcStateSnapshots[i].weight;
				if (minWeight > w)
				{
					minWeight = w;
					minWeightIndex = i;
				}
			}
			if (minWeightIndex != srcStateSnapshots.length - 1)
				srcStateSnapshots[minWeightIndex] = srcStateSnapshots[^1];
			srcStateSnapshots.Length -= 1;
		}
		
		//	Scale existing weights
		for (var i = 0; i < srcStateSnapshots.length; ++i)
		{
			ref var sn = ref srcStateSnapshots.ElementAt(i);
			sn.weight *= 1 - weight;
		}
		var ss = new StateSnapshot() { id = stateID, weight = weight, normalizedTime = normalizedTime };
		srcStateSnapshots.Add(ss);
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////

	public static RuntimeAnimatorData MakeDefault()
	{
		var rv = new RuntimeAnimatorData();
		rv.srcState = StateData.MakeDefault();
		rv.dstState = StateData.MakeDefault();
		rv.activeTransition = TransitionData.MakeDefault();
		return rv;
	}
}

}
