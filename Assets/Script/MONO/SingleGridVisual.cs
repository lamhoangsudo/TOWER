using UnityEngine;

public class SingleGridVisual : MonoBehaviour
{
    [SerializeField] private Color validBuildPointColor;
    [SerializeField] private Color unvalidBuidPointColor;
    [SerializeField] private SpriteRenderer spriteRenderer;
    public Enum.PointBuidStatus pointStatus;
    private void Update()
    {
        UpdateColor();
    }
    private void UpdateColor()
    {
        switch (pointStatus)
        {
            case Enum.PointBuidStatus.unvalidPointBuid:
                spriteRenderer.color = unvalidBuidPointColor;
                break;
            case Enum.PointBuidStatus.validPointBuid:
                spriteRenderer.color = validBuildPointColor;
                break;
            case Enum.PointBuidStatus.none:
            default:
                spriteRenderer.color = Color.white;
                break;
        }
    }
}
