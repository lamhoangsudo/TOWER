using UnityEngine;

public class WorldPointView : MonoBehaviour
{
    public static WorldPointView Instance;
    public void Awake()
    {
        if (Instance == null) Instance = this;
    }
}
