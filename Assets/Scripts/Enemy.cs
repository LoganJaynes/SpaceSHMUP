using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for all enemies. Used directly as "Enemy_0" (flies straight down).
/// Enemy_1..Enemy_4 override Move() for different flight patterns.
/// </summary>
public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> ALL = new List<Enemy>();

    [Header("Inscribed")]
    public float speed = 10f;
    public float health = 4f;
    public int score = 100;
    public float showDamageDuration = 0.1f;
    public float powerUpDropChance = 0.3f;
    public int collisionDamage = 1;       // shield levels the hero loses when rammed
    public float explosionScale = 1f;

    [Header("Dynamic")]
    public Color[] originalColors;
    public Material[] materials;
    public bool showingDamage = false;
    public float damageDoneTime;

    protected BoundsCheck bndCheck;
    bool dying;

    public Vector3 pos
    {
        get { return transform.position; }
        set { transform.position = value; }
    }

    void Awake()
    {
        bndCheck = GetComponent<BoundsCheck>();
        materials = Utils.GetAllMaterials(gameObject);
        originalColors = new Color[materials.Length];
        for (int i = 0; i < materials.Length; i++) originalColors[i] = materials[i].color;
    }

    void OnEnable() { ALL.Add(this); }
    void OnDisable() { ALL.Remove(this); }

    void Update()
    {
        Move();
        if (showingDamage && Time.time >= damageDoneTime) UnShowDamage();
        if (bndCheck != null && bndCheck.offDown) Destroy(gameObject);
    }

    public virtual void Move()
    {
        Vector3 tempPos = pos;
        tempPos.y -= speed * Time.deltaTime;
        pos = tempPos;
    }

    void OnTriggerEnter(Collider other)
    {
        Projectile p = other.GetComponentInParent<Projectile>();
        if (p == null || !p.fromHero) return;

        // Enemies above the top of the screen can't be shot yet
        if (bndCheck != null && !bndCheck.isOnScreen) { Destroy(p.gameObject); return; }

        ShowDamage();
        health -= p.damage;
        Destroy(p.gameObject);
        if (health <= 0) Explode(true);
    }

    /// <summary>Destroys this enemy. awardScore = true when the player shot it down.</summary>
    public void Explode(bool awardScore)
    {
        if (dying) return;
        dying = true;
        if (awardScore)
        {
            Main.AddScore(score);
            Main.ShipDestroyed(this);
        }
        Main.SpawnExplosion(pos, explosionScale);
        CameraShake.Add(0.12f * explosionScale);
        Destroy(gameObject);
    }

    void ShowDamage()
    {
        foreach (Material m in materials) m.color = Color.red;
        showingDamage = true;
        damageDoneTime = Time.time + showDamageDuration;
    }

    void UnShowDamage()
    {
        for (int i = 0; i < materials.Length; i++) materials[i].color = originalColors[i];
        showingDamage = false;
    }

    public static Enemy FindNearestOnScreen(Vector3 from)
    {
        Enemy best = null;
        float bestD = float.MaxValue;
        float camH = Camera.main != null ? Camera.main.orthographicSize : 40f;
        foreach (Enemy e in ALL)
        {
            if (e == null || e.dying) continue;
            if (e.transform.position.y > camH) continue; // not on screen yet
            float d = (e.transform.position - from).sqrMagnitude;
            if (d < bestD) { bestD = d; best = e; }
        }
        return best;
    }
}
