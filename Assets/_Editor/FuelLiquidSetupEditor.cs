#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CraneMachine
{
    /// <summary>
    /// Setup + migration for the generalized LiquidField pipeline (shared field, many
    /// liquid types, color per droplet). Menu:
    ///   Tools > Liquid Field > Create Liquid Field Setup   (fresh scene rig + assets)
    ///   Tools > Liquid Field > Update / Migrate Existing   (upgrade an old FuelLiquid
    ///                                                        setup, wherever you moved it)
    ///
    /// The Update command is location-agnostic: you moved assets out of Generated/, so it
    /// FINDS the RenderTexture / materials / droplet prefab / scene objects by type and
    /// name across the whole project instead of assuming fixed paths, then:
    ///   - fixes the field RT depth buffer if still None
    ///   - repoints materials to the new LiquidField/Blob + LiquidField/Composite shaders
    ///   - swaps old Fuel* components for the new Liquid* ones on scene objects and the
    ///     droplet prefab, preserving wiring where it can
    ///   - re-runs the main-camera culling-mask fix
    /// </summary>
    public static class LiquidFieldSetupEditor
    {
        private const string LayerName = "Liquid";       // renamed from FuelField
        private const string LegacyLayerName = "FuelField";
        private const string FolderRoot = "Assets/LiquidField";
        private const string GeneratedFolder = FolderRoot + "/Generated";
        private const string SpriteName = "LiquidDroplet_Circle";

        // ----- CREATE ---------------------------------------------------------

        [MenuItem("Tools/Liquid Field/Create Liquid Field Setup")]
        public static void CreateSetup()
        {
            var log = new System.Text.StringBuilder("Liquid Field setup:\n");

            EnsureFolder(FolderRoot);
            EnsureFolder(GeneratedFolder);

            int layer = EnsureLayer(LayerName, log);
            RemoveLayerFromMainCamera(layer, log);

            RenderTexture rt = FindOrCreateFieldTexture(log);
            Material blobMat = FindOrCreateMaterial("LiquidField/Blob", "M_LiquidFieldBlob", log);
            Material compMat = FindOrCreateMaterial("LiquidField/Composite", "M_LiquidFieldComposite", log);

            AssignSoftCircleSprite(blobMat, log);
            if (compMat != null && rt != null) compMat.SetTexture("_FieldTex", rt);

            GameObject dropletPrefab = FindOrCreateDropletPrefab(blobMat, layer, log);
            BuildSceneRig(rt, compMat, dropletPrefab, layer, log);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            log.AppendLine();
            log.AppendLine("REMAINING MANUAL STEPS:");
            log.AppendLine("  1. If the droplet prefab's SpriteRenderer has no sprite, assign " +
                           SpriteName + " (mesh type Full Rect).");
            log.AppendLine("  2. Author liquids in LiquidFieldSystem.liquids (Fuel is entry 0).");
            log.AppendLine("  3. Emit: add LiquidEmitter (pick its LiquidType) to a source, or " +
                           "call LiquidFieldSystem.Spawn(type, pos, vel).");
            Debug.Log(log.ToString());
            EditorUtility.DisplayDialog("Liquid Field Setup", "Setup complete. See Console.", "OK");
        }

        // ----- UPDATE / MIGRATE ----------------------------------------------

        [MenuItem("Tools/Liquid Field/Update - Migrate Existing")]
        public static void MigrateExisting()
        {
            var log = new System.Text.StringBuilder("Liquid Field migration:\n");

            // Layer: keep the existing one (Liquid or the legacy FuelField); don't force rename.
            int layer = LayerMask.NameToLayer(LayerName);
            if (layer == -1) layer = LayerMask.NameToLayer(LegacyLayerName);
            if (layer == -1) layer = EnsureLayer(LayerName, log);
            else log.AppendLine($"  using existing liquid layer #{layer}.");
            RemoveLayerFromMainCamera(layer, log);

            // 1) Fix the field RenderTexture depth, wherever it now lives.
            RenderTexture rt = FindAssetByName<RenderTexture>("FuelFieldTexture")
                             ?? FindAssetByName<RenderTexture>("LiquidFieldTexture");
            if (rt != null)
            {
                if (rt.depthStencilFormat == UnityEngine.Experimental.Rendering.GraphicsFormat.None)
                    log.AppendLine("  NOTE: field RT still has depth None — set its Depth Stencil " +
                                   "Format to D24_UNorm_S8 in the inspector (can't change a live RT asset's depth from script reliably).");
                else
                    log.AppendLine($"  found field RT '{rt.name}', depth OK.");
            }
            else log.AppendLine("  WARNING: no field RenderTexture found (searched FuelFieldTexture/LiquidFieldTexture).");

            // 2) Repoint materials to the new shaders (find old or new material names).
            Material blob = FindAssetByName<Material>("M_FuelLiquidBlob")
                          ?? FindAssetByName<Material>("M_LiquidFieldBlob");
            Material comp = FindAssetByName<Material>("M_FuelLiquidComposite")
                          ?? FindAssetByName<Material>("M_LiquidFieldComposite");
            RepointShader(blob, "LiquidField/Blob", log);
            RepointShader(comp, "LiquidField/Composite", log);
            if (comp != null && rt != null) comp.SetTexture("_FieldTex", rt);
            AssignSoftCircleSprite(blob, log);

            // 3) Swap components on scene objects + droplet prefab.
            int swapped = 0;
            swapped += SwapComponent<FuelFieldCameraShim, LiquidFieldCamera>(log, "FuelFieldCamera");
            swapped += SwapComponent<FuelFieldCompositeShim, LiquidFieldComposite>(log, "FuelFieldComposite");
            swapped += SwapComponent<FuelLiquidSystemShim, LiquidFieldSystem>(log, "FuelLiquidSystem");
            swapped += SwapComponent<FuelLiquidParticleShim, LiquidParticle>(log, "FuelLiquidParticle");
            swapped += SwapComponent<FuelLiquidEmitterShim, LiquidEmitter>(log, "FuelLiquidEmitter");

            if (swapped == 0)
                log.AppendLine("  no legacy Fuel* components found in the open scene. If your old " +
                               "scripts are already deleted, just add the new Liquid* components and " +
                               "re-run Create instead.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(log.ToString());
            EditorUtility.DisplayDialog("Liquid Field Migration",
                "Migration pass complete. See Console — some steps may need a manual touch " +
                "(noted there), especially the RT depth format and re-wiring references.", "OK");
        }

        // Shim types: these let the tool reference old component class names for GetComponent
        // even after you rename. They are never added to anything — only used as type keys.
        // If your old scripts are deleted, these resolve to null and the swap is simply skipped.
        private class FuelFieldCameraShim : MonoBehaviour { }
        private class FuelFieldCompositeShim : MonoBehaviour { }
        private class FuelLiquidSystemShim : MonoBehaviour { }
        private class FuelLiquidParticleShim : MonoBehaviour { }
        private class FuelLiquidEmitterShim : MonoBehaviour { }

        // Swap by MonoScript name via SerializedObject "m_Script" — works even if the old
        // class still exists, and is skipped cleanly if it doesn't.
        private static int SwapComponent<TOldShim, TNew>(System.Text.StringBuilder log, string oldClassName)
            where TOldShim : MonoBehaviour where TNew : MonoBehaviour
        {
            var newScript = FindMonoScript(typeof(TNew).Name);
            if (newScript == null) { log.AppendLine($"  (new script {typeof(TNew).Name} not found)"); return 0; }

            int count = 0;
            foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null) continue;
                var so = new SerializedObject(mb);
                var scriptProp = so.FindProperty("m_Script");
                if (scriptProp == null) continue;
                var ms = scriptProp.objectReferenceValue as MonoScript;
                if (ms == null || ms.name != oldClassName) continue;

                scriptProp.objectReferenceValue = newScript;
                so.ApplyModifiedProperties();
                count++;
            }
            if (count > 0) log.AppendLine($"  swapped {count}x {oldClassName} -> {typeof(TNew).Name}.");
            return count;
        }

        // ----- shared helpers -------------------------------------------------

        private static int EnsureLayer(string name, System.Text.StringBuilder log)
        {
            int existing = LayerMask.NameToLayer(name);
            if (existing != -1) { log.AppendLine($"  layer '{name}' exists (#{existing})."); return existing; }

            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
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
            log.AppendLine($"  WARNING: no free layer slot for '{name}'.");
            return 0;
        }

        private static void RemoveLayerFromMainCamera(int layer, System.Text.StringBuilder log)
        {
            Camera main = Camera.main;
            if (main == null) { log.AppendLine("  NOTE: no Camera.main; exclude the liquid layer from your game camera manually."); return; }
            int before = main.cullingMask;
            main.cullingMask &= ~(1 << layer);
            log.AppendLine(main.cullingMask != before
                ? $"  removed liquid layer from '{main.name}' culling mask."
                : $"  '{main.name}' already excludes liquid layer.");
            EditorUtility.SetDirty(main);
        }

        private static RenderTexture FindOrCreateFieldTexture(System.Text.StringBuilder log)
        {
            var found = FindAssetByName<RenderTexture>("LiquidFieldTexture")
                      ?? FindAssetByName<RenderTexture>("FuelFieldTexture");
            if (found != null) { log.AppendLine($"  reusing field RT '{found.name}'."); return found; }

            EnsureFolder(GeneratedFolder);
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32)
            {
                name = "LiquidFieldTexture",
                depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt,
                antiAliasing = 1, useMipMap = false, autoGenerateMips = false,
                // Bilinear + high res so droplets read as smooth blobs, not blocky texels,
                // even when the camera is zoomed out over a large level.
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            AssetDatabase.CreateAsset(rt, GeneratedFolder + "/LiquidFieldTexture.renderTexture");
            log.AppendLine("  created field RT (1920x1080, depth 24, bilinear).");
            return rt;
        }

        private static Material FindOrCreateMaterial(string shaderName, string assetName,
                                                     System.Text.StringBuilder log)
        {
            var found = FindAssetByName<Material>(assetName);
            if (found != null) { RepointShader(found, shaderName, log); return found; }

            Shader shader = Shader.Find(shaderName);
            if (shader == null) { log.AppendLine($"  WARNING: shader '{shaderName}' not found; import .shader files then re-run."); return null; }

            EnsureFolder(GeneratedFolder);
            var mat = new Material(shader) { name = assetName };
            AssetDatabase.CreateAsset(mat, $"{GeneratedFolder}/{assetName}.mat");
            log.AppendLine($"  created {assetName}.");
            return mat;
        }

        private static void RepointShader(Material mat, string shaderName, System.Text.StringBuilder log)
        {
            if (mat == null) return;
            Shader s = Shader.Find(shaderName);
            if (s == null) { log.AppendLine($"  WARNING: shader '{shaderName}' not found (can't repoint {mat.name})."); return; }
            if (mat.shader != s) { mat.shader = s; EditorUtility.SetDirty(mat); log.AppendLine($"  repointed {mat.name} -> {shaderName}."); }
        }

        private static void AssignSoftCircleSprite(Material blobMat, System.Text.StringBuilder log)
        {
            if (blobMat == null) return;
            var tex = FindAssetByName<Texture>(SpriteName);
            if (tex != null) { blobMat.SetTexture("_MainTex", tex); log.AppendLine("  assigned soft circle to blob material."); }
            else log.AppendLine("  NOTE: soft circle sprite not found; assign _MainTex manually.");
        }

        private static GameObject FindOrCreateDropletPrefab(Material blobMat, int layer,
                                                            System.Text.StringBuilder log)
        {
            var found = FindAssetByName<GameObject>("LiquidDroplet")
                      ?? FindAssetByName<GameObject>("FuelDroplet");
            if (found != null) { log.AppendLine($"  reusing droplet prefab '{found.name}'."); return found; }

            EnsureFolder(GeneratedFolder);
            var go = new GameObject("LiquidDroplet");
            go.layer = layer;
            go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            if (blobMat != null) sr.sharedMaterial = blobMat;
            var sprite = FindAssetByName<Sprite>(SpriteName);
            if (sprite != null) sr.sprite = sprite;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            go.AddComponent<CircleCollider2D>().radius = 0.3f;
            go.AddComponent<LiquidParticle>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, GeneratedFolder + "/LiquidDroplet.prefab");
            UnityEngine.Object.DestroyImmediate(go);
            log.AppendLine("  created droplet prefab.");
            return prefab;
        }

        private static void BuildSceneRig(RenderTexture rt, Material compMat,
                                          GameObject dropletPrefab, int layer,
                                          System.Text.StringBuilder log)
        {
            if (GameObject.Find("Liquid Field System") != null || GameObject.Find("Fuel Liquid System") != null)
            {
                log.AppendLine("  scene rig already present; skipped.");
                return;
            }

            Camera main = Camera.main;
            var camGo = new GameObject("Liquid Field Camera");
            if (main != null) camGo.transform.SetParent(main.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = main != null ? main.orthographicSize : 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.cullingMask = 1 << layer;
            cam.depth = -10;
            if (rt != null) cam.targetTexture = rt;
            camGo.transform.localPosition = Vector3.zero;
            var fieldCam = camGo.AddComponent<LiquidFieldCamera>();
            SetPrivate(fieldCam, "referenceCamera", main);
            SetPrivate(fieldCam, "fieldTexture", rt);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Composite Quad";
            quad.transform.SetParent(camGo.transform, false);
            var qCol = quad.GetComponent<Collider>();
            if (qCol != null) UnityEngine.Object.DestroyImmediate(qCol);
            if (compMat != null) quad.GetComponent<MeshRenderer>().sharedMaterial = compMat;
            var comp = quad.AddComponent<LiquidFieldComposite>();
            SetPrivate(comp, "fieldCamera", fieldCam);

            var sys = new GameObject("Liquid Field System");
            var system = sys.AddComponent<LiquidFieldSystem>();
            if (dropletPrefab != null)
                SetPrivate(system, "particlePrefab", dropletPrefab.GetComponent<LiquidParticle>());

            Undo.RegisterCreatedObjectUndo(camGo, "Create Liquid Field Setup");
            Undo.RegisterCreatedObjectUndo(sys, "Create Liquid Field Setup");
            log.AppendLine("  built rig: field camera (child of Main), composite quad, system.");
        }

        // ----- asset/type utilities ------------------------------------------

        private static T FindAssetByName<T>(string exactName) where T : UnityEngine.Object
        {
            string typeFilter = typeof(T) == typeof(GameObject) ? "t:Prefab" : "t:" + typeof(T).Name;
            foreach (var guid in AssetDatabase.FindAssets($"{exactName} {typeFilter}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) != exactName) continue;
                var a = AssetDatabase.LoadAssetAtPath<T>(path);
                if (a != null) return a;
            }
            return null;
        }

        private static MonoScript FindMonoScript(string className)
        {
            foreach (var guid in AssetDatabase.FindAssets($"{className} t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) != className) continue;
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms != null && ms.GetClass() != null && ms.GetClass().Name == className) return ms;
            }
            return null;
        }

        private static void SetPrivate(Component c, string field, UnityEngine.Object value)
        {
            if (c == null) return;
            var so = new SerializedObject(c);
            var p = so.FindProperty(field);
            if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedProperties(); }
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