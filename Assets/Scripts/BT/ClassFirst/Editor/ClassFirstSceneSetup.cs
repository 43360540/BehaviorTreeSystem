#if UNITY_EDITOR
using System.Collections.Generic;
using BehaviorTree.ClassFirst;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// One-click Editor utility that re-configures SampleScene for the
/// Class-First NPC demo: disables the existing State-Style Enemy, drops a few
/// obstacles, and spawns 2 each of Warrior / Archer / Patroller.
///
/// All spawned content sits under a single "ClassFirst_Demo" root GameObject so
/// it's easy to delete or re-run.
/// </summary>
public static class ClassFirstSceneSetup
{
    private const string ROOT_NAME = "ClassFirst_Demo";
    private const string SOURCE_ENEMY_PATH = "Enemy"; // root-level object in SampleScene

    [MenuItem("Tools/BT ClassFirst/Setup Demo Scene")]
    public static void SetupDemoScene()
    {
        var scene = EditorSceneManager.GetActiveScene();

        // 1. Disable existing State-Style Enemy (root level, NOT the child of
        //    the same name). GameObject.Find walks the whole scene by name and
        //    may grab the inner child first, so be explicit about root.
        GameObject oldEnemy = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == SOURCE_ENEMY_PATH) { oldEnemy = root; break; }
        }
        if (oldEnemy != null)
        {
            oldEnemy.SetActive(false);
            Debug.Log($"[ClassFirstSetup] Disabled root /{SOURCE_ENEMY_PATH}");
        }
        else
        {
            Debug.LogError($"[ClassFirstSetup] No root /{SOURCE_ENEMY_PATH} found — abort. " +
                           "Open SampleScene first.");
            return;
        }

        // 2. Clear previous demo root if any (so the menu is idempotent).
        var prev = GameObject.Find(ROOT_NAME);
        if (prev != null)
        {
            Object.DestroyImmediate(prev);
            Debug.Log($"[ClassFirstSetup] Removed previous /{ROOT_NAME}");
        }

        var demoRoot = new GameObject(ROOT_NAME);

        // 3. Obstacles
        var obstaclesRoot = new GameObject("Obstacles");
        obstaclesRoot.transform.SetParent(demoRoot.transform);
        SpawnObstacle(obstaclesRoot.transform, "Obstacle_A", new Vector3( 4f, 1f,  3f), new Vector3(2.5f, 2f, 2.5f));
        SpawnObstacle(obstaclesRoot.transform, "Obstacle_B", new Vector3(-4f, 1f, -3f), new Vector3(3f, 2f, 1.5f));
        SpawnObstacle(obstaclesRoot.transform, "Obstacle_C", new Vector3( 0f, 1f,  6f), new Vector3(1.5f, 2f, 3f));
        SpawnObstacle(obstaclesRoot.transform, "Obstacle_D", new Vector3(-6f, 1f,  2f), new Vector3(1.2f, 2f, 1.2f));

        // 4. Build NPC sensor layer mask (everything except Ignore Raycast & UI).
        //    They all live on the Default layer in this demo, so the mask is just Default.
        LayerMask sensorLayer = LayerMask.GetMask("Default");

        // Patrol routes for Patrollers (in world space).
        Vector3[] patrolA = {
            new Vector3( 7f, 0f,  7f),
            new Vector3( 7f, 0f, -3f),
            new Vector3( 2f, 0f, -3f),
            new Vector3( 2f, 0f,  7f),
        };
        Vector3[] patrolB = {
            new Vector3(-7f, 0f, -6f),
            new Vector3(-7f, 0f,  4f),
            new Vector3(-2f, 0f,  4f),
            new Vector3(-2f, 0f, -6f),
        };

        // 5. Spawn 6 NPCs (2 of each type) at distinct positions, each with a tint.
        var npcRoot = new GameObject("NPCs");
        npcRoot.transform.SetParent(demoRoot.transform);

        var sourceTemplate = oldEnemy; // we clone its hierarchy as visuals

        SpawnNpc<WarriorRunner>(
            npcRoot.transform, sourceTemplate, "Warrior_A",
            position: new Vector3( 8f, 0f,  0f),
            tint: new Color(0.85f, 0.15f, 0.15f),
            faction: Faction.TeamA, sensorLayer: sensorLayer,
            walkSpeed: 4.0f, attackRange: 2.0f, sensorRadius: 12f, maxHp: 120f, dmg: 22f);

        SpawnNpc<WarriorRunner>(
            npcRoot.transform, sourceTemplate, "Warrior_B",
            position: new Vector3(-8f, 0f,  0f),
            tint: new Color(0.6f, 0.05f, 0.05f),
            faction: Faction.TeamB, sensorLayer: sensorLayer,
            walkSpeed: 3.5f, attackRange: 2.0f, sensorRadius: 10f, maxHp: 140f, dmg: 18f);

        SpawnNpc<ArcherRunner>(
            npcRoot.transform, sourceTemplate, "Archer_A",
            position: new Vector3( 0f, 0f,  9f),
            tint: new Color(0.2f, 0.55f, 0.95f),
            faction: Faction.TeamA, sensorLayer: sensorLayer,
            walkSpeed: 3.5f, attackRange: 8.0f, sensorRadius: 14f, maxHp: 70f, dmg: 18f,
            archerRetreat: 4.5f, archerCooldown: 1.0f);

        SpawnNpc<ArcherRunner>(
            npcRoot.transform, sourceTemplate, "Archer_B",
            position: new Vector3( 0f, 0f, -9f),
            tint: new Color(0.05f, 0.3f, 0.7f),
            faction: Faction.TeamB, sensorLayer: sensorLayer,
            walkSpeed: 4.0f, attackRange: 7.5f, sensorRadius: 14f, maxHp: 60f, dmg: 22f,
            archerRetreat: 5.0f, archerCooldown: 1.3f);

        SpawnNpc<PatrollerRunner>(
            npcRoot.transform, sourceTemplate, "Patroller_A",
            position: new Vector3( 4f, 0f,  4f),
            tint: new Color(0.2f, 0.7f, 0.2f),
            faction: Faction.TeamA, sensorLayer: sensorLayer,
            walkSpeed: 3.0f, attackRange: 2.0f, sensorRadius: 9f, maxHp: 100f, dmg: 15f,
            patrolPoints: patrolA);

        SpawnNpc<PatrollerRunner>(
            npcRoot.transform, sourceTemplate, "Patroller_B",
            position: new Vector3(-4f, 0f, -4f),
            tint: new Color(0.05f, 0.45f, 0.05f),
            faction: Faction.TeamB, sensorLayer: sensorLayer,
            walkSpeed: 2.5f, attackRange: 2.0f, sensorRadius: 11f, maxHp: 110f, dmg: 14f,
            patrolPoints: patrolB);

        // Rebake NavMesh — obstacles + scene-copy might leave the bake stale.
        RebakeNavMesh();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[ClassFirstSetup] Done. /{ROOT_NAME} contains 4 obstacles + 6 NPCs.");
    }

    private static void RebakeNavMesh()
    {
        var surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        foreach (var s in surfaces) s.BuildNavMesh();
        Debug.Log($"[ClassFirstSetup] Rebaked {surfaces.Length} NavMeshSurface(s).");
    }

    [MenuItem("Tools/BT ClassFirst/Clear Demo Scene")]
    public static void ClearDemoScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var rootGo in scene.GetRootGameObjects())
        {
            if (rootGo.name == ROOT_NAME)
            {
                Object.DestroyImmediate(rootGo);
                Debug.Log($"[ClassFirstSetup] Removed /{ROOT_NAME}");
            }
            else if (rootGo.name == SOURCE_ENEMY_PATH)
            {
                rootGo.SetActive(true);
                Debug.Log($"[ClassFirstSetup] Re-enabled /{SOURCE_ENEMY_PATH}");
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
    }

    // ----- helpers ----------------------------------------------------------

    private static void ActivateRecursive(GameObject go)
    {
        go.SetActive(true);
        for (int i = 0; i < go.transform.childCount; i++)
            ActivateRecursive(go.transform.GetChild(i).gameObject);
    }


    private static GameObject SpawnObstacle(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = size;

        var obstacle = go.AddComponent<NavMeshObstacle>();
        obstacle.carving = true;
        obstacle.carveOnlyStationary = false;
        // Size is auto-derived from collider for default cube, but force-set so
        // it matches the visual scale precisely.
        obstacle.size = Vector3.one;

        // Tint dark grey for visibility.
        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            // sharedMaterial mutation would leak; create a per-instance material.
            var mat = new Material(rend.sharedMaterial);
            mat.color = new Color(0.3f, 0.3f, 0.32f);
            rend.sharedMaterial = mat;
        }
        return go;
    }

    private static void SpawnNpc<TRunner>(
        Transform parent,
        GameObject template,
        string name,
        Vector3 position,
        Color tint,
        Faction faction,
        LayerMask sensorLayer,
        float walkSpeed,
        float attackRange,
        float sensorRadius,
        float maxHp,
        float dmg,
        Vector3[] patrolPoints = null,
        float archerRetreat = 4f,
        float archerCooldown = 1.2f) where TRunner : BaseNPCRunner
    {
        // Clone the visual hierarchy from the State-Style Enemy template.
        var clone = Object.Instantiate(template, parent);
        clone.name = name;
        ActivateRecursive(clone);
        clone.transform.position = position;
        clone.transform.rotation = Quaternion.identity;

        // Remove the old NPCSample (State-Style); we'll attach our own Runner.
        var oldRunner = clone.GetComponent<NPCSample>();
        if (oldRunner != null) Object.DestroyImmediate(oldRunner);

        // Attach the Class-First runner and configure via SerializedObject so
        // the Inspector reflects values after save.
        var runner = clone.AddComponent<TRunner>();
        ApplyRunnerStats(runner, faction, sensorLayer,
            walkSpeed, attackRange, sensorRadius, maxHp, dmg, patrolPoints);

        if (runner is ArcherRunner archer)
            ApplyArcherStats(archer, archerRetreat, archerCooldown);

        // Tint all renderers on this instance to differentiate factions.
        foreach (var rend in clone.GetComponentsInChildren<Renderer>())
        {
            var mat = new Material(rend.sharedMaterial);
            mat.color = tint;
            rend.sharedMaterial = mat;
        }
    }

    private static void ApplyRunnerStats(
        BaseNPCRunner runner,
        Faction faction,
        LayerMask sensorLayer,
        float walkSpeed,
        float attackRange,
        float sensorRadius,
        float maxHp,
        float dmg,
        Vector3[] patrolPoints)
    {
        var so = new SerializedObject(runner);
        so.FindProperty("_faction").enumValueIndex = (int)faction;
        so.FindProperty("_walkSpeed").floatValue = walkSpeed;
        so.FindProperty("_attackRange").floatValue = attackRange;
        so.FindProperty("_sensorRadius").floatValue = sensorRadius;
        so.FindProperty("_maxHp").floatValue = maxHp;
        so.FindProperty("_attackDamage").floatValue = dmg;
        so.FindProperty("_sensorLayer").intValue = sensorLayer.value;

        var patrol = so.FindProperty("_patrolPoints");
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            patrol.arraySize = patrolPoints.Length;
            for (int i = 0; i < patrolPoints.Length; i++)
                patrol.GetArrayElementAtIndex(i).vector3Value = patrolPoints[i];
        }
        else patrol.arraySize = 0;

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplyArcherStats(ArcherRunner archer, float retreat, float cooldown)
    {
        var so = new SerializedObject(archer);
        var retreatProp = so.FindProperty("_retreatRange");
        var cdProp = so.FindProperty("_shootCooldown");
        if (retreatProp != null) retreatProp.floatValue = retreat;
        if (cdProp != null) cdProp.floatValue = cooldown;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
