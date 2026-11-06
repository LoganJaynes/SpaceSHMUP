using UnityEngine;

/// <summary>Enemy_2: swoops sideways across the screen on an eased (sine) lerp between two points.</summary>
public class Enemy_2 : Enemy
{
    [Header("Enemy_2 Inscribed")]
    public float lifeTime = 6;
    public float sinEccentricity = 0.6f;

    Vector3 p0, p1;
    float birthTime;

    void Start()
    {
        float cw = bndCheck.camWidth, ch = bndCheck.camHeight;
        float edge = cw + bndCheck.radius;               // start and end just off-screen
        p0 = new Vector3(-edge, Random.Range(0f, ch), 0);
        p1 = new Vector3(edge, Random.Range(-ch, ch * 0.3f), 0);
        if (Random.value > 0.5f) { p0.x *= -1; p1.x *= -1; }

        birthTime = Time.time;
        pos = p0;
    }

    public override void Move()
    {
        float u = (Time.time - birthTime) / lifeTime;
        if (u > 1) { Destroy(gameObject); return; }

        u = u + sinEccentricity * Mathf.Sin(u * Mathf.PI * 2);
        pos = (1 - u) * p0 + u * p1;
    }
}
