using UnityEngine;
using System.IO;
using System.Collections.Generic;

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
    
    public enum FishingState
    {
        Idle,
        Casting,
        WaitingForBite,
        BiteActive,
        Minigame,
        CatchSuccess,
        CatchFailure
    }
    [Header("Fishing State Machine")]
    public FishingState fishingState = FishingState.Idle;
    
    private float castTimer = 0f;       // Bộ đếm thời gian quăng cần
    public float castDuration = 0.85f;  // Thời gian quăng cần (khớp với Animation Clip)

    [Header("Fishing Setup")]
    public Sprite bobberSprite;         // Sprite phao câu
    private LineRenderer lineRenderer;  // Component vẽ dây câu
    private GameObject currentBobber;   // Đối tượng phao câu hiện tại
    private BobberController currentBobberController;
    
    public float castDistance = 5.0f;   // Khoảng cách quăng cần câu (quăng xa hơn)
    public float castArcHeight = 1.2f;  // Chiều cao bay vòng cung của phao câu
    public float bobberYOffset = -0.3f; // Độ lệch Y khi phao nổi (để nổi sát mặt đất/nước hơn)
    private bool isBobberActive = false; // Trạng thái phao câu đang hoạt động ở dưới nước

    private float biteWaitTimer = 0f;
    private float biteReactionTimer = 0f;
    private float minigameProgress = 0f;
    private float minigameTimer = 0f;
    private float minigameFishPos = 0.5f;
    private float minigameFishTarget = 0.5f;
    private float minigameFishMoveTimer = 0f;
    private float minigameBarPos = 0.2f;
    private float minigameBarVelocity = 0f;
    public float minigameBarSize = 0.22f; // Height of catch bar in 0-1 range
    public float minigameDuration = 15f;  // Duration of the fishing minigame

    private FishingMinigameUI ui;

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

        if (FishingMinigameUI.Instance == null)
        {
            GameObject uiGo = new GameObject("FishingMinigameUI_Manager");
            uiGo.AddComponent<FishingMinigameUI>();
        }
        ui = FishingMinigameUI.Instance;
    }

    void Update()
    {
        // 1. Read input events
        bool fKeyPressed = false;
        bool castKeyPressed = false; // Left Click or Space
        float moveX = 0f;
        float moveZ = 0f;

#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (keyboard != null)
        {
            fKeyPressed = keyboard.fKey.wasPressedThisFrame;
            castKeyPressed = keyboard.spaceKey.wasPressedThisFrame;
            
            bool canMove = (fishingState == FishingState.Idle || fishingState == FishingState.WaitingForBite || fishingState == FishingState.BiteActive);
            if (canMove)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveX = -1f;
                else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX = 1f;

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveZ = -1f;
                else if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveZ = 1f;
            }
        }
        if (mouse != null)
        {
            castKeyPressed = castKeyPressed || mouse.leftButton.wasPressedThisFrame;
        }
