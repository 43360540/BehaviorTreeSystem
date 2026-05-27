#if UNITY_EDITOR
using System.IO;
using BehaviorTree.ClassFirst;
using BehaviorTree.ClassFirst.Duel;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace BehaviorTree.ClassFirst.Duel.Editor
{
    /// <summary>
    /// One-click Editor utility that builds the 1v1 tactical duel demo:
    /// 40x40m arena with two roofless buildings, a handful of free-standing
    /// walls + pillars (each tagged with a CoverPoint), and one Duelist + one
    /// Marksman dropped on opposite corners.
    ///
    /// <para>Menu items:</para>
    /// <list type="bullet">
    /// <item><c>Tools/BT Duel/Create Duel Scene</c> — copy SampleScene -> SampleScene_Duel.</item>
    /// <item><c>Tools/BT Duel/Setup Duel Arena</c>  — wipe old /Duel_Demo, build arena, spawn 2 NPCs.</item>
    /// <item><c>Tools/BT Duel/Clear Duel</c>        — remove /Duel_Demo.</item>
    /// </list>
    ///
    /// <para>NavMesh bake is intentionally left as a manual step (consistent
    /// with WarSceneSetup) — runtime BuildNavMesh doesn't persist the
    /// NavMeshData asset to disk so PlayMode would otherwise lose the bake.</para>
    /// </summary>
    public static class DuelSceneSetup
    {
        private const string ROOT_NAME = "Duel_Demo";
        private const string SOURCE_ENEMY_PATH = "Enemy";
        private const string SOURCE_SCENE_PATH = "Assets/Scenes/SampleScene.unity";
        private const string TARGET_SCENE_PATH = "Assets/Scenes/SampleScene_Duel.unity";

        // Arena dims
        private const float ARENA_SIZE = 40f;          // 40m x 40m
        private const float PLANE_SCALE = ARENA_SIZE / 10f; // default Plane primitive is 10m
        private const float NAVMESH_BOUND = ARENA_SIZE + 4f;

        // Tint materials (reuse the per-class assets created for the war demo).
        private const string DUELIST_MAT_PATH  = "Assets/Materials/Tints/TeamA_Warrior.mat";
        private const string MARKSMAN_MAT_PATH = "Assets/Materials/Tints/TeamB_Archer.mat";

        // -----------------------------------------------------------------
        // Menu: Create Duel Scene
        // -----------------------------------------------------------------
        [MenuItem("Tools/BT Duel/Create Duel Scene")]
        public static void CreateDuelScene()
        {
            if (!File.Exists(SOURCE_SCENE_PATH))
            {
                Debug.LogError($"[DuelSetup] Source scene not found at {SOURCE_SCENE_PATH}");
                return;
            }
            if (File.Exists(TARGET_SCENE_PATH))
            {
                if (!EditorUtility.DisplayDialog("Replace existing scene?",
                    $"{TARGET_SCENE_PATH} already exists. Overwrite?",
                    "Overwrite", "Cancel"))
                {
                    Debug.Log("[DuelSetup] Aborted.");
                    return;
                }
                AssetDatabase.DeleteAsset(TARGET_SCENE_PATH);
            }
            if (!AssetDatabase.CopyAsset(SOURCE_SCENE_PATH, TARGET_SCENE_PATH))
            {
                Debug.LogError("[DuelSetup] Failed to copy scene.");
                return;
            }
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(TARGET_SCENE_PATH, OpenSceneMode.Single);
            Debug.Log($"[DuelSetup] Opened {TARGET_SCENE_PATH}. Now run \"Tools/BT Duel/Setup Duel Arena\".");
        }

        // -----------------------------------------------------------------
        // Menu: Setup Duel Arena
        // -----------------------------------------------------------------
        [MenuItem("Tools/BT Duel/Setup Duel Arena")]
        public static void SetupDuelArena()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != TARGET_SCENE_PATH)
            {
                Debug.LogWarning($"[DuelSetup] Current scene is not {TARGET_SCENE_PATH}. " +
                                 "Run \"Create Duel Scene\" first or open it manually.");
                return;
            }

            // Disable existing root /Enemy (the demo template used for visuals).
            GameObject sourceEnemy = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == SOURCE_ENEMY_PATH) { sourceEnemy = root; break; }
            if (sourceEnemy == null)
            {
                Debug.LogError($"[DuelSetup] No root /{SOURCE_ENEMY_PATH} found.");
                return;
            }
            sourceEnemy.SetActive(false);

            // Wipe previous arena (idempotent).
            var prev = GameObject.Find(ROOT_NAME);
            if (prev != null) Object.DestroyImmediate(prev);

            // Resize plane + NavMeshSurface for the duel arena.
            var ground = GameObject.Find("Ground");
            if (ground != null)
            {
                var plane = ground.transform.Find("Plane");
                if (plane != null) plane.localScale = new Vector3(PLANE_SCALE, 1f, PLANE_SCALE);

                var surface = ground.GetComponent<NavMeshSurface>();
                if (surface != null)
                {
                    surface.collectObjects = CollectObjects.Volume;
                    surface.size = new Vector3(NAVMESH_BOUND, 10f, NAVMESH_BOUND);
                    surface.center = new Vector3(0f, 2f, 0f);
                }
            }

            // Camera — look diagonally down so the whole arena fits and depth
            // (cover positions) is readable.
            // var cam = Camera.main;
            // if (cam != null)
            // {
            //     cam.transform.position = new Vector3(0f, 35f, -28f);
            //     cam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            //     cam.farClipPlane = 200f;
            // }

            // Build hierarchy
            var demoRoot = new GameObject(ROOT_NAME);
            demoRoot.AddComponent<DuelHud>();
            demoRoot.AddComponent<DuelStateDumper>();
            var arenaRoot = new GameObject("Arena");
            arenaRoot.transform.SetParent(demoRoot.transform);
            var coverRoot = new GameObject("Cover");
            coverRoot.transform.SetParent(demoRoot.transform);
            var npcRoot = new GameObject("NPCs");
            npcRoot.transform.SetParent(demoRoot.transform);

            BuildArena(arenaRoot.transform, coverRoot.transform);

            // NPCs in the duel scene are spawned from a template that lives on
            // the "Ground" layer (inherited from SampleScene's Enemy prefab),
            // so the sensor sphere must include Ground or Sense returns null
            // and both NPCs idle forever. Including Default too keeps it
            // future-proof against template layer changes.
            LayerMask sensorLayer = LayerMask.GetMask("Default", "Ground");

            var duelistMat  = AssetDatabase.LoadAssetAtPath<Material>(DUELIST_MAT_PATH);
            var marksmanMat = AssetDatabase.LoadAssetAtPath<Material>(MARKSMAN_MAT_PATH);
            if (duelistMat == null)  Debug.LogWarning($"[DuelSetup] Missing {DUELIST_MAT_PATH} — duelist will be untinted.");
            if (marksmanMat == null) Debug.LogWarning($"[DuelSetup] Missing {MARKSMAN_MAT_PATH} — marksman will be untinted.");

            Vector3 duelistPos  = new Vector3(-15f, 0f, -15f);
            Vector3 marksmanPos = new Vector3(+15f, 0f, +15f);

            SpawnDuelist(npcRoot.transform, sourceEnemy,
                position: duelistPos, enemyPos: marksmanPos,
                mat: duelistMat, sensorLayer: sensorLayer);

            SpawnMarksman(npcRoot.transform, sourceEnemy,
                position: marksmanPos, enemyPos: duelistPos,
                mat: marksmanMat, sensorLayer: sensorLayer);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[DuelSetup] Arena built + 1 Duelist + 1 Marksman spawned. Scene saved.");
            Debug.LogWarning(
                "[DuelSetup] NEXT STEPS (manual NavMesh bake — needed when scene/plane size changes):\n" +
                "  1. Select /Ground in Hierarchy\n" +
                "  2. In Inspector -> NavMeshSurface component -> click Bake\n" +
                "  3. Ctrl+S to save scene again (with the new NavMeshData reference)\n" +
                "  4. Press Play");
        }

        // -----------------------------------------------------------------
        // Menu: Clear Duel
        // -----------------------------------------------------------------
        [MenuItem("Tools/BT Duel/Clear Duel")]
        public static void Clear()
        {
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == ROOT_NAME)
                {
                    Object.DestroyImmediate(root);
                    Debug.Log($"[DuelSetup] Removed /{ROOT_NAME}");
                }
                else if (root.name == SOURCE_ENEMY_PATH)
                {
                    root.SetActive(true);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        // =================================================================
        // Arena building
        // =================================================================

        private static void BuildArena(Transform arena, Transform cover)
        {
            // Two buildings: NE quadrant and SW quadrant. Entrance side picked
            // so the gap faces the arena center (where most fighting happens).
            // entranceSideIdx convention: 0 = -Z, 1 = +X, 2 = +Z, 3 = -X.
            BuildRooflessBox(arena, cover,
                center: new Vector3(+8f, 0f, +8f),
                size: new Vector2(8f, 8f),
                entranceSideIdx: 3); // NE building, entrance on -X side -> opens toward arena center

            BuildRooflessBox(arena, cover,
                center: new Vector3(-8f, 0f, -8f),
                size: new Vector2(8f, 8f),
                entranceSideIdx: 1); // SW building, entrance on +X side -> opens toward arena center

            // High walls (4m wide x 3m tall x 0.3m thick) — directional cover.
            // coverFromSide convention: NPC stands on the OPPOSITE side of `outward`.
            //   +1 (yaw=0)  -> outward = +Z, NPC stands south of wall, blocks +Z threats.
            //   -1 (yaw=0)  -> outward = -Z, NPC stands north of wall, blocks -Z threats.
            //   +1 (yaw=90) -> outward = +X, NPC stands west of wall, blocks +X threats.
            //   -1 (yaw=90) -> outward = -X, NPC stands east of wall, blocks -X threats.

            // South-arena wall (z=-5): Duelist (south spawn) hides behind to block Marksman's shots from the north.
            SpawnWall(arena, cover,
                center: new Vector3(0f, 0f, -5f),
                size: new Vector3(4f, 3f, 0.3f), yawDeg: 0f,
                coverFromSide: +1);

            // North-arena wall (z=+5): Marksman hides behind to block Duelist's approach from the south.
            SpawnWall(arena, cover,
                center: new Vector3(0f, 0f, +5f),
                size: new Vector3(4f, 3f, 0.3f), yawDeg: 0f,
                coverFromSide: -1);

            // West side wall (yaw=90): blocks +X threats; cover on west face.
            SpawnWall(arena, cover,
                center: new Vector3(-5f, 0f, +2f),
                size: new Vector3(3f, 3f, 0.3f), yawDeg: 90f,
                coverFromSide: +1);

            // East side wall (yaw=90): blocks -X threats; cover on east face.
            SpawnWall(arena, cover,
                center: new Vector3(+5f, 0f, -2f),
                size: new Vector3(3f, 3f, 0.3f), yawDeg: 90f,
                coverFromSide: -1);

            // Low walls (3m x 1.2m tall). Smaller silhouette, mid-arena cover.
            SpawnWall(arena, cover,
                center: new Vector3(-3f, 0f, -3f),
                size: new Vector3(3f, 1.2f, 0.3f), yawDeg: 30f,
                coverFromSide: +1);
            SpawnWall(arena, cover,
                center: new Vector3(+3f, 0f, +3f),
                size: new Vector3(3f, 1.2f, 0.3f), yawDeg: -30f,
                coverFromSide: -1);

            // Pillars (omnidirectional cover) sprinkled around the open ground.
            var pillarPositions = new[]
            {
                new Vector3(-10f, 0f, +2f),
                new Vector3(+10f, 0f, -2f),
                new Vector3(-2f, 0f, +12f),
                new Vector3(+2f, 0f, -12f),
                new Vector3(-12f, 0f, +10f),
                new Vector3(+12f, 0f, -10f),
            };
            foreach (var p in pillarPositions)
                SpawnPillar(arena, cover, p);
        }

        /// <summary>
        /// Four walls forming a 'C' or 'U' room (one side has a 2.5m wide entrance gap).
        /// Adds cover points at the outside of each wall.
        /// <para>entranceSideIdx: 0 = -Z (south), 1 = +X (east), 2 = +Z (north), 3 = -X (west).</para>
        /// </summary>
        private static void BuildRooflessBox(Transform arena, Transform cover,
            Vector3 center, Vector2 size, int entranceSideIdx)
        {
            float halfX = size.x * 0.5f;
            float halfZ = size.y * 0.5f;
            float wallH = 3f;
            float wallT = 0.3f;
            float gap   = 2.5f;

            int sideIdx = ((entranceSideIdx % 4) + 4) % 4;

            // Build each side
            for (int s = 0; s < 4; s++)
            {
                bool isEntrance = (s == sideIdx);
                Vector3 wallCenter;
                Vector3 wallSize;
                float yaw;
                int coverFromSide; // +1 = cover from "outside" the box, -1 = from "inside"

                switch (s)
                {
                    case 0: // -Z side
                        wallCenter = center + new Vector3(0f, 0f, -halfZ);
                        wallSize = new Vector3(size.x, wallH, wallT);
                        yaw = 0f;
                        coverFromSide = -1;
                        break;
                    case 1: // +X side
                        wallCenter = center + new Vector3(halfX, 0f, 0f);
                        wallSize = new Vector3(size.y, wallH, wallT);
                        yaw = 90f;
                        coverFromSide = +1;
                        break;
                    case 2: // +Z side
                        wallCenter = center + new Vector3(0f, 0f, halfZ);
                        wallSize = new Vector3(size.x, wallH, wallT);
                        yaw = 0f;
                        coverFromSide = +1;
                        break;
                    default: // -X side
                        wallCenter = center + new Vector3(-halfX, 0f, 0f);
                        wallSize = new Vector3(size.y, wallH, wallT);
                        yaw = 90f;
                        coverFromSide = -1;
                        break;
                }

                if (!isEntrance)
                {
                    SpawnWall(arena, cover, wallCenter, wallSize, yaw, coverFromSide);
                }
                else
                {
                    // Split into two segments with a gap.
                    float segLen = (wallSize.x - gap) * 0.5f;
                    if (segLen < 0.1f) continue; // wall smaller than gap; skip
                    Vector3 forwardLocal = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
                    Vector3 a = wallCenter + forwardLocal * (gap * 0.5f + segLen * 0.5f);
                    Vector3 b = wallCenter - forwardLocal * (gap * 0.5f + segLen * 0.5f);
                    SpawnWall(arena, cover, a, new Vector3(segLen, wallH, wallT), yaw, coverFromSide);
                    SpawnWall(arena, cover, b, new Vector3(segLen, wallH, wallT), yaw, coverFromSide);
                }
            }
        }

        // -----------------------------------------------------------------
        // Geometry spawners
        // -----------------------------------------------------------------

        private static void SpawnWall(Transform arena, Transform cover,
            Vector3 center, Vector3 size, float yawDeg, int coverFromSide)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Wall_{center.x:F1}_{center.z:F1}";
            go.transform.SetParent(arena);
            go.transform.position = center + Vector3.up * (size.y * 0.5f);
            go.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
            go.transform.localScale = size;

            TintGray(go);
            // NavMeshObstacle carves so agents path around. Cube collider stays
            // for physics + LOS raycast blocking.
            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.size = Vector3.one; // local-space; effective size = scale.

            // Determine outward normal in WORLD space. Wall lies along its local
            // X axis (the long side), so the protected face is along world Z
            // (or X depending on yaw). Use yaw + coverFromSide to derive.
            Vector3 outward = Quaternion.Euler(0f, yawDeg, 0f) * (coverFromSide >= 0 ? Vector3.forward : Vector3.back);

            // Cover point sits 0.6m behind the wall on the safe side (opposite to outward).
            Vector3 standOffset = -outward * 0.7f;
            var coverGo = new GameObject($"Cover_Wall_{center.x:F1}_{center.z:F1}");
            coverGo.transform.SetParent(cover);
            coverGo.transform.position = center + standOffset;
            var cp = coverGo.AddComponent<CoverPoint>();
            cp.SafeDirection = outward;
            cp.ProtectionArcDeg = 120f;
            cp.Radius = 0.6f;
        }

        private static void SpawnPillar(Transform arena, Transform cover, Vector3 center)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = $"Pillar_{center.x:F1}_{center.z:F1}";
            go.transform.SetParent(arena);
            go.transform.position = center + Vector3.up * 1.5f; // half of default cylinder height
            go.transform.localScale = new Vector3(1.2f, 1.5f, 1.2f);

            TintGray(go);
            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.shape = NavMeshObstacleShape.Capsule;
            obstacle.radius = 0.6f;
            obstacle.height = 3f;

            // Pillar = 4 cover points (N / E / S / W), each protecting the
            // hemisphere on its outward side. Lets the BT actually pick the
            // right side based on where the threat is, rather than pretending
            // one omnidirectional point is enough.
            Vector4[] sides =
            {
                new Vector4( 0f,  1f, 0f, 0f),  // +Z stand, safe from threats at +Z
                new Vector4( 1f,  0f, 0f, 0f),  // +X stand
                new Vector4( 0f, -1f, 0f, 0f),  // -Z stand
                new Vector4(-1f,  0f, 0f, 0f),  // -X stand
            };
            for (int i = 0; i < sides.Length; i++)
            {
                Vector3 dir = new Vector3(sides[i].x, 0f, sides[i].y);
                var coverGo = new GameObject($"Cover_Pillar_{center.x:F1}_{center.z:F1}_{i}");
                coverGo.transform.SetParent(cover);
                coverGo.transform.position = center + dir * 0.9f;        // stand on the outward side
                var cp = coverGo.AddComponent<CoverPoint>();
                // Pillar lies in -dir from the stand point — threats from -dir
                // get blocked, so SafeDirection (which points from the stand
                // toward the protected-against threat direction) is -dir.
                cp.SafeDirection = -dir;
                cp.ProtectionArcDeg = 180f;
                cp.Radius = 0.6f;
            }
        }

        private static void TintGray(GameObject go)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null) return;
            var mat = new Material(rend.sharedMaterial);
            mat.color = new Color(0.35f, 0.35f, 0.38f);
            rend.sharedMaterial = mat;
        }

        // -----------------------------------------------------------------
        // NPC spawn helpers
        // -----------------------------------------------------------------

        private static void SpawnDuelist(Transform parent, GameObject template,
            Vector3 position, Vector3 enemyPos, Material mat, LayerMask sensorLayer)
        {
            Vector3 enemyDir = ComputeEnemyDir(position, enemyPos);
            var go = CloneNpcTemplate(parent, template, "Duelist", position, enemyDir, mat);
            var runner = go.AddComponent<DuelistRunner>();
            ApplyBaseStats(runner, Faction.TeamA, sensorLayer,
                walkSpeed: 4.2f, attackRange: 2f, sensorRadius: 45f,
                maxHp: 130f, dmg: 22f, enemyDir: enemyDir);
        }

        private static void SpawnMarksman(Transform parent, GameObject template,
            Vector3 position, Vector3 enemyPos, Material mat, LayerMask sensorLayer)
        {
            Vector3 enemyDir = ComputeEnemyDir(position, enemyPos);
            var go = CloneNpcTemplate(parent, template, "Marksman", position, enemyDir, mat);
            var runner = go.AddComponent<MarksmanRunner>();
            // attackRange bumped to 22 (matches MarksmanRunner._engagementRange);
            // gives the ranged class a real distance advantage over melee's 2 m.
            ApplyBaseStats(runner, Faction.TeamB, sensorLayer,
                walkSpeed: 3.6f, attackRange: 22f, sensorRadius: 45f,
                maxHp: 90f, dmg: 22f, enemyDir: enemyDir);
        }

        /// <summary>
        /// Flat (XZ) unit vector pointing from self toward the enemy spawn.
        /// Used to seed each Runner's perception "LastKnownDir" — they know
        /// which way the enemy is, just not the precise position.
        /// </summary>
        private static Vector3 ComputeEnemyDir(Vector3 selfPos, Vector3 enemyPos)
        {
            Vector3 d = enemyPos - selfPos;
            d.y = 0f;
            return d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.forward;
        }

        private static GameObject CloneNpcTemplate(Transform parent, GameObject template,
            string name, Vector3 position, Vector3 facingDir, Material mat)
        {
            var clone = Object.Instantiate(template, parent);
            clone.name = name;
            ActivateRecursive(clone);

            Vector3 spawn = position;
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                spawn = hit.position;
            clone.transform.position = spawn;
            Vector3 flatFacing = facingDir; flatFacing.y = 0f;
            if (flatFacing.sqrMagnitude < 1e-4f) flatFacing = Vector3.forward;
            clone.transform.rotation = Quaternion.LookRotation(flatFacing.normalized);

            var oldNpc = clone.GetComponent<NPCSample>();
            if (oldNpc != null) Object.DestroyImmediate(oldNpc);

            if (mat != null)
                foreach (var rend in clone.GetComponentsInChildren<Renderer>())
                    rend.sharedMaterial = mat;

            return clone;
        }

        private static void ApplyBaseStats(BaseNPCRunner runner, Faction faction, LayerMask sensorLayer,
            float walkSpeed, float attackRange, float sensorRadius, float maxHp, float dmg, Vector3 enemyDir)
        {
            var so = new SerializedObject(runner);
            so.FindProperty("_faction").enumValueIndex = (int)faction;
            so.FindProperty("_walkSpeed").floatValue = walkSpeed;
            so.FindProperty("_attackRange").floatValue = attackRange;
            so.FindProperty("_sensorRadius").floatValue = sensorRadius;
            so.FindProperty("_maxHp").floatValue = maxHp;
            so.FindProperty("_attackDamage").floatValue = dmg;
            so.FindProperty("_sensorLayer").intValue = sensorLayer.value;
            // For the duel, NPC spawns are diagonal so enemyDirection must be
            // a true XZ unit vector (the legacy war-demo (facing,0,0) form would
            // mis-seed the perception's LastKnownDir).
            so.FindProperty("_enemyDirection").vector3Value = enemyDir;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ActivateRecursive(GameObject go)
        {
            go.SetActive(true);
            for (int i = 0; i < go.transform.childCount; i++)
                ActivateRecursive(go.transform.GetChild(i).gameObject);
        }
    }
}
#endif
