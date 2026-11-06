using UnityEngine;

/// <summary>
/// Keeps a GameObject on screen (keepOnScreen) and/or reports which edge it has left.
/// Based on the BoundsCheck class from Bond, Chapter 31.
/// </summary>
public class BoundsCheck : MonoBehaviour
{
    public enum eType { center, inset, outset }

    [Header("Inscribed")]
    public eType boundsType = eType.center;
    public float radius = 1f;
    public bool keepOnScreen = true;

    [Header("Dynamic")]
    public bool isOnScreen = true;
    public float camWidth;
    public float camHeight;

    [HideInInspector] public bool offRight, offLeft, offUp, offDown;

    void Awake() { UpdateCameraSize(); }

    void UpdateCameraSize()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        camHeight = cam.orthographicSize;
        camWidth = camHeight * cam.aspect;
    }

    void LateUpdate()
    {
        UpdateCameraSize(); // handles the game window being resized

        float checkRadius = 0;
        if (boundsType == eType.inset) checkRadius = -radius;
        if (boundsType == eType.outset) checkRadius = radius;

        Vector3 pos = transform.position;
        isOnScreen = true;
        offRight = offLeft = offUp = offDown = false;

        if (pos.x > camWidth + checkRadius)  { pos.x = camWidth + checkRadius;  offRight = true; }
        if (pos.x < -camWidth - checkRadius) { pos.x = -camWidth - checkRadius; offLeft = true; }
        if (pos.y > camHeight + checkRadius) { pos.y = camHeight + checkRadius; offUp = true; }
        if (pos.y < -camHeight - checkRadius){ pos.y = -camHeight - checkRadius; offDown = true; }

        isOnScreen = !(offRight || offLeft || offUp || offDown);

        if (keepOnScreen && !isOnScreen)
        {
            transform.position = pos;
            isOnScreen = true;
            offRight = offLeft = offUp = offDown = false;
        }
    }
}
