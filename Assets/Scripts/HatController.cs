using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HatController : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite hatUp;
    public Sprite hatDown;
    public Sprite hatLeft;
    public Sprite hatRight;

    [Header("Offsets")]
    public Vector3 offsetUp = new Vector3(0, 0.8f, -0.05f);
    public Vector3 offsetDown = new Vector3(0, 0.8f, -0.05f);
    public Vector3 offsetLeft = new Vector3(-0.05f, 0.8f, -0.05f);
    public Vector3 offsetRight = new Vector3(0.05f, 0.8f, -0.05f);

    [Header("Sorting Settings")]
    public int sortingOrderOffset = 1;

    private SpriteRenderer hatSR;
    private SpriteRenderer parentSR;
    private Animator parentAnimator;

    void Start()
    {
        hatSR = GetComponent<SpriteRenderer>();
        parentSR = transform.parent != null ? transform.parent.GetComponent<SpriteRenderer>() : null;
        parentAnimator = transform.parent != null ? transform.parent.GetComponent<Animator>() : null;
    }

    void LateUpdate()
    {
        if (parentAnimator == null || hatSR == null) return;

        // Sync sorting order
        if (parentSR != null)
        {
            hatSR.sortingLayerID = parentSR.sortingLayerID;
            hatSR.sortingLayerName = parentSR.sortingLayerName;
            hatSR.sortingOrder = parentSR.sortingOrder + sortingOrderOffset;
        }

        // Get direction from Animator
        float lastHorizontal = parentAnimator.GetFloat("LastHorizontal");
        float lastVertical = parentAnimator.GetFloat("LastVertical");

        // Determine direction and apply sprite and offset
        if (lastVertical > 0.1f) // Up
        {
            hatSR.sprite = hatUp;
            transform.localPosition = offsetUp;
            hatSR.flipX = false;
        }
        else if (lastVertical < -0.1f) // Down
        {
            hatSR.sprite = hatDown;
            transform.localPosition = offsetDown;
            hatSR.flipX = false;
        }
        else if (lastHorizontal < -0.1f) // Left
        {
            hatSR.sprite = hatLeft;
            transform.localPosition = offsetLeft;
            hatSR.flipX = (hatLeft == hatRight && hatLeft != null);
        }
        else if (lastHorizontal > 0.1f) // Right
        {
            hatSR.sprite = hatRight;
            transform.localPosition = offsetRight;
            hatSR.flipX = false;
        }
        else
        {
            // Default to down if no input history is found
            hatSR.sprite = hatDown;
            transform.localPosition = offsetDown;
            hatSR.flipX = false;
        }
    }
}
