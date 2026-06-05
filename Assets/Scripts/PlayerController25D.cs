using UnityEngine;

public class PlayerController25D : MonoBehaviour
{
    private CharacterController controller;
    private Animator animator;
    
    public float speed = 5f;        // Tốc độ di chuyển
    public float gravity = -9.81f;  // Trọng lực để nhân vật không bị bay lên trời
    
    private Vector3 velocity;
    private float lastHorizontal = 0f;  // Ghi nhớ hướng ngang cuối cùng
    private float lastVertical = 0f;    // Ghi nhớ hướng dọc cuối cùng (mặc định 0 = nhìn lên)
    private bool hasRod = false;        // Trạng thái cầm cần câu
    
    private bool isCasting = false;     // Trạng thái đang quăng cần câu
    private float castTimer = 0f;       // Bộ đếm thời gian quăng cần
    public float castDuration = 0.85f;  // Thời gian quăng cần (khớp với Animation Clip)

    [Header("Fishing Setup")]
    public Sprite bobberSprite;         // Sprite phao câu
    private LineRenderer lineRenderer;  // Component vẽ dây câu
    private GameObject currentBobber;   // Đối tượng phao câu hiện tại
    
    public float castDistance = 2.5f;   // Khoảng cách quăng cần câu
    public float castArcHeight = 0.8f;  // Chiều cao bay vòng cung của phao câu
    private bool isBobberActive = false; // Trạng thái phao câu đang hoạt động ở dưới nước

    [Header("Rod Tip Anchors")]
    public Transform rodTipUp;
    public Transform rodTipDown;
    public Transform rodTipLeft;
    public Transform rodTipRight;

