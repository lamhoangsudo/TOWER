using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace MyCollector
{
    public partial struct IgnoreEntityCollector : ICollector<RaycastHit>
    {
        private Entity ignoreEntity;
        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction { get; }

        public int NumHits => Hits.Length;

        public NativeList<RaycastHit> Hits;
        public IgnoreEntityCollector(Entity ignoreEntity, float maxFraction, Allocator allocator)
        {
            this.ignoreEntity = ignoreEntity;
            MaxFraction = maxFraction;
            Hits = new NativeList<RaycastHit>(allocator);
        }
        public bool AddHit(RaycastHit hit)
        {
            if (hit.Entity == ignoreEntity) return false;
            Hits.Add(hit);
            return true;
        }
    }
}
