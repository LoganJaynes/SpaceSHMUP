using UnityEngine;

public class Hero : MonoBehaviour
{
    static public Hero S { get; private set; }

    [Header("Inscribed")]
    public float speed = 30;
    public float rollMult = -45;
    public float pitchMult = 30;
    public int maxShield = 4;
    public Transform model;      // container that banks/pitches (holds ship mesh + gun slots)
    public Shield shield;
    public Weapon[] weapons;

    [Header("Dynamic")]
    [SerializeField] int _shieldLevel = 3;

    public delegate void WeaponFireDelegate();
    public event WeaponFireDelegate fireEvent;

    public int shieldLevel
    {
        get { return _shieldLevel; }
        private set
        {
            _shieldLevel = Mathf.Min(value, maxShield);
            if (value < 0) Die();
        }
    }

    void Awake()
    {
        if (S == null) S = this;
        else Debug.LogError("Hero.Awake() - Attempted to assign second Hero.S!");
        foreach (Weapon w in weapons) fireEvent += w.Fire;
    }

    void Start()
    {
        foreach (Weapon w in weapons) w.SetType(eWeaponType.none);
        weapons[0].SetType(eWeaponType.blaster);
    }

    void OnDestroy() { if (S == this) S = null; }

    void Update()
    {
        float xAxis = InputHelper.Horizontal;
        float yAxis = InputHelper.Vertical;

        Vector3 pos = transform.position;
        pos.x += xAxis * speed * Time.deltaTime;
        pos.y += yAxis * speed * Time.deltaTime;
        transform.position = pos;

        // Bank and pitch only the ship model, so the collider and shield stay level
        if (model != null) model.localRotation = Quaternion.Euler(yAxis * pitchMult, xAxis * rollMult, 0);

        if (InputHelper.FireHeld && fireEvent != null) fireEvent();
    }

    GameObject lastTriggerGo;

    void OnTriggerEnter(Collider other)
    {
        // Enemy ship rams the hero: hero takes damage, enemy is destroyed
        Enemy e = other.GetComponentInParent<Enemy>();
        if (e != null)
        {
            if (e.gameObject == lastTriggerGo) return;
            lastTriggerGo = e.gameObject;
            TakeHit(e.collisionDamage);
            e.Explode(false);
            return;
        }

        // Enemy bullet hits the hero
        Projectile p = other.GetComponentInParent<Projectile>();
        if (p != null)
        {
            if (p.fromHero) return;
            TakeHit(Mathf.Max(1, Mathf.RoundToInt(p.damage)));
            Destroy(p.gameObject);
            return;
        }

        PowerUp pu = other.GetComponentInParent<PowerUp>();
        if (pu != null) AbsorbPowerUp(pu);
    }

    public void TakeHit(int dmg)
    {
        if (shield != null) shield.Flash(0.15f);
        CameraShake.Add(0.45f);
        shieldLevel -= dmg;
    }

    void Die()
    {
        Main.SpawnExplosion(transform.position, 3f);
        CameraShake.Add(1f);
        Destroy(gameObject);
        Main.HeroDied();
    }

    public void AbsorbPowerUp(PowerUp pu)
    {
        switch (pu.type)
        {
            case eWeaponType.shield:
                shieldLevel++;
                break;
            default:
                if (pu.type == weapons[0].type)
                {
                    Weapon w = GetEmptyWeaponSlot();
                    if (w != null) w.SetType(pu.type);
                    else Main.AddScore(250); // every slot already full: bonus points
                }
                else
                {
                    foreach (Weapon w in weapons) w.SetType(eWeaponType.none);
                    weapons[0].SetType(pu.type);
                }
                break;
        }
        pu.AbsorbedBy(gameObject);
    }

    Weapon GetEmptyWeaponSlot()
    {
        for (int i = 0; i < weapons.Length; i++)
            if (weapons[i].type == eWeaponType.none) return weapons[i];
        return null;
    }

    public int ActiveWeaponCount()
    {
        int n = 0;
        foreach (Weapon w in weapons) if (w.type != eWeaponType.none) n++;
        return n;
    }
}
