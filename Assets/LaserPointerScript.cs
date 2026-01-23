using UnityEngine;
using UnityEngine.UI;

public class LaserPointerScript : MonoBehaviour
{
    [Tooltip("How fast the pointer fades out (alpha per second)")]
    public float fadeSpeed = 1f;  // Adjust in Inspector as needed
    public bool debugMode = false;
    private Image pointerImage;
    private float currentAlpha = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        // Get the Image component attached to this GameObject
        pointerImage = GetComponent<Image>();

        // Ensure the pointer starts fully transparent
        SetAlpha(0f);
        MoveToPosition(Vector3.zero);
    }

    // Update is called once per frame
    void Update()
    {
        
        // Gradually decrease the alpha if the pointer is visible
        if (currentAlpha > 0f)
        {
            currentAlpha -= fadeSpeed * Time.deltaTime;
            currentAlpha = Mathf.Clamp01(currentAlpha);
            SetAlpha(currentAlpha);
        }
    }

    /// <summary>
    /// Call this method when an event occurs.
    /// It moves the pointer to a new screen position and restores full opacity.
    /// </summary>
    /// <param name="newScreenPosition">The new position in screen coordinates.</param>
    public void MoveToPosition(Vector3 newScreenPosition)
    {
        transform.position = newScreenPosition;
        if (debugMode)
        {
            currentAlpha = 1f;
            SetAlpha(currentAlpha);
        }
        
    }

    /// <summary>
    /// Updates the alpha value of the Image component.
    /// </summary>
    /// <param name="alpha">The new alpha value (0 to 1).</param>
    private void SetAlpha(float alpha)
    {
        Color color = pointerImage.color;
        color.a = alpha;
        pointerImage.color = color;
            
    }
}
