using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class NPCController : MonoBehaviour
{
    [Header("Animation Setup")]
    public List<Sprite> idleSprites = new List<Sprite>();
    public float animationRate = 0.5f;

    private SpriteRenderer spriteRenderer;
    private int currentFrameIndex = 0;
    private float timer = 0f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (idleSprites != null && idleSprites.Count > 0)
        {
            spriteRenderer.sprite = idleSprites[0];
        }
    }

    void Update()
    {
        if (idleSprites == null || idleSprites.Count <= 1)
            return;

        timer += Time.deltaTime;
        if (timer >= animationRate)
        {
            timer -= animationRate;
            currentFrameIndex = (currentFrameIndex + 1) % idleSprites.Count;
            spriteRenderer.sprite = idleSprites[currentFrameIndex];
        }
    }
}
