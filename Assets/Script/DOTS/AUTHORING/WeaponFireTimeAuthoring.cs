using Unity.Entities;
using Unity.VisualScripting;
using UnityEngine;

public class WeaponFireTimeAuthoring : MonoBehaviour
{
    public float burstDelayMax;
    public int burstCountMax;
    public class WeaponFireTimeAuthoringBaker : Baker<WeaponFireTimeAuthoring>
    {
        public override void Bake(WeaponFireTimeAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new WeaponFireTime
            {
                burstCountMax = authoring.burstCountMax,
                burstDelayMax = authoring.burstDelayMax,
            });
        }
    }
}
public struct WeaponFireTime : IComponentData
{
    public int burstCountMax;
    public int burstCount;
    public float burstDelayMax;
    public float burstDelay;
}


