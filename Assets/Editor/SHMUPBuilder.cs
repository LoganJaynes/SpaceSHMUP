using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click project setup. Menu:  SHMUP > Build Everything
/// Builds all prefabs from the Starter Kit models, creates the materials, and assembles
/// Assets/Scenes/_Scene_0.unity (camera, light, scrolling starfield, Main, Hero).
/// Safe to run again at any time; it rebuilds the generated files.
/// </summary>
[InitializeOnLoad]
public static class SHMUPBuilder
{
    const string PKG = "Assets/Assets by Peter Burroughs";
    const string SCENE_PATH = "Assets/Scenes/_Scene_0.unity";

    // World units per FBX unit. The hero is 528.8 FBX units wide and we want ~6 world units;
    // enemies use the book's 1.5x scale on top of the 0.01 import scale.
    const float HERO_K = 6f / 528.8f;
    const float ENEMY_K = 0.015f;

    static SHMUPBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(SCENE_PATH)) return;
            if (SessionState.GetBool("SHMUP_Asked", false)) return;
            SessionState.SetBool("SHMUP_Asked", true);
            if (EditorUtility.DisplayDialog("Space SHMUP",
                "The game scene has not been built yet.\n\nBuild the prefabs, materials and scene now?\n(You can also do this later from the SHMUP menu.)",
                "Build now", "Later"))
                BuildEverything();
        };
    }

    [MenuItem("SHMUP/Build Everything (prefabs, materials, scene)")]
    public static void BuildEverything()
    {
        if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Space SHMUP", "Stop Play mode first.", "OK"); return; }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PKG + "/Models/heroShip.fbx") == null)
        {
            EditorUtility.DisplayDialog("Space SHMUP",
                "Starter Kit assets not found.\nImport IGDPD3_31_Space_SHMUP_Starter_Kit.unitypackage first (Assets > Import Package > Custom Package).", "OK");
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("Space SHMUP", "Preparing folders...", 0.05f);
            foreach (string f in new[] { "Assets/Prefabs", "Assets/Scenes", "Assets/Materials", "Assets/Textures" })
                Directory.CreateDirectory(f);
            AssetDatabase.Refresh();

            // Work in a fresh scene so temporary objects never touch the user's scene
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EditorUtility.DisplayProgressBar("Space SHMUP", "Textures and materials...", 0.2f);
            SetRepeat("Assets/_Materials/Space.png");
            SetRepeat("Assets/_Materials/Space_Transparent.png");
            Texture2D glow = MakeGlowTexture();

            Shader unlit = Shader.Find("ProtoTools/UnlitAlpha");
            Shader particle = Shader.Find("SHMUP/ParticleAdditive");
            if (unlit == null || particle == null)
            {
                EditorUtility.DisplayDialog("Space SHMUP", "Shaders not imported yet. Wait for Unity to finish importing, then run SHMUP > Build Everything again.", "OK");
                return;
            }

            Material matSpace  = MakeMat("Mat_Space", unlit, LoadTex("Assets/_Materials/Space.png"));
            Material matStars1 = MakeMat("Mat_Stars1", unlit, LoadTex("Assets/_Materials/Space_Transparent.png"));
            Material matStars2 = MakeMat("Mat_Stars2", unlit, LoadTex("Assets/_Materials/Space_Transparent.png"));
            Material matShield = MakeMat("Mat_Shield", unlit, LoadTex("Assets/_Materials/Shields.psd"));
            matShield.mainTextureScale = new Vector2(0.2f, 1f);     // 5 frames in a row
            EditorUtility.SetDirty(matShield);
            Material matPower  = MakeMat("Mat_PowerUp", unlit, LoadTex("Assets/_Materials/PowerUp.psd"));
            Material matShot   = MakeMat("Mat_EnemyShot", unlit, null);
            matShot.color = new Color(1f, 0.25f, 0.2f, 1f);
            EditorUtility.SetDirty(matShot);
            Material matBoom   = MakeMat("Mat_Explosion", particle, glow);

            EditorUtility.DisplayProgressBar("Space SHMUP", "Building prefabs...", 0.45f);
            GameObject[] weaponModels =
            {
                BuildWeaponModel("WeaponModel_Blaster", "weapon_Blaster", 39.7f),
                BuildWeaponModel("WeaponModel_Spread", "weapon_SpreadShot", 50.2f),
                BuildWeaponModel("WeaponModel_Phaser", "weapon_Phaser", 39.7f),
                BuildWeaponModel("WeaponModel_Missile", "weapon_MissileLauncher", 60.1f),
            };

            GameObject heroPrefab = BuildHero(matShield);
            GameObject[] enemies = new GameObject[5];
            for (int i = 0; i < 5; i++) enemies[i] = BuildEnemy(i);

            GameObject projHero = BuildHeroProjectile();
            GameObject projEnemy = BuildEnemyProjectile(matShot);
            GameObject powerUp = BuildPowerUp(matPower);
            GameObject explosion = BuildExplosion(matBoom);

            EditorUtility.DisplayProgressBar("Space SHMUP", "Assembling scene...", 0.8f);
            BuildScene(heroPrefab, enemies, projHero, projEnemy, powerUp, explosion, weaponModels,
                       matSpace, matStars1, matStars2);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Space SHMUP", "Done! Open Assets/Scenes/_Scene_0 and press Play.\n\nTip: set the Game view to a portrait aspect (e.g. 3:4) like in the book.", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ---------------------------------------------------------------- helpers

    static GameObject LoadFbx(string name)
    {
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(PKG + "/Models/" + name + ".fbx");
        if (go == null) throw new System.Exception("Missing model: " + name);
        return go;
    }

    static Texture2D LoadTex(string path) { return AssetDatabase.LoadAssetAtPath<Texture2D>(path); }

    static void SetRepeat(string path)
    {
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null || ti.wrapMode == TextureWrapMode.Repeat) return;
        ti.wrapMode = TextureWrapMode.Repeat;
        ti.SaveAndReimport();
    }

    static Material MakeMat(string name, Shader shader, Texture tex)
    {
        string path = "Assets/Materials/" + name + ".mat";
        AssetDatabase.DeleteAsset(path);
        Material m = new Material(shader);
        if (tex != null) m.mainTexture = tex;
        AssetDatabase.CreateAsset(m, path);
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    static Texture2D MakeGlowTexture()
    {
        const int N = 64;
        Texture2D t = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = a * a;
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        string path = "Assets/Textures/Glow.png";
        File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.alphaIsTransparency = true; ti.mipmapEnabled = false; ti.wrapMode = TextureWrapMode.Clamp;
        ti.SaveAndReimport();
        return LoadTex(path);
    }

    static Bounds GetBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    /// <summary>Scales a model so its world-space width (x) equals targetWidth. Makes the result
    /// independent of how Unity applied the FBX unit scale.</summary>
    static void NormalizeWidth(GameObject model, float targetWidth)
    {
        Bounds b = GetBounds(model);
        if (b.size.x > 0.0001f) model.transform.localScale *= targetWidth / b.size.x;
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t)
        {
            Transform r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    static GameObject SavePrefab(GameObject go, string name)
    {
        string path = "Assets/Prefabs/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static Rigidbody AddKinematicBody(GameObject go)
    {
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        return rb;
    }

    // ---------------------------------------------------------------- prefabs

    static GameObject BuildWeaponModel(string prefabName, string fbxName, float fbxWidth)
    {
        GameObject root = new GameObject(prefabName);
        GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(LoadFbx(fbxName));
        m.transform.SetParent(root.transform, false);
        m.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // gun points up the screen
        NormalizeWidth(m, fbxWidth * HERO_K);
        return SavePrefab(root, prefabName);
    }

    static GameObject BuildHero(Material shieldMat)
    {
        GameObject root = new GameObject("_Hero");

        GameObject container = new GameObject("Model");
        container.transform.SetParent(root.transform, false);

        GameObject ship = (GameObject)PrefabUtility.InstantiatePrefab(LoadFbx("heroShip"));
        ship.transform.SetParent(container.transform, false);
        ship.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);    // nose up, top toward camera
        NormalizeWidth(ship, 6f);

        // 5 gun slots placed on the hardpoint_0..4 nodes the artist put in heroShip.fbx
        Vector3[] fallback = { new Vector3(0, 2.6f, -0.5f), new Vector3(2.2f, -0.6f, -0.5f), new Vector3(-2.2f, -0.6f, -0.5f), new Vector3(1.5f, -0.4f, -0.5f), new Vector3(-1.5f, -0.4f, -0.5f) };
        Weapon[] weapons = new Weapon[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject w = new GameObject("Weapon_" + i);
            w.transform.SetParent(container.transform, false);
            Transform hp = FindDeep(ship.transform, "hardpoint_" + i);
            w.transform.position = hp != null ? hp.position : fallback[i];
            weapons[i] = w.AddComponent<Weapon>();
        }

        GameObject shieldGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        shieldGO.name = "Shield";
        Object.DestroyImmediate(shieldGO.GetComponent<Collider>());
        shieldGO.transform.SetParent(root.transform, false);
        shieldGO.transform.localPosition = new Vector3(0, 0, -1.5f);
        shieldGO.transform.localScale = Vector3.one * 9f;
        shieldGO.GetComponent<Renderer>().sharedMaterial = shieldMat;
        Shield shield = shieldGO.AddComponent<Shield>();

        Hero hero = root.AddComponent<Hero>();
        hero.model = container.transform;
        hero.shield = shield;
        hero.weapons = weapons;

        AddKinematicBody(root);
        BoxCollider col = root.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(4.2f, 4.6f, 2f);
        col.center = new Vector3(0, -0.3f, 0);

        BoundsCheck bc = root.AddComponent<BoundsCheck>();
        bc.boundsType = BoundsCheck.eType.inset;
        bc.radius = 3f;
        bc.keepOnScreen = true;

        return SavePrefab(root, "_Hero");
    }

    static GameObject BuildEnemy(int i)
    {
        // FBX widths measured from the models (x extent, FBX units)
        float[] fbxWidth = { 232.1f, 386.1f, 234.4f, 268.5f, 428.3f };

        GameObject root = new GameObject("Enemy_" + i);
        GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(LoadFbx("enemy" + i));
        m.transform.SetParent(root.transform, false);
        m.transform.localRotation = new Quaternion(0f, 0.7071068f, -0.7071068f, 0f);   // nose down (same as the kit's prefabs)
        NormalizeWidth(m, fbxWidth[i] * ENEMY_K);
        Bounds b = GetBounds(m);
        float halfW = b.size.x * 0.5f;

        Enemy e;
        float inset = halfW;
        switch (i)
        {
            case 1:
                Enemy_1 e1 = root.AddComponent<Enemy_1>();
                e1.waveWidth = 4f; e1.waveFrequency = 2f; e1.waveRotY = 45f;
                inset = halfW + e1.waveWidth;
                e = e1; break;
            case 2: e = root.AddComponent<Enemy_2>(); break;
            case 3: e = root.AddComponent<Enemy_3>(); break;
            case 4: e = root.AddComponent<Enemy_4>(); break;
            default: e = root.AddComponent<Enemy>(); break;
        }

        //                         speed health score drop  boom
        float[,] stats = { {  12f,  3f,  100f, 0.20f, 1.0f },
                           {  14f,  4f,  150f, 0.25f, 1.1f },
                           {  10f,  5f,  200f, 0.30f, 1.2f },
                           {  10f,  6f,  300f, 0.35f, 1.3f },
                           {   8f, 16f,  700f, 1.00f, 2.5f } };
        e.speed = stats[i, 0]; e.health = stats[i, 1]; e.score = (int)stats[i, 2];
        e.powerUpDropChance = stats[i, 3]; e.explosionScale = stats[i, 4];

        AddKinematicBody(root);
        BoxCollider col = root.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = new Vector3(b.center.x, b.center.y, 0);
        col.size = new Vector3(b.size.x * 0.7f, b.size.y * 0.7f, 2f);

        BoundsCheck bc = root.AddComponent<BoundsCheck>();
        bc.boundsType = BoundsCheck.eType.center;
        bc.radius = inset;
        bc.keepOnScreen = false;

        return SavePrefab(root, "Enemy_" + i);
    }

    static GameObject BuildHeroProjectile()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "ProjectileHero";
        go.transform.localScale = new Vector3(0.4f, 1.3f, 0.4f);
        go.GetComponent<Collider>().isTrigger = true;
        go.GetComponent<Renderer>().sharedMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(PKG + "/Materials/weapon_BlasterProjectile.mat");
        AddKinematicBody(go);
        go.AddComponent<Projectile>();
        BoundsCheck bc = go.AddComponent<BoundsCheck>();
        bc.boundsType = BoundsCheck.eType.center; bc.radius = 0f; bc.keepOnScreen = false;
        return SavePrefab(go, "ProjectileHero");
    }

    static GameObject BuildEnemyProjectile(Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "ProjectileEnemy";
        go.transform.localScale = Vector3.one * 1.6f;
        go.GetComponent<Collider>().isTrigger = true;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        AddKinematicBody(go);
        go.AddComponent<Projectile>();
        BoundsCheck bc = go.AddComponent<BoundsCheck>();
        bc.boundsType = BoundsCheck.eType.center; bc.radius = 0f; bc.keepOnScreen = false;
        return SavePrefab(go, "ProjectileEnemy");
    }

    static GameObject BuildPowerUp(Material mat)
    {
        GameObject root = new GameObject("PowerUp");

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Cube";
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        cube.transform.SetParent(root.transform, false);
        cube.transform.localScale = Vector3.one * 2.8f;
        cube.GetComponent<Renderer>().sharedMaterial = mat;

        PowerUp pu = root.AddComponent<PowerUp>();
        pu.cube = cube;

        // Letter on the cube (B / S / P / M / +)
        Font font = null;
#if UNITY_2022_2_OR_NEWER
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        if (font != null)
        {
            GameObject lg = new GameObject("Letter");
            lg.transform.SetParent(root.transform, false);
            lg.transform.localPosition = new Vector3(0, 0, -2f);
            TextMesh tm = lg.AddComponent<TextMesh>();
            tm.font = font;
            tm.text = "B";
            tm.fontSize = 60;
            tm.characterSize = 0.3f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            lg.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            pu.letter = tm;
        }

        AddKinematicBody(root);
        SphereCollider sc = root.AddComponent<SphereCollider>();
        sc.isTrigger = true; sc.radius = 1.8f;

        BoundsCheck bc = root.AddComponent<BoundsCheck>();
        bc.boundsType = BoundsCheck.eType.inset; bc.radius = 2f; bc.keepOnScreen = true;

        return SavePrefab(root, "PowerUp");
    }

    static GameObject BuildExplosion(Material mat)
    {
        GameObject go = new GameObject("Explosion");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 18f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 4f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0.1f, 1f), new Color(1f, 0.9f, 0.4f, 1f));
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.maxParticles = 100;

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 45) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        psr.sharedMaterial = mat;

        return SavePrefab(go, "Explosion");
    }

    // ---------------------------------------------------------------- scene

    static void BuildScene(GameObject heroPrefab, GameObject[] enemies, GameObject projHero, GameObject projEnemy,
                           GameObject powerUp, GameObject explosion, GameObject[] weaponModels,
                           Material matSpace, Material matStars1, Material matStars2)
    {
        // Camera (orthographic, size 40, like the book)
        GameObject camGO = new GameObject("_MainCamera");
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0, 0, -10);
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 40;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 100f;
        camGO.AddComponent<AudioListener>();
        camGO.AddComponent<CameraShake>();

        // Light shining toward +Z so the ships (which face the camera) are lit
        GameObject lightGO = new GameObject("Directional Light");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.shadows = LightShadows.None;
        lightGO.transform.rotation = Quaternion.Euler(20f, 20f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.55f);

        // Scrolling starfield: three layers, far to near
        GameObject field = new GameObject("Starfield");
        MakeLayer(field.transform, "Stars_Space", 20f, matSpace, 3f, 0.03f, 1f);
        MakeLayer(field.transform, "Stars_Far", 19f, matStars1, 9f, 0.08f, 1f);
        MakeLayer(field.transform, "Stars_Near", 18f, matStars2, 18f, 0.15f, 1.7f);

        // Game manager
        GameObject mainGO = new GameObject("_Main");
        Main main = mainGO.AddComponent<Main>();
        mainGO.AddComponent<HUD>();
        main.prefabEnemies = enemies;
        main.prefabPowerUp = powerUp;
        main.explosionPrefab = explosion;
        main.projectileHeroPrefab = projHero;
        main.projectileEnemyPrefab = projEnemy;
        main.weaponDefinitions = Main.DefaultWeaponDefinitions();
        foreach (WeaponDefinition d in main.weaponDefinitions)
        {
            switch (d.type)
            {
                case eWeaponType.blaster:
                    d.weaponModelPrefab = weaponModels[0];
                    d.projectileMaterial = AssetDatabase.LoadAssetAtPath<Material>(PKG + "/Materials/weapon_BlasterProjectile.mat"); break;
                case eWeaponType.spread:
                    d.weaponModelPrefab = weaponModels[1];
                    d.projectileMaterial = AssetDatabase.LoadAssetAtPath<Material>(PKG + "/Materials/weapon_SpreadShotProjectile.mat"); break;
                case eWeaponType.phaser:
                    d.weaponModelPrefab = weaponModels[2];
                    d.projectileMaterial = AssetDatabase.LoadAssetAtPath<Material>(PKG + "/Materials/weapon_PhaserProjectile.mat"); break;
                case eWeaponType.missile:
                    d.weaponModelPrefab = weaponModels[3];
                    d.projectileMaterial = AssetDatabase.LoadAssetAtPath<Material>(PKG + "/Materials/weapon_MissileLauncherProjectile.mat"); break;
            }
        }

        // Hero
        GameObject hero = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab);
        hero.transform.position = new Vector3(0, -30, 0);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), SCENE_PATH);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(SCENE_PATH, true) };
    }

    static void MakeLayer(Transform parent, string name, float z, Material mat, float speed, float parallax, float tiling)
    {
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = name;
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.transform.SetParent(parent, false);
        q.transform.position = new Vector3(0, 0, z);
        q.transform.localScale = new Vector3(120, 120, 1);
        Renderer r = q.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        ScrollingBackground sb = q.AddComponent<ScrollingBackground>();
        sb.scrollSpeed = speed; sb.heroParallax = parallax; sb.tiling = tiling;
    }
}
