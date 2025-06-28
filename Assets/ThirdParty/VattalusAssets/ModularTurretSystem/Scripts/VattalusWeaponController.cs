using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class VattalusWeaponController : MonoBehaviour
{
    public List<VattalusBarrelAnimator> barrelsList = new List<VattalusBarrelAnimator>();
    public enum VattalusFiringPattern
    {
        Individual, //Fires one barrel at a time
        Simultaneous, //Fires all barrels at once.
        Gatling, //Barrels will spin up and start firing when at full speed.
        MissileLauncher //Requires a list of GameObjects that represent the loaded missiles. When the fire command is received one (or more) loaded missiles will be disabled and a missile projectile will be spawned in its place
    }
    [Header("Fire pattern settings")]
    [Tooltip("Individual: one barrel at a time. Simultaneous: All barrels at once. Gatling: barrel spins up before firing")]
    public VattalusFiringPattern firingPattern;
    [Tooltip("The number of shots fired in succession")]
    public int burstShots = 1;
    [Tooltip("Delay (in seconds) between burst shots")]
    public float burstDelay = 0.2f;
    [Tooltip("Time (in seconds) before the weapon can fire again")]
    public float cooldown = 1f; //time before the weapon is able to fire again
    [Tooltip("Amount (in degrees) of bullet spread")]
    public float spreadAngle = 0f;
    private float curentGatlingRotation = 0f;

    [Tooltip("Only used for Gatling-type weapons. Speed at which barrel rotates at full speed. Negative value to reverse rotation direction")]
    public float gatlingRotationSpeed = 0f;

    [Header("Projectile Settings")]
    [Tooltip("References to the loaded missiles in the launcher. This is so that the script knows to hide them, how many there are etc")]
    public List<GameObject> loadedMissilesList = new List<GameObject>();
    [Tooltip("Reference to the projectile prefab that will be spawned at each weapon fire")]
    public GameObject projectilePrefab;
    public Color projectileColor;
    public float projectileScale = 1f;
    [Tooltip("Starting speed of the projectile (will accelerate to 'projectileMaxSpeed' if 'projectileAcceleration' > 0)")]
    public float projectileSpeed = 100f;
    [Tooltip("Max speed of the projectile")]
    public float projectileMaxSpeed = 100f;
    [Tooltip("Projectile speed increase per second until the 'projectileMaxSpeed' is reached")]
    public float projectileAcceleration = 0f;
    [Tooltip("Time after which projectile is diabled/destroyed")]
    public float projectileLifetime = 3f;
    [Tooltip("Projectile will track towards target direction")]
    public Transform projectileHomingTarget = null;
    [Tooltip("Speed with which projectile tracks towards target (Set to 0 to disable homing)")]
    public float projectileHomingSpeed = 0f;
    [Tooltip("Projectile will track towards the predicted position of the target instead of current position")]
    public bool projectileUsePrediction = false;
    [Tooltip("Layer mask filtering for impact detection")]
    public LayerMask impactDetectionLayer;

    [Header("SFX")]
    [Tooltip("SFX prefab that will be spawned when firing. This is so that we can have multiple firing sound effects playing at the same time")]
    public VattalusOneShotSFX SFXInstancePrefab;
    [Tooltip("Audio clip will play when firing")]
    public AudioClip sfxClip;
    [Tooltip("Baseline SFX Pitch upon which we apply some variance")]
    public float sfxPitch = 1f;
    [Tooltip("Baseline SFX Volume upon which we apply some variance")]
    public float sfxVolume = 1f;

    //internal variables to figure out when the weapon can be fired again
    private float lastFireTime = 0f;
    private float TimeSinceLastFire { get { return Time.time - lastFireTime; } }
    public bool CanFire { get { return TimeSinceLastFire >= cooldown; } }

    private int barrelIndex = 0;
    private bool firingContinously = false;


    void Start()
    {
        curentGatlingRotation = 0f;
        barrelsList = new List<VattalusBarrelAnimator>();
        foreach (var barrelComponent in GetComponentsInChildren<VattalusBarrelAnimator>())
        {
            barrelsList.Add(barrelComponent);
            if (firingPattern == VattalusFiringPattern.Gatling) barrelComponent.tipRotateDegrees = 0f; //as a precaution, if this weapon is a gatling type, disable the rotation component of the barrel firing animation
        }

        //clean up the projectile parameters
        if (projectileScale <= 0f) projectileScale = 1f;
        projectileSpeed = Mathf.Abs(projectileSpeed);
        if (projectileSpeed > projectileMaxSpeed) projectileMaxSpeed = projectileSpeed;
        projectileLifetime = Mathf.Abs(projectileLifetime);
        spreadAngle = Mathf.Clamp(Mathf.Abs(spreadAngle), 0f, 45f);
    }

    //Behaviour in which the weapon will fire continously (according to firing pattern behaviour) until given the StopContinousFire command
    public void StartContinousFire(Transform homingTarget = null)
    {
        projectileHomingTarget = homingTarget;
        firingContinously = true;
    }

    public void StopContinousFire()
    {
        firingContinously = false;
    }

    //Called internally if set to fire continously (will fire whenever conditions are met, mainly realted to cooldown)
    //Called externally from a turret for example. Weapon will still adhere to cooldown limiation
    public void FireWeapon(Transform homingTarget = null)
    {
        projectileHomingTarget = homingTarget;

        if (CanFire)
        {
            StartCoroutine(FireWeaponCoroutine());
        }
    }

    void Update()
    {
        if (barrelsList != null && barrelsList.Count > 0)
        {
            //special code for gatling guns: When firing continously, ramp up spinning until the barrel reaches it's tipRotateDegrees value, after which it can fire. Once firingContinously becomes false start ramping down rotation.
            if (firingPattern == VattalusFiringPattern.Gatling)
            {
                float gatlingRotationSpeedChange = gatlingRotationSpeed * (1f / barrelsList[0].animationDuration) * Time.deltaTime;
                if (firingContinously)
                {
                    if (curentGatlingRotation < gatlingRotationSpeed) curentGatlingRotation += gatlingRotationSpeedChange;
                }
                else
                {
                    if (curentGatlingRotation > 0) curentGatlingRotation -= gatlingRotationSpeedChange;
                }

                //clamp rotation value
                curentGatlingRotation = Mathf.Clamp(curentGatlingRotation, 0f, gatlingRotationSpeed);
                //rotation animation
                if (curentGatlingRotation > 0f)
                    foreach (var barrel in barrelsList)
                    {
                        barrel.PlayGatlingSpinEffect(curentGatlingRotation, curentGatlingRotation / gatlingRotationSpeed);
                    }
            }
        }

        //check if conditions are met for firing
        if (firingContinously && CanFire)
        {
            if (firingPattern == VattalusFiringPattern.Gatling)
            {
                if (curentGatlingRotation >= gatlingRotationSpeed)
                {
                    FireWeapon();
                }
            }
            else
            {
                FireWeapon();
            }
        }
    }

    IEnumerator FireWeaponCoroutine()
    {
        if (barrelsList == null || barrelsList.Count == 0) yield break;

        lastFireTime = Time.time;

        switch (firingPattern)
        {
            case VattalusFiringPattern.Individual:

                for (int i = 0; i < burstShots; i++)
                {
                    if (barrelIndex >= barrelsList.Count) barrelIndex = 0;

                    barrelsList[barrelIndex].PlayFireEffects();
                    PlaySoundEffect(barrelsList[barrelIndex].muzzleFlashObject.transform.position);
                    SpawnProjectile(barrelsList[barrelIndex]);
                    lastFireTime = Time.time;

                    barrelIndex++;
                    yield return new WaitForSeconds(burstDelay);
                }

                break;

            case VattalusFiringPattern.Simultaneous:
            case VattalusFiringPattern.Gatling:

                foreach (VattalusBarrelAnimator barrel in barrelsList)
                {
                    barrel.PlayFireEffects();
                    PlaySoundEffect(barrel.muzzleFlashObject.transform.position);
                    SpawnProjectile(barrel);
                }

                break;

            case VattalusFiringPattern.MissileLauncher:

                for (int i = 0; i < burstShots; i++)
                {
                    //Play fire effects, spawn a projectile, and hide the respective loaded missile model
                    barrelsList[0].PlayFireEffects();
                    PlaySoundEffect(barrelsList[0].muzzleFlashObject.transform.position);
                    lastFireTime = Time.time;

                    //if there is a list of loaded projectiles, hide the next one, and spawn the projectile in it's place
                    if (loadedMissilesList != null && i < loadedMissilesList.Count)
                    {
                        loadedMissilesList[i].SetActive(false);
                        SpawnProjectile(barrelsList[0], loadedMissilesList[i].transform.position);
                    }
                    else
                    {
                        //There is no corresponding loaded missile asset in the barrel, just fire a missile from the center of the barrel
                        SpawnProjectile(barrelsList[0], barrelsList[0].transform.position);
                    }

                    yield return new WaitForSeconds(burstDelay);
                }

                //we have launched all missiles. Set the cooldown timer at the end of which missiles will be respawned/reloaded (Gameojects will be re-enabled)
                yield return new WaitForSeconds(cooldown);

                //reload missiles and reset barrel index
                barrelIndex = 0;
                foreach (var missile in loadedMissilesList)
                {
                    missile.gameObject.SetActive(true);
                }

                break;
        }
    }

    private void PlaySoundEffect([CanBeNull]Vector3? position)
    {
        if (position == null) position = transform.position;

        //SFX
        if (SFXInstancePrefab != null)
        {
            GameObject newSFXInstance = Instantiate(SFXInstancePrefab.gameObject, (Vector3)position, Quaternion.identity);

            VattalusOneShotSFX SFXScript = newSFXInstance.GetComponent<VattalusOneShotSFX>();
            if (SFXScript != null)
            {
                SFXScript.Initialize(sfxClip, sfxPitch, 0.25f, sfxVolume, 0.25f, true);
            }
        }
    }

    private void SpawnProjectile(VattalusBarrelAnimator fromBarrel, Vector3? spawnPositionOverride = null)
    {
        if (projectilePrefab == null) return;

        GameObject newProjectile = GameObject.Instantiate(projectilePrefab);
        Vector3 spreadAngleEuler = new Vector3(UnityEngine.Random.Range(-spreadAngle, spreadAngle), UnityEngine.Random.Range(-spreadAngle, spreadAngle), UnityEngine.Random.Range(-spreadAngle, spreadAngle));
        newProjectile.transform.eulerAngles = new Vector3(fromBarrel.transform.eulerAngles.x, fromBarrel.transform.eulerAngles.y, fromBarrel.transform.eulerAngles.z) + spreadAngleEuler;
        Vector3 spawnPosition = spawnPositionOverride != null ? (Vector3)spawnPositionOverride : fromBarrel.muzzleFlashObject.transform.position;
        newProjectile.transform.position = spawnPosition + fromBarrel.transform.forward * projectileScale * 3f;
        if (projectileScale > 0f) newProjectile.transform.localScale = Vector3.one * projectileScale;

        VattalusProjectile projectileScript = newProjectile.GetComponent<VattalusProjectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(projectileColor, null, projectileSpeed, projectileMaxSpeed, projectileAcceleration, projectileLifetime, projectileHomingTarget, projectileHomingSpeed, projectileUsePrediction, impactDetectionLayer);
        }
    }
}
