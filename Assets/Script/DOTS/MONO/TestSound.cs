using FMOD.Studio;
using FMODUnity;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
public class TestSound : MonoBehaviour
{
    private float time;
    public float volume;
    public float pitch;
    [field: SerializeField] public EventReference eventSound {  get; private set; }
    private void Update()
    {
        time -= Time.deltaTime;
        if(time <= 0)
        {
            FmodSoundManager.PlayOneShotFireSound(eventSound.Guid, pitch, volume, (float3)gameObject.transform.position);
            time = 0.13f;
        }
    }

}
