using Unity.Entities;
using UnityEngine;

public class ExplosionVFXAuthoring : MonoBehaviour
{
    public float lifeTimeMax;
    public class ExplosionVFXAuthoringBaker : Baker<ExplosionVFXAuthoring>
    {
        public override void Bake(ExplosionVFXAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Explosion
            {
                lifeTime = authoring.lifeTimeMax,
                lifeTimeMax = authoring.lifeTimeMax,
            });
        }
    }
}
public struct Explosion : IComponentData
{
    public float lifeTime;
    public float lifeTimeMax;
}


