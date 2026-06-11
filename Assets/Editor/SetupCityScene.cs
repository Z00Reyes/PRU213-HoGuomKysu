#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class SetupCityScene {
    [MenuItem("Tools/Setup City Scene")]
    public static void Run() {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "MCScence") {
            Debug.LogError("Active scene is not MCScence. Please open it first.");
            return;
        }

        // --- Clean up old objects ---
        string[] oldNames = {
            "Terrain", "Ground", "Street_Road", "Sidewalk",
            "Lake_Water", "Curb", "Lake_Barrier", "City_Decors",
            "LakePath_North", "LakePath_South", "LakePath_East", "LakePath_West",
            "LakePath_Connector",
            "Lake_Rim_N", "Lake_Rim_S", "Lake_Rim_E", "Lake_Rim_W",
            "Street_Road_North", "Street_Road_South",
            "Road_Guardrail_North", "Road_Guardrail_South",
            "Ground_North", "Ground_South",
            "Lake_Island", "Lake_Island_Rim"
        };
        foreach (var n in oldNames) {
            var go = GameObject.Find(n);
            if (go != null) Undo.DestroyObjectImmediate(go);
        }

        var decorsHolder = new GameObject("City_Decors");
        Undo.RegisterCreatedObjectUndo(decorsHolder, "Create City Decors");

        var pbrMatPath = "Assets/RPG Tiny Fantasy Forest PBR/Material/DefaultPBR.mat";
        var baseMat = AssetDatabase.LoadAssetAtPath<Material>(pbrMatPath);

        // ============================================================
        //  MAP LAYOUT (top-down, Z = depth):
        //
        //   Z= 80  ┌─────────────────────────────────────────────────┐ North End
        //          │              NORTH SIDEWALK (z=75)              │
        //   Z= 55  │  [LAKE ~30x30 centered at 0,0,55]              │
        //          │     ╔═══════════════════╗                       │
        //          │     ║  LAKE WATER       ║                       │
        //          │     ╚═══════════════════╝                       │
        //   Z= 30  │     [LAKE PATH surrounds lake]                  │
        //          │              SOUTH SIDEWALK (z=2.5)             │
        //   Z=  0  │─────────── CURB ───────────────────────────────│
        //   Z= -7  │              ROAD (z=-7.5)                      │
        //   Z=-20  └─────────────────────────────────────────────────┘ South End
        // ============================================================

        // ROAD - runs full length along bottom
        CreateRoad(decorsHolder.transform);

        // SIDEWALK SOUTH - strip between road and lake zone
        CreateSidewalkSouth(decorsHolder.transform, baseMat);

        // CURB - border between road and sidewalk
        CreateCurb(decorsHolder.transform, baseMat);

        // LAKE at center - at Z=55, X=0
        float lakeX = 0f;
        float lakeZ = 60f;
        float lakeHalfW = 40f;   // half-width X (bigger lake)
        float lakeHalfD = 30f;   // half-depth Z (bigger lake)
        CreateLake(lakeX, lakeZ, lakeHalfW * 2, lakeHalfD * 2, decorsHolder.transform, baseMat);

        // ISLAND - small raised land in the center of the lake (for tower placement)
        CreateLakeIsland(lakeX, lakeZ, decorsHolder.transform, baseMat);

        // LAKE SURROUNDING PATH (walkable path around the lake, redcobblestone texture)
        CreateLakeSurroundPath(lakeX, lakeZ, lakeHalfW, lakeHalfD, decorsHolder.transform, baseMat);

        // ROAD GUARDRAILS - concrete barriers on both sides of the road
        CreateRoadGuardrails(decorsHolder.transform, baseMat);

        // NORTH TERRAIN (grass beyond the lake)
        CreateNorthGround(decorsHolder.transform);

        // SOUTH GROUND (grass beyond the road)
        CreateSouthGround(decorsHolder.transform);

        // STREET LAMPS - south sidewalk
        float[] lampXPositions = { -35f, -20f, -5f, 5f, 20f, 35f };
        foreach (var x in lampXPositions)
            CreateStreetLamp(x, 0.1f, decorsHolder.transform, baseMat);

        // STREET LAMPS - around lake path (corners + midpoints)
        float pathPad = 6f;
        float[] lakeLampX = { -(lakeHalfW + pathPad), lakeHalfW + pathPad };
        float[] lakeLampZ = { lakeZ - lakeHalfD - pathPad, lakeZ, lakeZ + lakeHalfD + pathPad };
        foreach (var x in lakeLampX)
            foreach (var z in lakeLampZ)
                CreateStreetLamp(x, z, decorsHolder.transform, baseMat);
        // Extra lamps along north and south lake paths
        float[] lakeFrontLampX = { -lakeHalfW * 0.5f, lakeHalfW * 0.5f };
        foreach (var x in lakeFrontLampX) {
            CreateStreetLamp(x, lakeZ - lakeHalfD - pathPad, decorsHolder.transform, baseMat);
            CreateStreetLamp(x, lakeZ + lakeHalfD + pathPad, decorsHolder.transform, baseMat);
        }

        // Move Player (MC)
        var mcGo = GameObject.Find("MC");
        if (mcGo != null) {
            Undo.RecordObject(mcGo.transform, "Move MC to sidewalk");
            mcGo.transform.position = new Vector3(0f, 0.765f, 2.5f);

            var playerCtrl = mcGo.GetComponent<PlayerController25D>();
            if (playerCtrl != null) {
                Undo.RecordObject(playerCtrl, "Initialize Player orientation");
                var lastVertField = typeof(PlayerController25D).GetField("lastVertical",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (lastVertField != null) lastVertField.SetValue(playerCtrl, 1f);
                var lastHorizField = typeof(PlayerController25D).GetField("lastHorizontal",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (lastHorizField != null) lastHorizField.SetValue(playerCtrl, 0f);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.Refresh();
        Debug.Log("✅ Bigger lake + redcobblestone path + road guardrails created!");
    }

    // =====================================================================
    // ROAD
    // =====================================================================
    static void CreateRoad(Transform parent) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Street_Road";
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(0f, -0.55f, -7.5f);
        go.transform.localScale = new Vector3(200f, 1f, 15f);

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader) { name = "Street_Road_Mat" };
        mat.SetFloat("_Surface", 0f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        mat.SetOverrideTag("RenderType", "Opaque");
        mat.SetTexture("_BaseMap", null);
        mat.SetTexture("_MetallicGlossMap", null);
        mat.SetTexture("_BumpMap", null);
        mat.SetTexture("_EmissionMap", null);
        mat.SetTexture("_OcclusionMap", null);
        mat.DisableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.black);
        mat.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.14f, 1f));
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.02f);

        var path = "Assets/RPG Tiny Fantasy Forest PBR/Material/Street_Road_Mat.mat";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mat, path);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // =====================================================================
    // SOUTH SIDEWALK (between road and lake zone)
    // =====================================================================
    static void CreateSidewalkSouth(Transform parent, Material baseMat) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Sidewalk";
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(0f, -0.5f, 2.5f);
        go.transform.localScale = new Vector3(200f, 1f, 5f);

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader) { name = "Sidewalk_Mat" };
        mat.SetFloat("_Surface", 0f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        mat.SetOverrideTag("RenderType", "Opaque");
        mat.SetTexture("_MetallicGlossMap", null);
        mat.SetTexture("_BumpMap", null);
        mat.SetTexture("_EmissionMap", null);
        mat.SetTexture("_OcclusionMap", null);
        mat.SetTexture("_DetailAlbedoMap", null);
        mat.DisableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.black);
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.05f);

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Model/Texture/Floor/cobblestonefloor.png");
        if (tex != null) {
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.mainTextureScale = new Vector2(40f, 3f);
        } else {
            mat.SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.55f, 1f));
        }

        var path = "Assets/RPG Tiny Fantasy Forest PBR/Material/Sidewalk_Mat.mat";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mat, path);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // =====================================================================
    // CURB
    // =====================================================================
    static void CreateCurb(Transform parent, Material baseMat) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Curb";
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(0f, -0.025f, 0f);
        go.transform.localScale = new Vector3(200f, 0.05f, 0.2f);

        if (baseMat != null) {
            var mat = new Material(baseMat) { name = "Curb_Mat" };
            mat.mainTexture = null;
            mat.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            var path = "Assets/RPG Tiny Fantasy Forest PBR/Material/Curb_Mat.mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mat, path);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }

    // =====================================================================
    // LAKE ISLAND (small raised land in the center of the lake)
    // =====================================================================
    static void CreateLakeIsland(float cx, float cz, Transform parent, Material baseMat) {
        var shader = Shader.Find("Universal Render Pipeline/Lit");

        // Grass/dirt ground — island surface
        var islandMat = new Material(shader) { name = "Lake_Island_Mat" };
        islandMat.SetFloat("_Surface", 0f);
        islandMat.SetTexture("_BaseMap", null);
        islandMat.SetTexture("_EmissionMap", null);
        islandMat.DisableKeyword("_EMISSION");
        islandMat.SetColor("_EmissionColor", Color.black);
        islandMat.SetColor("_BaseColor", new Color(0.25f, 0.48f, 0.20f, 1f));  // grass green
        islandMat.SetFloat("_Metallic", 0f);
        islandMat.SetFloat("_Smoothness", 0.0f);
        var ip = "Assets/RPG Tiny Fantasy Forest PBR/Material/Lake_Island_Mat.mat";
        AssetDatabase.DeleteAsset(ip); AssetDatabase.CreateAsset(islandMat, ip);

        // Stone rim material
        var rimMat = new Material(shader) { name = "Lake_Island_Rim_Mat" };
        rimMat.SetFloat("_Surface", 0f);
        rimMat.SetTexture("_BaseMap", null);
        rimMat.SetTexture("_EmissionMap", null);
        rimMat.DisableKeyword("_EMISSION");
        rimMat.SetColor("_EmissionColor", Color.black);
        rimMat.SetColor("_BaseColor", new Color(0.50f, 0.46f, 0.40f, 1f));  // stone grey-brown
        rimMat.SetFloat("_Metallic", 0f);
        rimMat.SetFloat("_Smoothness", 0.1f);
        var rp = "Assets/RPG Tiny Fantasy Forest PBR/Material/Lake_Island_Rim_Mat.mat";
        AssetDatabase.DeleteAsset(rp); AssetDatabase.CreateAsset(rimMat, rp);

        // Main island platform — sits above water surface (water top ~Y=-0.2)
        float islandW = 12f;    // width X
        float islandD = 12f;    // depth Z
        float islandY = -0.1f;  // top surface just above water
        float islandH = 0.7f;   // thickness (enough to hide below water)

        var island = GameObject.CreatePrimitive(PrimitiveType.Cube);
        island.name = "Lake_Island";
        island.transform.SetParent(parent);
        island.transform.position = new Vector3(cx, islandY - islandH * 0.5f, cz);
        island.transform.localScale = new Vector3(islandW, islandH, islandD);
        island.GetComponent<MeshRenderer>().sharedMaterial = islandMat;

        // Stone rim/border around the island edges (slightly raised)
        float rimThick = 0.6f;
        float rimH     = 0.25f;
        float rimY     = islandY + rimH * 0.5f;

        // North & South rims
        foreach (var zOff in new float[] { islandD * 0.5f - rimThick * 0.5f,
                                           -(islandD * 0.5f - rimThick * 0.5f) }) {
            var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.name = "Lake_Island_Rim";
            r.transform.SetParent(parent);
            r.transform.position = new Vector3(cx, rimY, cz + zOff);
            r.transform.localScale = new Vector3(islandW, rimH, rimThick);
            r.GetComponent<MeshRenderer>().sharedMaterial = rimMat;
        }
        // East & West rims
        foreach (var xOff in new float[] { islandW * 0.5f - rimThick * 0.5f,
                                           -(islandW * 0.5f - rimThick * 0.5f) }) {
            var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.name = "Lake_Island_Rim";
            r.transform.SetParent(parent);
            r.transform.position = new Vector3(cx + xOff, rimY, cz);
            r.transform.localScale = new Vector3(rimThick, rimH, islandD);
            r.GetComponent<MeshRenderer>().sharedMaterial = rimMat;
        }
    }

    // =====================================================================
    // LAKE
    // =====================================================================
    static void CreateLake(float cx, float cz, float w, float d, Transform parent, Material baseMat) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Lake_Water";
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(cx, -0.7f, cz);
        go.transform.localScale = new Vector3(w, 1f, d);

        var waterMat = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/RPG Tiny Fantasy Forest PBR/Material/Special/Water_River.mat");
        if (waterMat != null)
            go.GetComponent<MeshRenderer>().sharedMaterial = waterMat;
    }

    // =====================================================================
    // LAKE SURROUNDING PATH
    // Creates 4 flat strips around the lake that the player can walk on
    // =====================================================================
    static void CreateLakeSurroundPath(float cx, float cz, float lakeHW, float lakeHD,
                                       Transform parent, Material baseMat) {
        float pathW = 6f;         // width of the walkable path strip (wider)
        float rimT = 0.35f;       // rim thickness

        // *** Use redcobblestone texture for lake path ***
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Model/Texture/Floor/redcobblestone.png");
        var shader = Shader.Find("Universal Render Pipeline/Lit");

        Material MakeLakeMat(string name, Vector2 tiling) {
            var mat = new Material(shader) { name = name };
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
                mat.mainTextureScale = tiling;
            } else {
                mat.SetColor("_BaseColor", new Color(0.65f, 0.22f, 0.18f, 1f));
            }
            return mat;
        }

        // NORTH path strip
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "LakePath_North";
            go.transform.SetParent(parent);
            float totalW = (lakeHW + pathW) * 2f;
            float zPos = cz + lakeHD + pathW * 0.5f;
            go.transform.position = new Vector3(cx, -0.5f, zPos);
            go.transform.localScale = new Vector3(totalW, 1f, pathW);
            var mat = MakeLakeMat("LakePath_N_Mat", new Vector2(totalW / 2f, 2f));
            var p = "Assets/RPG Tiny Fantasy Forest PBR/Material/LakePath_N_Mat.mat";
            AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(mat, p);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // SOUTH path strip
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "LakePath_South";
            go.transform.SetParent(parent);
            float totalW = (lakeHW + pathW) * 2f;
            float zPos = cz - lakeHD - pathW * 0.5f;
            go.transform.position = new Vector3(cx, -0.5f, zPos);
            go.transform.localScale = new Vector3(totalW, 1f, pathW);
            var mat = MakeLakeMat("LakePath_S_Mat", new Vector2(totalW / 2f, 2f));
            var p = "Assets/RPG Tiny Fantasy Forest PBR/Material/LakePath_S_Mat.mat";
            AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(mat, p);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // EAST path strip (only the side strips, not overlapping corners)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "LakePath_East";
            go.transform.SetParent(parent);
            float xPos = cx + lakeHW + pathW * 0.5f;
            go.transform.position = new Vector3(xPos, -0.5f, cz);
            go.transform.localScale = new Vector3(pathW, 1f, lakeHD * 2f);
            var mat = MakeLakeMat("LakePath_E_Mat", new Vector2(2f, lakeHD / 2f));
            var p = "Assets/RPG Tiny Fantasy Forest PBR/Material/LakePath_E_Mat.mat";
            AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(mat, p);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // WEST path strip
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "LakePath_West";
            go.transform.SetParent(parent);
            float xPos = cx - lakeHW - pathW * 0.5f;
            go.transform.position = new Vector3(xPos, -0.5f, cz);
            go.transform.localScale = new Vector3(pathW, 1f, lakeHD * 2f);
            var mat = MakeLakeMat("LakePath_W_Mat", new Vector2(2f, lakeHD / 2f));
            var p = "Assets/RPG Tiny Fantasy Forest PBR/Material/LakePath_W_Mat.mat";
            AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(mat, p);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // CONNECTION PATH (south sidewalk → lake south path)
        {
            // Stop at the SOUTH EDGE of the south lake path (not its center)
            float southPathEdgeZ = cz - lakeHD - pathW;
            float sidewalkTopZ = 5f;
            float connLen = southPathEdgeZ - sidewalkTopZ;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "LakePath_Connector";
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(cx, -0.5f, sidewalkTopZ + connLen * 0.5f);
            go.transform.localScale = new Vector3(12f, 1f, connLen);
            var mat = MakeLakeMat("LakePath_Conn_Mat", new Vector2(4f, connLen / 2f));
            var p = "Assets/RPG Tiny Fantasy Forest PBR/Material/LakePath_Conn_Mat.mat";
            AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(mat, p);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // CONNECTOR PATH SIDE BORDERS (2 stone rails on east & west of the entry path)
        {
            float southPathEdgeZ = cz - lakeHD - pathW;
            float sidewalkTopZ = 5f;
            float connLen = southPathEdgeZ - sidewalkTopZ;
            float connCenterZ = sidewalkTopZ + connLen * 0.5f;
            float connHalfW = 6f;   // half of connector width (12 / 2)
            float borderThick = 0.35f;
            float borderH = 0.3f;
            float borderY = -0.5f + borderH * 0.5f + 0.5f;  // sit on top of path surface

            var borderShader = Shader.Find("Universal Render Pipeline/Lit");
            var borderMat = new Material(borderShader) { name = "Connector_Border_Mat" };
            borderMat.SetFloat("_Surface", 0f);
            borderMat.SetTexture("_BaseMap", null);
            borderMat.SetTexture("_EmissionMap", null);
            borderMat.DisableKeyword("_EMISSION");
            borderMat.SetColor("_EmissionColor", Color.black);
            borderMat.SetColor("_BaseColor", new Color(0.55f, 0.52f, 0.48f, 1f));  // stone
            borderMat.SetFloat("_Metallic", 0f);
            borderMat.SetFloat("_Smoothness", 0.15f);
            var bp = "Assets/RPG Tiny Fantasy Forest PBR/Material/Connector_Border_Mat.mat";
            AssetDatabase.DeleteAsset(bp); AssetDatabase.CreateAsset(borderMat, bp);

            // East border
            var eastBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastBorder.name = "Connector_Border_E";
            eastBorder.transform.SetParent(parent);
            eastBorder.transform.position = new Vector3(cx + connHalfW, borderY, connCenterZ);
            eastBorder.transform.localScale = new Vector3(borderThick, borderH, connLen);
            eastBorder.GetComponent<MeshRenderer>().sharedMaterial = borderMat;

            // West border
            var westBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westBorder.name = "Connector_Border_W";
            westBorder.transform.SetParent(parent);
            westBorder.transform.position = new Vector3(cx - connHalfW, borderY, connCenterZ);
            westBorder.transform.localScale = new Vector3(borderThick, borderH, connLen);
            westBorder.GetComponent<MeshRenderer>().sharedMaterial = borderMat;
        }
    }

    // =====================================================================
    // ROAD GUARDRAILS (concrete barriers on both sides of the road)
    // =====================================================================
    static void CreateRoadGuardrails(Transform parent, Material baseMat) {
        var shader = Shader.Find("Universal Render Pipeline/Lit");

        // Concrete material - light grey
        var concreteMat = new Material(shader) { name = "Guardrail_Mat" };
        concreteMat.SetFloat("_Surface", 0f);
        concreteMat.SetTexture("_BaseMap", null);
        concreteMat.SetTexture("_EmissionMap", null);
        concreteMat.DisableKeyword("_EMISSION");
        concreteMat.SetColor("_EmissionColor", Color.black);
        concreteMat.SetColor("_BaseColor", new Color(0.72f, 0.72f, 0.72f, 1f));
        concreteMat.SetFloat("_Metallic", 0f);
        concreteMat.SetFloat("_Smoothness", 0.15f);
        var gp = "Assets/RPG Tiny Fantasy Forest PBR/Material/Guardrail_Mat.mat";
        AssetDatabase.DeleteAsset(gp); AssetDatabase.CreateAsset(concreteMat, gp);

        // Yellow stripe material for visibility
        var stripeMat = new Material(shader) { name = "Guardrail_Stripe_Mat" };
        stripeMat.SetFloat("_Surface", 0f);
        stripeMat.SetTexture("_BaseMap", null);
        stripeMat.SetTexture("_EmissionMap", null);
        stripeMat.DisableKeyword("_EMISSION");
        stripeMat.SetColor("_EmissionColor", Color.black);
        stripeMat.SetColor("_BaseColor", new Color(0.95f, 0.78f, 0.05f, 1f));
        stripeMat.SetFloat("_Metallic", 0f);
        stripeMat.SetFloat("_Smoothness", 0.2f);
        var sp = "Assets/RPG Tiny Fantasy Forest PBR/Material/Guardrail_Stripe_Mat.mat";
        AssetDatabase.DeleteAsset(sp); AssetDatabase.CreateAsset(stripeMat, sp);

        // Road runs at Z = -7.5 ± 7.5  →  edges at Z=0 and Z=-15
        // North edge of road = Z = 0 (connects sidewalk)
        // South edge of road = Z = -15
        float roadLength = 200f;
        float roadNorthZ = 0f;      // border with sidewalk
        float roadSouthZ = -15f;    // outer south edge
        float guardrailH  = 0.5f;   // height of main barrier
        float guardrailW  = 0.4f;   // thickness
        float stripeH     = 0.08f;  // thin yellow stripe on top
        float yBase = -0.05f;       // sit on the road surface

        // --- NORTH guardrail (road-sidewalk border) ---
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Road_Guardrail_North";
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(0f, yBase + guardrailH * 0.5f, roadNorthZ - guardrailW * 0.5f);
            go.transform.localScale = new Vector3(roadLength, guardrailH, guardrailW);
            go.GetComponent<MeshRenderer>().sharedMaterial = concreteMat;

            // Yellow stripe on top
            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "Stripe_N";
            stripe.transform.SetParent(go.transform, false);
            stripe.transform.localPosition = new Vector3(0f, 0.5f + stripeH * 0.5f, 0f);
            stripe.transform.localScale = new Vector3(1f, stripeH / guardrailH, 1f);
            Object.DestroyImmediate(stripe.GetComponent<Collider>());
            stripe.GetComponent<MeshRenderer>().sharedMaterial = stripeMat;
        }

        // --- SOUTH guardrail (outer road edge) ---
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Road_Guardrail_South";
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(0f, yBase + guardrailH * 0.5f, roadSouthZ + guardrailW * 0.5f);
            go.transform.localScale = new Vector3(roadLength, guardrailH, guardrailW);
            go.GetComponent<MeshRenderer>().sharedMaterial = concreteMat;

            // Yellow stripe on top
            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "Stripe_S";
            stripe.transform.SetParent(go.transform, false);
            stripe.transform.localPosition = new Vector3(0f, 0.5f + stripeH * 0.5f, 0f);
            stripe.transform.localScale = new Vector3(1f, stripeH / guardrailH, 1f);
            Object.DestroyImmediate(stripe.GetComponent<Collider>());
            stripe.GetComponent<MeshRenderer>().sharedMaterial = stripeMat;
        }
    }

    static void CreateRim(string name, float x, float z, float w, float d,
                          Material mat, Transform parent) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, 0.05f, z);
        go.transform.localScale = new Vector3(w, 0.15f, d);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void CreateRimZ(string name, float x, float z, float w, float d,
                           Material mat, Transform parent) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, 0.05f, z);
        go.transform.localScale = new Vector3(w, 0.15f, d);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // =====================================================================
    // GROUND PLANES (grass/terrain fill)
    // =====================================================================
    static void CreateNorthGround(Transform parent) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Ground_North";
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(0f, -1f, 55f);
        go.transform.localScale = new Vector3(300f, 1f, 100f);

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader) { name = "Ground_North_Mat" };
        mat.SetFloat("_Surface", 0f);
        mat.SetColor("_BaseColor", new Color(0.22f, 0.45f, 0.18f, 1f));
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.0f);
        mat.DisableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.black);
        var p = "Assets/RPG Tiny Fantasy Forest PBR/Material/Ground_North_Mat.mat";
        AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(mat, p);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void CreateSouthGround(Transform parent) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Ground_South";
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(0f, -1f, -40f);
        go.transform.localScale = new Vector3(300f, 1f, 50f);

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader) { name = "Ground_South_Mat" };
        mat.SetFloat("_Surface", 0f);
        mat.SetColor("_BaseColor", new Color(0.22f, 0.45f, 0.18f, 1f));
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.0f);
        mat.DisableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.black);
        var p = "Assets/RPG Tiny Fantasy Forest PBR/Material/Ground_South_Mat.mat";
        AssetDatabase.DeleteAsset(p); AssetDatabase.CreateAsset(mat, p);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // =====================================================================
    // BENCH
    // =====================================================================
    static void CreateBench(float x, float z, Transform parent) {
        var benchObj = new GameObject("Bench_" + x);
        benchObj.transform.SetParent(parent, false);
        benchObj.transform.position = new Vector3(x, 0.2f, z);

        var seatMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        seatMat.color = new Color(0.45f, 0.28f, 0.15f, 1f);
        var legMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        legMat.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seat.name = "Seat";
        seat.transform.SetParent(benchObj.transform, false);
        seat.transform.localPosition = Vector3.zero;
        seat.transform.localScale = new Vector3(1.5f, 0.1f, 0.5f);
        seat.GetComponent<MeshRenderer>().sharedMaterial = seatMat;

        var legL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        legL.name = "Leg_L";
        legL.transform.SetParent(benchObj.transform, false);
        legL.transform.localPosition = new Vector3(-0.6f, -0.2f, 0f);
        legL.transform.localScale = new Vector3(0.1f, 0.3f, 0.4f);
        legL.GetComponent<MeshRenderer>().sharedMaterial = legMat;

        var legR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        legR.name = "Leg_R";
        legR.transform.SetParent(benchObj.transform, false);
        legR.transform.localPosition = new Vector3(0.6f, -0.2f, 0f);
        legR.transform.localScale = new Vector3(0.1f, 0.3f, 0.4f);
        legR.GetComponent<MeshRenderer>().sharedMaterial = legMat;
    }

    // =====================================================================
    // STREET LAMP
    // =====================================================================
    static void CreateStreetLamp(float x, float z, Transform parent, Material baseMat) {
        var lampObj = new GameObject("StreetLamp_" + x + "_" + z);
        lampObj.transform.SetParent(parent, false);
        lampObj.transform.position = new Vector3(x, 0f, z);

        var ironMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        ironMat.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Post";
        post.transform.SetParent(lampObj.transform, false);
        post.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        post.transform.localScale = new Vector3(0.1f, 1.5f, 0.1f);
        post.GetComponent<MeshRenderer>().sharedMaterial = ironMat;
        Object.DestroyImmediate(post.GetComponent<Collider>());

        var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "Arm";
        arm.transform.SetParent(lampObj.transform, false);
        arm.transform.localPosition = new Vector3(0f, 2.95f, 0.25f);
        arm.transform.localScale = new Vector3(0.1f, 0.1f, 0.5f);
        arm.GetComponent<MeshRenderer>().sharedMaterial = ironMat;
        Object.DestroyImmediate(arm.GetComponent<Collider>());

        var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulb.name = "Bulb";
        bulb.transform.SetParent(lampObj.transform, false);
        bulb.transform.localPosition = new Vector3(0f, 2.85f, 0.45f);
        bulb.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        Object.DestroyImmediate(bulb.GetComponent<Collider>());

        var bulbMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bulbMat.color = new Color(1f, 0.95f, 0.6f, 1f);
        bulbMat.EnableKeyword("_EMISSION");
        bulbMat.SetColor("_EmissionColor", new Color(1f, 0.9f, 0.5f) * 2f);
        bulb.GetComponent<MeshRenderer>().sharedMaterial = bulbMat;

        var lightGo = new GameObject("LightSource");
        lightGo.transform.SetParent(lampObj.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 2.7f, 0.45f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.92f, 0.7f);
        light.range = 12f;
        light.intensity = 2f;
    }

    // =====================================================================
    // TREE HELPER
    // =====================================================================
    static void PlantTree(GameObject prefab, float x, float z, Transform parent) {
        if (prefab == null) return;
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        obj.transform.position = new Vector3(x, 0f, z);
        obj.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
        obj.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
    }
}
#endif
