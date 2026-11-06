using UnityEngine;

public class PowerUp : MonoBehaviour
{
    [Header("Inscribed")]
    public Vector2 rotMinMax = new Vector2(15, 90);
    public Vector2 driftMinMax = new Vector2(2, 8);   // units / second
    public float lifeTime = 8f;
    public float fadeTime = 4f;
    public GameObject cube;
    public TextMesh letter;

    [Header("Dynamic")]
    public eWeaponType type = eWeaponType.blaster;

    Vector3 rotPerSecond, drift;
    float birthTime;
    BoundsCheck bndCheck;
    Renderer cubeRend;

    void Awake()
    {
        bndCheck = GetComponent<BoundsCheck>();
        if (cube != null) cubeRend = cube.GetComponent<Renderer>();
        rotPerSecond = new Vector3(Random.Range(rotMinMax.x, rotMinMax.y), Random.Range(rotMinMax.x, rotMinMax.y), Random.Range(rotMinMax.x, rotMinMax.y));
        Vector2 dir = Random.insideUnitCircle.normalized;
        drift = new Vector3(dir.x, dir.y, 0) * Random.Range(driftMinMax.x, driftMinMax.y);
        birthTime = Time.time;
    }

    public void SetType(eWeaponType wt)
    {
        type = wt;
        WeaponDefinition def = Main.GetWeaponDefinition(wt);
        if (cubeRend != null) cubeRend.material.color = def.powerUpColor;
        if (letter != null) letter.text = def.letter;
    }

    void Update()
    {
        if (cube != null) cube.transform.rotation = Quaternion.Euler(rotPerSecond * (Time.time - birthTime));

        Vector3 pos = transform.position;
        pos += drift * Time.deltaTime;
        if (bndCheck != null)
        {
            float r = bndCheck.radius;
            if (pos.x > bndCheck.camWidth - r) drift.x = -Mathf.Abs(drift.x);
            if (pos.x < -bndCheck.camWidth + r) drift.x = Mathf.Abs(drift.x);
            if (pos.y > bndCheck.camHeight - r) drift.y = -Mathf.Abs(drift.y);
            if (pos.y < -bndCheck.camHeight + r) drift.y = Mathf.Abs(drift.y);
        }
        transform.position = pos;

        // Blink and shrink before disappearing
        float u = (Time.time - (birthTime + lifeTime)) / fadeTime;
        if (u >= 1f) { Destroy(gameObject); return; }
        if (u > 0f && cube != null)
        {
            bool on = Mathf.Sin(Time.time * 20f) > -0.3f;
            if (cubeRend != null) cubeRend.enabled = on;
            if (letter != null) letter.gameObject.SetActive(on);
        }
    }

    public void AbsorbedBy(GameObject target) { Destroy(gameObject); }
}
