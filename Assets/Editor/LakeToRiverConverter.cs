#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class LakeToRiverConverter {
    [MenuItem("Tools/Convert Lake to River")]
    public static void Convert() {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "MCScence") {
            Debug.LogError("Active scene is not MCScence. Please open it first.");
            return;
        }

        var decorsHolder = GameObject.Find("City_Decors");
        if (decorsHolder == null) {
            Debug.LogError("City_Decors GameObject not found!");
            return;
        }

        // 1. Modify Water Plane (Lake_Water)
        var water = decorsHolder.transform.Find("Lake_Water");
        if (water != null) {
            Undo.RecordObject(water, "Modify Water");
            water.localPosition = new Vector3(0f, -0.6f, 60f);
            water.localScale = new Vector3(300f, 1f, 60f);
            Debug.Log("Extended Lake_Water scale to 300x1x60");
        } else {
            Debug.LogWarning("Lake_Water not found under City_Decors");
        }

        // 2. Modify North & South Paths
        var pathN = decorsHolder.transform.Find("LakePath_North");
        if (pathN != null) {
            Undo.RecordObject(pathN, "Modify Path N");
            pathN.localPosition = new Vector3(0f, -0.5f, 93f);
            pathN.localScale = new Vector3(300f, 1f, 6f);
            
            var renderer = pathN.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null) {
                Undo.RecordObject(renderer.sharedMaterial, "Modify Path N Material Tiling");
                renderer.sharedMaterial.mainTextureScale = new Vector2(150f, 2f);
            }
        }
        var pathS = decorsHolder.transform.Find("LakePath_South");
        if (pathS != null) {
            Undo.RecordObject(pathS, "Modify Path S");
            pathS.localPosition = new Vector3(0f, -0.5f, 27f);
            pathS.localScale = new Vector3(300f, 1f, 6f);
            
            var renderer = pathS.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null) {
                Undo.RecordObject(renderer.sharedMaterial, "Modify Path S Material Tiling");
                renderer.sharedMaterial.mainTextureScale = new Vector2(150f, 2f);
            }
        }

        // 3. Destroy East/West Paths & Recreate Connector
        string[] pathsToDestroy = { "LakePath_East", "LakePath_West" };
        foreach (var pName in pathsToDestroy) {
            var pTrans = decorsHolder.transform.Find(pName);
            if (pTrans != null) {
                Undo.DestroyObjectImmediate(pTrans.gameObject);
                Debug.Log("Destroyed " + pName);
            }
        }

        // Recreate Connector if missing
        var connector = decorsHolder.transform.Find("LakePath_Connector");
        if (connector == null) {
            CreateConnectionPath(decorsHolder.transform);
            Debug.Log("Recreated Connector Path and Borders");
        }

        // 4. Destroy Temple and Island GameObjects
        var pagoda = GameObject.Find("cai chua");
        if (pagoda != null) {
            Undo.DestroyObjectImmediate(pagoda);
            Debug.Log("Destroyed Pagoda (cai chua)");
        }
        var tower = GameObject.Find("tripo_convert_0262afa5-ad99-4da5-af23-b85a7b2158b3");
        if (tower != null) {
            Undo.DestroyObjectImmediate(tower);
            Debug.Log("Destroyed Turtle Tower (tripo_convert)");
        }
        var island = decorsHolder.transform.Find("Lake_Island");
        if (island != null) {
            Undo.DestroyObjectImmediate(island.gameObject);
            Debug.Log("Destroyed Lake_Island");
        }
        for (int i = decorsHolder.transform.childCount - 1; i >= 0; i--) {
            var child = decorsHolder.transform.GetChild(i);
            if (child.name == "Lake_Island_Rim") {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        // 5. Clean up fences and rebuild them along the full map width
        var fencesParent = decorsHolder.transform.Find("Fences");
        if (fencesParent != null) {
            var fencePrefabPath = "Assets/Model/House/3D Voxel Park Pack/Park Furnishings/Park_Fence_Edge.obj";
            var fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fencePrefabPath);
            if (fencePrefab != null) {
                for (int i = fencesParent.childCount - 1; i >= 0; i--) {
                    Undo.DestroyObjectImmediate(fencesParent.GetChild(i).gameObject);
                }

                float startX = -150f;
                float endX = 150f;
                float stepX = 1.6f;
                
                int index = 0;
                for (float x = startX; x <= endX; x += stepX) {
                    // South Fence (with a gap between X = -5.9 and X = 5.9 for the entrance path)
                    if (x <= -5.9f || x >= 5.9f) {
                        var fenceS = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab, fencesParent);
                        fenceS.name = "Fence_South_" + index;
                        fenceS.transform.localPosition = new Vector3(x, -0.5f, 24f);
                        fenceS.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                        Undo.RegisterCreatedObjectUndo(fenceS, "Create South Fence");
                    }

                    // North Fence (continuous)
                    var fenceN = (GameObject)PrefabUtility.InstantiatePrefab(fencePrefab, fencesParent);
                    fenceN.name = "Fence_North_" + index;
                    fenceN.transform.localPosition = new Vector3(x, -0.5f, 96f);
                    fenceN.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    Undo.RegisterCreatedObjectUndo(fenceN, "Create North Fence");

                    index++;
                }
                Debug.Log("Rebuilt " + index * 2 + " fences along North and South banks.");
            } else {
                Debug.LogError("Fence Prefab not found at path: " + fencePrefabPath);
            }
        } else {
            Debug.LogWarning("Fences folder not found under City_Decors");
        }

        // 6. Clean up Obstruction objects (Trees/Props) inside Z = 30 to 90 recursively
        CleanUpRiverZoneRecursive(decorsHolder.transform);

        // 7. Auto-align Bridge if found in the scene
        var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects) {
            string name = obj.name.ToLower();
            if ((name.Contains("tripo_convert") || name.Contains("bridge") || name.Contains("cau")) && 
                name != "tripo_convert_0262afa5-ad99-4da5-af23-b85a7b2158b3") {
                Undo.RecordObject(obj.transform, "Auto-align Bridge");
                obj.transform.position = new Vector3(obj.transform.position.x, -5.37f, 59.76f);
                obj.transform.rotation = Quaternion.Euler(0f, 270f, 1.12f);
                obj.transform.localScale = new Vector3(68.05f, 30.0f, 20.0f);
                Debug.Log("Auto-aligned bridge: " + obj.name);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("✅ Lake converted to River successfully!");
    }

    private static void CleanUpRiverZoneRecursive(Transform current) {
        for (int i = current.childCount - 1; i >= 0; i--) {
            var child = current.GetChild(i);
            string name = child.name.ToLower();
            
            if (name.Contains("foliage_tree") || name.Contains("lamp") || name.Contains("bench") || name.Contains("house")) {
                float z = child.position.z;
                if (z > 30f && z < 90f) {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    continue; 
                }
            }
            
            if (child != null) {
                CleanUpRiverZoneRecursive(child);
            }
        }
    }

    private static void CreateConnectionPath(Transform parent) {
        string dir = "Assets/RPG Tiny Fantasy Forest PBR/Material";
        if (!System.IO.Directory.Exists(dir)) {
            System.IO.Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }
        var path = "Assets/RPG Tiny Fantasy Forest PBR/Material/LakePath_Conn_Mat.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Model/Texture/Floor/redcobblestone.png");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(shader) { name = "LakePath_Conn_Mat" };
            mat.SetFloat("_Surface", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.SetTexture("_MetallicGlossMap", null);
            mat.SetTexture("_BumpMap", null);
            mat.SetTexture("_EmissionMap", null);
            mat.SetTexture("_OcclusionMap", null);
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.08f);
            if (tex != null) {
                mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", Color.white);
                mat.mainTextureScale = new Vector2(4f, 9.5f);
            } else {
                mat.SetColor("_BaseColor", new Color(0.65f, 0.22f, 0.18f, 1f));
            }
            AssetDatabase.CreateAsset(mat, path);
        }

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "LakePath_Connector";
        go.transform.SetParent(parent);
        go.transform.localPosition = new Vector3(0f, -0.5f, 14.5f);
        go.transform.localScale = new Vector3(12f, 1f, 19f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Undo.RegisterCreatedObjectUndo(go, "Create Connector Path");

        // Borders
        var bp = "Assets/RPG Tiny Fantasy Forest PBR/Material/Connector_Border_Mat.mat";
        var borderMat = AssetDatabase.LoadAssetAtPath<Material>(bp);
        if (borderMat == null) {
            var borderShader = Shader.Find("Universal Render Pipeline/Lit");
            borderMat = new Material(borderShader) { name = "Connector_Border_Mat" };
            borderMat.SetFloat("_Surface", 0f);
            borderMat.SetTexture("_BaseMap", null);
            borderMat.SetTexture("_EmissionMap", null);
            borderMat.DisableKeyword("_EMISSION");
            borderMat.SetColor("_EmissionColor", Color.black);
            borderMat.SetColor("_BaseColor", new Color(0.55f, 0.52f, 0.48f, 1f));  // stone
            borderMat.SetFloat("_Metallic", 0f);
            borderMat.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(borderMat, bp);
        }

        // East border
        var eastBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        eastBorder.name = "Connector_Border_E";
        eastBorder.transform.SetParent(parent);
        eastBorder.transform.localPosition = new Vector3(6f, 0.15f, 14.5f);
        eastBorder.transform.localScale = new Vector3(0.35f, 0.3f, 19f);
        eastBorder.GetComponent<MeshRenderer>().sharedMaterial = borderMat;
        Undo.RegisterCreatedObjectUndo(eastBorder, "Create Connector Border E");

        // West border
        var westBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        westBorder.name = "Connector_Border_W";
        westBorder.transform.SetParent(parent);
        westBorder.transform.localPosition = new Vector3(-6f, 0.15f, 14.5f);
        westBorder.transform.localScale = new Vector3(0.35f, 0.3f, 19f);
        westBorder.GetComponent<MeshRenderer>().sharedMaterial = borderMat;
        Undo.RegisterCreatedObjectUndo(westBorder, "Create Connector Border W");
    }
}

#endif
