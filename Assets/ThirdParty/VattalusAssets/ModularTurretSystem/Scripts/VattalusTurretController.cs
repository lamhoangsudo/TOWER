using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//This script handles the behaviour of the turret (movement, target prediction, sends fire commands to weapons)
//Attached weapons are automatically found and referenced as long as they are children of this GO.
public class VattalusTurretController : MonoBehaviour
{
    [Header("Turret Components")]
    [Tooltip("Game Object that rotates left-right. Most (if not all) turret components should be parented to this GO")]
    public GameObject turretHeadingParent;
    [Tooltip("Game Object that rotates up-down. Weapons and some attachments are parented to this GO")]
    public GameObject turretElevationParent;
    private List<VattalusWeaponController> weapons = new List<VattalusWeaponController>();

    [Header("Effects")]
    [Tooltip("Reference to AudioSource for the heading rotation. Change pitch directly on the AudioSource Component")]
    public AudioSource HeadingRotationSFX;
    [Tooltip("Reference to AudioSource for the elevation rotation. Change pitch directly on the AudioSource Component")]
    public AudioSource ElevationRotationSFX;
    private float HeadingRotationSFXInitialPitch;
    private float ElevationRotationSFXInitialPitch;


    [Header("Turret Settings")]
    [Tooltip("Gameoject that the turret will try to track while respecting heading/elevation angle constraints (if applicable)")]
    public Transform target;
    [Tooltip("If enabled, turret will try to anticipate the target's movements taking into account the target's velocity and the equipped weapon projectile velocity")]
    public bool useTargetPrediction = false;

    public enum TurretMovement
    {
        TargetTracking, //Turret moves autonomously based on given target
        DirectControl //Turret aimed using externally provided heading/elevation values
    }
    [Tooltip("Sets the movement behavior of the turret. Automatic: -moves autonomously based on given target, TargetTracking: -Turret aimed using externally provided heading/elevation values")]
    public TurretMovement turretMovement;

    public enum FiringPattern
    {
        Individual,
        Simultaneous
    }
    [Tooltip("in case of multiple weapons, this handles the logic of managing that")]
    public FiringPattern firingPattern; //in case of multiple weapons, this handles the logic of managing that\
    private int weaponIndex = 0;
    private float lastFireTime;

    [Tooltip("Turret will automatically fire whenever it is aligned with the target")]
    public bool autoFire = true;

    [Tooltip("Minimum angle deviation to target before firing automatically (Ex: Use higher values for homing missile launchers since they do not require precise alignment)")]
    public float targetAquiredAngle = 0.5f;
    [Tooltip("Turret will return to a neutral position when a target is not set")]
    public bool resetOrientationWhenTargetLost = true;

    [Tooltip("Max heading rotation speed")]
    public float HeadingRotationSpeed = 100f;
    [Tooltip("heading rotation acceleration/deceleration")]
    public float HeadingRotationAcceleration = 25f; //  degrees/second acceleration until rotationSpeed cap is met
    [Tooltip("Max elevation rotation speed")]
    public float ElevationRotationSpeed = 100f;
    [Tooltip("elevation rotation acceleration/deceleration")]
    public float ElevationRotationAcceleration = 25f; //  degrees/second acceleration until pitchSpeed cap is met

    private float currHeadingRotSpeed = 0f;
    private float currElevationRotSpeed = 0f;

    [Tooltip("Angle restrictions for heading (left-right). Set to (-180, 180) to remove restrictions")]
    public Vector2 HeadingAngleConstraints = new Vector2(-180, 180);
    private bool HeadingLimited { get { return !(HeadingAngleConstraints.x <= -180 && HeadingAngleConstraints.y >= 180f); } }
    private bool TargetOutsideHeadingConstraints { get { return headingTarget < HeadingAngleConstraints.x || headingTarget > HeadingAngleConstraints.y; } }
    private bool TargetOutsideElevationConstraints { get { return elevationTarget < ElevationAngleConstraints.x || elevationTarget > ElevationAngleConstraints.y; } }
    [Tooltip("Angle restrictions for elevation (up-down)")]
    public Vector2 ElevationAngleConstraints = new Vector2(-10f, 65f);
    public bool TargetAquired { get { return AbsAngleDifference(headingTarget, turretHeadingParent.transform.localEulerAngles.y) <= targetAquiredAngle && AbsAngleDifference(elevationTarget, -turretElevationParent.transform.localEulerAngles.x) <= targetAquiredAngle; } } //returns true when the turret is aimed at the target


