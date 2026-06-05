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