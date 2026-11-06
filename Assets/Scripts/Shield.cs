using UnityEngine;

/// <summary>
/// Shield visual. Shields.psd (from the Starter Kit) holds 5 frames side by side
/// (0 = empty ... 4 = full), so we just slide the texture offset to the current level.
/// </summary>
public class Shield : MonoBehaviour
{
    public float rotationsPerSecond = 0.1f;
    Material mat;
    int levelShown = -1;
    float flashEnd;

    void Awake() { mat = GetComponent<Renderer>().material; }

    public void Flash(float duration) { flashEnd = Time.time + duration; }

    void Update()
    {
        if (Hero.S == null) return;
        bool flash = Time.time < flashEnd;
        int lvl = Mathf.Clamp(Hero.S.shieldLevel, 0, 4);
        if (flash) lvl = Mathf.Max(lvl, 1);

        if (lvl != levelShown)
        {
            levelShown = lvl;
            mat.mainTextureOffset = new Vector2(0.2f * lvl, 0);
        }
        mat.color = flash ? new Color(1f, 0.35f, 0.35f, 1f) : Color.white;
        transform.rotation = Quaternion.Euler(0, 0, 360f * rotationsPerSecond * Time.time);
    }
}
