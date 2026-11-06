using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Game manager: spawns enemies, drops power-ups, keeps score, restarts the game.</summary>
public class Main : MonoBehaviour
{
    static public Main S { get; private set; }
    static Dictionary<eWeaponType, WeaponDefinition> WEAP_DICT;
    static Transform _anchor;
    public static int HighScore;
    const string HIGHSCORE_KEY = "SHMUP_HighScore";

    [Header("Inscribed")]
    public bool spawnEnemies = true;
    public GameObject[] prefabEnemies;                 // Enemy_0 ... Enemy_4
    public float enemySpawnPerSecond = 0.6f;
    public float enemyInsetDefault = 1.5f;
    public float gameRestartDelay = 5f;
    public GameObject prefabPowerUp;
    public GameObject explosionPrefab;
    public GameObject projectileHeroPrefab;
    public GameObject projectileEnemyPrefab;
    public WeaponDefinition[] weaponDefinitions;
    public eWeaponType[] powerUpFrequency = new eWeaponType[]
    {
        eWeaponType.blaster, eWeaponType.blaster,
        eWeaponType.spread, eWeaponType.phaser, eWeaponType.missile,
        eWeaponType.shield, eWeaponType.shield
    };

    [Header("Dynamic")]
    public int score;
    public bool gameOver;
    public float elapsed;

    public int Level { get { return 1 + Mathf.FloorToInt(elapsed / 20f); } }

    public static Transform Anchor
    {
        get
        {
            if (_anchor == null) _anchor = new GameObject("_ProjectileAnchor").transform;
            return _anchor;
        }
    }

    void Awake()
    {
        S = this;
        HighScore = PlayerPrefs.GetInt(HIGHSCORE_KEY, 0);

        if (weaponDefinitions == null || weaponDefinitions.Length == 0)
            weaponDefinitions = DefaultWeaponDefinitions();

        WEAP_DICT = new Dictionary<eWeaponType, WeaponDefinition>();
        foreach (WeaponDefinition def in weaponDefinitions) WEAP_DICT[def.type] = def;
    }

    void Start()
    {
        Invoke(nameof(SpawnEnemy), 1.5f);
    }

    void Update()
    {
        if (!gameOver) elapsed += Time.deltaTime;
        if (gameOver && InputHelper.RestartPressed) Restart();
    }

    float CurrentSpawnRate()
    {
        // The game gets steadily busier: +25% per level, capped at 3x
        return Mathf.Min(enemySpawnPerSecond * (1f + 0.25f * (Level - 1)), enemySpawnPerSecond * 3f);
    }

    public void SpawnEnemy()
    {
        if (gameOver) return;
        if (spawnEnemies && prefabEnemies != null && prefabEnemies.Length > 0)
        {
            int ndx = Random.Range(0, prefabEnemies.Length);
            GameObject go = Instantiate(prefabEnemies[ndx]);

            float enemyInset = enemyInsetDefault;
            BoundsCheck eb = go.GetComponent<BoundsCheck>();
            if (eb != null) enemyInset = Mathf.Abs(eb.radius);

            Camera cam = Camera.main;
            float camH = cam.orthographicSize;
            float camW = camH * cam.aspect;

            Vector3 pos = Vector3.zero;
            float xMin = -camW + enemyInset;
            float xMax = camW - enemyInset;
            pos.x = Random.Range(xMin, xMax);
            pos.y = camH + enemyInset;
            go.transform.position = pos;
        }
        Invoke(nameof(SpawnEnemy), 1f / CurrentSpawnRate());
    }

    // ----- static helpers used by other scripts -----

    public static WeaponDefinition GetWeaponDefinition(eWeaponType wt)
    {
        if (WEAP_DICT != null && WEAP_DICT.ContainsKey(wt)) return WEAP_DICT[wt];
        return new WeaponDefinition();   // type == none
    }

    public static void AddScore(int amount)
    {
        if (S == null) return;
        S.score += amount;
        if (S.score > HighScore) HighScore = S.score;
    }

    public static void SpawnExplosion(Vector3 pos, float scale)
    {
        if (S == null || S.explosionPrefab == null) return;
        GameObject go = Instantiate(S.explosionPrefab, pos, Quaternion.identity);
        go.transform.localScale = Vector3.one * scale;
        Destroy(go, 3f);
    }

    public static void ShipDestroyed(Enemy e)
    {
        if (S == null || S.prefabPowerUp == null || S.powerUpFrequency.Length == 0) return;
        if (Random.value > e.powerUpDropChance) return;

        eWeaponType puType = S.powerUpFrequency[Random.Range(0, S.powerUpFrequency.Length)];
        GameObject go = Instantiate(S.prefabPowerUp);
        go.GetComponent<PowerUp>().SetType(puType);
        go.transform.position = e.transform.position;
    }

    public static void HeroDied()
    {
        if (S == null) return;
        S.gameOver = true;
        PlayerPrefs.SetInt(HIGHSCORE_KEY, HighScore);
        PlayerPrefs.Save();
        S.Invoke(nameof(Restart), S.gameRestartDelay);
    }

    public void Restart()
    {
        CancelInvoke();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Used when the weapon list hasn't been filled in the Inspector
    public static WeaponDefinition[] DefaultWeaponDefinitions()
    {
        return new WeaponDefinition[]
        {
            new WeaponDefinition { type = eWeaponType.blaster, letter = "B",
                powerUpColor = new Color(0.25f, 0.6f, 1f), projectileColor = new Color(0f, 0.95f, 1f),
                projectileScale = new Vector3(0.4f, 1.3f, 0.4f),
                damageOnHit = 1, delayBetweenShots = 0.2f, velocity = 60 },

            new WeaponDefinition { type = eWeaponType.spread, letter = "S",
                powerUpColor = new Color(1f, 0.45f, 0.2f), projectileColor = new Color(1f, 0.6f, 0.2f),
                projectileScale = new Vector3(0.35f, 1.0f, 0.35f),
                damageOnHit = 1, delayBetweenShots = 0.35f, velocity = 50 },

            new WeaponDefinition { type = eWeaponType.phaser, letter = "P",
                powerUpColor = new Color(0.3f, 1f, 0.4f), projectileColor = new Color(0.4f, 1f, 0.5f),
                projectileScale = new Vector3(0.45f, 1.0f, 0.45f),
                damageOnHit = 2, delayBetweenShots = 0.3f, velocity = 40 },

            new WeaponDefinition { type = eWeaponType.missile, letter = "M",
                powerUpColor = new Color(1f, 0.85f, 0.2f), projectileColor = new Color(1f, 0.9f, 0.3f),
                projectileScale = new Vector3(0.6f, 1.6f, 0.6f),
                damageOnHit = 4, delayBetweenShots = 0.8f, velocity = 32 },

            new WeaponDefinition { type = eWeaponType.shield, letter = "+",
                powerUpColor = new Color(0.4f, 0.9f, 1f) },
        };
    }
}
