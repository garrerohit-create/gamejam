using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MapBuilder
{
    private const string SPRITE_PATH = "Assets/Art/Environment/";
    private const string ENEMY_PREFAB_PATH = "Assets/Prefabs/Enemies/ArmoredGuard.prefab";
    private const string BOSS_PREFAB_PATH = "Assets/Prefabs/Enemies/AbyssalBoss.prefab";
    private const string GATE_PREFAB_PATH = "Assets/Prefabs/Environment/EndLevelGate.prefab";

    [MenuItem("Tools/Build Side-Scrolling Map")]
    public static void BuildMap()
    {
        // 1. Clean existing level objects if present
        var oldEnv = GameObject.Find("[ENVIRONMENT]");
        if (oldEnv != null) Object.DestroyImmediate(oldEnv);

        var oldSquare = GameObject.Find("Square");
        if (oldSquare != null) oldSquare.SetActive(false);

        var oldEnemies = GameObject.FindObjectsByType<ArmoredGuardAI>();
        foreach (var e in oldEnemies)
        {
            Object.DestroyImmediate(e.gameObject);
        }

        var oldBosses = GameObject.FindObjectsByType<BossAI>();
        foreach (var b in oldBosses)
        {
            Object.DestroyImmediate(b.gameObject);
        }

        var oldGates = GameObject.FindObjectsByType<EndLevelGate>();
        foreach (var g in oldGates)
        {
            Object.DestroyImmediate(g.gameObject);
        }

        var oldBarriers = GameObject.FindObjectsByType<ArenaEntranceBarrier>();
        foreach (var b in oldBarriers)
        {
            Object.DestroyImmediate(b.gameObject);
        }

        // Ensure AudioManager & GameManager
        var audioMgr = GameObject.Find("[AudioManager]");
        if (audioMgr == null)
        {
            audioMgr = new GameObject("[AudioManager]");
            audioMgr.AddComponent<AudioManager>();
        }

        var gameMgr = GameObject.Find("[GameManager]");
        if (gameMgr == null)
        {
            gameMgr = new GameObject("[GameManager]");
            gameMgr.AddComponent<GameManager>();
        }

        // 2. Setup Camera Follow
        var cam = Camera.main;
        if (cam != null)
        {
            var follow = cam.GetComponent<CameraFollow2D>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow2D>();

            var knight = GameObject.Find("Knight");
            if (knight != null)
            {
                var targetField = typeof(CameraFollow2D).GetField("target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (targetField != null) targetField.SetValue(follow, knight.transform);

                // Position Knight at start of Zone 1 above the rock surface
                knight.transform.position = new Vector3(-1.5f, 0.8f, 0f);

                // Assign frictionless material to knight collider
                var kCol = knight.GetComponent<Collider2D>();
                var frictionless = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Art/Effects/Knight_Frictionless.physicsMaterial2D");
                if (frictionless != null && kCol != null) kCol.sharedMaterial = frictionless;
            }
        }

        // 3. Create Root Hierarchy
        var root = new GameObject("[ENVIRONMENT]");
        var bgRoot = new GameObject("Background_Parallax");
        bgRoot.transform.SetParent(root.transform);
        bgRoot.AddComponent<ParallaxLayer>();

        var platformsRoot = new GameObject("Platforms");
        platformsRoot.transform.SetParent(root.transform);

        var obstaclesRoot = new GameObject("Obstacles_Spikes");
        obstaclesRoot.transform.SetParent(root.transform);

        var decorRoot = new GameObject("Decorations");
        decorRoot.transform.SetParent(root.transform);

        var enemiesRoot = new GameObject("Enemies");
        enemiesRoot.transform.SetParent(root.transform);

        // Atmosphere: Drifting glowing embers & ash
        var embersObj = new GameObject("Atmospheric_Embers");
        embersObj.transform.SetParent(root.transform);
        embersObj.AddComponent<AtmosphericEmbers>();

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer == -1) groundLayer = 0;

        // 4. Parallax Background Panels (1024x341 at PPU 25 -> 40.96 units per panel)
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH + "Background/Background_Hellscape.png");
        if (bgSprite != null)
        {
            // Panel 2 aligned so skull sits right at X = 74.5 in arena background
            float[] panelXPositions = new float[] { 0f, 38.0f, 60.8f, 96.0f };
            for (int i = 0; i < panelXPositions.Length; i++)
            {
                var bgObj = new GameObject($"BG_Hellscape_Panel_{i}");
                bgObj.transform.SetParent(bgRoot.transform);
                bgObj.transform.position = new Vector3(panelXPositions[i], 4.0f, 10f);
                var sr = bgObj.AddComponent<SpriteRenderer>();
                sr.sprite = bgSprite;
                sr.sortingOrder = -100;
            }
        }

        // 5. ZONE 1: The Ashen Outpost (Spawn & Intro)
        // Starter Main Plateau (Visual Top: -0.26, spans X: -5.40 to 12.40)
        CreatePlatform(platformsRoot, "Terrain_Plateau_Huge_0", new Vector2(3.5f, -2.8f), new Vector2(17.80f, 1.20f), new Vector2(0f, 1.938f), groundLayer);

        // Zone 1 Decorations
        CreateDecoration(decorRoot, "Gravestone_Cross_11", new Vector2(1.5f, 0.45f), 0.9f);
        CreateDecoration(decorRoot, "Stone_Pillar_14", new Vector2(-4.2f, 0.70f), 1.0f);
        CreateDecoration(decorRoot, "Gravestone_Cross_15", new Vector2(9.5f, 0.45f), 0.95f);

        // Zone 1 Enemy (Armored Guard #1) - standing at Y = -0.25 on top of rock
        SpawnEnemy(enemiesRoot, new Vector3(7.5f, -0.26f, 0f), 3.5f);

        // 6. ZONE 2: Spiked Chasm & Floating Stepping Stones (Parkour Gap 1)
        // Lethal Spike Beds along the bottom of the chasm
        CreateSpikeHazard(obstaclesRoot, "Spikes_Jagged_Ridge_29", new Vector2(15.5f, -6.0f), new Vector2(6.5f, 2.8f));
        CreateSpikeHazard(obstaclesRoot, "Spikes_Jagged_Ridge_31", new Vector2(23.0f, -6.0f), new Vector2(6.0f, 2.5f));

        // Stepping Stones (Colliders precisely aligned with top rock surfaces, comfortable gaps)
        // Island 1: Low stone step (Visual Top: -0.01, spans X: 13.20 to 16.80)
        CreatePlatform(platformsRoot, "Platform_Medium_Stone_1", new Vector2(15.0f, -1.4f), new Vector2(3.60f, 1.20f), new Vector2(0f, 0.787f), groundLayer);
        // Island 2: Mid stone step (Visual Top: 1.39, spans X: 17.55 to 22.05)
        CreatePlatform(platformsRoot, "Platform_Rock_Medium_1", new Vector2(19.8f, 0.1f), new Vector2(4.50f, 1.20f), new Vector2(0f, 0.688f), groundLayer);
        // Island 3: High stone step (Visual Top: 2.24, spans X: 22.90 to 25.10)
        CreatePlatform(platformsRoot, "Platform_Small_Stone_7", new Vector2(24.0f, 1.6f), new Vector2(2.20f, 1.00f), new Vector2(0f, 0.137f), groundLayer);

        // Upper Arch Landmark above parkour
        CreateDecoration(decorRoot, "Gothic_Arch_Ruin_19", new Vector2(19.8f, 4.3f), 1.1f);
        CreateDecoration(decorRoot, "Hanging_Chain_17", new Vector2(19.5f, 5.3f), 1.0f);

        // 7. ZONE 3: Crypt Ruins & High Bridge (Multi-tier Area)
        // Landing plateau (Visual Top: 2.80, spans X: 26.10 to 33.50)
        CreatePlatform(platformsRoot, "Platform_Large_Stone_6", new Vector2(29.8f, 1.5f), new Vector2(7.40f, 1.20f), new Vector2(0f, 0.70f), groundLayer);

        // Checkpoint 1
        CreateCheckpoint(platformsRoot, new Vector3(27.5f, 3.2f, 0f));

        // Stone Stairs leading up to high bridge with exact step contour
        CreateStairsPlatform(platformsRoot, new Vector2(34.2f, 2.6f), groundLayer);

        // High Ruin Bridge: Elevated combat ledge (Visual Top: 4.76, spans X: 38.25 to 44.90)
        CreatePlatform(platformsRoot, "Platform_Large_Stone_13", new Vector2(40.5f, 4.2f), new Vector2(6.65f, 1.20f), new Vector2(1.075f, -0.04f), groundLayer);

        // High bridge decorations
        CreateDecoration(decorRoot, "Stone_Pillar_14", new Vector2(44.2f, 5.8f), 1.0f);
        CreateDecoration(decorRoot, "Hanging_Chain_18", new Vector2(37.5f, 6.2f), 1.0f);

        // Lower hazard beneath the high bridge
        CreateSpikeHazard(obstaclesRoot, "Spikes_Stone_20", new Vector2(38.0f, -3.5f), new Vector2(4.8f, 2.2f));

        // Zone 3 Enemy (Armored Guard #2 on high bridge)
        SpawnEnemy(enemiesRoot, new Vector3(41.0f, 4.76f, 0f), 3.5f);

        // 8. ZONE 4: The Descent Across the Spiked Abyss (Precision Parkour Gap 2)
        // Giant Spiked Pit below
        CreateSpikeHazard(obstaclesRoot, "Spikes_Jagged_Ridge_24", new Vector2(51.0f, -5.8f), new Vector2(6.2f, 2.6f));
        CreateSpikeHazard(obstaclesRoot, "Spikes_Stone_21", new Vector2(56.5f, -5.8f), new Vector2(4.5f, 2.2f));

        // Stepping stone platforms descending towards arena
        // Step D: High descent platform (Visual Top: 3.55, spans X: 45.38 to 49.63)
        CreatePlatform(platformsRoot, "Platform_Medium_Stone_4", new Vector2(47.5f, 2.4f), new Vector2(4.25f, 1.20f), new Vector2(0f, 0.550f), groundLayer);
        // Step E: Mid descent stepping stone (Visual Top: 1.96, spans X: 51.10 to 53.30)
        CreatePlatform(platformsRoot, "Platform_Small_Stone_8", new Vector2(52.2f, 1.2f), new Vector2(2.20f, 1.00f), new Vector2(0f, 0.262f), groundLayer);
        // Step F: Low landing platform before arena (Visual Top: 1.09, spans X: 53.95 to 58.45)
        CreatePlatform(platformsRoot, "Platform_Rock_Medium_2", new Vector2(56.2f, -0.4f), new Vector2(4.50f, 1.20f), new Vector2(0f, 0.888f), groundLayer);

        // Checkpoint 2
        CreateCheckpoint(platformsRoot, new Vector3(56.2f, 1.5f, 0f));

        // 9. ZONE 5: The Skull Throne Arena (Final Combat Arena)
        // Seamless Arena Floor (Visual Top: -0.08, spanning X: 59.00 to 86.00 continuous)
        CreatePlatform(platformsRoot, "Terrain_Plateau_Huge_4", new Vector2(65.0f, -2.4f), new Vector2(12.00f, 1.20f), new Vector2(0f, 1.725f), groundLayer);
        CreatePlatform(platformsRoot, "Platform_Large_Stone_0", new Vector2(78.5f, -1.43f), new Vector2(15.00f, 1.20f), new Vector2(0f, 0.750f), groundLayer);

        // Arena Gothic Atmosphere & The Giant Skull Throne
        CreateDecoration(decorRoot, "Skull_Throne_Giant", new Vector2(74.5f, 3.2f), 1.35f, sortingOrder: -5);
        CreateDecoration(decorRoot, "Stone_Pillar_14", new Vector2(61.5f, 0.90f), 1.05f);
        CreateDecoration(decorRoot, "Gravestone_Cross_11", new Vector2(63.0f, 0.65f), 1.0f);
        CreateDecoration(decorRoot, "Gravestone_Cross_15", new Vector2(82.5f, 0.65f), 1.0f);
        CreateDecoration(decorRoot, "Hanging_Chain_17", new Vector2(73.0f, 5.8f), 1.0f);

        // Arena Vanguard Guard
        SpawnEnemy(enemiesRoot, new Vector3(66.0f, -0.08f, 0f), 3.5f);

        // Arena Entrance Sealing Barrier (Locks player inside arena during boss encounter)
        var barrierObj = new GameObject("ArenaEntranceBarrier");
        barrierObj.transform.SetParent(obstaclesRoot.transform);
        barrierObj.transform.position = new Vector3(58.5f, 2.5f, 0f);
        var barrierCol = barrierObj.AddComponent<BoxCollider2D>();
        barrierCol.size = new Vector2(0.8f, 10f);
        barrierCol.isTrigger = false;
        var barrierSR = barrierObj.AddComponent<SpriteRenderer>();
        barrierSR.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Stone_Pillar_14.png");
        barrierSR.color = new Color(0.9f, 0.1f, 0.1f, 0.85f);
        barrierSR.sortingOrder = 5;
        barrierObj.AddComponent<ArenaEntranceBarrier>();

        // Arena Boss (The Infernal Executioner — Warden of the Damned)
        SpawnBoss(enemiesRoot, new Vector3(74.5f, 5.8f, 0f));

        // 10. The End-of-Map Gate (Passage to the Ascended Realm)
        SpawnEndGate(platformsRoot, new Vector3(84.0f, -0.08f, 0f));

        // 11. Fall Chasm Trigger (Catches any fall below the entire map)
        var chasmObj = new GameObject("FallChasmBarrier");
        chasmObj.transform.SetParent(obstaclesRoot.transform);
        chasmObj.transform.position = new Vector3(40.0f, -8.5f, 0f);
        var chasmCol = chasmObj.AddComponent<BoxCollider2D>();
        chasmCol.size = new Vector2(110f, 3f);
        chasmCol.isTrigger = true;
        chasmObj.AddComponent<FallChasmTrigger>();

        // Wire Boss Health UI onto HUD Canvas
        var hudCanvas = GameObject.Find("Player_HUD_Canvas");
        if (hudCanvas != null && hudCanvas.GetComponent<BossHealthBarUI>() == null)
        {
            hudCanvas.AddComponent<BossHealthBarUI>();
        }

        EditorUtility.SetDirty(root);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("Side-scrolling dark fantasy map with calibrated colliders built successfully!");
    }

    private static GameObject CreatePlatform(GameObject parent, string spriteName, Vector2 pos, Vector2 colSize, Vector2 colOffset, int layer, bool isOneWay = false)
    {
        var obj = new GameObject(spriteName);
        obj.transform.SetParent(parent.transform);
        obj.transform.position = new Vector3(pos.x, pos.y, 0f);
        obj.layer = layer;

        var sr = obj.AddComponent<SpriteRenderer>();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH + "Platforms/" + spriteName + ".png");
        if (sprite != null) sr.sprite = sprite;
        sr.sortingOrder = 0;

        var col = obj.AddComponent<BoxCollider2D>();
        col.size = colSize;
        col.offset = colOffset;

        if (isOneWay)
        {
            col.usedByEffector = true;
            var effector = obj.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useOneWayGrouping = true;
            effector.surfaceArc = 160f;
        }

        return obj;
    }

    private static GameObject CreateStairsPlatform(GameObject parent, Vector2 pos, int layer)
    {
        var obj = new GameObject("Stone_Stairs_12");
        obj.transform.SetParent(parent.transform);
        obj.transform.position = new Vector3(pos.x, pos.y, 0f);
        obj.layer = layer;

        var sr = obj.AddComponent<SpriteRenderer>();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH + "Platforms/Stone_Stairs_12.png");
        if (sprite != null) sr.sprite = sprite;
        sr.sortingOrder = 0;

        var col = obj.AddComponent<PolygonCollider2D>();
        col.points = new Vector2[] {
            new Vector2(-0.70f, 0.20f),
            new Vector2(2.45f, 2.16f),
            new Vector2(4.05f, 2.16f),
            new Vector2(4.05f, -2.10f),
            new Vector2(-0.70f, -2.10f)
        };

        return obj;
    }

    private static GameObject CreateSpikeHazard(GameObject parent, string spriteName, Vector2 pos, Vector2 colSize)
    {
        var obj = new GameObject(spriteName);
        obj.transform.SetParent(parent.transform);
        obj.transform.position = new Vector3(pos.x, pos.y, 0f);

        var sr = obj.AddComponent<SpriteRenderer>();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH + "Obstacles/" + spriteName + ".png");
        if (sprite != null) sr.sprite = sprite;
        sr.sortingOrder = 5;

        var col = obj.AddComponent<BoxCollider2D>();
        col.size = colSize;
        col.isTrigger = true;
        obj.AddComponent<SpikeHazard>();

        return obj;
    }

    private static GameObject CreateDecoration(GameObject parent, string spriteName, Vector2 pos, float scale = 1f, int sortingOrder = -2)
    {
        var obj = new GameObject(spriteName);
        obj.transform.SetParent(parent.transform);
        obj.transform.position = new Vector3(pos.x, pos.y, 0f);
        obj.transform.localScale = Vector3.one * scale;

        var sr = obj.AddComponent<SpriteRenderer>();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH + "Decorations/" + spriteName + ".png");
        if (sprite != null) sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;

        return obj;
    }

    private static GameObject CreateCheckpoint(GameObject parent, Vector3 pos)
    {
        var obj = new GameObject("Checkpoint");
        obj.transform.SetParent(parent.transform);
        obj.transform.position = pos;

        var col = obj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(3f, 4f);
        col.isTrigger = true;
        obj.AddComponent<LevelCheckpointTrigger>();

        return obj;
    }

    private static void SpawnEnemy(GameObject parent, Vector3 pos, float patrolDist)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_PREFAB_PATH);
        if (prefab == null) return;

        var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        enemy.transform.position = pos;

        var ai = enemy.GetComponent<ArmoredGuardAI>();
        if (ai != null)
        {
            var pField = typeof(ArmoredGuardAI).GetField("patrolDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (pField != null) pField.SetValue(ai, patrolDist);
        }
    }

    private static void SpawnBoss(GameObject parent, Vector3 pos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BOSS_PREFAB_PATH);
        if (prefab == null) return;

        var boss = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        boss.transform.position = pos;
    }

    private static void SpawnEndGate(GameObject parent, Vector3 pos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GATE_PREFAB_PATH);
        if (prefab == null) return;

        var gate = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        gate.transform.position = pos;
    }
}
