using Unity.Entities;
using UnityEngine;

public class BuildPositionTagAuthoring : MonoBehaviour
{
    public class MouseWorldPositionTrackAuthoringBaker : Baker<BuildPositionTagAuthoring>
    {
        public override void Bake(BuildPositionTagAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new BuildPositionTag()
            {

            });
        }
    }
}
public struct BuildPositionTag : IComponentData
{

}


