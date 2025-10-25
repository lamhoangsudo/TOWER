using Unity.Entities;
using UnityEngine;

public class WeaponFireTimeAuthoring : MonoBehaviour
{
    public float burstDelayMax;
    public int burstCountMax;
    public float timeOverHeatMax;
    public class WeaponFireTimeAuthoringBaker : Baker<WeaponFireTimeAuthoring>
    {
        public override void Bake(WeaponFireTimeAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new WeaponFireTime
            {
                burstCountMax = authoring.burstCountMax,
                burstDelayMax = authoring.burstDelayMax,
                timeOverHeatMax = authoring.timeOverHeatMax,
                barrelTipIndex = 0,
                pointShootIndex = 0,
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
    public int barrelTipIndex;
    public int pointShootIndex;
    public float timeOverHeatMax;
    public float timeOverHeat;
}


