using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
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
    public GameObject muteButtonPlaying;
    public GameObject muteButtonMuted;
    public GameObject remoteButton;
    public GameObject playerButton;
    public TextMeshProUGUI stationNameText;
    public TextMeshProUGUI ipAddressText;
    public GameObject settingsPanel;
    public TMP_InputField playerIPAddressInputField;
    public TextMeshProUGUI settingsTestResultText;

    [Header("Station List")]
    public Transform stationListParent;
    public GameObject stationButtonPrefab;
    
    [Header("Radio Stations")]
    public List<RadioStation> radioStations = new List<RadioStation>();
    
    [Header("Components")]
    public FMODRadioStreamer radioStreamer;
    public ScreensaverController screensaverController;
    
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
        settingsPanel.SetActive(false);

        if (Settings.GetOperatingMode() == Settings.OperatingMode.Player)
        {
            remoteButton.SetActive(true);
            playerButton.SetActive(false);
            if (radioStations.Count > 0)
            {
                SelectStation(currentStationIndex);
            }
        } else
        {
            remoteButton.SetActive(false);
            playerButton.SetActive(true);
        }

        ipAddressText.text = Utils.GetLocalIPAddress();
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
        // Clear existing buttons
        foreach (Transform child in stationListParent)
        {
            Destroy(child.gameObject);
        }

        string currentStationName = Settings.GetCurrentStationName();

        // Create buttons for each station
        for (int i = 0; i < radioStations.Count; i++)
        {
            int stationIndex = i; // Capture for usage in SelectStation lambda
            GameObject buttonObj = Instantiate(stationButtonPrefab, stationListParent);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            UnityEngine.UI.Image buttonImage = buttonObj.GetComponent<UnityEngine.UI.Image>();
            
            // Try to load station image
            Sprite stationSprite = CreateStationSprite(radioStations[i]);
            if (stationSprite != null)
            {
                buttonImage.sprite = stationSprite;
                // Hide text when image is available
                buttonText.gameObject.SetActive(false);
            }
            else
            {
                // Image not found or no image path, use text
                buttonText.text = radioStations[i].name;
                buttonText.gameObject.SetActive(true);
            }
            
            button.onClick.AddListener(() => SelectStation(stationIndex));

            if (radioStations[i].name == currentStationName)
            {
                currentStationIndex = i;
            }
        }
        
        // Force content to be wide enough for horizontal scrolling
        EnsureContentWidthForScrolling();
    }
    
    void EnsureContentWidthForScrolling()
    {
        RectTransform contentRect = stationListParent.GetComponent<RectTransform>();
        GridLayoutGroup gridLayout = contentRect.GetComponent<GridLayoutGroup>();
        
        // Let the Grid Layout Group calculate the natural size first
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        
        // Calculate required width based on grid layout with 2 rows
        int stationCount = radioStations.Count;
        float cellWidth = gridLayout.cellSize.x;
        float spacing = gridLayout.spacing.x;
        
        // Calculate number of columns needed (3 rows, so stations per row)
        int columns = Mathf.CeilToInt(stationCount / 3f);
        
        // Calculate total width: number of columns * cell width + spacing between columns
        float totalWidth = (columns * cellWidth) + ((columns - 1) * spacing);
        
        // Add some extra padding for better scrolling experience
        float extraPadding = 100f;
        float finalWidth = totalWidth + extraPadding;
        
        // For anchored content, we need to set the sizeDelta to the difference
        // between desired size and current anchored size
        float currentAnchoredWidth = contentRect.rect.width;
        float widthDifference = finalWidth - currentAnchoredWidth;
        
        // Set the content width to ensure scrolling is enabled
        contentRect.sizeDelta = new Vector2(widthDifference, contentRect.sizeDelta.y);
    }
    
    void SelectStation(int stationIndex)
    {
        if (stationIndex < 0 || stationIndex >= radioStations.Count) return;

        currentStationIndex = stationIndex;
        radioStreamer.PlayStream(radioStations[currentStationIndex].url);
        Settings.SetCurrentStationName(radioStations[currentStationIndex].name);
        
        // Update screensaver with current station image
        UpdateScreensaverStationImage();
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
        UpdateMuteButton(radioStreamer.IsMuted());
    }
    
    void UpdateMuteButton(bool isMuted)
    {
        muteButtonMuted.SetActive(isMuted);
        muteButtonPlaying.SetActive(!isMuted);
    }
    
    void Update()
    {
        if (stationNameText != null && radioStations.Count > 0)
        {
            stationNameText.text = radioStations[currentStationIndex].name;
        }
        
        DetectUITouch();
    }
    
    void DetectUITouch()
    {
        
        // Check for touch input (Android) using new Input System
        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            var touch = Touchscreen.current.touches[0];
            if (touch.press.wasPressedThisFrame)
            {
                screensaverController.ResetTimer();
            }
        }
        
        // Check for mouse input (editor/desktop testing) using new Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screensaverController.ResetTimer();
        }
    }
    
    Sprite CreateStationSprite(RadioStation station)
    {
        if (string.IsNullOrEmpty(station.image))
        {
            return null;
        }
        
        // Remove leading slash if present for Resources.Load
        string imagePath = station.image.StartsWith("/") ? 
            station.image.Substring(1) : station.image;
        
        // Remove .png extension for Resources.Load
        if (imagePath.EndsWith(".png"))
        {
            imagePath = imagePath.Substring(0, imagePath.Length - 4);
        }
        
        Texture2D stationTexture = Resources.Load<Texture2D>(imagePath);
        if (stationTexture != null)
        {
            // Convert texture to sprite
            return Sprite.Create(stationTexture, 
                new Rect(0, 0, stationTexture.width, stationTexture.height), 
                new Vector2(0.5f, 0.5f));
        }
        
        return null;
    }
    
    void UpdateScreensaverStationImage()
    {
        Sprite stationSprite = CreateStationSprite(radioStations[currentStationIndex]);
        screensaverController.SetStationImage(stationSprite);
    }

    public void OnRemote()
    {
        // No ip-address yet configured? and  can we connect to the player
        if (Settings.GetPlayerIPAddress().Length == 0 || !TestConnectionToPlayer())
        {
            OnSettings();
        }

        // switching from being the Remote to being the Player
        if (TestConnectionToPlayer())
        {
            Settings.SetOperatingMode(Settings.OperatingMode.Remote);
            remoteButton.SetActive(false);
            playerButton.SetActive(true);
        }
    }

    public void OnPlayer()
    {
        // switching from being the Remote to being the Player
        Settings.SetOperatingMode(Settings.OperatingMode.Player);
        remoteButton.SetActive(true);
        playerButton.SetActive(false);
        if (radioStations.Count > 0)
        {
            SelectStation(currentStationIndex);
        }
    }

    public void OnSettings()
    {
        playerIPAddressInputField.text = Settings.GetPlayerIPAddress();
        settingsTestResultText.text = "";
        settingsPanel.SetActive(true);
    }

    public void OnSettingsTest()
    {
        if (TestConnectionToPlayer())
        {
            settingsTestResultText.text = "OK";
            Settings.SetPlayerIPAddress(playerIPAddressInputField.text);
        }
        else
        {
            settingsTestResultText.text = "Error";
        }
    }

    bool TestConnectionToPlayer()
    {
        return true;
    }

    public void OnSettingsBack()
    {
        if (TestConnectionToPlayer())
        {
            Settings.SetPlayerIPAddress(playerIPAddressInputField.text);
        }
        settingsPanel.SetActive(false);
    }

    public void OnExit()
    {
        Debug.Log("Quitting");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(); 
#endif
    }

}
