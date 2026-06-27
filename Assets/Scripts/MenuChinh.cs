using UnityEngine;
using UnityEngine.SceneManagement; // Thư viện để chuyển cảnh game
using UnityEngine.Video; // Thư viện video của Unity

public class MenuChinh : MonoBehaviour
{
    [Header("Video Intro Settings")]
    [SerializeField] private VideoPlayer videoPlayer; // Kéo component VideoPlayer vào đây
    [SerializeField] private GameObject videoPanel;    // Canvas Panel chứa UI hiển thị video (nếu dùng UI RawImage)
    [SerializeField] private GameObject mainMenuPanel; // Canvas hoặc Panel chứa toàn bộ UI nút bấm/hình nền cần ẩn đi khi chạy video
    [SerializeField] private AudioSource backgroundMusic; // Kéo AudioSource nhạc nền vào đây (hoặc để trống tự tìm)

    private bool isPlayingVideo = false;

    private void Start()
    {
        // Ẩn panel video khi mới vào main menu
        if (videoPanel != null)
        {
            videoPanel.SetActive(false);
        }

        if (videoPlayer != null)
        {
            // Đăng ký sự kiện gọi khi video chạy hết
            videoPlayer.loopPointReached += OnVideoFinished;
            
            // Chuẩn bị sẵn video (Pre-buffer) để tránh bị đơ hình khi bắt đầu phát
            videoPlayer.Prepare();
        }
    }

    // Hàm xử lý khi bấm nút START
    public void BamNutStart()
    {
        if (videoPlayer != null)
        {
            // Bật panel video lên để hiển thị
            if (videoPanel != null)
            {
                videoPanel.SetActive(true);
            }

            // Ẩn UI của Main Menu đi để không che mất video trên Camera
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            // Tắt nhạc nền để tránh đè âm thanh
            TatNhacNen();

            // Bắt đầu phát video
            videoPlayer.Play();
            isPlayingVideo = true;
        }
        else
        {
            // Nếu không gán VideoPlayer, vào thẳng game
            StartGame();
        }
    }

    // Hàm tạm dừng nhạc nền
    private void TatNhacNen()
    {
        if (backgroundMusic != null)
        {
            backgroundMusic.Stop();
        }
        else
        {
            // Nếu chưa gán thủ công, tự động tìm và tắt tất cả các AudioSource khác đang phát trong Scene
            AudioSource[] allAudio = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (AudioSource audio in allAudio)
            {
                if (audio != null && audio.isPlaying)
                {
                    // Tránh tắt nhầm AudioSource gắn trên chính VideoPlayer (nếu có)
                    if (videoPlayer != null && audio.gameObject == videoPlayer.gameObject)
                    {
                        continue;
                    }
                    audio.Stop();
                }
            }
        }
    }

    private void Update()
    {
        // Cho phép người chơi nhấn phím Space, Enter hoặc Escape để bỏ qua video
        if (isPlayingVideo)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return))
            {
                SkipVideo();
            }
        }
    }

    // Hàm bỏ qua video
    public void SkipVideo()
    {
        if (isPlayingVideo)
        {
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }
            StartGame();
        }
    }

    // Hàm gọi tự động khi video chạy hết
    private void OnVideoFinished(VideoPlayer vp)
    {
        StartGame();
    }

    // Hàm chuyển sang màn chơi chính
    private void StartGame()
    {
        isPlayingVideo = false;
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