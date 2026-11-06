using UnityEngine;

/// <summary>
/// Enemy_4 (the "boss"): glides to a spot in the upper half of the screen, hovers while
/// firing aimed shots at the hero, then retreats downward. Tougher and worth more points.
/// </summary>
public class Enemy_4 : Enemy
{
    enum State { Entering, Holding, Leaving }

    [Header("Enemy_4 Inscribed")]
    public float enterTime = 2f;
    public float holdTime = 6f;
    public float shotInterval = 1.1f;
    public float shotSpeed = 28f;
    public float shotDamage = 1f;

    State state = State.Entering;
    Vector3 p0, p1;
    float stateStart, nextShot;

    void Start()
    {
        float cw = bndCheck.camWidth, ch = bndCheck.camHeight;
        p0 = pos;
        p1 = new Vector3(Random.Range(-cw * 0.6f, cw * 0.6f), Random.Range(ch * 0.3f, ch * 0.75f), 0);
        stateStart = Time.time;
    }

    public override void Move()
    {
        float t = Time.time - stateStart;
        switch (state)
        {
            case State.Entering:
                float u = Mathf.Clamp01(t / enterTime);
                u = 1f - Mathf.Pow(1f - u, 2f);              // ease out
                pos = Vector3.Lerp(p0, p1, u);
                if (t >= enterTime) { state = State.Holding; stateStart = Time.time; nextShot = Time.time + 0.4f; }
                break;

            case State.Holding:
                Vector3 hover = p1;
                hover.x += Mathf.Sin(Time.time * 1.5f) * 6f;  // sway while shooting
                pos = hover;
                if (Time.time >= nextShot) { Shoot(); nextShot = Time.time + shotInterval; }
                if (t >= holdTime) { state = State.Leaving; stateStart = Time.time; }
                break;

            case State.Leaving:
                base.Move();
                break;
        }
    }

    void Shoot()
    {
        if (Main.S == null || Main.S.projectileEnemyPrefab == null) return;
        Vector3 dir = Vector3.down;
        if (Hero.S != null) dir = (Hero.S.transform.position - pos).normalized;
        dir.z = 0;

        GameObject go = Instantiate(Main.S.projectileEnemyPrefab, pos, Quaternion.identity, Main.Anchor);
        go.GetComponent<Projectile>().InitEnemy(dir * shotSpeed, shotDamage);
    }
}
