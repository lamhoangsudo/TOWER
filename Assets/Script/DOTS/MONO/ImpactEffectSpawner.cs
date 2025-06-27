using System;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class ImpactEffectSpawner : MonoBehaviour
{
    public GameObject impactPrefab;
    public AudioClip impactSound;
    public EntityManager entityManager;
    private void Start()
    {
        EventBusMono.Instance.onProjecTileEntityHit += onProjecTileEntityHit;
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    private void onProjecTileEntityHit(object sender, EventArgs e)
    {
        Entity entity = (Entity)sender;
        if(entityManager.HasComponent<ProjecTile>(entity))
        {
            LocalTransform localTransform = entityManager.GetComponentData<LocalTransform>(entity);
            // play vfx
            if (impactPrefab != null)
            {
                Instantiate(impactPrefab, localTransform.Position, Quaternion.identity);
            }

            // play sfx
            if (impactSound != null)
            {
                AudioSource.PlayClipAtPoint(impactSound, localTransform.Position);
            }
        }
    }
}
