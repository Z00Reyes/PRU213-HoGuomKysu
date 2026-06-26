#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class PlayFromMainMenu
{
    static PlayFromMainMenu()
    {
        // Ghi đè nút Play của Unity: Luôn chạy từ MainMenu dù đang mở Scene nào
        string scenePath = "Assets/Scenes/MainMenu.unity";
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        
        if (sceneAsset != null)
        {
            EditorSceneManager.playModeStartScene = sceneAsset;
        }
    }
}
#endif
