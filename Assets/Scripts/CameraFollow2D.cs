using UnityEngine;

/// <summary>
/// Camera controller cho game 2.5D - chỉ follow theo trục X
/// Attach script này vào Main Camera
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Drag Player object vào đây")]
    public Transform target;
    
    [Header("Follow Settings")]
    [Tooltip("Tốc độ camera bám theo player")]
    public float smoothSpeed = 5f;
    
    [Tooltip("Offset từ player (X: trái/phải, Y: cao, Z: khoảng cách)")]
    public Vector3 offset = new Vector3(0, 2, -10);
    
    [Header("Boundaries (Optional)")]
    [Tooltip("Giới hạn camera theo X và Y")]
    public bool useBoundaries = false;
    public float minX = -10f;
    public float maxX = 50f;
    public float minY = -5f;
    public float maxY = 10f;

    [Header("Zoom Settings")]
    [Tooltip("Cho phép phóng to/thu nhỏ bằng con lăn chuột")]
    public bool enableZoom = true;
    [Tooltip("Tốc độ zoom")]
    public float zoomSpeed = 5f;
    [Tooltip("Khoảng cách/size zoom nhỏ nhất (phóng to nhất)")]
    public float minZoom = 10f;
    [Tooltip("Khoảng cách/size zoom lớn nhất (thu nhỏ nhất)")]
    public float maxZoom = 25f;

    private Camera cam;
    private Vector3 offsetDirection;
    private float currentZoomDistance;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        // Lưu hướng vector và khoảng cách offset ban đầu từ inspector
        offsetDirection = offset.normalized;
        currentZoomDistance = Mathf.Max(offset.magnitude, minZoom);
        offset = offsetDirection * currentZoomDistance;
        
        if (cam != null)
        {
            if (cam.orthographic)
            {
                maxZoom = Mathf.Max(maxZoom, cam.orthographicSize * 2.5f);
            }
            else
            {
                maxZoom = Mathf.Max(maxZoom, currentZoomDistance * 2.5f);
            }
        }
    }

    void Update()
    {
        if (enableZoom && cam != null)
        {
            // Do not zoom camera if pointer is over a UI element (like the shop scroll view)
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scrollInput) > 0.01f)
            {
                if (cam.orthographic)
                {
                    cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scrollInput * zoomSpeed * 2f, minZoom, maxZoom);
                }
                else
                {
                    // Thu phóng bằng cách tiến/lùi dọc theo vector hướng nhìn của camera đến player
                    currentZoomDistance = Mathf.Clamp(currentZoomDistance - scrollInput * zoomSpeed * 2f, minZoom, maxZoom);
                    offset = offsetDirection * currentZoomDistance;
                }
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        
        // Tính vị trí mong muốn - Follow X, Y và Z
        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x,  // Follow theo X
            target.position.y + offset.y,  // Follow theo Y
            target.position.z + offset.z   // Follow theo Z (lên/xuống trong 2.5D)
        );
        
        // Apply boundaries nếu có
        if (useBoundaries)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }
        
        // Smooth movement
        transform.position = Vector3.Lerp(
            transform.position, 
            desiredPosition, 
            smoothSpeed * Time.deltaTime
        );
    }
    
    // Visualize offset trong Scene view
    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(target.position + offset, 0.5f);
            Gizmos.DrawLine(target.position, target.position + offset);
        }
    }
}