using UnityEngine;

/// <summary>Enemy_3: follows a quadratic Bezier curve: dives in from the top, loops, and leaves.</summary>
public class Enemy_3 : Enemy
{
    [Header("Enemy_3 Inscribed")]
    public float lifeTime = 6;

    Vector3[] points;
    float birthTime;

    void Start()
    {
        float cw = bndCheck.camWidth, ch = bndCheck.camHeight;
        points = new Vector3[3];
        points[0] = pos;                                                        // spawn point (above the screen)
        points[1] = new Vector3(Random.Range(-cw * 0.8f, cw * 0.8f), Random.Range(-ch * 0.8f, -ch * 0.2f), 0);
        points[2] = new Vector3(Random.Range(-cw * 0.8f, cw * 0.8f), ch + bndCheck.radius, 0);
        birthTime = Time.time;
    }

    public override void Move()
    {
        float u = (Time.time - birthTime) / lifeTime;
        if (u > 1) { Destroy(gameObject); return; }

        Vector3 p01 = (1 - u) * points[0] + u * points[1];
        Vector3 p12 = (1 - u) * points[1] + u * points[2];
        pos = (1 - u) * p01 + u * p12;
    }
}
