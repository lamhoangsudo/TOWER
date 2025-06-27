using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VattalusBarrelAnimator : MonoBehaviour
{
    //This script is attached to the barrel of a weapons and it handles the animation/effects being played when the the fire command is received.
    [Header("References")]
    [Tooltip("(Optional). The barrel base is the model the barrel tip is attached to. Used for animation")]
    public GameObject barrelBaseObject;
    [Tooltip("Reference to barrel tip Gameobject. Used for animation")]
    public GameObject barrelTipObject;
    [Tooltip("Reference to muzzle flash effect. Is enabled when fire animation starts playing")]
    public GameObject muzzleFlashObject;

    [Header("AnimationParameters")]
    [Tooltip("Duration (in seconds) of the firing animation (both sliding and rotating). Also controls spin up/down time for gatling-type weapons")]
    public float animationDuration = 0.5f;
    [Tooltip("Distance of the sliding animation applied to the base. Animation curve given by 'slideCurve' parameter")]
    public float baseSlideDistance = 0f;
    [Tooltip("Distance of the sliding animation applied to the tip. Animation curve given by 'slideCurve' parameter")]
    public float tipSlideAmountDistance = 0f;
    [Tooltip("Angle of the rotation animation applied to the tip. Animation curve given by 'rotationCurve' parameter. IMPORTANT: Should be 0 for gatling-type barrels (set in WeaponController script)")]
    public float tipRotateDegrees = 0f;
    [Tooltip("Sliding animation curve")]
    public AnimationCurve slideCurve;
    [Tooltip("Rotating animation curve")]
    public AnimationCurve rotationCurve;

    private float lastFireTime = 0f; //we use this timestamp to figure out animation progress
    private bool animationPlaying = false; //check if animation playing for various reasons

    //variables related to SFX
    [Tooltip("Reference to the AudioSource that plays the gattling spin sound. Change pitch directly on the AudioSource Component")]
    public AudioSource gatlingSpinAudio;
    private float gatlingSpinAudioInitialPitch;

    private float TimeSinceLastFire
    {
        get { return Time.time - lastFireTime; }
    }
    private Vector3 tipInitialPosition = Vector3.zero;
    private Vector3 tipInitialRotation = Vector3.zero;
    private float tipRotationAtFire = 0f;

    void Start()
    {
        if (barrelTipObject != null)
        {
            tipInitialPosition = barrelTipObject.transform.localPosition;
            tipInitialRotation = barrelTipObject.transform.localEulerAngles;
        }

        if (muzzleFlashObject != null)
        {
            muzzleFlashObject.SetActive(false);
        }

        if (gatlingSpinAudio != null) gatlingSpinAudioInitialPitch = gatlingSpinAudio.pitch;
    }

    public void PlayFireEffects()
    {
        animationPlaying = true;

        lastFireTime = Time.time;
        tipRotationAtFire = barrelTipObject.transform.localEulerAngles.y;

        //enable muzzle flash (we force a state reset on the GO because it might contain scripts with OnEnable logic)
        if (muzzleFlashObject != null)
        {
            muzzleFlashObject.SetActive(false);
            muzzleFlashObject.SetActive(true);
        }
    }

    public void PlayGatlingSpinEffect(float gatlingRotation, float gatlingRotationFactor)
    {
        if (barrelTipObject != null) barrelTipObject.transform.Rotate(0f, gatlingRotation * Time.deltaTime, 0f);
        if (gatlingSpinAudio != null)
        {
            if (gatlingRotationFactor > 0.05f)
            {
                if (!gatlingSpinAudio.isPlaying) gatlingSpinAudio.Play();
                gatlingSpinAudio.pitch = gatlingSpinAudioInitialPitch * gatlingRotationFactor;
                gatlingSpinAudio.volume = Mathf.Lerp(0f, 1f, gatlingRotationFactor);
            }
            else
            {
                gatlingSpinAudio.Stop();
            }
        }
    }

    void Update()
    {
        //play animation if needed
        if (animationPlaying && TimeSinceLastFire < animationDuration)
        {
            UpdateAnimation(TimeSinceLastFire / animationDuration);
        }
        else
        {
            //this only triggers on the first frame AFTER the animation duration expired.
            if (animationPlaying)
            {
                UpdateAnimation(1f);
                animationPlaying = false;
            }
        }
    }

    //sets the barrel animation according to the normalized animationProgress parameter (clamped to 0:1)
    private void UpdateAnimation(float animationProgress)
    {
        animationProgress = Mathf.Clamp01(animationProgress);

        //set base position
        if (barrelBaseObject != null) barrelBaseObject.transform.SetLocalPositionAndRotation(new Vector3(0f, 0f, -slideCurve.Evaluate(animationProgress) * baseSlideDistance), barrelBaseObject.transform.localRotation);

        //set tip position/rotation
        if (barrelTipObject != null)
        {
            //animate tip
            float positionValue = tipInitialPosition.y + slideCurve.Evaluate(animationProgress) * tipSlideAmountDistance;
            barrelTipObject.transform.localPosition = new Vector3(tipInitialPosition.x, positionValue, tipInitialPosition.z);

            if (tipRotateDegrees != 0f)
            {
                float rotationValue = Mathf.Lerp(tipRotationAtFire, tipRotationAtFire + tipRotateDegrees, rotationCurve.Evaluate(animationProgress));
                barrelTipObject.transform.localRotation = Quaternion.Euler(new Vector3(tipInitialRotation.x, rotationValue, tipInitialRotation.z));
            }
        }
    }
}
