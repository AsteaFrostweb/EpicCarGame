using UnityEngine;

public class MinimapUIController : MonoBehaviour
{
    public RectTransform minimapCarIcon;
    public Transform carTransform;
    public Camera minimapCamera;
    [SerializeField] private RectTransform minimapRect;
    [SerializeField] private bool clampToMinimap = true;

    void Start()
    {
        if (minimapRect == null && minimapCarIcon != null)
        {
            minimapRect = minimapCarIcon.parent as RectTransform;
        }
    }

    private void LateUpdate()
    {
        if (carTransform == null && Player.mainPlayer != null)
        {
            carTransform = Player.mainPlayer.transform;
        }

        if (minimapCarIcon == null || carTransform == null || minimapCamera == null || minimapRect == null)
        {
            return;
        }

        Vector3 viewportPosition = minimapCamera.WorldToViewportPoint(carTransform.position);
        Vector2 normalizedPosition = new Vector2(viewportPosition.x, viewportPosition.y);

        if (clampToMinimap)
        {
            normalizedPosition.x = Mathf.Clamp01(normalizedPosition.x);
            normalizedPosition.y = Mathf.Clamp01(normalizedPosition.y);
        }

        Rect rect = minimapRect.rect;
        Vector2 minimapLocalPosition = new Vector2(
            rect.xMin + normalizedPosition.x * rect.width,
            rect.yMin + normalizedPosition.y * rect.height);

        minimapCarIcon.localPosition = new Vector3(
            minimapLocalPosition.x,
            minimapLocalPosition.y,
            minimapCarIcon.localPosition.z);
    }
}
