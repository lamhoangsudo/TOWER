using System.Globalization;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
[UpdateAfter(typeof(TurretHeadingSystem))]
[UpdateAfter(typeof(TurretElevationSystem))]
partial struct TurretFireSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Turret, /*WeaponBuffer,*/ TurretFireTime>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        TurretFireJob turretFireJob = new()
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            ecb = ecb.AsParallelWriter(),
            componentLookupWeapon = SystemAPI.GetComponentLookup<Weapon>(isReadOnly: false),
            componentLookupWeaponFireTime = SystemAPI.GetComponentLookup<WeaponFireTime>(isReadOnly: true),
        };
        JobHandle jobHandle = turretFireJob.ScheduleParallel(state.Dependency);
        jobHandle.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
    [BurstCompile]
    public partial struct TurretFireJob : IJobEntity
    {
        public float deltaTime;
        private bool shouldFire;
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public ComponentLookup<Weapon> componentLookupWeapon;
        [ReadOnly] public ComponentLookup<WeaponFireTime> componentLookupWeaponFireTime;
        public void Execute(in Turret turret, ref Weapons weapons, ref TurretFireTime turretFireTime, [ChunkIndexInQuery] int sortkey)
        {
            if (turretFireTime.burstCount == 0) turretFireTime.cooldown -= deltaTime;
            if (turretFireTime.cooldown > 0) return;
            shouldFire = (turret.autoFire && turret.isHeadingRotationTarget && turret.isElevationRotationTarget);

            Weapon weaponWritter = default;
            ref WeaponBlobDatabase weaponBlobDatabase = ref weapons.weaponBlobReference.Value;
            ref BlobArray<WeaponBlobData> weaponBlobDataArray = ref weaponBlobDatabase.weapons;
            switch (turretFireTime.firingPattern)
            {
                case Enum.TurretFiringPattern.Simultaneous:
                    Simultaneous(turretFireTime, ref weaponBlobDataArray, deltaTime, weaponWritter, componentLookupWeapon, shouldFire, turret, ecb, sortkey);
                    break;
                case Enum.TurretFiringPattern.Individual:
                    Individual(turretFireTime, ref weaponBlobDataArray, deltaTime, weaponWritter, componentLookupWeapon, shouldFire, turret, ecb, sortkey);
                    break;
                case Enum.TurretFiringPattern.Gatling:
                    Gatling(turretFireTime, componentLookupWeaponFireTime, ref weaponBlobDataArray, deltaTime, weaponWritter, componentLookupWeapon, shouldFire, turret, ecb, sortkey);
                    break;
            }
            return;
        }
        private void Gatling(TurretFireTime turretFireTime, ComponentLookup<WeaponFireTime> componentLookupWeaponFireTime, ref BlobArray<WeaponBlobData> weaponBlobDataArray, float deltaTime, Weapon weaponWritter, ComponentLookup<Weapon> componentLookupWeapon, bool shouldFire, Turret turret, EntityCommandBuffer.ParallelWriter ecb, int sortkey)
        {
            if (turretFireTime.burstCount >= turretFireTime.burstCountMax)
            {
                turretFireTime.cooldown = turretFireTime.cooldownMax + componentLookupWeaponFireTime[weaponBlobDataArray[0].weapon].timeOverHeatMax;
                turretFireTime.burstCount = 0;
            }
            else
            {
                turretFireTime.burstDelay += deltaTime;
                if (turretFireTime.burstDelay < turretFireTime.burstDelayMax) return;
                for (int i = 0; i < weaponBlobDataArray.Length; i++)
                {
                    Entity weapon = weaponBlobDataArray[i].weapon;
                    weaponWritter = componentLookupWeapon[weapon];
                    if (weaponWritter.startFire == shouldFire) continue;
                    turretFireTime.burstCount++;
                    weaponWritter.startFire = shouldFire;
                    if (weaponWritter.targetEntity == Entity.Null || weaponWritter.targetEntity != turret.target) weaponWritter.targetEntity = turret.target;
                    ecb.SetComponent<Weapon>(sortkey, weapon, weaponWritter);
                }
            }
            turretFireTime.burstDelay = 0f;
        }
        private void Individual(TurretFireTime turretFireTime, ref BlobArray<WeaponBlobData> weaponBlobDataArray, float deltaTime, Weapon weaponWritter, ComponentLookup<Weapon> componentLookupWeapon, bool shouldFire, Turret turret, EntityCommandBuffer.ParallelWriter ecb, int sortkey)
        {
            if (turretFireTime.burstCount >= turretFireTime.burstCountMax)
            {
                turretFireTime.cooldown = turretFireTime.cooldownMax;
                turretFireTime.burstCount = 0;
                return;
            }
            turretFireTime.burstDelay += deltaTime;
            if (turretFireTime.burstDelay < turretFireTime.burstDelayMax) return;
            weaponWritter = componentLookupWeapon[weaponBlobDataArray[turretFireTime.indexWeapons].weapon];
            if (weaponWritter.startFire == shouldFire) return;
            turretFireTime.indexWeapons++;
            if (turretFireTime.indexWeapons >= weaponBlobDataArray.Length) turretFireTime.indexWeapons = 0;
            turretFireTime.burstCount++;
            turretFireTime.burstCount = math.clamp(turretFireTime.burstCount, 0, turretFireTime.burstCountMax);
            turretFireTime.burstDelay = 0f;
            weaponWritter.startFire = shouldFire;
            if (weaponWritter.targetEntity == Entity.Null || weaponWritter.targetEntity != turret.target) weaponWritter.targetEntity = turret.target;
            ecb.SetComponent<Weapon>(sortkey, weaponBlobDataArray[turretFireTime.indexWeapons].weapon, weaponWritter);
        }
        private void Simultaneous(TurretFireTime turretFireTime, ref BlobArray<WeaponBlobData> weaponBlobDataArray, float deltaTime, Weapon weaponWritter, ComponentLookup<Weapon> componentLookupWeapon, bool shouldFire, Turret turret, EntityCommandBuffer.ParallelWriter ecb, int sortkey)
        {
            if (weaponBlobDataArray.Length <= 1) return;
            if (turretFireTime.burstCount >= turretFireTime.burstCountMax)
            {
                turretFireTime.cooldown = turretFireTime.cooldownMax;
                turretFireTime.burstCount = 0;
                return;
            }
            turretFireTime.burstDelay += deltaTime;
            if (turretFireTime.burstDelay < turretFireTime.burstDelayMax) return;
            for (int i = 0; i < weaponBlobDataArray.Length; i++)
            {
                Entity weapon = weaponBlobDataArray[i].weapon;
                weaponWritter = componentLookupWeapon[weapon];
                if (weaponWritter.startFire == shouldFire) continue;
                turretFireTime.burstCount = math.clamp(turretFireTime.burstCount, 0, turretFireTime.burstCountMax);
                weaponWritter.startFire = shouldFire;
                turretFireTime.burstCount++;
                if (weaponWritter.targetEntity == Entity.Null || weaponWritter.targetEntity != turret.target) weaponWritter.targetEntity = turret.target;
                ecb.SetComponent<Weapon>(sortkey, weapon, weaponWritter);
            }
            turretFireTime.burstDelay = 0f;
        }
    }
}
