using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class SettingsData
{
    public RadioStation[] station;
}

public class MainSceneController : MonoBehaviour
{
    [Header("UI References")]
    public Button muteButton;
    public TextMeshProUGUI muteButtonText;
    public TextMeshProUGUI stationNameText;
    public TextMeshProUGUI statusText;
    
    [Header("Station List")]
    public Transform stationListParent;
    public GameObject stationButtonPrefab;
    
    [Header("Radio Stations")]
    public List<RadioStation> radioStations = new List<RadioStation>();
    
    [Header("Components")]
    public FMODRadioStreamer radioStreamer;
    
    private int currentStationIndex = 0;
    
    void Awake()
    {
        // Prevent device from sleeping while app is running
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        
        SetupStations();
        SetupMuteButton();
    }
    
    void Start()
    {
        if (radioStreamer == null)
        {
            Debug.LogError("FMODRadioStreamer not assigned! Please assign it in the Inspector.");
            return;
        }
        
        CreateStationButtons();
        
        // Start playing the first station automatically
        if (radioStations.Count > 0)
        {
            SelectStation(0);
        }
    }
    
    void SetupStations()
    {
        // Load stations from JSON file
        TextAsset jsonFile = Resources.Load<TextAsset>("settings");
        if (jsonFile != null)
        {
            try
            {
                // Parse the JSON data
                var settingsData = JsonUtility.FromJson<SettingsData>(jsonFile.text);
                
                // Clear existing stations
                radioStations.Clear();
                
                // Add stations from JSON directly
                foreach (var station in settingsData.station)
                {
                    radioStations.Add(station);
                }
                
                Debug.Log($"Loaded {radioStations.Count} stations from settings.json");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse settings.json: {e.Message}");
                SetupFallbackStations();
            }
        }
        else
        {
            Debug.LogWarning("settings.json not found in Resources folder, using fallback stations");
            SetupFallbackStations();
        }
    }
    
    void SetupFallbackStations()
    {
        // Fallback stations if JSON loading fails
        radioStations.Add(new RadioStation
        {
            name = "ABC Triple J NSW",
            url = "https://live-radio01.mediahubaustralia.com/2TJW/mp3/"
        });
        
        radioStations.Add(new RadioStation
        {
            name = "Q-Music",
            url = "https://stream.qmusic.nl/qmusic/mp3"
        });
        
        radioStations.Add(new RadioStation
        {
            name = "Radio 538",
            url = "https://playerservices.streamtheworld.com/api/livestream-redirect/RADIO538.mp3"
        });
    }
    
    void CreateStationButtons()
    {
        if (stationListParent == null || stationButtonPrefab == null) return;
        
        // Clear existing buttons
        foreach (Transform child in stationListParent)
        {
            Destroy(child.gameObject);
        }
        
        // Create buttons for each station
        for (int i = 0; i < radioStations.Count; i++)
        {
            int stationIndex = i; // Capture for closure
            GameObject buttonObj = Instantiate(stationButtonPrefab, stationListParent);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            UnityEngine.UI.Image buttonImage = buttonObj.GetComponent<UnityEngine.UI.Image>();
            
            // Try to load station image
            if (!string.IsNullOrEmpty(radioStations[i].image))
            {
                // Remove leading slash if present for Resources.Load
                string imagePath = radioStations[i].image.StartsWith("/") ? 
                    radioStations[i].image.Substring(1) : radioStations[i].image;
                
                // Remove .png extension for Resources.Load
                if (imagePath.EndsWith(".png"))
                {
                    imagePath = imagePath.Substring(0, imagePath.Length - 4);
                }
                
                Texture2D stationTexture = Resources.Load<Texture2D>(imagePath);
                if (stationTexture != null && buttonImage != null)
                {
                    // Convert texture to sprite
                    Sprite stationSprite = Sprite.Create(stationTexture, 
                        new Rect(0, 0, stationTexture.width, stationTexture.height), 
                        new Vector2(0.5f, 0.5f));
                    buttonImage.sprite = stationSprite;
                    
                    // Hide text when image is available
                    if (buttonText != null)
                    {
                        buttonText.gameObject.SetActive(false);
                    }
                }
                else
                {
                    // Image not found, fall back to text
                    if (buttonText != null)
                    {
                        buttonText.text = radioStations[i].name;
                        buttonText.gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                // No image path, use text
                if (buttonText != null)
                {
                    buttonText.text = radioStations[i].name;
                    buttonText.gameObject.SetActive(true);
                }
            }
            
            if (button != null)
            {
                button.onClick.AddListener(() => SelectStation(stationIndex));
            }
        }
        
        // Force content to be wide enough for horizontal scrolling
        EnsureContentWidthForScrolling();
    }
    
    void EnsureContentWidthForScrolling()
    {
        RectTransform contentRect = stationListParent.GetComponent<RectTransform>();
        
        // Let the Grid Layout Group calculate the natural size first
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        
        // Get the current width and add extra space for scrolling
        float currentWidth = contentRect.sizeDelta.x;
        float extraWidth = 400f; // Add extra width to ensure scrolling
        
        // Set the content width to ensure scrolling is enabled
        contentRect.sizeDelta = new Vector2(currentWidth + extraWidth, contentRect.sizeDelta.y);
    }
    
    void SelectStation(int stationIndex)
    {
        if (stationIndex < 0 || stationIndex >= radioStations.Count) return;

        currentStationIndex = stationIndex;
        radioStreamer.SetStreamUrl(radioStations[currentStationIndex].url);
        radioStreamer.PlayStream();
    }

    public void ToggleMute()
    {
        radioStreamer.ToggleMuteStream();
    }
    
    void SetupMuteButton()
    {
        // Subscribe to mute state changes
        radioStreamer.OnMuteStateChanged += UpdateMuteButton;
        
        // Set initial button text
        UpdateMuteButton(radioStreamer.isMuted);
    }
    
    void UpdateMuteButton(bool isMuted)
    {
        if (muteButtonText != null)
        {
            muteButtonText.text = isMuted ? "Unmute" : "Mute";
        }
    }
    
    void Update()
    {
        // Update UI elements in real-time
        if (radioStreamer != null)
        {
            if (stationNameText != null && radioStations.Count > 0)
            {
                stationNameText.text = radioStations[currentStationIndex].name;
            }
        }
    }
}
