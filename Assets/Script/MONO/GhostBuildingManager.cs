using UnityEngine;
using UnityEngine.UIElements;
using static Enum;
using static Enum.PlacementMode;

public class GhostBuildingManager : MonoBehaviour
{
    [SerializeField] private GameObject ghostVisual;
    [SerializeField] private GameObject pointVisual;
    [Range(1f, 100f)]
    [SerializeField] private float lerpSpeed = 5f;
    private Vector3 buildPosition;
    private void Start()
    {
        Hide();
    }
    private void Update()
    {
        if(BuildingManager.Instance.placementMode != none)
        {
            if (!ghostVisual.activeSelf) Show();
            ghostVisual.transform.position = Vector3.Lerp(ghostVisual.transform.position, buildPosition, lerpSpeed * Time.deltaTime);
            pointVisual.transform.position = Vector3.Lerp(pointVisual.transform.position, buildPosition, lerpSpeed * Time.deltaTime);
        }
        else
        {
            if(ghostVisual.activeSelf) Hide();
        }
    }
    private void Hide()
    {
        ghostVisual.SetActive(false);
        pointVisual.SetActive(false);
    }
    private void Show()
    {
        ghostVisual.SetActive(true);
        pointVisual.SetActive(true);
    }
    public void SetPosition(Vector3 position)
    {
        if(buildPosition != position) buildPosition = position;
    }
}
