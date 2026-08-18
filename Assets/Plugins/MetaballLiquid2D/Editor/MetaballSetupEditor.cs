using UnityEditor;
using UnityEngine;

namespace MetaballLiquid2D.EditorTools
{
    /// <summary>
    /// One-click scene setup: creates the "Liquid" layer, the offscreen
    /// Metaball Camera, the visible Liquid Composite quad, the field
    /// RenderTexture asset, and materials for both shaders. Also excludes
    /// the "Liquid" layer from Camera.main's culling mask if a main camera
    /// exists.
    ///
    /// Run from the menu: Tools > Metaball Liquid 2D > Create Metaball Setup
    /// </summary>
    public static class MetaballSetupEditor
    {
        const string LayerName = "Liquid";
        const string BlobShaderName = "Metaball/BlobField";
        const string CompositeShaderName = "Metaball/LiquidComposite";
        const string AssetFolder = "Assets/MetaballLiquid2D/Generated";

        [MenuItem("Tools/Metaball Liquid 2D/Create Metaball Setup")]
        public static void CreateSetup()
        {
            EnsureLayer(LayerName);
            EnsureFolder(AssetFolder);

            int liquidLayer = LayerMask.NameToLayer(LayerName);
            if (liquidLayer < 0)
            {
                Debug.LogError("Metaball Liquid 2D: could not create/find the 'Liquid' layer. " +
                    "Add it manually in Project Settings > Tags and Layers, then run this again.");
                return;
            }

            // --- Field RenderTexture asset ---
            string rtPath = $"{AssetFolder}/MetaballField.renderTexture";
            RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
            if (rt == null)
            {
                RenderTextureFormat format = RenderTextureFormat.RHalf;
                if (!SystemInfo.SupportsRenderTextureFormat(format)) format = RenderTextureFormat.ARGB32;

                rt = new RenderTexture(512, 512, 0, format) { name = "MetaballField" };
                AssetDatabase.CreateAsset(rt, rtPath);
            }

            // --- Materials ---
            Material blobMat = FindOrCreateMaterial(BlobShaderName, $"{AssetFolder}/M_MetaballBlob.mat");
            Material compositeMat = FindOrCreateMaterial(CompositeShaderName, $"{AssetFolder}/M_MetaballComposite.mat");
            if (blobMat == null || compositeMat == null) return;

            // --- Metaball camera (offscreen, renders only the Liquid layer) ---
            GameObject camGO = GameObject.Find("Metaball Camera");
            if (camGO == null)
            {
                camGO = new GameObject("Metaball Camera");
                Undo.RegisterCreatedObjectUndo(camGO, "Create Metaball Camera");
            }

            Camera cam = camGO.GetComponent<Camera>();
            if (cam == null) cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.depth = -10;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = 1 << liquidLayer;
            if (camGO.transform.position == Vector3.zero)
            {
                camGO.transform.position = new Vector3(0f, 0f, -10f);
            }

            MetaballFieldCamera fieldCam = camGO.GetComponent<MetaballFieldCamera>();
            if (fieldCam == null) fieldCam = camGO.AddComponent<MetaballFieldCamera>();
            fieldCam.liquidLayer = 1 << liquidLayer;
            fieldCam.textureWidth = rt.width;
            fieldCam.textureHeight = rt.height;

            // --- Composite quad (what the player actually sees) ---
            GameObject quadGO = GameObject.Find("Liquid Composite");
            if (quadGO == null)
            {
                quadGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quadGO.name = "Liquid Composite";
                Object collider = quadGO.GetComponent<Collider>();
                if (collider != null) Object.DestroyImmediate(collider);
                Undo.RegisterCreatedObjectUndo(quadGO, "Create Liquid Composite");
            }

            MeshRenderer mr = quadGO.GetComponent<MeshRenderer>();
            mr.sharedMaterial = compositeMat;

            MetaballComposite comp = quadGO.GetComponent<MetaballComposite>();
            if (comp == null) comp = quadGO.AddComponent<MetaballComposite>();
            comp.fieldCamera = fieldCam;

            // --- Exclude the Liquid layer from the main camera ---
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.cullingMask &= ~(1 << liquidLayer);
            }
            else
            {
                Debug.LogWarning("Metaball Liquid 2D: no Main Camera found/tagged in the scene. " +
                    "Make sure your gameplay camera's Culling Mask excludes the 'Liquid' layer, " +
                    "or the raw blob sprites will render twice.");
            }

            AssetDatabase.SaveAssets();
            Selection.activeGameObject = quadGO;

            Debug.Log("Metaball Liquid 2D: setup complete.\n" +
                "- Put blob sprites on the 'Liquid' layer using the 'M_MetaballBlob' material.\n" +
                "- Tune merging/edges on the 'M_MetaballComposite' material (Threshold / Edge Softness).\n" +
                "- Move/resize 'Metaball Camera' to frame the area your blobs move in.");
        }

        static Material FindOrCreateMaterial(string shaderName, string path)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"Metaball Liquid 2D: shader '{shaderName}' not found. " +
                    "Make sure MetaballBlob.shader / MetaballComposite.shader are imported in the project.");
                return null;
            }

            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        static void EnsureLayer(string name)
        {
            if (LayerMask.NameToLayer(name) >= 0) return;

            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            // Layers 0-7 are Unity built-ins; user layers start at index 8.
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerSP.stringValue))
                {
                    layerSP.stringValue = name;
                    tagManager.ApplyModifiedProperties();
                    return;
                }
            }

            Debug.LogWarning("Metaball Liquid 2D: no free layer slots available (all 8-31 are used). " +
                "Free one up or add a 'Liquid' layer manually in Project Settings > Tags and Layers.");
        }
    }
}
