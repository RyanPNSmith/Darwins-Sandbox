using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreatureUICanvas : MonoBehaviour
{
    [Header("UI References")]
    public Slider hungerSlider;
    public Slider loveSlider;
    public TextMeshProUGUI hungerText;
    public TextMeshProUGUI loveText;
    public TextMeshProUGUI stateText;

    // Reference to the wolf's Creature component
    private Creature creature;
    
    // UI elements that will be created dynamically
    private Canvas uiCanvas;
    private Slider hungerBar;
    private Slider loveBar;
    
    // UI settings
    public float displayDistance = 5f;
    public Vector3 offset = new Vector3(0, 1.5f, 0);
    public float width = 120f;
    public float height = 150f;
    
    // Control visibility
    private bool isVisible = false;
    private Camera mainCamera;
    private Color orangeColor = new Color(1f, 0.5f, 0f, 1f); // Custom orange color

    void Start()
    {
        // Get components
        creature = GetComponent<Creature>();
        mainCamera = Camera.main;
        
        // Create UI elements
        CreateUIElements();
        
        // Initially hide UI
        SetUIVisibility(false);

        if (hungerSlider != null)
        {
            hungerSlider.minValue = 0f;
            hungerSlider.maxValue = 100f;
        }

        if (loveSlider != null)
        {
            loveSlider.minValue = 0f;
            loveSlider.maxValue = 100f;
        }
    }
    
    void Update()
    {
        // Check distance to camera
        float distanceToCamera = Vector3.Distance(transform.position, mainCamera.transform.position);
        
        // Show UI when close enough and wolf is alive
        bool shouldBeVisible = distanceToCamera < displayDistance && !creature.isDead;
        
        // Only update visibility if it changed
        if (shouldBeVisible != isVisible)
        {
            SetUIVisibility(shouldBeVisible);
        }
        
        // Update UI position to follow wolf
        if (isVisible)
        {
            UpdateUIPosition();
            UpdateUIValues();
        }

        if (creature == null || mainCamera == null) return;

        // Update UI values
        if (hungerSlider != null)
        {
            hungerSlider.value = creature.hunger;
            if (hungerText != null)
            {
                hungerText.text = $"Hunger: {creature.hunger:F1}";
            }
        }

        if (loveSlider != null)
        {
            loveSlider.value = creature.loveLevel;
            if (loveText != null)
            {
                loveText.text = $"Love: {creature.loveLevel:F1}";
            }
        }

        if (stateText != null)
        {
            stateText.text = $"State: {creature.currentState}";
        }

        // Position the UI in world space
        Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        
        if (hungerSlider != null)
        {
            hungerSlider.transform.position = screenPos;
        }
        
        if (loveSlider != null)
        {
            loveSlider.transform.position = screenPos + Vector3.up * 30f;
        }
        
        if (stateText != null)
        {
            stateText.transform.position = screenPos + Vector3.up * 60f;
        }
    }
    
    void CreateUIElements()
    {
        // Create canvas
        GameObject canvasObj = new GameObject("WolfCanvas");
        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.WorldSpace;
        
        // Add canvas scaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        
        // Add raycaster for interactions
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create background panel
        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(uiCanvas.transform, false);
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);
        
        RectTransform panelRect = panelImage.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(width, height);
        
        // Create state text
        GameObject stateObj = new GameObject("StateText");
        stateObj.transform.SetParent(panelRect, false);
        
        stateText = stateObj.AddComponent<TextMeshProUGUI>();
        stateText.alignment = TextAlignmentOptions.Center;
        stateText.fontSize = 12;
        stateText.color = Color.white;
        
        RectTransform stateRect = stateText.GetComponent<RectTransform>();
        stateRect.anchorMin = new Vector2(0, 1);
        stateRect.anchorMax = new Vector2(1, 1);
        stateRect.pivot = new Vector2(0.5f, 1);
        stateRect.sizeDelta = new Vector2(0, 20);
        stateRect.anchoredPosition = new Vector2(0, -10);
        
        // Create hunger text and bar
        CreateStatBar("Hunger", orangeColor, out hungerBar, out hungerText, panelRect, 0.7f);
        
        // Create love text and bar
        CreateStatBar("Love", new Color(1, 0, 1), out loveBar, out loveText, panelRect, 0.4f);
    }
    
    void CreateStatBar(string name, Color color, out Slider slider, out TextMeshProUGUI text, RectTransform parent, float verticalPosition)
    {
        // Create text
        GameObject textObj = new GameObject(name + "Text");
        textObj.transform.SetParent(parent, false);
        
        text = textObj.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Left;
        text.fontSize = 10;
        text.color = Color.white;
        
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, verticalPosition + 0.05f);
        textRect.anchorMax = new Vector2(1, verticalPosition + 0.15f);
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);
        
        // Create slider
        GameObject sliderObj = new GameObject(name + "Slider");
        sliderObj.transform.SetParent(parent, false);
        
        slider = sliderObj.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0, verticalPosition - 0.05f);
        sliderRect.anchorMax = new Vector2(1, verticalPosition + 0.05f);
        sliderRect.sizeDelta = Vector2.zero;
        sliderRect.offsetMin = new Vector2(10, 0);
        sliderRect.offsetMax = new Vector2(-10, 0);
        
        // Create slider background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderRect, false);
        
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f);
        
        RectTransform bgRect = bgImage.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        // Create slider fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(sliderRect, false);
        
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = color;
        
        RectTransform fillRect = fillImage.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.sizeDelta = Vector2.zero;
        
        // Set up slider properties
        slider.fillRect = fillRect;
        slider.targetGraphic = bgImage;
        slider.direction = Slider.Direction.LeftToRight;
    }
    
    void UpdateUIPosition()
    {
        if (uiCanvas != null)
        {
            // Update canvas position and rotation
            uiCanvas.transform.position = transform.position + offset;
            uiCanvas.transform.rotation = Quaternion.LookRotation(
                uiCanvas.transform.position - mainCamera.transform.position);
            
            // Set scale based on distance for consistent size
            float distance = Vector3.Distance(uiCanvas.transform.position, mainCamera.transform.position);
            uiCanvas.transform.localScale = Vector3.one * (distance / 10f);
        }
    }
    
    void UpdateUIValues()
    {
        if (creature != null)
        {
            // Update state text
            stateText.text = "Wolf - " + creature.currentState.ToString();
            
            // Update hunger values
            float hungerPercent = Mathf.Clamp01(creature.hunger / creature.maxHunger);
            hungerBar.value = hungerPercent;
            hungerText.text = "Hunger: " + Mathf.Round(creature.hunger) + "/" + creature.maxHunger;
            
            // Update love values
            float lovePercent = Mathf.Clamp01(creature.loveLevel / creature.maxLoveLevel);
            loveBar.value = lovePercent;
            loveText.text = "Love: " + Mathf.Round(creature.loveLevel) + "/" + creature.maxLoveLevel;
        }
    }
    
    void SetUIVisibility(bool visible)
    {
        isVisible = visible;
        
        if (uiCanvas != null)
        {
            uiCanvas.gameObject.SetActive(visible);
        }
    }
    
    void OnDestroy()
    {
        // Clean up UI elements when wolf is destroyed
        if (uiCanvas != null)
        {
            Destroy(uiCanvas.gameObject);
        }
    }

    private void OnGUI()
    {
        if (creature == null || mainCamera == null) return;

        // Draw hunger bar
        Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        float barWidth = 100f;
        float barHeight = 10f;
        float hungerPercent = creature.hunger / creature.maxHunger;

        // Background
        GUI.color = Color.gray;
        GUI.DrawTexture(new Rect(screenPos.x - barWidth/2, Screen.height - screenPos.y, barWidth, barHeight), Texture2D.whiteTexture);

        // Fill
        GUI.color = Color.Lerp(Color.green, orangeColor, hungerPercent);
        GUI.DrawTexture(new Rect(screenPos.x - barWidth/2, Screen.height - screenPos.y, barWidth * hungerPercent, barHeight), Texture2D.whiteTexture);

        // Draw love bar
        screenPos.y += 20f;
        float lovePercent = creature.loveLevel / creature.maxLoveLevel;

        // Background
        GUI.color = Color.gray;
        GUI.DrawTexture(new Rect(screenPos.x - barWidth/2, Screen.height - screenPos.y, barWidth, barHeight), Texture2D.whiteTexture);

        // Fill
        GUI.color = Color.Lerp(Color.blue, Color.magenta, lovePercent);
        GUI.DrawTexture(new Rect(screenPos.x - barWidth/2, Screen.height - screenPos.y, barWidth * lovePercent, barHeight), Texture2D.whiteTexture);
    }
} 