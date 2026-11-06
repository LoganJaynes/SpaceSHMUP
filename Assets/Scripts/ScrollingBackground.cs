using UnityEngine;

/// <summary>
/// One layer of the scrolling starfield (Chapter 32 "scrolling background").
/// Three of these at different depths/speeds give a parallax effect. The quad is
/// resized every frame to always cover the camera, and its texture offset scrolls.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class ScrollingBackground : MonoBehaviour
{
    [Tooltip("World units per second the stars move down the screen")]
    public float scrollSpeed = 5f;
    [Tooltip("How much this layer slides sideways as the hero moves (0 = none)")]
    public float heroParallax = 0.1f;
    [Tooltip("How many times the texture repeats across the quad's height")]
    public float tiling = 1f;
    public float coverMargin = 1.3f;

    Material mat;
    Camera cam;
    float offsetY;

    void Start()
    {
        cam = Camera.main;
        mat = GetComponent<MeshRenderer>().material; // instance so we can scroll it
    }

    void Update()
    {
        if (cam == null || mat == null) return;

        float viewH = cam.orthographicSize * 2f;
        float viewW = viewH * cam.aspect;
        float h = viewH * coverMargin;
        float w = viewW * coverMargin;
        transform.localScale = new Vector3(w, h, 1f);

        mat.mainTextureScale = new Vector2(w / h * tiling, tiling);

        offsetY = Mathf.Repeat(offsetY + scrollSpeed * tiling / h * Time.deltaTime, 1f);
        float offsetX = 0f;
        if (Hero.S != null) offsetX = Hero.S.transform.position.x * heroParallax * tiling / h;
        mat.mainTextureOffset = new Vector2(offsetX, offsetY);
    }
}
