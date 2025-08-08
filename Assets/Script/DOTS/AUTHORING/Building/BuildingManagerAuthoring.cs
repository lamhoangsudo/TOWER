using Unity.Entities;
using UnityEngine;

public class BuildingManagerAuthoring : MonoBehaviour
{
    public class BuildingManagerAuthoringBaker : Baker<BuildingManagerAuthoring>
    {
        public override void Bake(BuildingManagerAuthoring authoring)
        {

        }
    }
}
public struct BuildingManager : IComponentData
{

}


