using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class IslandGenerator : MonoBehaviour
{
    [Header("Base Island & Ocean")]
    public GameObject landMass100Prefab;
    public GameObject landMass40Prefab;
    public GameObject landMass20Prefab;
    public GameObject shorePrefab;
    public GameObject oceanPrefab;

    [Header("Cliff & Vertical Terrain")]
    public GameObject rockCliffPrefab;
    public GameObject rockMountainPrefab;

    [Header("Hydrology System")]
    public GameObject riverPrefab;
    public GameObject waterfallPrefab;

    [Header("Paths & Connectivity")]
    public GameObject stairPrefab;
    public GameObject bridgePrefab;
    public GameObject roadA01;
    public GameObject roadC01;

    [Header("Points of Interest")]
    public GameObject portalPrefab;
    public GameObject gatePrefab;
    public GameObject gate03Prefab;
    public GameObject treeStumpPrefab;

    [Header("Vegetation")]
    public GameObject treePrefab;
    public GameObject grassPrefab;
    public GameObject flowerPrefab;

    private Transform islandHolder;

    private void Reset()
    {
#if UNITY_EDITOR
        landMass100Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/LandMass/LM100RND.prefab");
        landMass40Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/LandMass/LM40RND.prefab");
        landMass20Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/LandMass/LM20.prefab");
        shorePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/LandMass/Shore01.prefab");
        oceanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/LandMass/Ocean.prefab");
        
        rockCliffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/Rock/RockCliff05.prefab");
        rockMountainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/Mountains/RockMountain01.prefab");
        
        riverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/RiverRoadLakeFall/RiverE02.prefab");
        waterfallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/RiverRoadLakeFall/Waterfall01.prefab");

        stairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/BuildingUtilityDeco/Stair01.prefab");
        bridgePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/BuildingUtilityDeco/Bridge06.prefab");
        portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/BuildingUtilityDeco/Portal01.prefab");
        gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/BuildingUtilityDeco/Gate01.prefab");
        gate03Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/BuildingUtilityDeco/Gate03.prefab");
        treeStumpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/BuildingUtilityDeco/TreeStump01.prefab");
        
        roadA01 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/RiverRoadLakeFall/RoadA01.prefab");
        roadC01 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/RiverRoadLakeFall/RoadC01.prefab");

        treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/TreePlants/Tree02.prefab");
        grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/TreePlants/Grass01.prefab");
        flowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Fantasy Forest PBR/Prefab/TreePlants/Flower01.prefab");
#endif
    }

    [ContextMenu("Build Island from Reference Image")]
    public void BuildIsland()
    {
        ClearOldIsland();
        Reset();

        GameObject holder = new GameObject("_IslandHolder");
        islandHolder = holder.transform;

        if (oceanPrefab) InstantiatePrefab(oceanPrefab, Vector3.zero, Quaternion.identity, Vector3.one, islandHolder);

        // --- LEVEL 1: BASE (Hanoi City Base) ---
        InstantiatePrefab(landMass100Prefab, Vector3.zero, Quaternion.identity, Vector3.one, islandHolder);

        // Entrance stairs at the corners
        InstantiatePrefab(stairPrefab, new Vector3(-43, 0.1f, -43), Quaternion.Euler(0, 45, 0), Vector3.one, islandHolder);
        InstantiatePrefab(stairPrefab, new Vector3(43, 0.1f, -43), Quaternion.Euler(0, -45, 0), Vector3.one, islandHolder);
        InstantiatePrefab(stairPrefab, new Vector3(-43, 0.1f, 43), Quaternion.Euler(0, 135, 0), Vector3.one, islandHolder);
        InstantiatePrefab(stairPrefab, new Vector3(43, 0.1f, 43), Quaternion.Euler(0, -135, 0), Vector3.one, islandHolder);

        // --- LEVEL 2: HO GUOM PARK ---
        float h1 = 3.5f;
        // Large expanded landmass for the park area
        InstantiatePrefab(landMass40Prefab, new Vector3(0, h1, 0), Quaternion.identity, new Vector3(1.6f, 1, 1.6f), islandHolder);

        // Inner stairs to park
        for (int i = 0; i < 4; i++) {
            float angle = i * 90;
            Vector3 stairPos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 28, 0.1f, Mathf.Sin(angle * Mathf.Deg2Rad) * 28);
            InstantiatePrefab(stairPrefab, stairPos, Quaternion.Euler(0, -angle + 90, 0), Vector3.one, islandHolder);
        }

        // --- THE LAKE (Há»’ GÆ¯Æ M) ---
        Vector3 lakeCenter = new Vector3(0, h1 + 0.05f, 0);
        float radiusX = 14f;
        float radiusZ = 22f;
        SpawnLake(lakeCenter, radiusX, radiusZ);

        // --- ROCKS (Decorations on the outer edge) ---
        SpawnRocks();

        // --- WALKING PATH (PHá» ÄI Bá»˜) ---
        SpawnWalkingPath(lakeCenter, radiusX + 3.5f, radiusZ + 3.5f, h1 + 0.1f);

        // --- VEGETATION ---
        SpawnTrees(h1);

        // --- ENTRANCE GATE ---
        if (gate03Prefab)
        {
            Vector3 entrancePos = lakeCenter + new Vector3(0, 0.1f, -(radiusZ + 3.5f));
            InstantiatePrefab(gate03Prefab, entrancePos, Quaternion.Euler(0, 0, 0), Vector3.one * 1.5f, islandHolder);
        }

        // Rotate diamond for isometric view
        islandHolder.rotation = Quaternion.Euler(0, 45, 0);

        Debug.Log("Ho Guom Island Redesigned Successfully!");
    }

    private void SpawnLake(Vector3 center, float rx, float rz)
    {
#if UNITY_EDITOR
        Material waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/RPG Tiny Fantasy Forest PBR/Material/Special/Water_River.mat");
#else
        Material waterMat = null;
#endif
        GameObject water = new GameObject("LakeWater");
        water.transform.SetParent(islandHolder, false);
        water.transform.localPosition = center;
        water.transform.localRotation = Quaternion.identity;
        
        MeshFilter mf = water.AddComponent<MeshFilter>();
        MeshRenderer mr = water.AddComponent<MeshRenderer>();
        
        // Generate oval mesh
        Mesh mesh = new Mesh();
        int segments = 40;
        Vector3[] vertices = new Vector3[segments + 1];
        Vector2[] uvs = new Vector2[segments + 1];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < segments; i++) {
            float a = i * Mathf.PI * 2f / segments;
            float x = Mathf.Cos(a) * rx;
            float z = Mathf.Sin(a) * rz;
            vertices[i + 1] = new Vector3(x, 0, z);
            uvs[i + 1] = new Vector2(x/(rx*2) + 0.5f, z/(rz*2) + 0.5f);
        }
        for (int i = 0; i < segments; i++) {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = (i + 1 == segments) ? 1 : i + 2;
            triangles[i * 3 + 2] = i + 1;
        }
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        mf.sharedMesh = mesh;
        if (waterMat != null) mr.sharedMaterial = waterMat;

        // Lake Border Rocks
        int rockCount = 70;
        for(int i = 0; i < rockCount; i++) {
            float angle = i * Mathf.PI * 2 / rockCount;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * rx, 0, Mathf.Sin(angle) * rz);
            pos += new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
            pos.y -= Random.Range(0.1f, 0.3f); 
            
            GameObject prefab = (Random.value > 0.2f) ? rockCliffPrefab : rockMountainPrefab;
            float rockScale = Random.Range(0.2f, 0.45f);
            InstantiatePrefab(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), new Vector3(rockScale, rockScale * Random.Range(0.8f, 1.2f), rockScale), islandHolder);
        }

        // --- TURTLE TOWER (THĂP RĂ™A) ---
        Vector3 turtleIslandPos = center + new Vector3(0, 0, -10f);
        InstantiatePrefab(landMass20Prefab, turtleIslandPos - new Vector3(0, 0.1f, 0), Quaternion.identity, new Vector3(0.15f, 1, 0.15f), islandHolder);
        if (gatePrefab) InstantiatePrefab(gatePrefab, turtleIslandPos, Quaternion.identity, Vector3.one, islandHolder);

        // --- NGOC SON TEMPLE (Äá»€N NGá»ŒC SÆ N) ---
        Vector3 ngocSonPos = center + new Vector3(-3f, 0, 12f);
        InstantiatePrefab(landMass20Prefab, ngocSonPos - new Vector3(0, 0.1f, 0), Quaternion.identity, new Vector3(0.3f, 1, 0.3f), islandHolder);
        if (portalPrefab) InstantiatePrefab(portalPrefab, ngocSonPos, Quaternion.Euler(0, 90, 0), Vector3.one, islandHolder);
        if (gatePrefab) InstantiatePrefab(gatePrefab, ngocSonPos + new Vector3(-2.5f, 0, 0), Quaternion.Euler(0, -90, 0), Vector3.one, islandHolder);

        // --- THE HUC BRIDGE (Cáº¦U THĂ HĂC) ---
        // Bridge connects Ngoc Son (-3, 12) to East Shore (-13, 10) roughly
        Vector3 bridgePos = center + new Vector3(-9f, 0.15f, 11f);
        if (bridgePrefab) InstantiatePrefab(bridgePrefab, bridgePos, Quaternion.Euler(0, 15, 0), new Vector3(1.5f, 1, 1), islandHolder);
    }

    private void SpawnWalkingPath(Vector3 center, float rx, float rz, float y)
    {
        int pathSegments = 32;
        for(int i = 0; i < pathSegments; i++) {
            float angle = i * Mathf.PI * 2 / pathSegments;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * rx, y - center.y, Mathf.Sin(angle) * rz);
            float deg = -angle * Mathf.Rad2Deg;
            InstantiatePrefab(roadA01, pos, Quaternion.Euler(0, deg, 0), new Vector3(1.2f, 1, 1.8f), islandHolder);

            // Add tree stumps (as benches) on the outer edge of the walking path
            if (i % 4 == 0 && treeStumpPrefab != null)
            {
                Vector3 stumpPos = center + new Vector3(Mathf.Cos(angle) * (rx + 2.0f), y - center.y, Mathf.Sin(angle) * (rz + 2.0f));
                // Face towards the lake
                float faceAngle = deg - 90;
                InstantiatePrefab(treeStumpPrefab, stumpPos, Quaternion.Euler(0, faceAngle, 0), Vector3.one * 0.8f, islandHolder);
            }

            // Add flowers along the inner edge of the walking path
            if (flowerPrefab != null)
            {
                float innerAngle = angle + (Mathf.PI / pathSegments); // offset between path segments
                Vector3 flowerPos = center + new Vector3(Mathf.Cos(innerAngle) * (rx - 1.5f), y - center.y, Mathf.Sin(innerAngle) * (rz - 1.5f));
                InstantiatePrefab(flowerPrefab, flowerPos, Quaternion.Euler(0, Random.Range(0, 360), 0), Vector3.one * 1.5f, islandHolder);
            }
        }
    }

    private void SpawnRocks()
    {
        for (int i = 0; i < 60; i++)
        {
            float t = (float)i / 60;
            float perimeter = 400; 
            float distance = t * perimeter;

            Vector3 pos;
            if (distance < 100) pos = new Vector3(-50 + distance, 0.5f, -50);
            else if (distance < 200) pos = new Vector3(50, 0.5f, -50 + (distance - 100));
            else if (distance < 300) pos = new Vector3(50 - (distance - 200), 0.5f, 50);
            else pos = new Vector3(-50, 0.5f, 50 - (distance - 300));

            if (pos.x < -35 && pos.z < -35) continue;

            Vector3 inwardDir = new Vector3(-pos.x, 0, -pos.z).normalized;
            pos += inwardDir * Random.Range(0f, 3f) + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            
            GameObject prefab = (Random.value > 0.5f) ? rockCliffPrefab : rockMountainPrefab;
            InstantiatePrefab(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), Vector3.one, islandHolder);
        }
    }

    private void SpawnTrees(float h1)
    {
        int totalTrees = 400;
        float rx = 14f;
        float rz = 22f;

        for (int i = 0; i < totalTrees; i++) 
        {
            float x = Random.Range(-45f, 45f);
            float z = Random.Range(-45f, 45f);
            
            // Avoid lake strictly
            if ((x*x)/(rx*rx) + (z*z)/(rz*rz) <= 1.4f) continue;
            // Avoid bridge area
            if (x < -4 && x > -16 && z > 8 && z < 16) continue;

            float y = 0.2f;
            if (x > -32 && x < 32 && z > -32 && z < 32) y = h1;

            Vector3 pos = new Vector3(x, y, z);
            GameObject prefab;
            float r = Random.value;
            if (r > 0.4f) prefab = treePrefab;
            else if (r > 0.15f) prefab = grassPrefab;
            else prefab = flowerPrefab;

            GameObject obj = InstantiatePrefab(prefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), Vector3.one, islandHolder);
            if(obj != null) {
                float scale = Random.Range(0.7f, 1.3f);
                obj.transform.localScale = new Vector3(scale, scale, scale);
            }
        }
    }

    private void ClearOldIsland()
    {
        GameObject oldHolder = GameObject.Find("_IslandHolder");
        if (oldHolder != null) DestroyImmediate(oldHolder);
    }

    private GameObject InstantiatePrefab(GameObject prefab, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Transform parent)
    {
        if (prefab == null) return null;
#if UNITY_EDITOR
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (obj != null)
        {
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = localRotation;
            obj.transform.localScale = localScale;
        }
        return obj;
#else
        GameObject obj = Instantiate(prefab, parent);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = localRotation;
        obj.transform.localScale = localScale;
        return obj;
#endif
    }
}