    //heading and elevation values (-180 to +180) of the designated target. The turret will do it's best to move as fast and as close as possible to these values
    private float headingTarget = 0f;
    private float elevationTarget = 0f;

    //heading and elevation values of the given target limited to the andgle constraints of the turret.
    private float headingAim = 0f;
    private float elevationAim = 0f;


    [Header("Debug")]
    [Tooltip("Render debug information such as angle restrictions, aim information")]
    public bool debugMode = false;
    public GameObject debugTargetDirectionIndicator;
    private MeshRenderer debugTargetDirectionIndicatorMR;
    public Slider debugHeadingConstraintIndicator;
    public Slider debugElevationConstraintIndicator;


    void Start()
    {
        //automatically populate the list of attached weapons
        weapons = new List<VattalusWeaponController>();
        foreach (VattalusWeaponController weapon in GetComponentsInChildren<VattalusWeaponController>())
        {
            weapons.Add(weapon);
        }

        if (HeadingRotationSFX != null) HeadingRotationSFXInitialPitch = HeadingRotationSFX.pitch * UnityEngine.Random.Range(0.95f, 1.05f);
        if (ElevationRotationSFX != null) ElevationRotationSFXInitialPitch = ElevationRotationSFX.pitch * UnityEngine.Random.Range(0.95f, 1.05f);

        //initialize debug stuff
        if (debugTargetDirectionIndicatorMR == null && debugTargetDirectionIndicator != null) debugTargetDirectionIndicatorMR = debugTargetDirectionIndicator.GetComponentInChildren<MeshRenderer>();
    }

