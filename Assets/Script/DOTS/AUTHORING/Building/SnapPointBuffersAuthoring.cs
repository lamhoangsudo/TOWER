using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

public class SnapPointBuffersAuthoring : MonoBehaviour
{
    private const string TAG = "SnapPoint";
    public List<SnapPointAuthoring> snapPointAuthorings;
    public Enum.SnapPointType defaultSnapPointType;
    public class SnapPointBuffersAuthoringBaker : Baker<SnapPointBuffersAuthoring>
    {
        public override void Bake(SnapPointBuffersAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            DynamicBuffer<SnapPointBuffer> snapPointBuffers = AddBuffer<SnapPointBuffer>(entity);
            if (authoring.snapPointAuthorings.Count > 0)
            {
                for (int i = 0; i < authoring.snapPointAuthorings.Count; i++)
                {
                    if (authoring.snapPointAuthorings[i].CompareTag(TAG))
                    {
                        snapPointBuffers.Add(new SnapPointBuffer
                        {
                            snapPointEntity = GetEntity(authoring.snapPointAuthorings[i].gameObject, TransformUsageFlags.Dynamic),
                            snapPointType = authoring.snapPointAuthorings[i].snapPointType,
                            offset = Vector3.Distance(authoring.snapPointAuthorings[i].transform.position, authoring.transform.parent.position),
                        });
                    }
                }
            }
            else
            {
                snapPointBuffers.Add(new SnapPointBuffer
                {
                    offset = Vector3.Distance(authoring.transform.position, authoring.transform.parent.position),
                    snapPointPosition = authoring.transform.position,
                    snapPointType = authoring.defaultSnapPointType,
                    isOccupied = false,
                    distanceSnapPointToBuildingGhost = 0f,
                });
            }
        }
    }
}

