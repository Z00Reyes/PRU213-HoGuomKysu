using UnityEngine;

public class BobberController : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 targetPos;
    private float flightDuration;
    private float arcHeight;
    private float elapsed = 0f;
    private bool isFlying = false;

    private bool isBobbing = false;
    public float bobSpeed = 4f;       // Tốc độ nhấp nhô
    public float bobHeight = 0.08f;    // Độ cao nhấp nhô
    private float bobCenterY;

    // Khởi tạo thông tin bay cho phao câu
    public void Initialize(Vector3 start, Vector3 target, float duration, float height)
    {
        startPos = start;
        targetPos = target;
        flightDuration = duration;
        arcHeight = height;
        elapsed = 0f;
        isFlying = true;
        isBobbing = false;
        
        transform.position = start;
    }

    void Update()
    {
        if (isFlying)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightDuration);

            // Nội suy tuyến tính trên mặt phẳng phẳng X và Z
            Vector3 currentGround = Vector3.Lerp(
                new Vector3(startPos.x, 0f, startPos.z), 
                new Vector3(targetPos.x, 0f, targetPos.z), 
                t
            );

            // Tính toán độ cao theo hình vòng cung parabol
            float arcY = Mathf.Sin(t * Mathf.PI) * arcHeight;
            
            // Nội suy độ cao cơ sở giữa điểm đầu và điểm cuối
            float baseHeight = Mathf.Lerp(startPos.y, targetPos.y, t);

            // Cập nhật vị trí phao câu
            transform.position = new Vector3(currentGround.x, baseHeight + arcY, currentGround.z);

            if (t >= 1f)
            {
                isFlying = false;
                isBobbing = true;
                bobCenterY = transform.position.y;
            }
        }
        else if (isBobbing)
        {
            // Hiệu ứng nhấp nhô nhẹ trên mặt nước/đất
            float newY = bobCenterY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
}
