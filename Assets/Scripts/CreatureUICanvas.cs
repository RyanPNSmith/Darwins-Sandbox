using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreatureUICanvas : MonoBehaviour
{
    // Reference to the wolf's Creature component
    private Creature creature;
    
    // UI elements that will be assigned in the inspector
    public static Canvas uiCanvas;
    public static Slider hungerBar;
    public static Slider loveBar;
    public static TextMeshProUGUI hungerText;
    public static TextMeshProUGUI loveText;
    public static TextMeshProUGUI stateText;
    public static GameObject statsPanel;
    
    // UI settings
    public float hoverRadius = 1f;  // Radius for hover detection
    
    // Control visibility
    private static bool isAnyWolfHovered = false;
    private static Creature hoveredCreature = null;
    private Camera mainCamera;
    private Color orangeColor = new Color(1f, 0.5f, 0f, 1f);
    private SphereCollider hoverCollider;

    void Start()
    {
        Debug.Log("CreatureUICanvas Start called");
        
        // Get components
        creature = GetComponent<Creature>();
        if (creature == null)
        {
            Debug.LogError("No Creature component found on " + gameObject.name);
            return;
        }
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found in scene");
            return;
        }
        
        // Add hover collider
        hoverCollider = gameObject.AddComponent<SphereCollider>();
        hoverCollider.radius = hoverRadius;
        hoverCollider.isTrigger = true;
        
        // Check if UI elements are assigned
        if (statsPanel == null)
        {
            Debug.LogError("Stats Panel not assigned in inspector. Please create a UI in the scene and assign it to the CreatureUICanvas component.");
            return;
        }
        
        // Initially hide UI
        SetUIVisibility(false);
    }
    
    void Update()
    {
        if (creature == null || mainCamera == null || statsPanel == null)
        {
            return;
        }
        
        // Check if mouse is over this creature using the collider
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        bool isMouseOver = Physics.Raycast(ray, out hit) && hit.collider == hoverCollider;
        
        // If mouse is over this creature, set it as the hovered creature
        if (isMouseOver && !creature.isDead)
        {
            hoveredCreature = creature;
            isAnyWolfHovered = true;
            UpdateUIValues(); // Update values when a new wolf is hovered
            SetUIVisibility(true); // Show UI
        }
        // If this was the hovered creature and mouse is no longer over it
        else if (hoveredCreature == creature && !isMouseOver)
        {
            hoveredCreature = null;
            isAnyWolfHovered = false;
            SetUIVisibility(false); // Hide UI
        }
    }
    
    void UpdateUIValues()
    {
        if (hoveredCreature != null)
        {
            // Update state text
            if (stateText != null)
                stateText.text = "Wolf - " + hoveredCreature.currentState.ToString();
            
            // Update hunger values
            float hungerPercent = Mathf.Clamp01(hoveredCreature.hunger / hoveredCreature.maxHunger);
            if (hungerBar != null)
                hungerBar.value = hungerPercent * 100f;
            if (hungerText != null)
                hungerText.text = "Hunger: " + Mathf.Round(hoveredCreature.hunger) + "/" + hoveredCreature.maxHunger;
            
            // Update love values
            float lovePercent = Mathf.Clamp01(hoveredCreature.loveLevel / hoveredCreature.maxLoveLevel);
            if (loveBar != null)
                loveBar.value = lovePercent * 100f;
            if (loveText != null)
                loveText.text = "Love: " + Mathf.Round(hoveredCreature.loveLevel) + "/" + hoveredCreature.maxLoveLevel;
        }
    }
    
    void SetUIVisibility(bool visible)
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(visible);
        }
    }
    
    void OnDestroy()
    {
        // If this was the hovered creature, clear when destroyed
        if (hoveredCreature == creature)
        {
            hoveredCreature = null;
            isAnyWolfHovered = false;
            SetUIVisibility(false);
        }
        
        // Don't destroy the canvas when a single wolf is destroyed
        // It will be used by other wolves
    }
} 