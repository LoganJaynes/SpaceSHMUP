using UnityEngine;

/// <summary>Used for both the hero's shots and enemy shots.</summary>
public class Projectile : MonoBehaviour
{
    public bool fromHero = true;
    public eWeaponType type = eWeaponType.blaster;
    public float damage = 1f;
    public Vector3 velocity;

    [Header("Phaser")]
    public float waveWidth = 2.5f;
    public float waveFrequency = 2f; // cycles per second

    [Header("Missile")]
    public float turnRateDegPerSec = 200f;

    float waveSign, age;
    Vector3 basePos;
    BoundsCheck bndCheck;
    Renderer rend;
    Enemy target;

    void Awake()
    {
        bndCheck = GetComponent<BoundsCheck>();
        rend = GetComponent<Renderer>();
    }

    public void InitHero(WeaponDefinition def, Vector3 vel, float phaseSign)
    {
        fromHero = true; type = def.type; damage = def.damageOnHit;
        velocity = vel; waveSign = phaseSign; basePos = transform.position;
        transform.localScale = def.projectileScale;
        if (def.projectileMaterial != null) rend.sharedMaterial = def.projectileMaterial;
        else rend.material.color = def.projectileColor;
        Orient();
    }

    public void InitEnemy(Vector3 vel, float dmg)
    {
        fromHero = false; type = eWeaponType.none; damage = dmg;
        velocity = vel; basePos = transform.position;
        Orient();
    }

    void Orient()
    {
        if (velocity.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.FromToRotation(Vector3.up, velocity.normalized);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;

        if (type == eWeaponType.missile && fromHero) SteerTowardEnemy(dt);

        basePos += velocity * dt;
        Vector3 p = basePos;
        if (type == eWeaponType.phaser && waveSign != 0f)
            p.x += Mathf.Sin(age * waveFrequency * Mathf.PI * 2f) * waveWidth * waveSign;
        transform.position = p;

        if (bndCheck != null && !bndCheck.isOnScreen) Destroy(gameObject);
    }

    void SteerTowardEnemy(float dt)
    {
        if (target == null) target = Enemy.FindNearestOnScreen(basePos);
        if (target == null) return;
        Vector3 want = target.transform.position - basePos;
        want.z = 0;
        if (want.sqrMagnitude < 0.01f) return;
        velocity = Vector3.RotateTowards(velocity, want.normalized * velocity.magnitude,
                                         turnRateDegPerSec * Mathf.Deg2Rad * dt, 0f);
        Orient();
    }
}
