using UnityEngine;
using static Enum;
using static Enum.BuidingState;

public class GhostBuildingManager : MonoBehaviour
{
    [SerializeField] private GameObject visual;
    private void Start()
    {
        Hide();
    }
    private void Update()
    {
        if(BuildingManager.Instance.buidingState != none)
        {
            if (!visual.activeSelf) Show();
        }
        else
        {
            if(visual.activeSelf) Hide();
        }
    }
    private void Hide()
    {
        visual.SetActive(false);
    }
    private void Show()
    {
        visual.SetActive(true);
    }
}