    void Start()
    {
        // Tự động lấy cấu phần Character Controller gắn trên nhân vật
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        animator.speed = 1f;  // Force animation playback speed = 1x

        // Tự động tải Sprite phao câu từ Assets nếu chưa gán
#if UNITY_EDITOR
        if (bobberSprite == null)
        {
            bobberSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Model/bobber-removebg-preview.png");
        }
#endif

        // Tìm kiếm các điểm neo đầu cần câu được kéo tay trong Editor dưới MC
        rodTipUp = transform.Find("RodTip_Up");
        rodTipDown = transform.Find("RodTip_Down");
        rodTipLeft = transform.Find("RodTip_Left");
        rodTipRight = transform.Find("RodTip_Right");

        // Thiết lập LineRenderer cho dây câu
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        lineRenderer.startWidth = 0.015f;
        lineRenderer.endWidth = 0.015f;
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
        
        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader != null)
        {
            lineRenderer.material = new Material(lineShader);
        }
        
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;
    }

    void Update()
    {
        // Xử lý F key - toggle cầm/thả cần câu (chỉ thực hiện khi không đang quăng cần)
        if (!isCasting && Input.GetKeyDown(KeyCode.F))
        {
            if (isBobberActive)
            {
                DestroyBobber();
                isBobberActive = false;
            }
            hasRod = !hasRod;
        }

        // Xử lý quăng cần câu khi cầm cần câu và nhấn chuột trái hoặc phím Space
        if (hasRod && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            if (isBobberActive)
            {
                // Nếu phao đang ở dưới nước, bấm lần nữa sẽ thu cần câu
                DestroyBobber();
                isBobberActive = false;
            }
            else if (!isCasting)
            {
                // Bắt đầu động tác quăng cần câu
                isCasting = true;
                castTimer = castDuration;
                if (animator != null)
                {
                    animator.SetTrigger("Cast");
                }
            }
        }

        // Cập nhật trạng thái quăng cần câu
        if (isCasting)
        {
            castTimer -= Time.deltaTime;
            if (castTimer <= 0f)
            {
                isCasting = false;
                isBobberActive = true;
                
                // Sinh phao câu và vẽ dây câu sau khi hoàn thành animation quăng
                SpawnBobber();
            }
        }

        // Lấy nút bấm di chuyển từ bàn phím (A/D/W/S hoặc Mũi tên)
        // Nếu đang quăng cần câu, không nhận di chuyển từ bàn phím (moveX, moveZ = 0)
        float moveX = 0f;
        float moveZ = 0f;
        
        if (!isCasting)
        {
            moveX = Input.GetAxisRaw("Horizontal");
            moveZ = Input.GetAxisRaw("Vertical"); // Nhận trục Z thay vì trục Y cũ
            
            // Tự động thu cần câu nếu người chơi di chuyển khi phao đang dưới nước
            if (isBobberActive && (moveX != 0f || moveZ != 0f))
            {
                DestroyBobber();
                isBobberActive = false;
            }
        }

        // Tạo Vector di chuyển trên mặt phẳng phẳng X và Z
        Vector3 moveDirection = new Vector3(moveX, 0f, moveZ).normalized;

        // Thực hiện di chuyển
        controller.Move(moveDirection * speed * Time.deltaTime);

        // Áp dụng trọng lực để nhân vật luôn bám sát mặt đất
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Giữ nhân vật chắc chắn trên đất
        }
        
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Cập nhật Animator parameters cho animation
        if (animator != null)
        {
            // Set input hiện tại
            animator.SetFloat("Horizontal", moveX);
            animator.SetFloat("Vertical", moveZ);

            // Tính tốc độ di chuyển (chỉ để check có đang di chuyển không)
            float currentSpeed = moveDirection.magnitude > 0 ? 1f : 0f;  // 1 = moving, 0 = idle
            animator.SetFloat("Speed", currentSpeed);

            // Ghi nhớ hướng cuối cùng nếu đang di chuyển
            if (moveX != 0f || moveZ != 0f)
            {
                lastHorizontal = moveX;
                lastVertical = moveZ;
            }

            // Set hướng cuối cùng cho animation idle
            animator.SetFloat("LastHorizontal", lastHorizontal);
            animator.SetFloat("LastVertical", lastVertical);
            
            // Set rod state
            animator.SetBool("HasRod", hasRod);
        }

        // Cập nhật vị trí dây câu khi phao câu đang hoạt động
        if (isBobberActive && currentBobber != null)
        {
            lineRenderer.enabled = true;
            Vector3 rodTipPos = GetRodTipPosition();
            lineRenderer.SetPosition(0, rodTipPos);
            lineRenderer.SetPosition(1, currentBobber.transform.position);
        }
        else
        {
            if (lineRenderer != null && lineRenderer.enabled)
            {
                lineRenderer.enabled = false;
            }
        }
    }

    private void SpawnBobber()
    {
        // Tiêu hủy phao cũ nếu có
        DestroyBobber();

        // Điểm bắt đầu ở đầu cần ảo
        Vector3 startPos = GetRodTipPosition();

        // Tính hướng quăng
        Vector3 facingDir = new Vector3(lastHorizontal, 0f, lastVertical).normalized;
        if (facingDir == Vector3.zero)
        {
            facingDir = new Vector3(0f, 0f, -1f); // mặc định hướng xuống
        }

        Vector3 targetPos = transform.position + facingDir * castDistance;
        targetPos.y = transform.position.y; // Đáp xuống mặt đất

        // Tạo GameObject phao câu
        currentBobber = new GameObject("FishingBobber");
        var sr = currentBobber.AddComponent<SpriteRenderer>();
        sr.sprite = bobberSprite;
        sr.sortingOrder = 10;
        currentBobber.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);

        // Thêm component điều khiển phao câu
        var bc = currentBobber.AddComponent<BobberController>();
        bc.Initialize(startPos, targetPos, 0.5f, castArcHeight);
    }

    private void DestroyBobber()
    {
        if (currentBobber != null)
        {
            Destroy(currentBobber);
            currentBobber = null;
        }
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private Vector3 GetRodTipPosition()
    {
        Transform anchor = null;
        if (lastVertical < -0.1f) // Hướng xuống
        {
            anchor = rodTipDown;
        }
        else if (lastVertical > 0.1f) // Hướng lên
        {
            anchor = rodTipUp;
        }
        else if (lastHorizontal < -0.1f) // Hướng trái
        {
            anchor = rodTipLeft;
        }
        else if (lastHorizontal > 0.1f) // Hướng phải
        {
            anchor = rodTipRight;
        }

        if (anchor != null)
        {
            return anchor.position;
        }
        
        // Dự phòng nếu không có anchor
        return transform.position + new Vector3(-0.05f, 0.7f, -0.2f);
    }

    // Sự kiện được gọi tự động từ Animation Event 'StartCast' của Animator
    // Giữ trống để tránh lỗi "no receiver" nhưng phao câu thực tế sẽ sinh ra 
    // sau khi kết thúc toàn bộ animation quăng cần.
    public void StartCast()
    {
    }

    void OnDestroy()
    {
        DestroyBobber();
    }
}