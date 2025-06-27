using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VattalusMuzzleFlash : MonoBehaviour
{
    public GameObject muzzleFlashObject;

    //with each triggering, we can randomize the size of the muzzle flash using the following parameters
    [Tooltip("Randomization factor of the muzzle flash scale at each animation start (0: no variation, 1: means +-1 variation)")]
    public float scaleVariance = 0f;
    [Tooltip("Randomization factor of the muzzle flash length at each animation start (0: no variation, 1: means +-1 variation)")]
    public float lengthVariance = 0f;
    [Tooltip("Duration (in seconds) of the muzzle flash animation")]
    public float muzzleFlashDuration = 0.05f;

    [Tooltip("Will destroy GO when it is disabled. Set to false if you wish to use this in a pooling system to enable GO recycling")]
    public bool destroyOnDisable = false;

    private float elapsedTime;
    private float startScale;
    private float endScale;
    private float startLength;
    private float endLength;

    private Vector3 scaleInitial;

    void Awake()
    {
        if (muzzleFlashObject != null)
        {
            scaleInitial = muzzleFlashObject.transform.localScale;
        }
    }

    void OnEnable()
    {
        elapsedTime = 0f;

        if (muzzleFlashObject != null)
        {
            //randomize rotation
            muzzleFlashObject.gameObject.SetActive(true);
            muzzleFlashObject.transform.localEulerAngles = new Vector3(muzzleFlashObject.transform.localEulerAngles.x, muzzleFlashObject.transform.localEulerAngles.y, UnityEngine.Random.Range(-180f, 180f));

            //randomize muzzle flash scale
            startScale = scaleInitial.x + UnityEngine.Random.Range(-1f, 1f) * scaleVariance / 2f;
            endScale = startScale * UnityEngine.Random.Range(0.6f, 0.8f);

            startLength = scaleInitial.y + UnityEngine.Random.Range(-1f, 1f) * lengthVariance / 2f;
            endLength = startLength * UnityEngine.Random.Range(1.75f, 3f);

            //set the randomize muzzle flash scale to the gameobject
            muzzleFlashObject.transform.localScale = new Vector3(startScale, startScale, startLength);
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (destroyOnDisable) Destroy(gameObject);
    }

    void Update()
    {
        if (elapsedTime > muzzleFlashDuration)
        {
            gameObject.SetActive(false);
            return;
        }

        muzzleFlashObject.transform.localScale = new Vector3(Mathf.Lerp(startScale, endScale, elapsedTime / muzzleFlashDuration), Mathf.Lerp(startScale, endScale, elapsedTime / muzzleFlashDuration), Mathf.Lerp(startLength, endLength, elapsedTime / muzzleFlashDuration));

        elapsedTime += Time.deltaTime;
    }
}
