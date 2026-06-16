using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using System.IO;

[InitializeOnLoad]
public class SceneOpenerAndRenderer
{
    static SceneOpenerAndRenderer()
    {
        string requestPath = "Temp/render_request.txt";
        if (File.Exists(requestPath))
        {
            EditorApplication.delayCall += () => {
                Debug2DScene();
                try { File.Delete(requestPath); } catch {}
            };
        }
    }

    [MenuItem("Tools/Debug 2D Scene")]
    public static void Debug2DScene()
    {
        string scenePath = "Assets/Scenes/HoGuom2DMap.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);
        Debug.Log("DEBUG: Scene active: " + scene.name);
        var grid = GameObject.Find("2D_Grid");
        if (grid == null)
        {
            Debug.LogError("DEBUG: 2D_Grid not found!");
            return;
        }
        Debug.Log("DEBUG: 2D_Grid found at: " + grid.transform.position);
        foreach (Transform child in grid.transform)
        {
            var tilemap = child.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                tilemap.CompressBounds();
                Debug.Log($"DEBUG: Tilemap {child.name} has bounds {tilemap.cellBounds} and used tiles count: {tilemap.GetUsedTilesCount()}");
            }
            else
            {
                Debug.Log($"DEBUG: Child {child.name} has no Tilemap component.");
            }
        }
    }
}
// debug trigger
// debug trigger2
