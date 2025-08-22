using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class SnapPointBuffersAuthoring : MonoBehaviour
{
    private const string TAG = "SnapPoint";
    public class SnapPointBuffersAuthoringBaker : Baker<SnapPointBuffersAuthoring>
    {
        public override void Bake(SnapPointBuffersAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            DynamicBuffer<SnapPointBuffer> snapPointBuffers = AddBuffer<SnapPointBuffer>(entity);
            for (int i = 0; i < authoring.transform.childCount; i++)
            {
                if (authoring.transform.GetChild(i).CompareTag(TAG))
                {
                    snapPointBuffers.Add(new SnapPointBuffer
                    {
                        snapPointEntity = GetEntity(authoring.transform.GetChild(i).gameObject, TransformUsageFlags.Dynamic),
                        offset = Vector3.Distance(authoring.transform.position, authoring.transform.parent.position),
                    });
                }
            }
        }
    }
}

