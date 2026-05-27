#if UNITY_EDITOR
using System.IO;
using BehaviorTree.ClassFirst;
using BehaviorTree.ClassFirst.War;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Builds a 100-vs-100 stress-test demo for the Class-First stack.
/// Generates a separate scene (SampleScene_War) so existing demos are untouched.
/// </summary>
public static class WarSceneSetup
{
    private const string ROOT_NAME = "War_Demo";
    private const string SOURCE_ENEMY_PATH = "Enemy";
    private const string SOURCE_SCENE_PATH = "Assets/Scenes/SampleScene.unity";
    private const string TARGET_SCENE_PATH = "Assets/Scenes/SampleScene_War.unity";

    // Per-tint material assets. One sharedMaterial per (faction × class) so that
    // 10000 NPCs collapse onto ~10 unique sharedMaterial references — required for
    // GPU Instancing to actually batch draws.
    //
    // Why not MaterialPropertyBlock? URP Lit declares _BaseColor in the per-material
    // CBUFFER, NOT in UNITY_INSTANCING_BUFFER, so MPB-driven color overrides are
    // silently ignored once Enable GPU Instancing is checked on the material —
    // every NPC ends up rendering with the sharedMaterial's color (all the same).
    // Using distinct material assets sidesteps the shader's instanced-property
    // limitation entirely.
    private const string TINT_MAT_DIR = "Assets/Materials/Tints";

    private static Material LoadTint(string name)
    {
        var path = $"{TINT_MAT_DIR}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) Debug.LogError($"[WarSetup] Missing tint material: {path}");
        return m;
    }

    // Per-team composition (sum = 100). Same ratios as the 5000-team stress test
    // (40/25/20/10/5). Map size + camera + NavMesh stay sized for the big battle
    // so we can scale back up by just bumping these numbers + COLS_PER_ROW.
    private const int WARRIORS = 40;
    private const int SPEARMEN = 25;
    private const int ARCHERS  = 20;
    private const int KNIGHTS  = 10;
    private const int HEALERS  =  5;

    [MenuItem("Tools/BT War/Create War Scene")]
    public static void CreateWarScene()
    {
        if (!File.Exists(SOURCE_SCENE_PATH))
        {
            Debug.LogError($"[WarSetup] Source scene not found at {SOURCE_SCENE_PATH}");
            return;
        }
        if (File.Exists(TARGET_SCENE_PATH))
        {
            if (!EditorUtility.DisplayDialog("Replace existing scene?",
                $"{TARGET_SCENE_PATH} already exists. Overwrite?",
                "Overwrite", "Cancel"))
            {
                Debug.Log("[WarSetup] Aborted.");
                return;
            }
            AssetDatabase.DeleteAsset(TARGET_SCENE_PATH);
        }
        if (!AssetDatabase.CopyAsset(SOURCE_SCENE_PATH, TARGET_SCENE_PATH))
        {
            Debug.LogError($"[WarSetup] Failed to copy scene.");
            return;
        }
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(TARGET_SCENE_PATH, OpenSceneMode.Single);
        Debug.Log($"[WarSetup] Opened {TARGET_SCENE_PATH}. Now run \"Tools/BT War/Setup Battle Demo\".");
    }

