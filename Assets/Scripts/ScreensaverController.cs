using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class ScreensaverController : MonoBehaviour
{

    public Button screensaverButton; // Full-screen button for touch detection
    public Image stationImage; // Bouncing station image (child of button)

    private float inactivityTimeout = 10f; // seconds before screensaver activates
    private float bounceSpeed = 200f;

    private bool isScreensaverActive = false;
    private float lastInputTime;
    private Vector2 currentVelocity;
    private RectTransform stationImageRect;
    private RectTransform screensaverButtonRect;
    private Vector2 screenBounds;
    
    void Start()
    {
        lastInputTime = Time.time;
        screensaverButtonRect = screensaverButton.GetComponent<RectTransform>();
        screensaverButton.gameObject.SetActive(false); // Start hidden
        stationImageRect = stationImage.GetComponent<RectTransform>();
        screenBounds = new Vector2(
            screensaverButtonRect.rect.width * 0.5f - stationImageRect.rect.width * 0.5f,
            screensaverButtonRect.rect.height * 0.5f - stationImageRect.rect.height * 0.5f
        );
    }

    void Update()
    {
        // Check if screensaver should activate
        if (!isScreensaverActive && Time.time - lastInputTime > inactivityTimeout)
        {
            EnterScreensaver();
        }
        
        // Update bouncing animation
        if (isScreensaverActive)
        {
            UpdateBouncingAnimation();
        }
    }
    
    public void OnScreensaverTouched()
    {
        lastInputTime = Time.time;
        isScreensaverActive = false;
        screensaverButton.gameObject.SetActive(false);
    }
    
    public void ResetTimer()
    {
        lastInputTime = Time.time;
        Debug.Log("Screensaver timer reset");
    }

    void EnterScreensaver()
    {
        isScreensaverActive = true;
        screensaverButton.gameObject.SetActive(true);
        // Set random initial position and velocity
        SetRandomPositionAndVelocity();
        
    }
    
    void SetRandomPositionAndVelocity()
    {
        // Random position within bounds
        Vector2 randomPos = new Vector2(
            Random.Range(-screenBounds.x, screenBounds.x),
            Random.Range(-screenBounds.y, screenBounds.y)
        );
        stationImageRect.anchoredPosition = randomPos;
        
        // Random velocity
        currentVelocity = new Vector2(
            Random.Range(-bounceSpeed, bounceSpeed),
            Random.Range(-bounceSpeed, bounceSpeed)
        );
    }
    
    void UpdateBouncingAnimation()
    {
        if (stationImageRect == null) return;
        
        // Update position
        Vector2 newPosition = stationImageRect.anchoredPosition + currentVelocity * Time.deltaTime;
        
        // Bounce off edges - simply reverse direction, keep same speed
        if (newPosition.x > screenBounds.x || newPosition.x < -screenBounds.x)
        {
            currentVelocity.x = -currentVelocity.x;
            newPosition.x = Mathf.Clamp(newPosition.x, -screenBounds.x, screenBounds.x);
        }
        
        if (newPosition.y > screenBounds.y || newPosition.y < -screenBounds.y)
        {
            currentVelocity.y = -currentVelocity.y;
            newPosition.y = Mathf.Clamp(newPosition.y, -screenBounds.y, screenBounds.y);
        }
        
        // Apply new position
        stationImageRect.anchoredPosition = newPosition;
    }
    
    public void SetStationImage(Sprite stationSprite)
    {
        if (stationSprite != null)
        {
            stationImage.sprite = stationSprite;
        }
        else
        {
            // TODO create default image
            Debug.Log("Screensaver: No station image available, hiding image");
        }
    }
    
}
