using Unity.Entities;
using UnityEngine;

public class MouseWorldPositionTrackAuthoring : MonoBehaviour
{
    public float range;
    public class MouseWorldPositionTrackAuthoringBaker : Baker<MouseWorldPositionTrackAuthoring>
    {
        public override void Bake(MouseWorldPositionTrackAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MouseWorldPositionTrack()
            {
                range = authoring.range
            });
        }
    }
}
public struct MouseWorldPositionTrack : IComponentData
{
    public float range;
}