    [MenuItem("Tools/BT War/Setup Battle Demo")]
    public static void SetupBattle()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != TARGET_SCENE_PATH)
        {
            Debug.LogWarning($"[WarSetup] Current scene is not {TARGET_SCENE_PATH}. " +
                             "Run \"Create War Scene\" first or open it manually.");
            return;
        }

        // Disable root /Enemy.
        GameObject oldEnemy = null;
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == SOURCE_ENEMY_PATH) { oldEnemy = root; break; }
        if (oldEnemy == null)
        {
            Debug.LogError($"[WarSetup] No root /{SOURCE_ENEMY_PATH} found.");
            return;
        }
        oldEnemy.SetActive(false);

        // Clear previous battle root (idempotent).
        var prev = GameObject.Find(ROOT_NAME);
        if (prev != null) Object.DestroyImmediate(prev);

        // Expand Ground for a wider battlefield + grow the NavMeshSurface bake
        // volume so the new Plane is fully covered (default 10x10x10 is way
        // too small once Plane is scaled up).
        var ground = GameObject.Find("Ground");
        if (ground != null)
        {
            var plane = ground.transform.Find("Plane");
            // 5000-per-team layout needs ~200×180m of NavMesh.
            if (plane != null) plane.localScale = new Vector3(22f, 1f, 20f);

            var surface = ground.GetComponent<NavMeshSurface>();
            if (surface != null)
            {
                surface.collectObjects = CollectObjects.Volume;
                surface.size = new Vector3(240f, 10f, 220f);
                surface.center = new Vector3(0f, 2f, 0f);
            }
        }

        // Camera positioned to see the full 200×180m battlefield.
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 140f, -110f);
            cam.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            cam.farClipPlane = 500f;
        }

        // NavMesh bake is intentionally left to the user (manual step) —
        // calling NavMeshSurface.BuildNavMesh() in Editor doesn't persist the
        // NavMeshData asset; PlayMode will reload the scene with the OLD nav
        // mesh and agents won't attach. The proper Bake (NavMeshAssetManager.
        // StartBakingSurfaces) is the Inspector "Bake" button. See the
        // Debug.LogWarning at the end of this method for the user steps.

        var demoRoot = new GameObject(ROOT_NAME);

        // HUD components (BattleStatus + PerfMonitor) + PerfDump (writes JSON to disk).
        demoRoot.AddComponent<BattleStatus>();
        demoRoot.AddComponent<PerfMonitor>();
        demoRoot.AddComponent<PerfDump>();

        // NPC containers
        var npcRoot = new GameObject("NPCs");
        npcRoot.transform.SetParent(demoRoot.transform);

        var teamARoot = new GameObject("TeamA");
        teamARoot.transform.SetParent(npcRoot.transform);
        var teamBRoot = new GameObject("TeamB");
        teamBRoot.transform.SetParent(npcRoot.transform);

        LayerMask sensorLayer = LayerMask.GetMask("Default");

        // Team A — frontline anchored at x = -15 (closer to TeamB).
        SpawnTeam(teamARoot.transform, oldEnemy, Faction.TeamA, sensorLayer,
            anchorX: -15f, facing: +1f,
            warriorMat:  LoadTint("TeamA_Warrior"),
            spearmanMat: LoadTint("TeamA_Spearman"),
            archerMat:   LoadTint("TeamA_Archer"),
            knightMat:   LoadTint("TeamA_Knight"),
            healerMat:   LoadTint("TeamA_Healer"));

        // Team B — frontline at x = +15
        SpawnTeam(teamBRoot.transform, oldEnemy, Faction.TeamB, sensorLayer,
            anchorX: +15f, facing: -1f,
            warriorMat:  LoadTint("TeamB_Warrior"),
            spearmanMat: LoadTint("TeamB_Spearman"),
            archerMat:   LoadTint("TeamB_Archer"),
            knightMat:   LoadTint("TeamB_Knight"),
            healerMat:   LoadTint("TeamB_Healer"));

        EditorSceneManager.MarkSceneDirty(scene);

        // Save scene immediately so the spawned NPCs persist to disk. (If we
        // don't, entering PlayMode reloads the scene from disk and the just-
        // spawned NPCs are gone — only the previous scene state remains.)
        EditorSceneManager.SaveScene(scene);

        int total = (WARRIORS + SPEARMEN + ARCHERS + KNIGHTS + HEALERS) * 2;
        Debug.Log($"[WarSetup] Done. {total} NPCs ({WARRIORS + SPEARMEN + ARCHERS + KNIGHTS + HEALERS} per team). Scene saved.");
        Debug.LogWarning(
            "[WarSetup] NEXT STEPS (manual NavMesh bake — needed when scene/plane size changes):\n" +
            "  1. Select /Ground in Hierarchy\n" +
            "  2. In Inspector → NavMeshSurface component → click Bake\n" +
            "  3. Ctrl+S to save scene again (with the new NavMeshData reference)\n" +
            "  4. Press Play");
    }

    [MenuItem("Tools/BT War/Clear Battle")]
    public static void Clear()
    {
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var rootGo in scene.GetRootGameObjects())
        {
            if (rootGo.name == ROOT_NAME)
            {
                Object.DestroyImmediate(rootGo);
                Debug.Log($"[WarSetup] Removed /{ROOT_NAME}");
            }
            else if (rootGo.name == SOURCE_ENEMY_PATH)
            {
                rootGo.SetActive(true);
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Lay out one team's 100 NPCs in a rough frontline formation.
    /// "facing" controls whether ranks deepen toward +x (1) or -x (-1).
    /// </summary>
    private static void SpawnTeam(
        Transform parent, GameObject template, Faction faction, LayerMask sensorLayer,
        float anchorX, float facing,
        Material warriorMat, Material spearmanMat, Material archerMat, Material knightMat, Material healerMat)
    {
        // Z layout: 30 m wide. Rows tile from front (closest to enemy) to back.
        const float ROW_SPACING = 1.6f;       // x distance between ranks
        const float COL_SPACING = 1.6f;       // z distance between troops

        // We'll keep a running rank index. Rank 0 is the frontline (closest to enemy);
        // rank grows as we move back.
        int rank = 0;

        // 10-wide columns at 100-per-team scale (formation ~16m wide / type).
        const int COLS_PER_ROW = 10;

        // ---- Warriors (20 ranks × 100) front line ----
        for (int i = 0; i < WARRIORS; i++)
        {
            int row = i / COLS_PER_ROW;
            int col = i % COLS_PER_ROW;
            Vector3 pos = FormationPos(anchorX, facing, rank + row, col, COLS_PER_ROW, ROW_SPACING, COL_SPACING);
            SpawnNpc<WarriorRunner>(parent, template, $"Warrior_{i:D4}", pos, warriorMat, faction, sensorLayer,
                walkSpeed: 3.5f, attackRange: 2f, sensorRadius: 12f, maxHp: 120f, dmg: 18f, facing: facing);
        }
        rank += (WARRIORS + COLS_PER_ROW - 1) / COLS_PER_ROW;

        // ---- Spearmen (13 ranks × 100) second line ----
        for (int i = 0; i < SPEARMEN; i++)
        {
            int row = i / COLS_PER_ROW;
            int col = i % COLS_PER_ROW;
            Vector3 pos = FormationPos(anchorX, facing, rank + row, col, COLS_PER_ROW, ROW_SPACING, COL_SPACING);
            SpawnNpc<SpearmanRunner>(parent, template, $"Spearman_{i:D4}", pos, spearmanMat, faction, sensorLayer,
                walkSpeed: 3.5f, attackRange: 4f, sensorRadius: 13f, maxHp: 100f, dmg: 20f, facing: facing);
        }
        rank += (SPEARMEN + COLS_PER_ROW - 1) / COLS_PER_ROW;

        // ---- Archers (10 ranks × 100) third line ----
        for (int i = 0; i < ARCHERS; i++)
        {
            int row = i / COLS_PER_ROW;
            int col = i % COLS_PER_ROW;
            Vector3 pos = FormationPos(anchorX, facing, rank + row, col, COLS_PER_ROW, ROW_SPACING, COL_SPACING);
            SpawnArcher(parent, template, $"Archer_{i:D4}", pos, archerMat, faction, sensorLayer,
                walkSpeed: 3.5f, attackRange: 9f, retreatRange: 4f, shootCooldown: 1.0f,
                sensorRadius: 15f, maxHp: 70f, dmg: 18f, facing: facing);
        }
        rank += (ARCHERS + COLS_PER_ROW - 1) / COLS_PER_ROW;

        // ---- Healers (3 ranks × 100, last row only 50) ----
        for (int i = 0; i < HEALERS; i++)
        {
            int row = i / COLS_PER_ROW;
            int col = i % COLS_PER_ROW;
            Vector3 pos = FormationPos(anchorX, facing, rank + row, col, COLS_PER_ROW, ROW_SPACING, COL_SPACING);
            SpawnHealer(parent, template, $"Healer_{i:D4}", pos, healerMat, faction, sensorLayer,
                walkSpeed: 4f, healRange: 6f, retreatRange: 4f, healCooldown: 1.5f,
                sensorRadius: 10f, maxHp: 60f, healPower: 12f, facing: facing);
        }
        rank += (HEALERS + COLS_PER_ROW - 1) / COLS_PER_ROW;

        // ---- Knights — 5 ranks × 100 in FRONT of warrior line (rank = -1..-5)
        // At this scale (500 knights) the original "split along z flanks" doesn't
        // fit, so they form their own ranks ahead of the main line, charging first.
        for (int i = 0; i < KNIGHTS; i++)
        {
            int row = i / COLS_PER_ROW;
            int col = i % COLS_PER_ROW;
            int knightRank = -(row + 1); // -1 = closest to enemy, -5 = furthest knight rank
            Vector3 pos = FormationPos(anchorX, facing, knightRank, col, COLS_PER_ROW, ROW_SPACING, COL_SPACING);
            SpawnNpc<KnightRunner>(parent, template, $"Knight_{i:D4}", pos, knightMat, faction, sensorLayer,
                walkSpeed: 6f, attackRange: 2.5f, sensorRadius: 14f, maxHp: 150f, dmg: 28f, facing: facing);
        }
    }

    /// <summary>
    /// Returns a world position for the given rank (rows from front) and column (z axis).
    /// </summary>
    private static Vector3 FormationPos(float anchorX, float facing, int rank, int col,
        int colsPerRow, float rowSpacing, float colSpacing)
    {
        // anchorX = front line. Higher rank = deeper into team (further from enemy).
        float x = anchorX - facing * rank * rowSpacing;
        float z = (col - (colsPerRow - 1) / 2f) * colSpacing;
        return new Vector3(x, 0f, z);
    }

    // -------------------------------------------------------------------------
    // Spawn helpers

    private static void ActivateRecursive(GameObject go)
    {
        go.SetActive(true);
        for (int i = 0; i < go.transform.childCount; i++)
            ActivateRecursive(go.transform.GetChild(i).gameObject);
    }

    private static GameObject CloneTemplate(Transform parent, GameObject template, string name, Vector3 pos, Material mat, float facing)
    {
        var clone = Object.Instantiate(template, parent);
        clone.name = name;
        ActivateRecursive(clone);

        // SamplePosition onto the freshly baked NavMesh so the agent doesn't
        // land in mid-air. baseOffset=1 puts the agent body 1m up; we set the
        // transform to the snapped point.
        Vector3 spawn = pos;
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            spawn = hit.position;
        clone.transform.position = spawn;
        clone.transform.rotation = Quaternion.LookRotation(new Vector3(facing, 0f, 0f));

        // Keep NavMeshAgent enabled — NavMesh is now baked-and-saved into the
        // scene asset (see RebakeNavMesh + SaveScene at end of SetupDemoScene),
        // so PlayMode will load it and agents can attach properly.
        // Colliders stay solid (non-trigger) so the agent crowd-avoidance system
        // works as expected.

        var oldNpc = clone.GetComponent<NPCSample>();
        if (oldNpc != null) Object.DestroyImmediate(oldNpc);

        // Assign the per-class sharedMaterial. All NPCs of the same (faction, class)
        // share the same Material asset reference → GPU Instancing collapses them
        // into a single draw call per (mesh, material) combination.
        if (mat != null)
        {
            foreach (var rend in clone.GetComponentsInChildren<Renderer>())
                rend.sharedMaterial = mat;
        }
        return clone;
    }

    private static void ApplyBaseStats(BaseNPCRunner runner, Faction faction, LayerMask sensorLayer,
        float walkSpeed, float attackRange, float sensorRadius, float maxHp, float dmg, float facing)
    {
        var so = new SerializedObject(runner);
        so.FindProperty("_faction").enumValueIndex = (int)faction;
        so.FindProperty("_walkSpeed").floatValue = walkSpeed;
        so.FindProperty("_attackRange").floatValue = attackRange;
        so.FindProperty("_sensorRadius").floatValue = sensorRadius;
        so.FindProperty("_maxHp").floatValue = maxHp;
        so.FindProperty("_attackDamage").floatValue = dmg;
        so.FindProperty("_sensorLayer").intValue = sensorLayer.value;
        // March direction = "toward enemy". facing was already set up so that
        // +1 means enemy lives in +x, -1 means -x.
        so.FindProperty("_enemyDirection").vector3Value = new Vector3(facing, 0f, 0f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SpawnNpc<TRunner>(
        Transform parent, GameObject template, string name, Vector3 pos, Material mat,
        Faction faction, LayerMask sensorLayer,
        float walkSpeed, float attackRange, float sensorRadius, float maxHp, float dmg, float facing)
        where TRunner : BaseNPCRunner
    {
        var go = CloneTemplate(parent, template, name, pos, mat, facing);
        var runner = go.AddComponent<TRunner>();
        ApplyBaseStats(runner, faction, sensorLayer, walkSpeed, attackRange, sensorRadius, maxHp, dmg, facing);
    }

    private static void SpawnArcher(Transform parent, GameObject template, string name, Vector3 pos, Material mat,
        Faction faction, LayerMask sensorLayer,
        float walkSpeed, float attackRange, float retreatRange, float shootCooldown,
        float sensorRadius, float maxHp, float dmg, float facing)
    {
        var go = CloneTemplate(parent, template, name, pos, mat, facing);
        var runner = go.AddComponent<ArcherRunner>();
        ApplyBaseStats(runner, faction, sensorLayer, walkSpeed, attackRange, sensorRadius, maxHp, dmg, facing);
        var so = new SerializedObject(runner);
        so.FindProperty("_retreatRange").floatValue = retreatRange;
        so.FindProperty("_shootCooldown").floatValue = shootCooldown;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SpawnHealer(Transform parent, GameObject template, string name, Vector3 pos, Material mat,
        Faction faction, LayerMask sensorLayer,
        float walkSpeed, float healRange, float retreatRange, float healCooldown,
        float sensorRadius, float maxHp, float healPower, float facing)
    {
        var go = CloneTemplate(parent, template, name, pos, mat, facing);
        var runner = go.AddComponent<HealerRunner>();
        // healRange is stored in _attackRange (heal range = "attack" range), healPower in _attackDamage.
        ApplyBaseStats(runner, faction, sensorLayer, walkSpeed, healRange, sensorRadius, maxHp, healPower, facing);
        var so = new SerializedObject(runner);
        so.FindProperty("_retreatRange").floatValue = retreatRange;
        so.FindProperty("_healCooldown").floatValue = healCooldown;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Note: BuildNavMesh() runtime API does NOT persist the NavMeshData asset,
    // so PlayMode after enter doesn't see the new bake. Use Inspector "Bake"
    // button on the NavMeshSurface to get a proper saved asset.
}
#endif
