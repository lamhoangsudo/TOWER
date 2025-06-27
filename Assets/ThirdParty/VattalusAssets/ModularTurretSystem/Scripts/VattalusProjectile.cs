using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class VattalusProjectile : MonoBehaviour
{
    // Contains logic related to projectiles (speed, effects, lifetime etc)
    [Header("Component references")]
    public GameObject projectileGO;
    [Tooltip("List of objects to which to set color and apply scale jittering")]
    public List<GameObject> ListOfEffects = new List<GameObject>();
    public TrailRenderer trailEffect;
    private Light lightComponent = null;

    [Header("Visual Effects")]
    [Tooltip("Sets all colors (projectile effect, light, trail etc)")]
    public Color color;
    [Tooltip("Automatically creates a light component if value>0")]
    public float lightSourceIntensity = 0f;
    public float projectileSpin = 0f;
    public float scaleJitter = 0f;
    public float lengthJitter = 0f;

    [Header("Functionality")]
    [Tooltip("Starting projectile speed")]
    public float startingSpeed = 100f;
    public float maxSpeed = 100f;
    private float currentSpeed = 0f;
    public float acceleration = 0f;
    [Tooltip("Time (in seconds) after which the projectile self-deletes")]
    public float lifetime = 5f;
    private float creationTime; //time of creation. used to calculate lifetime
    [Tooltip("Will spawn this GO at the point of impact")]
    public GameObject impactEffect;
    [Tooltip("Will spawn impact effect when lifetime expires without impacting anything")]
    public bool spawnImpactAtDespawn = false;
    [Tooltip("Will destroy GO when it is disabled. Set to false if you wish to use this in a pooling system to enable GO recycling")]
    public bool destroyOnDisable = true;

    [Tooltip("If !=null projectile will move towards target")]
    public Transform homingTarget;
    private Vector3 vectorToTarget; //relative direction towards the desired target
    [Tooltip("Speed at which projectile changes direction towards target")]
    public float homingSpeed = 0f;
    [Tooltip("If enabled, target will anticipate target's position and will move towards that instead")]
    public bool usePrediction = false;

    //layermask filtering for collision
    private LayerMask impactLayerMask = -1;

    void OnEnable()
    {
        //Initialize
        creationTime = Time.time;
        Initialize(color, lightSourceIntensity, startingSpeed, maxSpeed, acceleration, lifetime, homingTarget, homingSpeed, usePrediction, impactLayerMask);
    }

    void OnDisable()
    {
        if (destroyOnDisable) Destroy(gameObject);
    }

    //We can additionally call this externally to update the parameters
    public void Initialize(Color? SetColor, float? SetLightIntensity, float? SetStartingSpeed, float? SetMaxSpeed, float? SetAcceleration, float? SetLifetime, [CanBeNull] Transform SetHomingTarget, float? SetHomingSpeed, bool? SetUsePrediction, [CanBeNull] LayerMask? SetImpactLayerMask)
    {
        if (SetColor != null) color = (Color)SetColor;
        if (SetLightIntensity != null) lightSourceIntensity = (float)SetLightIntensity;
        if (SetStartingSpeed != null) startingSpeed = (float)SetStartingSpeed;
        if (SetMaxSpeed != null) maxSpeed = (float)SetMaxSpeed;
        if (SetAcceleration != null) acceleration = (float)SetAcceleration;
        if (SetLifetime != null) lifetime = (float)SetLifetime;
        homingTarget = SetHomingTarget;
        if (SetHomingSpeed != null) homingSpeed = (float)SetHomingSpeed;
        if (SetImpactLayerMask != null) impactLayerMask = (LayerMask)SetImpactLayerMask;

        //colors
        foreach (GameObject effectGO in ListOfEffects)
        {
            MeshRenderer renderer = effectGO.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.SetColor("_Color", color);
                renderer.material.SetColor("_EmissionColor", color * 10f);
            }
        }

        //find and color the trail (if there is one)
        trailEffect = GetComponentInChildren<TrailRenderer>();
        if (trailEffect != null)
        {
            trailEffect.material.SetColor("_Color", trailEffect.material.GetColor("_Color") * color);
            trailEffect.material.SetColor("_EmissionColor", trailEffect.material.GetColor("_EmissionColor") * color);
        }

        //light
        if (lightSourceIntensity > 0f)
        {
            lightComponent = GetComponent<Light>();
            if (lightComponent == null) lightComponent = gameObject.AddComponent<Light>();
            lightComponent.color = color;
            lightComponent.intensity = lightSourceIntensity * transform.localScale.x;
            lightComponent.range = lightSourceIntensity * 2f;
        }

        //speed
        currentSpeed = startingSpeed;
    }

    void Update()
    {
        UpdateEffects();

        if (homingTarget != null) CalculateTargeting();

        UpdateProjectileMovement();

        //check if lifetime expired
        if (Time.time > creationTime + lifetime)
        {
            Despawn(null);
        }
    }

    void FixedUpdate()
    {
        //check for impact
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, currentSpeed / 20f, impactLayerMask))
        {
            Despawn(hit.transform.gameObject, (transform.position + hit.point) / 2f);
        }
    }

    //rotations, jitters etc
    private void UpdateEffects()
    {
        //scale jitter
        if (ListOfEffects != null && ListOfEffects.Count > 0 != null && !(scaleJitter <= 0f && lengthJitter <= 0f))
        {
            foreach (GameObject effectGO in ListOfEffects)
            {
                float newScale = Mathf.Lerp(0.85f, 1f + scaleJitter, UnityEngine.Random.Range(0f, 1f));
                float newLength = 1f;
                if (lengthJitter > 0f) newLength = Random.Range(1f, 1f + lengthJitter);
                effectGO.transform.localScale = new Vector3(newScale, newScale, newLength);
            }
        }

        //light jitter
        if (!(scaleJitter <= 0f && lengthJitter <= 0f))
        {
            float lightJitterFactor = Random.Range(1f - (scaleJitter + lengthJitter / 5f), 1f + (scaleJitter + lengthJitter / 10f));

            if (lightComponent != null)
            {
                lightComponent.color = color;
                lightComponent.intensity = lightSourceIntensity * lightJitterFactor * transform.localScale.x;
                lightComponent.range = lightComponent.intensity * 2f;
            }

            foreach (GameObject effectGO in ListOfEffects)
            {
                MeshRenderer renderer = effectGO.GetComponentInChildren<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material.SetColor("_EmissionColor", color * lightJitterFactor * 15f);
                }
            }
        }


        //spin
        if (projectileGO != null && projectileSpin > 0f)
        {
            projectileGO.transform.localEulerAngles += new Vector3(0f, 0f, projectileSpin * Time.deltaTime);
        }
    }

    //target prediction, calculate vector to target
    private void CalculateTargeting()
    {

    }

    //move forward, accelerate, change direction towards homing
    private void UpdateProjectileMovement()
    {
        if (acceleration > 0f) currentSpeed = Mathf.Clamp(currentSpeed + acceleration * Time.deltaTime, 0f, maxSpeed);
        transform.position += currentSpeed * Time.deltaTime * transform.forward;

        if (homingTarget != null && homingSpeed > 0f)
        {
            // Determine which direction to rotate towards
            Vector3 targetDirection = homingTarget.position - transform.position;

            // Rotate the forward vector towards the target direction by one step
            Vector3 newDirection = Vector3.RotateTowards(transform.forward, targetDirection, homingSpeed * Time.deltaTime, 0.0f);

            // Calculate a rotation a step closer to the target and applies rotation to this object
            transform.rotation = Quaternion.LookRotation(newDirection);
        }
    }

    //Called whenever projectile hits a target or lifetime expires.
    private void Despawn(GameObject impactedGO, Vector3? impactPosition = null)
    {
        if (impactedGO != null || spawnImpactAtDespawn)
        {
            if (impactEffect != null)
            {
                if (impactPosition == null) impactPosition = transform.position;
                SpawnImpactEffect((Vector3)impactPosition);
            }
        }

        //disable GO
        gameObject.SetActive(false);
    }

    private void SpawnImpactEffect(Vector3 impactPosition)
    {
        GameObject newImpactEffect = Instantiate(impactEffect, impactPosition, Random.rotation);
    }
}