#endif

        // Fallbacks
        fKeyPressed = fKeyPressed || Input.GetKeyDown(KeyCode.F);
        castKeyPressed = castKeyPressed || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);

        bool canMoveFallback = (fishingState == FishingState.Idle || fishingState == FishingState.WaitingForBite || fishingState == FishingState.BiteActive);
        if (canMoveFallback)
        {
            if (moveX == 0f) moveX = Input.GetAxisRaw("Horizontal");
            if (moveZ == 0f) moveZ = Input.GetAxisRaw("Vertical");
        }

        // 2. F Key Handling - Cancel or Toggle Rod
        if (fKeyPressed)
        {
            if (fishingState != FishingState.Idle)
            {
                // Cancel fishing
                DestroyBobber();
                isBobberActive = false;
                fishingState = FishingState.Idle;
                if (ui != null) ui.HideAll();
                PlayRodIdleAnimation();
            }
            else
            {
                // Toggle rod
                hasRod = !hasRod;
            }
        }

        // 3. State Machine Logic
        switch (fishingState)
        {
            case FishingState.Idle:
                // Start casting if holding rod and pressing cast key
                if (hasRod && castKeyPressed)
                {
                    fishingState = FishingState.Casting;
                    castTimer = castDuration;
                    if (animator != null)
                    {
                        animator.SetTrigger("Cast");
                    }
                }
                break;

            case FishingState.Casting:
                castTimer -= Time.deltaTime;
                if (castTimer <= 0f)
                {
                    // Calculate target position of bobber landing
                    Vector3 facingDir = new Vector3(lastHorizontal, 0f, lastVertical).normalized;
                    if (facingDir == Vector3.zero) facingDir = new Vector3(0f, 0f, -1f);
                    Vector3 targetPos = transform.position + facingDir * castDistance;
                    targetPos.y = transform.position.y + bobberYOffset;

                    // Verify if it lands in water
                    if (IsPositionInWater(targetPos))
                    {
                        isBobberActive = true;
                        SpawnBobber();
                        fishingState = FishingState.WaitingForBite;
                        biteWaitTimer = Random.Range(2.0f, 4.0f);
                    }
                    else
                    {
                        // Show warning and reset to Idle
                        if (ui != null)
                        {
                            ui.ShowMessage("MUST CAST INTO WATER!");
                        }
                        fishingState = FishingState.CatchFailure;
                        biteReactionTimer = 2.0f; // Show warning message for 2 seconds
                    }
                }
                break;

            case FishingState.WaitingForBite:
                // If player moves, cancel fishing
                if (moveX != 0f || moveZ != 0f)
                {
                    DestroyBobber();
                    isBobberActive = false;
                    fishingState = FishingState.Idle;
                    break;
                }

                // If player presses castKey, pull back empty
                if (castKeyPressed)
                {
                    DestroyBobber();
                    isBobberActive = false;
                    fishingState = FishingState.Idle;
                    break;
                }

                biteWaitTimer -= Time.deltaTime;
                if (biteWaitTimer <= 0f)
                {
                    // Fish bites! Start minigame immediately
                    if (ui != null)
                    {
                        ui.ShowMessage("🎣 BITE!");
                        ui.ShowMinigame();
                    }
                    
                    fishingState = FishingState.Minigame;
                    minigameProgress = 0.2f; // Give a small headstart
                    minigameTimer = minigameDuration;
                    minigameFishPos = 0.5f;
                    minigameFishTarget = 0.5f;
                    minigameFishMoveTimer = 0f;
                    minigameBarPos = 0.2f;
                    minigameBarVelocity = 0f;

                    // Make bobber bob vigorously
                    if (currentBobberController != null)
                    {
                        currentBobberController.bobSpeed = 20f;
                        currentBobberController.bobHeight = 0.2f;
                    }
                }
                break;

            case FishingState.BiteActive:
                // Deprecated: directly transitions from WaitingForBite to Minigame now
                fishingState = FishingState.Minigame;
                break;

            case FishingState.Minigame:
                // Minigame bar physics (controlled with holding mouse click)
                bool holdingLeftClick = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
                if (holdingLeftClick)
                {
                    minigameBarVelocity += 2.2f * Time.deltaTime;
                }
                else
                {
                    minigameBarVelocity -= 1.6f * Time.deltaTime;
                }
                
                minigameBarVelocity = Mathf.Clamp(minigameBarVelocity, -1.2f, 1.2f);
                minigameBarPos += minigameBarVelocity * Time.deltaTime;

                // Clamp to track boundaries
                float halfBar = minigameBarSize / 2f;
                if (minigameBarPos < halfBar)
                {
                    minigameBarPos = halfBar;
                    minigameBarVelocity = minigameBarVelocity * -0.25f; // damp bounce
                }
                else if (minigameBarPos > 1f - halfBar)
                {
                    minigameBarPos = 1f - halfBar;
                    minigameBarVelocity = minigameBarVelocity * -0.25f;
                }

                // Fish movement AI (wandering target) - Slower and smoother using Lerp
                minigameFishMoveTimer -= Time.deltaTime;
                if (minigameFishMoveTimer <= 0f)
                {
                    minigameFishTarget = Random.Range(0.15f, 0.85f);
                    minigameFishMoveTimer = Random.Range(1.2f, 2.2f); // Less frequent changes
                }
                minigameFishPos = Mathf.Lerp(minigameFishPos, minigameFishTarget, 2.0f * Time.deltaTime);

                // Progress calculation
                float diff = Mathf.Abs(minigameFishPos - minigameBarPos);
                if (diff <= halfBar)
                {
                    minigameProgress += 0.22f * Time.deltaTime;
                }
                else
                {
                    minigameProgress -= 0.12f * Time.deltaTime;
                }
                minigameProgress = Mathf.Clamp01(minigameProgress);

                // Timer countdown
                minigameTimer -= Time.deltaTime;

                // Update UI elements
                if (ui != null)
                {
                    ui.UpdateMinigame(minigameFishPos, minigameBarPos, minigameBarSize, minigameProgress, minigameTimer);
                }

                // Success condition
                if (minigameProgress >= 1f)
                {
                    if (ui != null) ui.HideMinigame();
                    
                    // Retrieve reward fish
                    string fishName;
                    Sprite fishSprite = GetRandomFish(out fishName);

                    if (ui != null)
                    {
                        ui.ShowTrophy(fishSprite, fishName);
                    }

                    // Play appropriate catch animation
                    PlayCatchAnimation();

                    DestroyBobber();
                    isBobberActive = false;

                    // Add caught fish to player inventory
                    var playerInventory = GetComponent<InventorySystem.Inventory>();
                    if (playerInventory == null)
                    {
                        playerInventory = FindAnyObjectByType<InventorySystem.Inventory>();
                    }
                    if (playerInventory != null)
                    {
                        var fishItem = GetOrCreateFishItemData(fishName, fishSprite);
                        bool added = playerInventory.AddItem(fishItem, 1);
                        if (added)
                        {
                            Debug.Log($"Added 1 {fishName} to Inventory.");
                        }
                        else
                        {
                            Debug.LogWarning($"Failed to add {fishName} to Inventory (inventory full).");
                        }
                    }

                    fishingState = FishingState.CatchSuccess;
                }
                // Failure condition
                else if (minigameTimer <= 0f)
                {
                    if (ui != null)
                    {
                        ui.HideMinigame();
                        ui.ShowMessage("FISH ESCAPED!");
                    }
                    DestroyBobber();
                    isBobberActive = false;
                    fishingState = FishingState.CatchFailure;
                    biteReactionTimer = 1.5f; // Reuse to show message
                }
                break;

            case FishingState.CatchSuccess:
                // Dismiss trophy popup only with Space
                bool dismissTrophy = Input.GetKeyDown(KeyCode.Space);
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    dismissTrophy = dismissTrophy || UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame;
                }
