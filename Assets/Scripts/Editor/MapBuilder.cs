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
            cam.orthographicSize = 6.2f;

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

        // 6. ZONE 2: The Shattered Viaduct of the Damned (Level 1000 Parkour Gauntlet)
        // Lethal Multi-layered Jagged Spike Beds along the bottom of the chasm
        CreateSpikeHazard(obstaclesRoot, "Spikes_Jagged_Ridge_29", new Vector2(15.0f, -6.0f), new Vector2(6.5f, 2.8f));
        CreateSpikeHazard(obstaclesRoot, "Spikes_Jagged_Ridge_31", new Vector2(20.5f, -6.0f), new Vector2(6.5f, 2.8f));
        CreateSpikeHazard(obstaclesRoot, "Spikes_Stone_22", new Vector2(25.0f, -5.8f), new Vector2(5.5f, 2.5f));

        // Broken Viaduct Outcrop (Solid anchor ledge jutting over abyss, Visual Top: -0.01)
        CreatePlatform(platformsRoot, "Platform_Medium_Stone_1", new Vector2(14.5f, -1.4f), new Vector2(3.60f, 1.20f), new Vector2(0f, 0.787f), groundLayer);

        // Floating Monolith 1: Wide Levitating Runic Slab with reactive hover (Visual Top: 0.65)
        CreateFloatingPlatform(platformsRoot, "Stepping_Stone_19", new Vector2(17.8f, 0.0f), new Vector2(1.80f, 0.80f), new Vector2(0f, 0.25f), groundLayer, 0.08f, 1.2f, new Color(1.0f, 0.55f, 0.18f, 1.0f));

        // Floating Monolith 2: Ancient Spire Stepping Stone with runic point light (Visual Top: 1.45)
        CreateFloatingPlatform(platformsRoot, "Stepping_Stone_12", new Vector2(20.7f, 0.6f), new Vector2(1.20f, 0.80f), new Vector2(0f, 0.45f), groundLayer, 0.07f, 0.95f, new Color(1.0f, 0.45f, 0.15f, 1.0f));

        // Floating Monolith 3: High Obsidian Monolith ascending to crypt plateau (Visual Top: 2.22)
        CreateFloatingPlatform(platformsRoot, "Stepping_Stone_30", new Vector2(23.6f, 1.0f), new Vector2(1.15f, 0.80f), new Vector2(0f, 0.82f), groundLayer, 0.09f, 1.35f, new Color(1.0f, 0.28f, 0.15f, 1.0f));

        // Gothic ruins & atmospheric set-pieces framing the parkour gap
        CreateDecoration(decorRoot, "Gothic_Arch_Ruin_19", new Vector2(20.5f, 5.0f), 1.25f);
        CreateDecoration(decorRoot, "Bridge_Arch_Rock_23", new Vector2(16.5f, 2.8f), 0.9f, sortingOrder: -4);
        CreateDecoration(decorRoot, "Bridge_Arch_Rock_27", new Vector2(24.2f, 3.5f), 0.85f, sortingOrder: -4);

        // Giant Iron Chains anchoring floating stones from the high arches
        CreateDecoration(decorRoot, "Hanging_Chain_17", new Vector2(17.8f, 5.2f), 1.1f);
        CreateDecoration(decorRoot, "Hanging_Chain_18", new Vector2(20.7f, 5.6f), 1.15f);
        CreateDecoration(decorRoot, "Hanging_Chain_17", new Vector2(23.6f, 5.4f), 1.05f);

        // 7. ZONE 3: Crypt Ruins & High Bridge (Multi-tier Area)
        // Landing plateau (Visual Top: 2.80, spans X: 26.10 to 33.50)
        CreatePlatform(platformsRoot, "Platform_Large_Stone_6", new Vector2(29.8f, 1.5f), new Vector2(7.40f, 1.20f), new Vector2(0f, 0.70f), groundLayer);

        // Checkpoint 1
        CreateCheckpoint(platformsRoot, new Vector3(27.5f, 3.2f, 0f));

        // Stone Stairs leading up to high bridge with exact step contour
        CreateStairsPlatform(platformsRoot, new Vector2(34.2f, 2.6f), groundLayer);

        // High Ruin Bridge: Elevated combat ledge (Visual Top: 4.76, spans X: 38.25 to 44.90)
        var highBridge = CreatePlatform(platformsRoot, "Platform_Large_Stone_13", new Vector2(40.5f, 4.2f), new Vector2(6.65f, 1.20f), new Vector2(1.075f, -0.04f), groundLayer);
        CreateRuneLight(highBridge, new Vector3(3.7f, 1.2f, 0f), new Color(1.0f, 0.55f, 0.2f, 1.0f), 1.4f, 4.5f);

        // High bridge decorations
        CreateDecoration(decorRoot, "Stone_Pillar_14", new Vector2(44.2f, 5.8f), 1.0f);
        CreateDecoration(decorRoot, "Hanging_Chain_18", new Vector2(37.5f, 6.2f), 1.0f);

        // Lower hazard beneath the high bridge
        CreateSpikeHazard(obstaclesRoot, "Spikes_Stone_20", new Vector2(38.0f, -3.5f), new Vector2(4.8f, 2.2f));

        // Zone 3 Enemy (Armored Guard #2 on high bridge)
        SpawnEnemy(enemiesRoot, new Vector3(41.0f, 4.76f, 0f), 3.5f);

        // 8. ZONE 4: The Abyssal Cascade — Precision Descent Gauntlet (Level 1000 Parkour)
        // Giant Overlapping Spiked Abyss below
        CreateSpikeHazard(obstaclesRoot, "Spikes_Jagged_Ridge_24", new Vector2(48.5f, -5.8f), new Vector2(6.5f, 2.8f));
        CreateSpikeHazard(obstaclesRoot, "Spikes_Stone_21", new Vector2(53.5f, -5.8f), new Vector2(5.5f, 2.4f));
        CreateSpikeHazard(obstaclesRoot, "Spikes_Jagged_Ridge_31", new Vector2(57.5f, -5.8f), new Vector2(5.0f, 2.5f));

        // Cascading 5-tier Descent:
        // Tier 1: Ruined High Viaduct Ledge (Visual Top: 3.95)
        CreatePlatform(platformsRoot, "Platform_Medium_Stone_4", new Vector2(47.2f, 2.8f), new Vector2(4.25f, 1.20f), new Vector2(0f, 0.550f), groundLayer);

        // Tier 2: Levitating Ruin Monolith 1 (Hovering rune rock, Visual Top: 2.70)
        CreateFloatingPlatform(platformsRoot, "Stepping_Stone_18", new Vector2(50.8f, 1.9f), new Vector2(1.10f, 0.70f), new Vector2(0f, 0.45f), groundLayer, 0.08f, 1.1f, new Color(1.0f, 0.50f, 0.15f, 1.0f));

        // Tier 3: Floating Shattered Rock 2 (Mid chasm stepping stone, Visual Top: 1.66)
        CreateFloatingPlatform(platformsRoot, "Platform_Small_Stone_8", new Vector2(53.6f, 1.0f), new Vector2(2.20f, 0.80f), new Vector2(0f, 0.262f), groundLayer, 0.06f, 1.4f, new Color(1.0f, 0.45f, 0.15f, 1.0f));

        // Tier 4: Levitating Ruin Monolith 3 (Low floating rune slab, Visual Top: 0.70)
        CreateFloatingPlatform(platformsRoot, "Stepping_Stone_26", new Vector2(56.3f, 0.2f), new Vector2(1.40f, 0.60f), new Vector2(0f, 0.20f), groundLayer, 0.07f, 1.25f, new Color(1.0f, 0.30f, 0.15f, 1.0f));

        // Tier 5: Arena Gateway Arrival Shelf (Solid foundation, Visual Top: 0.59)
        CreatePlatform(platformsRoot, "Platform_Rock_Medium_2", new Vector2(58.2f, -0.9f), new Vector2(3.60f, 1.20f), new Vector2(0f, 0.888f), groundLayer);

        // Suspended Iron Chains & Background Viaduct Arches
        CreateDecoration(decorRoot, "Bridge_Arch_Rock_21", new Vector2(48.5f, 1.2f), 0.95f, sortingOrder: -4);
        CreateDecoration(decorRoot, "Bridge_Arch_Rock_23", new Vector2(54.0f, 0.4f), 0.9f, sortingOrder: -4);
        CreateDecoration(decorRoot, "Stone_Pillar_14", new Vector2(45.5f, 4.8f), 1.0f);
        CreateDecoration(decorRoot, "Hanging_Chain_18", new Vector2(50.8f, 6.2f), 1.1f);
        CreateDecoration(decorRoot, "Hanging_Chain_17", new Vector2(56.3f, 5.0f), 1.05f);

        // Checkpoint 2 on arrival shelf before the boss arena
        CreateCheckpoint(platformsRoot, new Vector3(57.5f, 1.6f, 0f));

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
        CreateRuneLight(decorRoot, new Vector3(74.5f, 4.0f, 0f), new Color(1.0f, 0.12f, 0.12f, 1.0f), 2.2f, 8.5f);

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
        barrierSR.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH + "Decorations/Stone_Pillar_14.png");
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
        sr.sortingOrder = 2; // Always in front of background (-100), decorations (-2), and spikes (1)
        sr.color = new Color(1.18f, 1.15f, 1.15f, 1.0f); // Enhanced visibility & stone contrast

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
        sr.sortingOrder = 2;
        sr.color = new Color(1.18f, 1.15f, 1.15f, 1.0f);

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
        sr.sortingOrder = 1; // Renders behind platforms (2), above background (-100)

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
        if (sprite == null)
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH + "Platforms/" + spriteName + ".png");
        if (sprite != null) sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;

        return obj;
    }

    private static GameObject CreateFloatingPlatform(GameObject parent, string spriteName, Vector2 pos, Vector2 colSize, Vector2 colOffset, int layer, float bobAmp = 0.08f, float bobFreq = 1.2f, Color? runeLightColor = null)
    {
        var obj = CreatePlatform(parent, spriteName, pos, colSize, colOffset, layer);
        
        var floater = obj.AddComponent<FloatingRuinPlatform>();
        var ampField = typeof(FloatingRuinPlatform).GetField("bobAmplitude", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (ampField != null) ampField.SetValue(floater, bobAmp);
        var freqField = typeof(FloatingRuinPlatform).GetField("bobFrequency", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (freqField != null) freqField.SetValue(floater, bobFreq);

        if (runeLightColor.HasValue)
        {
            CreateRuneLight(obj, new Vector3(0f, colOffset.y + 0.15f, 0f), runeLightColor.Value, 1.35f, 3.8f);
        }

        return obj;
    }

    private static GameObject CreateRuneLight(GameObject parent, Vector3 localPos, Color color, float intensity, float radius)
    {
        var lightObj = new GameObject("RuneLight");
        lightObj.transform.SetParent(parent.transform);
        lightObj.transform.localPosition = localPos;

        var light = lightObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.15f;

        return lightObj;
    }

    private static GameObject CreateCheckpoint(GameObject parent, Vector3 pos)
    {
        var obj = new GameObject("Checkpoint");
        obj.transform.SetParent(parent.transform);
        obj.transform.position = pos;

        var sr = obj.AddComponent<SpriteRenderer>();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH + "Decorations/Gravestone_Cross_17.png");
        if (sprite != null) sr.sprite = sprite;
        sr.sortingOrder = 2;
        sr.color = new Color(0.4f, 1.0f, 0.7f, 0.95f); // Visible luminous emerald runic cross marker

        CreateRuneLight(obj, new Vector3(0f, 0.45f, 0f), new Color(0.2f, 1.0f, 0.65f, 1.0f), 1.6f, 4.2f);

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
        boss.transform.localScale = new Vector3(1.55f, 1.55f, 1.0f);
    }

    private static void SpawnEndGate(GameObject parent, Vector3 pos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GATE_PREFAB_PATH);
        if (prefab == null) return;

        var gate = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        gate.transform.position = pos;
    }
}
