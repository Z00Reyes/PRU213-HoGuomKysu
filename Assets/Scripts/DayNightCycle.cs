using UnityEngine;
using UnityEngine.UI;

public class DayNightCycle : MonoBehaviour
{
    private static DayNightCycle instance;

    [Header("Time Settings")]
    [Range(0, 24)]
    public float timeOfDay = 8f; // Start at 8:00 AM
    public float timeSpeed = 0.05f; // 1 real second = 3 in-game minutes
    
    [Header("Light")]
    public Light sunLight;
    
    private Text timeText;
    private GameObject clockUI;

    void Start()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // 1. Try to find the sun light if not assigned
        if (sunLight == null)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    sunLight = l;
                    break;
                }
            }
        }

        // 2. Setup the UI Clock automatically
        CreateClockUI();
    }

    void Update()
    {
        // Advance time
        timeOfDay += Time.deltaTime * timeSpeed;
        if (timeOfDay >= 24f)
        {
            timeOfDay -= 24f; // Loop back to midnight
        }

        UpdateSun();
        UpdateClockUI();
    }

    private void UpdateSun()
    {
        if (sunLight == null) return;

        float timePercent = timeOfDay / 24f;

        // X Axis Rotation: Midnight (0) -> -90, 6AM (0.25) -> 0, Noon (0.5) -> 90, 6PM (0.75) -> 180
        float sunAngle = (timePercent * 360f) - 90f;
        sunLight.transform.localRotation = Quaternion.Euler(sunAngle, 50f, 0f);

        // Adjust intensity based on time (Day vs Night)
        // Night: 18:00 to 06:00
        if (timeOfDay <= 5.5f || timeOfDay >= 18.5f)
        {
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, 0.15f, Time.deltaTime * 2f);
            sunLight.color = Color.Lerp(sunLight.color, new Color(0.2f, 0.3f, 0.5f), Time.deltaTime * 2f); // Blueish night
        }
        else if (timeOfDay > 5.5f && timeOfDay < 7f)
        {
            // Sunrise
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, 0.8f, Time.deltaTime * 2f);
            sunLight.color = Color.Lerp(sunLight.color, new Color(1f, 0.6f, 0.3f), Time.deltaTime * 2f); // Orange sunrise
        }
        else if (timeOfDay > 17f && timeOfDay < 18.5f)
        {
            // Sunset
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, 0.8f, Time.deltaTime * 2f);
            sunLight.color = Color.Lerp(sunLight.color, new Color(1f, 0.4f, 0.2f), Time.deltaTime * 2f); // Orange sunset
        }
        else
        {
            // Midday
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, 1.3f, Time.deltaTime * 2f);
            sunLight.color = Color.Lerp(sunLight.color, new Color(1f, 0.95f, 0.9f), Time.deltaTime * 2f); // Bright yellow/white
        }
    }

    private void CreateClockUI()
    {
        // Create Canvas for Clock
        GameObject canvasGo = new GameObject("ClockCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // High order to appear on top
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        
        // Create Background Panel
        GameObject bgGo = new GameObject("ClockBackground");
        bgGo.transform.SetParent(canvasGo.transform, false);
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
        
        RectTransform bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 1f); // Top Left
        bgRt.anchorMax = new Vector2(0f, 1f);
        bgRt.pivot = new Vector2(0f, 1f);
        bgRt.anchoredPosition = new Vector2(20f, -20f);
        bgRt.sizeDelta = new Vector2(150f, 50f);

        // Create Text
        GameObject textGo = new GameObject("ClockText");
        textGo.transform.SetParent(bgGo.transform, false);
        timeText = textGo.AddComponent<Text>();
        
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        timeText.font = uiFont;
        timeText.color = new Color(1f, 0.85f, 0f);
        timeText.fontSize = 24;
        timeText.fontStyle = FontStyle.Bold;
        timeText.alignment = TextAnchor.MiddleCenter;

        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        textRt.anchoredPosition = Vector2.zero;

        clockUI = canvasGo;
        DontDestroyOnLoad(clockUI);
    }

    private void UpdateClockUI()
    {
        if (timeText == null) return;

        int hours = Mathf.FloorToInt(timeOfDay);
        int minutes = Mathf.FloorToInt((timeOfDay - hours) * 60f);

        string ampm = hours < 12 ? "AM" : "PM";
        int displayHours = hours % 12;
        if (displayHours == 0) displayHours = 12;

        string icon = (timeOfDay >= 6f && timeOfDay < 18f) ? "☀" : "☾";
        timeText.text = $"{icon} {displayHours:00}:{minutes:00} {ampm}";
    }
}
