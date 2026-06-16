using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

[InitializeOnLoad]
public class Procedural2DMapGenerator
{
    static Procedural2DMapGenerator()
    {
        string flagPath = "Temp/map_generated.txt";
        if (!File.Exists(flagPath))
        {
            GenerateMap();
            File.WriteAllText(flagPath, "generated");
        }
    }

    [MenuItem("Tools/Regenerate 2D Map")]
    public static void ForceRegenerate()
    {
        GenerateMap();
    }

    private static void GenerateMap()
    {
        Debug.Log("Starting Procedural 2D Map Generation using model assets...");

        // Load high-quality sprites from Assets/Model/
        Tile waterTile = LoadTileFromSpritePath("Assets/Model/water.png", "water");
        Tile grassTile = LoadTileFromSpritePath("Assets/Model/grass.png", "grass");
        Tile roadTile = LoadTileFromSpritePath("Assets/Model/road.png", "road");
        Tile treeTile = LoadTileFromSpritePath("Assets/Model/willow.png", "tree");
        Tile houseTile = LoadTileFromSpritePath("Assets/Model/house.png", "house");
        Tile islandTile = LoadTileFromSpritePath("Assets/Model/island.png", "island");
        Tile turtleTowerTile = LoadTileFromSpritePath("Assets/Model/turtle_tower.png", "turtle_tower");

        UnityEngine.SceneManagement.Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        newScene.name = "HoGuom2DMap";

        GameObject gridGo = new GameObject("2D_Grid");
        Grid grid = gridGo.AddComponent<Grid>();
        grid.cellLayout = GridLayout.CellLayout.Isometric;
        grid.cellSize = new Vector3(1f, 0.5f, 1f);

        GameObject groundGo = new GameObject("Ground_Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
        groundGo.transform.SetParent(gridGo.transform);
        Tilemap groundMap = groundGo.GetComponent<Tilemap>();

        GameObject roadGo = new GameObject("Road_Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
        roadGo.transform.SetParent(gridGo.transform);
        Tilemap roadMap = roadGo.GetComponent<Tilemap>();
        TilemapRenderer roadRenderer = roadGo.GetComponent<TilemapRenderer>();
        roadRenderer.sortingOrder = 1;

        GameObject obstacleGo = new GameObject("Obstacles_Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
        obstacleGo.transform.SetParent(gridGo.transform);
        Tilemap obstacleMap = obstacleGo.GetComponent<Tilemap>();
        TilemapRenderer obstacleRenderer = obstacleGo.GetComponent<TilemapRenderer>();
        obstacleRenderer.sortingOrder = 2;

        int size = 35;
        for (int x = -size; x <= size; x++)
        {
            for (int y = -size; y <= size; y++)
            {
                float d = Mathf.Sqrt(x * x + y * y);
                bool isConnectingRoad = (x == 0 || y == 0 || x == 1 || y == 1);

                if (d < 2)
                {
                    groundMap.SetTile(new Vector3Int(x, y, 0), islandTile);
                    if (x == 0 && y == 0)
                    {
                        obstacleMap.SetTile(new Vector3Int(x, y, 0), turtleTowerTile);
                    }
                }
                else if (d < 10)
                {
                    groundMap.SetTile(new Vector3Int(x, y, 0), waterTile);
                }
                else if (d < 18)
                {
                    if (isConnectingRoad)
                    {
                        roadMap.SetTile(new Vector3Int(x, y, 0), roadTile);
                    }
                    else
                    {
                        groundMap.SetTile(new Vector3Int(x, y, 0), grassTile);
                        if (Random.value < 0.15f && Mathf.Abs(x) > 2 && Mathf.Abs(y) > 2)
                        {
                            obstacleMap.SetTile(new Vector3Int(x, y, 0), treeTile);
                        }
                    }
                }
                else if (d < 22)
                {
                    roadMap.SetTile(new Vector3Int(x, y, 0), roadTile);
                }
                else if (d <= 30)
                {
                    if (isConnectingRoad)
                    {
                        roadMap.SetTile(new Vector3Int(x, y, 0), roadTile);
                    }
                    else
                    {
                        groundMap.SetTile(new Vector3Int(x, y, 0), grassTile);
                        if ((x % 3 == 0 && y % 3 == 0) && Random.value < 0.5f && Mathf.Abs(x) > 2)
                        {
                            obstacleMap.SetTile(new Vector3Int(x, y, 0), houseTile);
                        }
                    }
                }
            }
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 12f;
            mainCam.transform.position = new Vector3(0, 0, -10);
            mainCam.transform.rotation = Quaternion.identity;
        }

        string scenePath = "Assets/Scenes/HoGuom2DMap.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        EditorSceneManager.OpenScene(scenePath);

        GameObject loadedGrid = GameObject.Find("2D_Grid");
        if (loadedGrid != null)
        {
            Selection.activeGameObject = loadedGrid;
            EditorApplication.delayCall += () => {
                SceneView.FrameLastActiveSceneView();
            };
        }

        Debug.Log("Procedural 2D Map Generated and Saved to: " + scenePath);
    }

    private static Tile LoadTileFromSpritePath(string spritePath, string tileName)
    {
        string folder = "Assets/ProceduralTiles";
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        string tilePath = $"{folder}/{tileName}.asset";

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogError($"Sprite not found at {spritePath}!");
        }

        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, tilePath);
        }

        tile.sprite = sprite;
        EditorUtility.SetDirty(tile);
        return tile;
    }
}
