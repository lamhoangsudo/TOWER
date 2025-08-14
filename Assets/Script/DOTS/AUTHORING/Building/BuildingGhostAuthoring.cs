using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BuildingGhostAuthoring : MonoBehaviour
{
    public List<SnapPointAuthoring> snapPointsAuthorings;
    public class BuildingGhostAuthoringBaker : Baker<BuildingGhostAuthoring>
    {
        public override void Bake(BuildingGhostAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BuildingGhost
            {

            });
            DynamicBuffer<SnapPointBuffer> snapPointBuffers = AddBuffer<SnapPointBuffer>(entity);
            for (int i = 0; i < authoring.snapPointsAuthorings.Count; i++)
            {
                snapPointBuffers.Add(new SnapPointBuffer
                {
                    snapPointEntity = GetEntity(authoring.snapPointsAuthorings[i].gameObject, TransformUsageFlags.Dynamic),
                    offset = math.distance(authoring.snapPointsAuthorings[i].transform.position, authoring.transform.position),
                });
            }
        }
    }
}
public struct BuildingGhost : IComponentData
{

}


