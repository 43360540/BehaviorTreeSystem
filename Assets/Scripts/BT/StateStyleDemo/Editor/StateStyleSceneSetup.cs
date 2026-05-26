#if UNITY_EDITOR
using System.IO;
using BehaviorTree.ClassFirst; // Faction enum (shared combat)
using BehaviorTree.StateStyleDemo;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Mirror of ClassFirstSceneSetup, but for the State-Style version of the demo.
/// Lives in a SEPARATE scene (SampleScene_StateStyle) so the two demos can
/// be inspected side-by-side without their NPCs sensing each other.
/// </summary>
public static class StateStyleSceneSetup
{
    private const string ROOT_NAME = "StateStyle_Demo";
    private const string SOURCE_ENEMY_PATH = "Enemy";
    private const string SOURCE_SCENE_PATH = "Assets/Scenes/SampleScene.unity";
    private const string TARGET_SCENE_PATH = "Assets/Scenes/SampleScene_StateStyle.unity";
    // Note: 3v3 demo uses `new Material()` per-NPC for simplicity. At 6 NPCs the
    // perf cost of unique material instances is negligible.

    [MenuItem("Tools/BT StateStyle/Create State Style Scene")]
    public static void CreateStateStyleScene()
    {
        if (!File.Exists(SOURCE_SCENE_PATH))
        {
            Debug.LogError($"[StateStyleSetup] Source scene not found at {SOURCE_SCENE_PATH}");
            return;
        }
        if (File.Exists(TARGET_SCENE_PATH))
        {
            if (!EditorUtility.DisplayDialog(
                "Replace existing scene?",
                $"{TARGET_SCENE_PATH} already exists. Overwrite?",
                "Overwrite", "Cancel"))
            {
                Debug.Log("[StateStyleSetup] Aborted by user.");
                return;
            }
            AssetDatabase.DeleteAsset(TARGET_SCENE_PATH);
        }

        bool ok = AssetDatabase.CopyAsset(SOURCE_SCENE_PATH, TARGET_SCENE_PATH);
        if (!ok)
        {
            Debug.LogError($"[StateStyleSetup] Failed to copy {SOURCE_SCENE_PATH} → {TARGET_SCENE_PATH}");
            return;
        }
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(TARGET_SCENE_PATH, OpenSceneMode.Single);
        Debug.Log($"[StateStyleSetup] Created and opened {TARGET_SCENE_PATH}. " +
                  "Now run \"Tools/BT StateStyle/Setup Demo Scene\".");
    }

