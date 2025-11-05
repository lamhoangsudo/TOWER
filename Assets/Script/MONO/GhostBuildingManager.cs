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
    private Vector3 ghostPosition;
    private Vector3 pointPosition;
    private Vector2 buildScale;
    private void Start()
    {
        Hide();
    }
    private void Update()
    {
        if(BuildingManager.Instance.placementMode != none)
        {
            if (!ghostVisual.activeSelf) Show();
            ghostVisual.transform.position = Vector3.Lerp(ghostVisual.transform.position, ghostPosition, lerpSpeed * Time.deltaTime);
            ghostVisual.transform.localScale = new Vector3(buildScale.x * 5f, ghostVisual.transform.localScale.y, buildScale.y * 5f);
            ghostVisual.transform.rotation = Quaternion.Euler(0f, BuildingManager.Instance.GetBuildRotationDirectionValue(), 0f);
            pointVisual.transform.position = Vector3.Lerp(pointVisual.transform.position, pointPosition, lerpSpeed  * 5f * Time.deltaTime);
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
    public void SetPositionAndScale(Vector3 ghostPosition, Vector2 scale, Vector3 pointPosition, bool isCanBuildable)
    {
        if (!isCanBuildable) return;
        if (this.ghostPosition != ghostPosition) this.ghostPosition = ghostPosition;
        if(buildScale != scale) buildScale = scale;
        if(this.pointPosition != pointPosition) this.pointPosition = pointPosition;
    }
}
