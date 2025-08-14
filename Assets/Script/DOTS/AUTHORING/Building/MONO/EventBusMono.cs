using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;

public class EventBusMono : MonoBehaviour
{
    public static EventBusMono Instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this instance across scenes
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }

    // Update is called once per frame
    public void Update()
    {
        
    }
}