#endif
                if (dismissTrophy)
                {
                    if (ui != null) ui.HideTrophy();
                    fishingState = FishingState.Idle;
                    PlayRodIdleAnimation();
                }
                break;

            case FishingState.CatchFailure:
                // Show failure message for 1.5s
                biteReactionTimer -= Time.deltaTime;
                if (biteReactionTimer <= 0f || castKeyPressed)
                {
                    if (ui != null) ui.HideAll();
                    fishingState = FishingState.Idle;
                    PlayRodIdleAnimation();
                }
                break;
        }

        // 4. Character movement & gravity (only when state allows moving)
        Vector3 moveDirection = new Vector3(moveX, 0f, moveZ).normalized;

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMovement = moveDirection * speed;
        finalMovement.y = velocity.y;

        controller.Move(finalMovement * Time.deltaTime);

        // 5. Update Animator parameters
        if (animator != null)
        {
            // Resolve dominant animation direction for diagonal movement
            float animHorizontal = 0f;
            float animVertical = 0f;

            if (moveX != 0f || moveZ != 0f)
            {
                lastHorizontal = moveX;
                lastVertical = moveZ;

                if (Mathf.Abs(moveX) >= Mathf.Abs(moveZ))
                {
                    animHorizontal = moveX;
                }
                else
                {
                    animVertical = moveZ;
                }
            }

            animator.SetFloat("Horizontal", animHorizontal);
            animator.SetFloat("Vertical", animVertical);

            float currentSpeed = moveDirection.magnitude > 0 ? 1f : 0f;
            animator.SetFloat("Speed", currentSpeed);

            animator.SetFloat("LastHorizontal", lastHorizontal);
            animator.SetFloat("LastVertical", lastVertical);
            animator.SetBool("HasRod", hasRod);
        }

        // 6. Update line renderer positions
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
        UpdateEquippedRodVisual();
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
        targetPos.y = transform.position.y + bobberYOffset; // Đáp xuống mặt đất/nước có offset

        // Load custom bobber sprite
        var inv = GetComponent<InventorySystem.Inventory>();
        if (inv != null)
        {
            bobberSprite = GetBobberSprite(inv.equippedBobberId);
        }

        // Tạo GameObject phao câu
        currentBobber = new GameObject("FishingBobber");
        var sr = currentBobber.AddComponent<SpriteRenderer>();
        sr.sprite = bobberSprite;
        sr.sortingOrder = 10;
        currentBobber.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);

        // Thêm component điều khiển phao câu
        currentBobberController = currentBobber.AddComponent<BobberController>();
        currentBobberController.Initialize(startPos, targetPos, 0.5f, castArcHeight);
    }

    private void DestroyBobber()
    {
        if (currentBobber != null)
        {
            Destroy(currentBobber);
            currentBobber = null;
        }
        currentBobberController = null;
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

    private void PlayRodIdleAnimation()
    {
        if (animator == null) return;

        string clipName = "rod idle down";
        if (lastVertical > 0.1f) // Up
        {
            clipName = "rod idle up";
        }
        else if (lastVertical < -0.1f) // Down
        {
            clipName = "rod idle down";
        }
        else if (lastHorizontal < -0.1f) // Left
        {
            clipName = "rod idle left";
        }
        else if (lastHorizontal > 0.1f) // Right
        {
            clipName = "rod idle right";
        }

        // Fallback to normal idle if rod is not equipped
        if (!hasRod)
        {
            clipName = clipName.Replace("rod ", "");
        }

        animator.Play(clipName);
    }

    private void PlayCatchAnimation()
    {
        if (animator == null) return;

        string clipName = "catch down";
        if (lastVertical > 0.1f) // Up
        {
            clipName = "catch up";
        }
        else if (lastVertical < -0.1f) // Down
        {
            clipName = "catch down";
        }
        else if (lastHorizontal < -0.1f) // Left
        {
            clipName = "catch left";
        }
        else if (lastHorizontal > 0.1f) // Right
        {
            clipName = "catch right";
        }

        animator.Play(clipName);
    }

    private Sprite GetRandomFish(out string fishName)
    {
        fishName = "Unknown Fish";
        Sprite sprite = null;

        string folderPath = Path.Combine(Application.dataPath, "Model/Fishes");
        if (Directory.Exists(folderPath))
        {
            string[] files = Directory.GetFiles(folderPath, "fish_fishing-*.png");
            if (files.Length > 0)
            {
                string randomFilePath = files[Random.Range(0, files.Length)];
                
                // Get relative path for AssetDatabase
                string relativePath = "Assets" + randomFilePath.Substring(Application.dataPath.Length).Replace('\\', '/');
                
                // Format fish name from file name
                string filename = Path.GetFileNameWithoutExtension(randomFilePath);
                string rawName = filename.Replace("fish_fishing-", "");
                
                fishName = FormatFishName(rawName);

#if UNITY_EDITOR
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
#endif
            }
        }
        return sprite;
    }

    private string FormatFishName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return "";
        
        string[] words = rawName.Split(new char[] { '-', '_' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }
        }
        string formatted = string.Join(" ", words);

        // Simple hardcoded mapping for known long words to look extra premium:
        if (formatted.Equals("Bigmouthbass", System.StringComparison.OrdinalIgnoreCase)) return "Bigmouth Bass";
        if (formatted.Equals("Blackspottedeel", System.StringComparison.OrdinalIgnoreCase)) return "Black Spotted Eel";
        if (formatted.Equals("Brooktrout", System.StringComparison.OrdinalIgnoreCase)) return "Brook Trout";
        if (formatted.Equals("Brownray", System.StringComparison.OrdinalIgnoreCase)) return "Brown Ray";
        if (formatted.Equals("Kingsalmon", System.StringComparison.OrdinalIgnoreCase)) return "King Salmon";
        if (formatted.Equals("Longnosegar", System.StringComparison.OrdinalIgnoreCase)) return "Longnose Gar";
        if (formatted.Equals("Northernpike", System.StringComparison.OrdinalIgnoreCase)) return "Northern Pike";
        if (formatted.Equals("Pinksalmon", System.StringComparison.OrdinalIgnoreCase)) return "Pink Salmon";
        if (formatted.Equals("Pufferfish", System.StringComparison.OrdinalIgnoreCase)) return "Puffer Fish";
        if (formatted.Equals("Rainbowtrout", System.StringComparison.OrdinalIgnoreCase)) return "Rainbow Trout";
        if (formatted.Equals("Redlionfish", System.StringComparison.OrdinalIgnoreCase)) return "Red Lionfish";
        if (formatted.Equals("Redporgy", System.StringComparison.OrdinalIgnoreCase)) return "Red Porgy";
        if (formatted.Equals("Redsnapper", System.StringComparison.OrdinalIgnoreCase)) return "Red Snapper";
        if (formatted.Equals("Sandbarshark", System.StringComparison.OrdinalIgnoreCase)) return "Sandbar Shark";
        if (formatted.Equals("Sharptoothcatfish", System.StringComparison.OrdinalIgnoreCase)) return "Sharptooth Catfish";
        if (formatted.Equals("Sockeyesalmon", System.StringComparison.OrdinalIgnoreCase)) return "Sockeye Salmon";
        if (formatted.Equals("Spadefish", System.StringComparison.OrdinalIgnoreCase)) return "Spade Fish";
        if (formatted.Equals("Spotcroacker", System.StringComparison.OrdinalIgnoreCase)) return "Spot Croacker";
        if (formatted.Equals("Yellowperch", System.StringComparison.OrdinalIgnoreCase)) return "Yellow Perch";

        return formatted;
    }

    private bool IsPositionInWater(Vector3 position)
    {
        // 1. Physics Raycast check downwards (highly precise, must hit Lake_Water specifically)
        RaycastHit hit;
        Vector3 rayStart = new Vector3(position.x, position.y + 10f, position.z);
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 20f))
        {
            if (hit.collider.gameObject.name == "Lake_Water")
            {
                return true;
            }
        }

        // 2. Coordinates check fallback: inside lake bounds AND outside island bounds
        bool insideLakeBounds = (position.x >= -40f && position.x <= 40f && position.z >= 30f && position.z <= 90f);
        bool insideIslandBounds = (position.x >= -6f && position.x <= 6f && position.z >= 54f && position.z <= 66f);
        if (insideLakeBounds && !insideIslandBounds)
        {
            return true;
        }

        return false;
    }

    private Dictionary<string, InventorySystem.ItemData> fishItemCache = new Dictionary<string, InventorySystem.ItemData>();

    private InventorySystem.ItemData GetOrCreateFishItemData(string fishName, Sprite fishSprite)
    {
        if (fishItemCache.TryGetValue(fishName, out var cachedItem))
        {
            return cachedItem;
        }

        InventorySystem.ItemData fishItem = ScriptableObject.CreateInstance<InventorySystem.ItemData>();
        fishItem.id = "fish_" + fishName.Replace(" ", "_").ToLower();
        fishItem.itemName = fishName;
        fishItem.description = $"A fresh caught {fishName}. Can be used for cooking or trading.";
        fishItem.type = InventorySystem.ItemType.Material;
        
        // Determine rarity & price
        if (fishName.Contains("Shark") || fishName.Contains("Ray") || fishName.Contains("Dinosaur"))
        {
            fishItem.rarity = InventorySystem.Rarity.Legendary;
            fishItem.sellPrice = 500;
        }
        else if (fishName.Contains("Salmon") || fishName.Contains("Trout") || fishName.Contains("Eel") || fishName.Contains("Pike"))
        {
            fishItem.rarity = InventorySystem.Rarity.Epic;
            fishItem.sellPrice = 150;
        }
        else if (fishName.Contains("Bass") || fishName.Contains("Gar") || fishName.Contains("Porgy") || fishName.Contains("Snapper") || fishName.Contains("Perch"))
        {
            fishItem.rarity = InventorySystem.Rarity.Rare;
            fishItem.sellPrice = 50;
        }
        else
        {
            fishItem.rarity = InventorySystem.Rarity.Common;
            fishItem.sellPrice = 15;
        }

        fishItem.maxStackSize = 20; // 20 fish max per slot!
        fishItem.icon = fishSprite;

        fishItemCache[fishName] = fishItem;
        return fishItem;
    }

    [Header("Equipped Rod Visual")]
    private GameObject equippedRodVisualGo;
    private SpriteRenderer equippedRodVisualSr;

    private void UpdateEquippedRodVisual()
    {
        var inv = GetComponent<InventorySystem.Inventory>();
        if (inv == null) return;

        bool shouldShow = false; // Vô hiệu hóa hình ảnh cần câu động do đã vẽ sẵn trong animation sheet

        if (shouldShow)
        {
            if (equippedRodVisualGo == null)
            {
                equippedRodVisualGo = new GameObject("EquippedRodVisual");
                equippedRodVisualSr = equippedRodVisualGo.AddComponent<SpriteRenderer>();
                equippedRodVisualSr.sortingOrder = 11; // Overlay on top of player character
            }

            Sprite rodSprite = GetRodSprite(inv.equippedRodId);
            if (equippedRodVisualSr.sprite != rodSprite)
            {
                equippedRodVisualSr.sprite = rodSprite;
            }

            equippedRodVisualGo.SetActive(true);

            // Compute math to stretch sprite from player's hand to active rod tip in world space
            Vector3 handPos = transform.position + new Vector3(0f, 0.8f, -0.05f);
            Vector3 tipPos = GetRodTipPosition();

            Vector3 direction = tipPos - handPos;
            float distance = direction.magnitude;

            // Position at the midpoint so standard center pivot works perfectly
            Vector3 midpoint = (handPos + tipPos) / 2f;
            equippedRodVisualGo.transform.position = midpoint;

            // Rotate to point from hand to tip (diagonal 45 deg sprites)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            equippedRodVisualGo.transform.rotation = Quaternion.Euler(0f, 0f, angle - 45f);

            // Stretch scale. 32x32 sprite at 100 PPu has diagonal length approx 0.45 units
            float spriteLength = 0.45f;
            float scaleVal = distance / spriteLength;
            equippedRodVisualGo.transform.localScale = new Vector3(scaleVal, scaleVal, 1f);
        }
        else
        {
            if (equippedRodVisualGo != null && equippedRodVisualGo.activeSelf)
            {
                equippedRodVisualGo.SetActive(false);
            }
        }
    }

    private Sprite GetRodSprite(string rodId)
    {
#if UNITY_EDITOR
        string path = "Assets/Model/Fishes/fishing_icons_32x32_6.png";
        switch (rodId)
        {
            case "fishing_rod_bamboo": path = "Assets/Model/Fishes/fishing_icons_32x32_6.png"; break;
            case "fishing_rod_fiberglass": path = "Assets/Model/Fishes/fishing_icons_32x32_7.png"; break;
            case "fishing_rod_carbon": path = "Assets/Model/Fishes/fishing_icons_32x32_8.png"; break;
            case "fishing_rod_master": path = "Assets/Model/Fishes/fishing_icons_32x32_18.png"; break;
            case "fishing_rod_golden": path = "Assets/Model/Fishes/fishing_icons_32x32_19.png"; break;
            case "fishing_rod_lava": path = "Assets/Model/Fishes/fishing_icons_32x32_20.png"; break;
        }
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
        return null;
#endif
    }

    private Sprite GetBobberSprite(string bobberId)
    {
#if UNITY_EDITOR
        string path = "Assets/Model/bobber-removebg-preview.png";
        switch (bobberId)
        {
            case "fish_bobber_standard": path = "Assets/Model/bobber-removebg-preview.png"; break;
            case "fish_bobber_bluecork": path = "Assets/Model/Fishes/fish_bobber-bluecork.png"; break;
            case "fish_bobber_clover": path = "Assets/Model/Fishes/fish_bobber-clover.png"; break;
            case "fish_bobber_donut": path = "Assets/Model/Fishes/fish_bobber-donut.png"; break;
            case "fish_bobber_rainbow": path = "Assets/Model/Fishes/fish_bobber-rainbow.png"; break;
            case "fish_bobber_crystal": path = "Assets/Model/Fishes/fish_bobber-crystal.png"; break;
        }
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
        return null;
#endif
    }

    void OnDestroy()
    {
        DestroyBobber();
        if (equippedRodVisualGo != null)
        {
            Destroy(equippedRodVisualGo);
        }
    }
}