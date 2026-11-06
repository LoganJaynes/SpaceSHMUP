using UnityEngine;

public enum eWeaponType { none, blaster, spread, phaser, missile, shield }

[System.Serializable]
public class WeaponDefinition
{
    public eWeaponType type = eWeaponType.none;
    public string letter;
    public Color powerUpColor = Color.white;
    [Tooltip("Gun model shown at the hero's hardpoint (from the Starter Kit)")]
    public GameObject weaponModelPrefab;
    [Tooltip("Projectile material (from the Starter Kit)")]
    public Material projectileMaterial;
    public Color projectileColor = Color.white;          // fallback if no material
    public Vector3 projectileScale = new Vector3(0.4f, 1.2f, 0.4f);
    public float damageOnHit = 0;
    public float delayBetweenShots = 0;
    public float velocity = 20;
}

/// <summary>One gun slot on the Hero (a hardpoint). Hero.fireEvent calls Fire() on every slot.</summary>
public class Weapon : MonoBehaviour
{
    [SerializeField] eWeaponType _type = eWeaponType.none;
    public WeaponDefinition def;
    public float lastShotTime;
    GameObject modelInstance;

    public eWeaponType type
    {
        get { return _type; }
        set { SetType(value); }
    }

    public void SetType(eWeaponType wt)
    {
        _type = wt;
        if (modelInstance != null) { Destroy(modelInstance); modelInstance = null; }

        if (_type == eWeaponType.none || _type == eWeaponType.shield)
        {
            _type = eWeaponType.none;
            def = null;
            return;
        }

        def = Main.GetWeaponDefinition(_type);
        if (def.weaponModelPrefab != null)
        {
            modelInstance = Instantiate(def.weaponModelPrefab, transform);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
        }
        lastShotTime = 0;
    }

    public void Fire()
    {
        if (_type == eWeaponType.none || def == null) return;
        if (Time.time - lastShotTime < def.delayBetweenShots) return;

        Vector3 vel = Vector3.up * def.velocity;
        switch (_type)
        {
            case eWeaponType.blaster:
            case eWeaponType.missile:
                MakeProjectile(vel, 0f);
                break;
            case eWeaponType.spread:
                MakeProjectile(vel, 0f);
                MakeProjectile(Quaternion.AngleAxis(10f, Vector3.back) * vel, 0f);
                MakeProjectile(Quaternion.AngleAxis(-10f, Vector3.back) * vel, 0f);
                break;
            case eWeaponType.phaser:
                MakeProjectile(vel, +1f);
                MakeProjectile(vel, -1f);
                break;
        }
        lastShotTime = Time.time;
    }

    void MakeProjectile(Vector3 vel, float phaseSign)
    {
        GameObject go = Instantiate(Main.S.projectileHeroPrefab, transform.position, Quaternion.identity, Main.Anchor);
        go.GetComponent<Projectile>().InitHero(def, vel, phaseSign);
    }
}
