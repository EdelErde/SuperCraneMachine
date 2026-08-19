#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace CraneMachine
{
    /// <summary>
    /// One-click setup for the FuelLiquid pipeline. Menu:
    ///   Tools > Fuel Liquid > Create Fuel Liquid Setup
    ///
    /// Automates everything that CAN be automated from the editor:
    ///   - ensures a "FuelField" layer exists (edits TagManager)
    ///   - creates the field RenderTexture asset (with a valid Render Graph desc)
    ///   - creates M_FuelLiquidBlob + M_FuelLiquidComposite materials from the
    ///     FuelLiquid/Blob and FuelLiquid/Composite shaders
    ///   - builds a droplet prefab (Rigidbody2D + CircleCollider2D + SpriteRenderer
    ///     + FuelLiquidParticle) on the FuelField layer
    ///   - creates the scene rig: a "Fuel Liquid" root holding the FuelFieldCamera
    ///     (culling mask = FuelField only, targeting the RT), the composite quad
    ///     (FuelFieldComposite), and the FuelLiquidSystem spawner
    ///
    /// What it CANNOT do for you (and will tell you about in the summary):
    ///   - pick the soft-circle sprite for droplets (assign one you like)
    ///   - position/scale the composite quad to your play area
    ///   - drop a FuelLiquidEmitter on your real FuelFilter object
    /// </summary>
    public static class FuelLiquidSetupEditor
    {
        private const string LayerName = "FuelField";
        private const string FolderRoot = "Assets/FuelLiquid";
        private const string GeneratedFolder = FolderRoot + "/Generated";

        [MenuItem("Tools/Fuel Liquid/Create Fuel Liquid Setup")]
        public static void CreateSetup()
        {
            var log = new System.Text.StringBuilder();
            log.AppendLine("Fuel Liquid setup:");

            EnsureFolder(FolderRoot);
            EnsureFolder(GeneratedFolder);

            int layer = EnsureLayer(LayerName, log);

            RenderTexture rt = CreateFieldTexture(log);
            Material blobMat = CreateMaterial("FuelLiquid/Blob", "M_FuelLiquidBlob", log);
            Material compMat = CreateMaterial("FuelLiquid/Composite", "M_FuelLiquidComposite", log);

            if (compMat != null && rt != null)
                compMat.SetTexture("_FieldTex", rt);

            GameObject dropletPrefab = CreateDropletPrefab(blobMat, layer, log);

            BuildSceneRig(rt, compMat, dropletPrefab, layer, log);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            log.AppendLine();
            log.AppendLine("MANUAL STEPS REMAINING:");
            log.AppendLine("  1. Assign a soft-circle sprite to the droplet prefab's " +
                           "SpriteRenderer (Assets/FuelLiquid/Generated/FuelDroplet.prefab). " +
                           "Sprite mesh type must be Full Rect.");
            log.AppendLine("  2. Move/scale the 'Fuel Liquid/Composite Quad' to cover your " +
                           "play area (it must match the field camera's framing).");
            log.AppendLine("  3. Add a FuelLiquidEmitter to your real FuelFilter object, " +
                           "OR call FuelLiquidSystem.Spawn(pos, vel) from your own code.");

            Debug.Log(log.ToString());
            EditorUtility.DisplayDialog("Fuel Liquid Setup",
                "Setup complete. See the Console for details and the 3 remaining manual steps.",
                "OK");
        }

        // ---- Layer -----------------------------------------------------------

        private static int EnsureLayer(string name, System.Text.StringBuilder log)
        {
            int existing = LayerMask.NameToLayer(name);
            if (existing != -1)
            {
                log.AppendLine($"  layer '{name}' already exists (#{existing}).");
                return existing;
            }

            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            // User layers are indices 8..31.
            for (int i = 8; i < 32; i++)
            {
                SerializedProperty sp = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = name;
                    tagManager.ApplyModifiedProperties();
                    log.AppendLine($"  created layer '{name}' at #{i}.");
                    return i;
                }
            }

            log.AppendLine($"  WARNING: no free user layer slot for '{name}'. " +
                           "Free one up and re-run.");
            return 0;
        }

        // ---- Assets ----------------------------------------------------------

        private static RenderTexture CreateFieldTexture(System.Text.StringBuilder log)
        {
            string path = GeneratedFolder + "/FuelFieldTexture.renderTexture";
            var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if (existing != null)
            {
                log.AppendLine("  field RenderTexture already exists.");
                return existing;
            }

            var rt = new RenderTexture(512, 256, 0, RenderTextureFormat.ARGB32)
            {
                name = "FuelFieldTexture",
                // No depth needed for a flat 2D field; explicit so it persists on the asset.
                depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None,
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false
            };
            AssetDatabase.CreateAsset(rt, path);
            log.AppendLine("  created field RenderTexture (512x256).");
            return rt;
        }

        private static Material CreateMaterial(string shaderName, string assetName,
                                               System.Text.StringBuilder log)
        {
            string path = $"{GeneratedFolder}/{assetName}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                log.AppendLine($"  material {assetName} already exists.");
                return existing;
            }

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                log.AppendLine($"  WARNING: shader '{shaderName}' not found. Import the " +
                               ".shader files first, then re-run.");
                return null;
            }

            var mat = new Material(shader) { name = assetName };
            AssetDatabase.CreateAsset(mat, path);
            log.AppendLine($"  created material {assetName}.");
            return mat;
        }

        private static GameObject CreateDropletPrefab(Material blobMat, int layer,
                                                      System.Text.StringBuilder log)
        {
            string path = GeneratedFolder + "/FuelDroplet.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                log.AppendLine("  droplet prefab already exists.");
                return existing;
            }

            var go = new GameObject("FuelDroplet");
            go.layer = layer;

            var sr = go.AddComponent<SpriteRenderer>();
            if (blobMat != null) sr.sharedMaterial = blobMat;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.15f;

            go.AddComponent<FuelLiquidParticle>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            log.AppendLine("  created droplet prefab (sprite unassigned — see manual steps).");
            return prefab;
        }

        // ---- Scene rig -------------------------------------------------------

        private static void BuildSceneRig(RenderTexture rt, Material compMat,
                                          GameObject dropletPrefab, int layer,
                                          System.Text.StringBuilder log)
        {
            if (GameObject.Find("Fuel Liquid") != null)
            {
                log.AppendLine("  scene rig 'Fuel Liquid' already present; skipped rig build.");
                return;
            }

            var root = new GameObject("Fuel Liquid");

            // Field camera
            var camGo = new GameObject("Fuel Field Camera");
            camGo.transform.SetParent(root.transform);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.cullingMask = 1 << layer;      // FuelField only
            cam.depth = -10;                   // render before main
            if (rt != null) cam.targetTexture = rt;
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<FuelFieldCamera>();

            // Composite quad (visible liquid)
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Composite Quad";
            quad.transform.SetParent(root.transform);
            var qCol = quad.GetComponent<Collider>();
            if (qCol != null) UnityEngine.Object.DestroyImmediate(qCol);
            var qr = quad.GetComponent<MeshRenderer>();
            if (compMat != null) qr.sharedMaterial = compMat;
            quad.transform.localScale = new Vector3(16f, 9f, 1f);
            quad.AddComponent<FuelFieldComposite>();

            // Spawner / system
            var sys = new GameObject("Fuel Liquid System");
            sys.transform.SetParent(root.transform);
            var system = sys.AddComponent<FuelLiquidSystem>();
            AssignPrivatePrefab(system, dropletPrefab, log);

            Undo.RegisterCreatedObjectUndo(root, "Create Fuel Liquid Setup");
            log.AppendLine("  built scene rig (camera + composite quad + system).");
        }

        // FuelLiquidSystem.particlePrefab is [SerializeField] private — set it via
        // SerializedObject so the tool wires the prefab reference automatically.
        private static void AssignPrivatePrefab(FuelLiquidSystem system,
                                                GameObject prefab,
                                                System.Text.StringBuilder log)
        {
            if (system == null || prefab == null) return;
            var so = new SerializedObject(system);
            var prop = so.FindProperty("particlePrefab");
            if (prop != null)
            {
                var particle = prefab.GetComponent<FuelLiquidParticle>();
                prop.objectReferenceValue = particle;
                so.ApplyModifiedProperties();
                log.AppendLine("  wired droplet prefab into FuelLiquidSystem.");
            }
            else
            {
                log.AppendLine("  NOTE: assign the droplet prefab to FuelLiquidSystem manually.");
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif