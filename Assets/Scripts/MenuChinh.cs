using UnityEngine;
using UnityEngine.SceneManagement; // Thư viện để chuyển cảnh game

public class MenuChinh : MonoBehaviour
{
    // Hàm xử lý khi bấm nút START
    public void BamNutStart()
    {
        // Chuyển sang màn chơi tiếp theo (ví dụ màn chơi chính)
        // Bạn có thể đổi tên "MapScene" thành tên Scene game của bạn
        SceneManager.LoadScene("MCScence");
    }

    // Hàm xử lý khi bấm nút SETTING
    public void BamNutSetting()
    {
        PauseMenu pm = FindAnyObjectByType<PauseMenu>();
        if (pm == null)
        {
            GameObject go = new GameObject("PauseManager");
            pm = go.AddComponent<PauseMenu>();
        }
        pm.OpenSettings();
    }

    // Hàm xử lý khi bấm nút QUIT
    public void BamNutQuit()
    {
        Debug.Log("Đã thoát game thành công!"); // Lệnh này để kiểm tra trong Unity
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(); // Lệnh thoát game thực tế khi xuất file (.exe)
#endif
    }
}