    [MenuItem("Tools/BT StateStyle/Setup Demo Scene")]
    public static void SetupDemoScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != TARGET_SCENE_PATH)
        {
            Debug.LogWarning($"[StateStyleSetup] Current scene is not {TARGET_SCENE_PATH}. " +
                             "Run \"Create State Style Scene\" first, or open it manually.");
            return;
        }

        // Disable root /Enemy (State-Style NPCSample) — same trick as ClassFirstSceneSetup.
        GameObject oldEnemy = null;
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == SOURCE_ENEMY_PATH) { oldEnemy = root; break; }
        if (oldEnemy == null)
        {
            Debug.LogError($"[StateStyleSetup] No root /{SOURCE_ENEMY_PATH} found.");
            return;
        }
        oldEnemy.SetActive(false);

        // Also clear any leftover ClassFirst_Demo root if user duplicated from a
        // SetupClassFirst scene.
        var leftover = GameObject.Find("ClassFirst_Demo");
        if (leftover != null) Object.DestroyImmediate(leftover);

        var prev = GameObject.Find(ROOT_NAME);
        if (prev != null) Object.DestroyImmediate(prev);

        var demoRoot = new GameObject(ROOT_NAME);

        // Obstacles (same layout as ClassFirst for visual parity).
        var obstaclesRoot = new GameObject("Obstacles");
        obstaclesRoot.transform.SetParent(demoRoot.transform);
        SpawnObstacle(obstaclesRoot.transform, "Obstacle_A", new Vector3( 4f, 1f,  3f), new Vector3(2.5f, 2f, 2.5f));
        SpawnObstacle(obstaclesRoot.transform, "Obstacle_B", new Vector3(-4f, 1f, -3f), new Vector3(3f, 2f, 1.5f));
        SpawnObstacle(obstaclesRoot.transform, "Obstacle_C", new Vector3( 0f, 1f,  6f), new Vector3(1.5f, 2f, 3f));
        SpawnObstacle(obstaclesRoot.transform, "Obstacle_D", new Vector3(-6f, 1f,  2f), new Vector3(1.2f, 2f, 1.2f));

        LayerMask sensorLayer = LayerMask.GetMask("Default");

        Vector3[] patrolA = {
            new Vector3( 7f, 0f,  7f), new Vector3( 7f, 0f, -3f),
            new Vector3( 2f, 0f, -3f), new Vector3( 2f, 0f,  7f),
        };
        Vector3[] patrolB = {
            new Vector3(-7f, 0f, -6f), new Vector3(-7f, 0f,  4f),
            new Vector3(-2f, 0f,  4f), new Vector3(-2f, 0f, -6f),
        };

        var npcRoot = new GameObject("NPCs");
        npcRoot.transform.SetParent(demoRoot.transform);

        SpawnWarrior(npcRoot.transform, oldEnemy, "Warrior_A", new Vector3( 8f, 0f,  0f),
            new Color(0.85f, 0.15f, 0.15f), Faction.TeamA, sensorLayer,
            walkSpeed: 4.0f, attackRange: 2.0f, sensorRadius: 12f, maxHp: 120f, dmg: 22f);
        SpawnWarrior(npcRoot.transform, oldEnemy, "Warrior_B", new Vector3(-8f, 0f,  0f),
            new Color(0.6f, 0.05f, 0.05f), Faction.TeamB, sensorLayer,
            walkSpeed: 3.5f, attackRange: 2.0f, sensorRadius: 10f, maxHp: 140f, dmg: 18f);

        SpawnArcher(npcRoot.transform, oldEnemy, "Archer_A", new Vector3( 0f, 0f,  9f),
            new Color(0.2f, 0.55f, 0.95f), Faction.TeamA, sensorLayer,
            walkSpeed: 3.5f, attackRange: 8.0f, retreatRange: 4.5f,
            shootCooldown: 1.0f, sensorRadius: 14f, maxHp: 70f, dmg: 18f);
        SpawnArcher(npcRoot.transform, oldEnemy, "Archer_B", new Vector3( 0f, 0f, -9f),
            new Color(0.05f, 0.3f, 0.7f), Faction.TeamB, sensorLayer,
            walkSpeed: 4.0f, attackRange: 7.5f, retreatRange: 5.0f,
            shootCooldown: 1.3f, sensorRadius: 14f, maxHp: 60f, dmg: 22f);

        SpawnPatroller(npcRoot.transform, oldEnemy, "Patroller_A", new Vector3( 4f, 0f,  4f),
            new Color(0.2f, 0.7f, 0.2f), Faction.TeamA, sensorLayer,
            walkSpeed: 3.0f, attackRange: 2.0f, sensorRadius: 9f, maxHp: 100f, dmg: 15f,
            patrolPoints: patrolA);
        SpawnPatroller(npcRoot.transform, oldEnemy, "Patroller_B", new Vector3(-4f, 0f, -4f),
            new Color(0.05f, 0.45f, 0.05f), Faction.TeamB, sensorLayer,
            walkSpeed: 2.5f, attackRange: 2.0f, sensorRadius: 11f, maxHp: 110f, dmg: 14f,
            patrolPoints: patrolB);

        RebakeNavMesh();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[StateStyleSetup] Done. /{ROOT_NAME} contains 4 obstacles + 6 NPCs.");
    }

    private static void RebakeNavMesh()
    {
        var surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        foreach (var s in surfaces) s.BuildNavMesh();
        Debug.Log($"[StateStyleSetup] Rebaked {surfaces.Length} NavMeshSurface(s).");
    }

    [MenuItem("Tools/BT StateStyle/Clear Demo Scene")]
    public static void ClearDemoScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var rootGo in scene.GetRootGameObjects())
        {
            if (rootGo.name == ROOT_NAME)
            {
                Object.DestroyImmediate(rootGo);
                Debug.Log($"[StateStyleSetup] Removed /{ROOT_NAME}");
            }
            else if (rootGo.name == SOURCE_ENEMY_PATH)
            {
                rootGo.SetActive(true);
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
    }

    // --- helpers --------------------------------------------------------------

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
        obstacle.size = Vector3.one;
        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = new Material(rend.sharedMaterial);
            mat.color = new Color(0.3f, 0.3f, 0.32f);
            rend.sharedMaterial = mat;
        }
        return go;
    }

    private static GameObject CloneTemplate(Transform parent, GameObject template, string name, Vector3 pos, Color tint)
    {
        var clone = Object.Instantiate(template, parent);
        clone.name = name;
        ActivateRecursive(clone);
        clone.transform.position = pos;
        clone.transform.rotation = Quaternion.identity;

        // Remove the State-Style NPCSample (from source Enemy) — we'll add our own.
        var oldNpc = clone.GetComponent<NPCSample>();
        if (oldNpc != null) Object.DestroyImmediate(oldNpc);

        foreach (var rend in clone.GetComponentsInChildren<Renderer>())
        {
            var mat = new Material(rend.sharedMaterial);
            mat.color = tint;
            rend.sharedMaterial = mat;
        }
        return clone;
    }

    private static void SpawnWarrior(Transform parent, GameObject template, string name, Vector3 pos, Color tint,
        Faction faction, LayerMask sensorLayer,
        float walkSpeed, float attackRange, float sensorRadius, float maxHp, float dmg)
    {
        var go = CloneTemplate(parent, template, name, pos, tint);
        var npc = go.AddComponent<WarriorState>();
        var so = new SerializedObject(npc);
        so.FindProperty("_faction").enumValueIndex = (int)faction;
        so.FindProperty("_walkSpeed").floatValue = walkSpeed;
        so.FindProperty("_attackRange").floatValue = attackRange;
        so.FindProperty("_sensorRadius").floatValue = sensorRadius;
        so.FindProperty("_maxHp").floatValue = maxHp;
        so.FindProperty("_attackDamage").floatValue = dmg;
        so.FindProperty("_sensorLayer").intValue = sensorLayer.value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SpawnArcher(Transform parent, GameObject template, string name, Vector3 pos, Color tint,
        Faction faction, LayerMask sensorLayer,
        float walkSpeed, float attackRange, float retreatRange, float shootCooldown,
        float sensorRadius, float maxHp, float dmg)
    {
        var go = CloneTemplate(parent, template, name, pos, tint);
        var npc = go.AddComponent<ArcherState>();
        var so = new SerializedObject(npc);
        so.FindProperty("_faction").enumValueIndex = (int)faction;
        so.FindProperty("_walkSpeed").floatValue = walkSpeed;
        so.FindProperty("_attackRange").floatValue = attackRange;
        so.FindProperty("_retreatRange").floatValue = retreatRange;
        so.FindProperty("_shootCooldown").floatValue = shootCooldown;
        so.FindProperty("_sensorRadius").floatValue = sensorRadius;
        so.FindProperty("_maxHp").floatValue = maxHp;
        so.FindProperty("_attackDamage").floatValue = dmg;
        so.FindProperty("_sensorLayer").intValue = sensorLayer.value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SpawnPatroller(Transform parent, GameObject template, string name, Vector3 pos, Color tint,
        Faction faction, LayerMask sensorLayer,
        float walkSpeed, float attackRange, float sensorRadius, float maxHp, float dmg,
        Vector3[] patrolPoints)
    {
        var go = CloneTemplate(parent, template, name, pos, tint);
        var npc = go.AddComponent<PatrollerState>();
        var so = new SerializedObject(npc);
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
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