    void OnEnable()
    {
        StartCoroutine(TargetingCalculationsCoroutine(20f));
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    //Sets the turret movement mode to DirectControl, and gives tells it to align with heading/elevation values
    public void SetTurretAim(float SetHeading, float SetElevation, bool SetAutoFire)
    {
        turretMovement = TurretMovement.DirectControl;

        //clamp them to the angle constraints
        headingTarget = NormalizeAngle(SetHeading);
        elevationTarget = NormalizeAngle(SetElevation);

        autoFire = SetAutoFire;
    }

    //Sets the turret movement to Automatic
    public void SetTurretTarget(Transform newTarget, bool SetAutoFire)
    {
        if (newTarget == null) return;

        turretMovement = TurretMovement.TargetTracking;
        target = newTarget;
        autoFire = SetAutoFire;
    }

    void Update()
    {
        ////Toggle debug mode (For testing only!)
        //if (Input.GetKeyDown(KeyCode.T)) debugMode = !debugMode;

        //determine if heading and elevation rotation speeds need to increase/decrease
        #region Animate Heading Rotation
        float HeadingAimAngleDifference = AngleDifference(headingAim, turretHeadingParent.transform.localEulerAngles.y);
        float CurrentHeading = NormalizeAngle(turretHeadingParent.transform.localEulerAngles.y);
        float HeadingSecondsToReachAim = HeadingAimAngleDifference / currHeadingRotSpeed; if (HeadingSecondsToReachAim < 0f) HeadingSecondsToReachAim = 999999f; //if rotation direction is opposite to the required direction of movement, time to reach aim is infinity (a really big number)
        float HeadingSecondsNeededToStop = Mathf.Abs(currHeadingRotSpeed) / HeadingRotationAcceleration;


        if (Mathf.Abs(HeadingAimAngleDifference) > 0.05f)
        {
            //determine relative heading direction to the target (to the left or right)
            bool AimToRight = CurrentHeading < headingAim;
            //reverse heading rotation direction if there is no angle limitation on the turret and the angle difference is >180
            if (HeadingLimited == false && Mathf.Abs(CurrentHeading - headingAim) > 180f) AimToRight = !AimToRight;

            //increase or decrease the rotation applied to the turret based on desired direction and how far off it is to the aim point.
            if (AimToRight)
            {
                if (HeadingSecondsToReachAim > HeadingSecondsNeededToStop * 0.65f)
                    currHeadingRotSpeed += HeadingRotationAcceleration * Time.deltaTime;
                else currHeadingRotSpeed -= HeadingRotationAcceleration * Time.deltaTime;
            }
            else
            {
                if (HeadingSecondsToReachAim > HeadingSecondsNeededToStop * 0.65f)
                    currHeadingRotSpeed -= HeadingRotationAcceleration * Time.deltaTime;
                else currHeadingRotSpeed += HeadingRotationAcceleration * Time.deltaTime;
            }

            currHeadingRotSpeed = Mathf.Clamp(currHeadingRotSpeed, -HeadingRotationSpeed, HeadingRotationSpeed);
        }
        else
        {
            //turret is on target heading-wise
            currHeadingRotSpeed = 0f;
        }

        //apply the heading rotation to the turret
        if (currHeadingRotSpeed != 0f) turretHeadingParent.transform.Rotate(0f, currHeadingRotSpeed * Time.deltaTime, 0f);
        #endregion

        #region Animate Elevation Rotation
        float ElevationAimAngleDifference = AngleDifference(elevationAim, -turretElevationParent.transform.localEulerAngles.x);
        float CurrentElevation = NormalizeAngle(-turretElevationParent.transform.localEulerAngles.x);
        float ElevationSecondsToReachAim = ElevationAimAngleDifference / currElevationRotSpeed; if (ElevationSecondsToReachAim < 0f) ElevationSecondsToReachAim = 999999f; //if movement direction is opposite to the required direction, time to reach aim is infinity (a really big number)
        float ElevationSecondsNeededToStop = Mathf.Abs(currElevationRotSpeed) / ElevationRotationAcceleration;

        if (Mathf.Abs(ElevationAimAngleDifference) > 0.05f)
        {
            //determine relative pitch direction to the target (up or down)
            bool AimUp = CurrentElevation < elevationAim;

            //increase or decrease the rotation applied to elevation based on desired direction and how far off it is to the aim point.
            if (AimUp)
            {
                if (ElevationSecondsToReachAim > ElevationSecondsNeededToStop * 0.65f)
                    currElevationRotSpeed += HeadingRotationAcceleration * Time.deltaTime;
                else currElevationRotSpeed -= HeadingRotationAcceleration * Time.deltaTime;
            }
            else
            {
                if (ElevationSecondsToReachAim > ElevationSecondsNeededToStop * 0.65f)
                    currElevationRotSpeed -= HeadingRotationAcceleration * Time.deltaTime;
                else currElevationRotSpeed += HeadingRotationAcceleration * Time.deltaTime;
            }

            currElevationRotSpeed = Mathf.Clamp(currElevationRotSpeed, -ElevationRotationSpeed, ElevationRotationSpeed);
        }
        else
        {
            //turret is on target pitch-wise
            currElevationRotSpeed = 0f;
        }

        //apply the rotation to the turret. Speed is dampened the closer the rotation is to the target
        if (currElevationRotSpeed != 0f) turretElevationParent.transform.Rotate(-currElevationRotSpeed * Time.deltaTime, 0f, 0f);
        #endregion

        //We correct the euler angle of the rotation parent to conform to our standard of -180 to +180 degrees calculation (negative values are to the left of the forward axis)
        turretHeadingParent.transform.localEulerAngles = new Vector3(0f, NormalizeAngle(turretHeadingParent.transform.localEulerAngles.y), 0f);

        //Check if weapons can/should be fired
        if (autoFire)
        {
            if (TargetAquired)
            {
                FireWeapons(false);
            }
            else
            {
                StopContinousFire();
            }
        }
        else
        {
            StopContinousFire();
        }

        //SFX
        if (HeadingRotationSFX != null)
        {
            if (Mathf.Abs(currHeadingRotSpeed) > 0.05f)
            {
                float headingSpeedFactor = Mathf.Abs(currHeadingRotSpeed) / HeadingRotationSpeed;
                if (!HeadingRotationSFX.isPlaying) HeadingRotationSFX.Play();
                HeadingRotationSFX.pitch = Mathf.Lerp(HeadingRotationSFXInitialPitch * 0.8f, HeadingRotationSFXInitialPitch, headingSpeedFactor);
                HeadingRotationSFX.volume = Mathf.Lerp(0f, 1f, headingSpeedFactor);
            }
            else
            {
                HeadingRotationSFX.Stop();
            }
        }

        if (ElevationRotationSFX != null)
        {
            if (Mathf.Abs(currElevationRotSpeed) > 0.05f)
            {
                float elevationSpeedFactor = Mathf.Abs(currElevationRotSpeed) / HeadingRotationSpeed;
                if (!ElevationRotationSFX.isPlaying) ElevationRotationSFX.Play();
                ElevationRotationSFX.pitch = Mathf.Lerp(ElevationRotationSFXInitialPitch * 0.8f, ElevationRotationSFXInitialPitch, elevationSpeedFactor);
                ElevationRotationSFX.volume = Mathf.Lerp(0f, 1f, elevationSpeedFactor);
            }
            else
            {
                ElevationRotationSFX.Stop();
            }
        }

        //debug stuff
        DoDebugStuff();
    }


    //Gives a one-time fire command to the weapons
    //if overrideDelay is true the cooldown restriction will be ignored (individual weapon's cooldowns however are still respected as that logic is handled by the WeaponController script)
    //overrideDelay mainly used when the turret is under direct player control so the player can choose how quickly to fire the multiple weapons sequencially
    private void FireWeapons(bool overrideDelay = false)
    {
        if (weapons == null || weapons.Count == 0) return;

        if (Time.time >= lastFireTime + weapons[0].cooldown / weapons.Count || overrideDelay == true)
        {
            if (firingPattern == FiringPattern.Simultaneous)
            {
                foreach (var weapon in weapons)
                {
                    //if the weapon is a gattling type we should call the FireContinously instead so it can play it's spin animation
                    if (weapon.firingPattern == VattalusWeaponController.VattalusFiringPattern.Gatling)
                        weapon.StartContinousFire();
                    else
                        weapon.FireWeapon(target);
                }
            }
            else
            {
                //fire the next weapon
                if (weaponIndex >= weapons.Count) weaponIndex = 0;

                //if the weapon is a gattling type we should call the FireContinously instead so it can play it's spin animation
                if (weapons[weaponIndex].firingPattern == VattalusWeaponController.VattalusFiringPattern.Gatling)
                    weapons[weaponIndex].StartContinousFire(target);
                else
                    weapons[weaponIndex].FireWeapon(target);

                weaponIndex++;
            }

            lastFireTime = Time.time;
        }
    }

    private void StartContinousFire()
    {
        foreach (VattalusWeaponController weapon in weapons)
        {
            weapon.StartContinousFire();
        }
    }

    private void StopContinousFire()
    {
        foreach (VattalusWeaponController weapon in weapons)
        {
            weapon.StopContinousFire();
        }
    }

    private float AngleDifference(float angle1, float angle2)
    {
        float angleDeviation = NormalizeAngle(angle1 - angle2);
        return angleDeviation;
    }

    private float AbsAngleDifference(float angle1, float angle2)
    {
        return Mathf.Abs(AngleDifference(angle1, angle2));
    }

    private float NormalizeAngle(float angle)
    {
        if (angle <= -180f) angle += 360f;
        if (angle > 180f) angle -= 360f;

        return angle;
    }

    //Target prediction calculations are done using a coroutine so that it can be separate from the framerate for optimization purposes. Higher frequency = more cpu usage.
    IEnumerator TargetingCalculationsCoroutine(float frequency = 10f)
    {
        Vector3 targetPreviousPosition = Vector3.zero;
        Vector3 avgTargetMovement = Vector3.zero;

        while (Application.isPlaying)
        {
            if (turretMovement == TurretMovement.TargetTracking && target != null)
            {
                if (target != null)
                {
                    Vector3 targetPos = target.transform.position;
                    if (useTargetPrediction)
                    {
                        //calculate predicted target position
                        Vector3 targetMovement = (target.transform.position - targetPreviousPosition) * frequency;
                        avgTargetMovement = (avgTargetMovement + targetMovement) / 2f;

                        float projectileSpeed = 100f;
                        if (weapons != null && weapons.Count > 0) projectileSpeed = weapons[0].projectileSpeed;
                        float projectileSecondsToTarget = Vector3.Distance(turretElevationParent.transform.position, target.transform.position) / projectileSpeed;

                        //update the target position with the predicted target position
                        targetPos += avgTargetMovement * projectileSecondsToTarget;

                        //save current positions
                        targetPreviousPosition = target.transform.position;
                    }

                    //update heading/elevation target values based on target position/predicted position
                    var dirToTarget = (targetPos - turretElevationParent.transform.position).normalized;

                    //draw a line to the target position
                    if (debugMode) Debug.DrawLine(turretElevationParent.transform.position, turretElevationParent.transform.position + dirToTarget * Vector3.Distance(turretElevationParent.transform.position, targetPos));

                    //we project the target onto the turret's plane in order to simplify the calculations
                    Vector3 planeProjectedTargetPos = Vector3.ProjectOnPlane(targetPos, transform.up) + Vector3.Dot(turretElevationParent.transform.position, transform.up) * transform.up;
                    Vector3 dirToProjectedTargetPos = planeProjectedTargetPos - turretElevationParent.transform.position;

                    //Using the plane projected position we calculate heading and elevation angles
                    headingTarget = NormalizeAngle(Vector3.SignedAngle(transform.forward, planeProjectedTargetPos - turretElevationParent.transform.position, transform.up));
                    elevationTarget = -NormalizeAngle(Vector3.SignedAngle(dirToProjectedTargetPos, dirToTarget, turretHeadingParent.transform.right));
                }
                else
                {
                    if (resetOrientationWhenTargetLost)
                        SetTurretAim(0f, 0f, false);
                }
            }

            //Clamp the heading and elevation aim values to the angle constraints
            headingAim = Mathf.Clamp(headingTarget, HeadingAngleConstraints.x, HeadingAngleConstraints.y);
            elevationAim = Mathf.Clamp(elevationTarget, ElevationAngleConstraints.x, ElevationAngleConstraints.y);

            yield return new WaitForSeconds(1f / frequency);
        }
    }

    private void DoDebugStuff()
    {
        if (debugMode)
        {
            //moves the aim target indicator and changes the color according to whether it is reachable (red) and if if the target is aquired (green)
            if (debugTargetDirectionIndicator != null)
            {
                debugTargetDirectionIndicator.SetActive(true);
                debugTargetDirectionIndicator.transform.localEulerAngles = new Vector3(-elevationTarget, headingTarget, 0f);

                if (debugTargetDirectionIndicatorMR != null)
                {
                    Color debugTargetIndicatorColor = Color.white;
                    if (TargetAquired) debugTargetIndicatorColor = Color.green;
                    if (TargetOutsideHeadingConstraints || TargetOutsideElevationConstraints) debugTargetIndicatorColor = Color.red;
                    debugTargetDirectionIndicatorMR.material.SetColor("_Color", debugTargetIndicatorColor);
                }
            }

            //updates the indicator that shows turret heading coverage
            if (debugHeadingConstraintIndicator != null)
            {
                debugHeadingConstraintIndicator.gameObject.SetActive(true);
                if (HeadingLimited == false) debugHeadingConstraintIndicator.value = 1f;
                else
                {
                    debugHeadingConstraintIndicator.fillRect.localRotation = Quaternion.Euler(0f, 0f, HeadingAngleConstraints.x);
                    debugHeadingConstraintIndicator.value = (HeadingAngleConstraints.y - HeadingAngleConstraints.x) / 360f;
                }
            }

            //updates the indicator that shows turret elevation coverage
            if (debugElevationConstraintIndicator != null)
            {
                debugElevationConstraintIndicator.gameObject.SetActive(true);
                debugElevationConstraintIndicator.fillRect.localRotation = Quaternion.Euler(0f, 0f, ElevationAngleConstraints.x);
                debugElevationConstraintIndicator.value = (ElevationAngleConstraints.y - ElevationAngleConstraints.x) / 360f;

                //match elevation indicator rotation to turret rotation
                debugElevationConstraintIndicator.transform.localEulerAngles = new Vector3(0f, turretHeadingParent.transform.localEulerAngles.y - 90f, 0f);
            }
        }
        else
        {
            if (debugTargetDirectionIndicator != null) debugTargetDirectionIndicator.SetActive(false);
            if (debugHeadingConstraintIndicator != null) debugHeadingConstraintIndicator.gameObject.SetActive(false);
            if (debugElevationConstraintIndicator != null) debugElevationConstraintIndicator.gameObject.SetActive(false);
        }
    }
}