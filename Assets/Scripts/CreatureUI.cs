using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureUI : MonoBehaviour
{
    private Creature creature;
    private bool isMouseOver = false;
    private Camera mainCamera;
    private Color orangeColor = new Color(1f, 0.5f, 0f, 1f); // Custom orange color

    // UI appearance settings
    public float barWidth = 1.0f;
    public float barHeight = 0.15f;
    public float barSpacing = 0.05f;
    public float barDistance = 0.5f;
    public Color hungerBarColor = new Color(1.0f, 0.5f, 0.0f); // Orange
    public Color loveBarColor = new Color(1.0f, 0.0f, 0.8f);   // Pink
    
    void Start()
    {
        creature = GetComponent<Creature>();
        mainCamera = Camera.main;
    }
    
    void Update()
    {
        // Check if the cursor is over this wolf using raycasting
        if (Input.GetMouseButtonDown(0) || Time.frameCount % 10 == 0) // Only check periodically to save performance
        {
            CheckMouseOver();
        }
    }
    
    void CheckMouseOver()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            // If the ray hits this wolf
            if (hit.transform == this.transform)
            {
                isMouseOver = true;
            }
            else
            {
                isMouseOver = false;
            }
        }
        else
        {
            isMouseOver = false;
        }
    }
    
    void OnMouseEnter()
    {
        isMouseOver = true;
    }
    
    void OnMouseExit()
    {
        isMouseOver = false;
    }
    
    void OnGUI()
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
    
    void OnRenderObject()
    {
        if (isMouseOver && creature != null)
        {
            // Draw 3D bars above the wolf when hovered
            DrawStatusBars();
        }
    }
    
    void DrawStatusBars()
    {
        // Create materials for bars if they don't exist
        Material hungerMaterial = new Material(Shader.Find("Unlit/Color"));
        hungerMaterial.color = hungerBarColor;
        
        Material loveMaterial = new Material(Shader.Find("Unlit/Color"));
        loveMaterial.color = loveBarColor;
        
        Material backgroundMaterial = new Material(Shader.Find("Unlit/Color"));
        backgroundMaterial.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        // Position for bars above wolf
        Vector3 barPosition = transform.position + Vector3.up * barDistance;
        
        // Draw hunger bar background
        DrawBar(barPosition, backgroundMaterial, 1.0f);
        
        // Draw hunger bar fill (uses energy percentage)
        float energyPercent = Mathf.Clamp01(creature.hunger / creature.maxHunger);
        DrawBar(barPosition, hungerMaterial, energyPercent);
        
        // Position for love bar (above hunger bar)
        barPosition += Vector3.up * (barHeight + barSpacing);
        
        // Draw love bar background
        DrawBar(barPosition, backgroundMaterial, 1.0f);
        
        // Draw love bar fill
        float lovePercent = Mathf.Clamp01(creature.loveLevel / creature.maxLoveLevel);
        DrawBar(barPosition, loveMaterial, lovePercent);
    }
    
    void DrawBar(Vector3 position, Material material, float fillAmount)
    {
        // Ensure the bars always face the camera
        Quaternion rotation = Quaternion.LookRotation(mainCamera.transform.forward);
        
        // Calculate width based on fill amount
        float actualWidth = barWidth * fillAmount;
        
        // Draw bar with GL lines
        GL.PushMatrix();
        material.SetPass(0);
        GL.LoadOrtho();
        
        Vector3 bottomLeft = mainCamera.WorldToViewportPoint(position + rotation * new Vector3(-barWidth/2, 0, 0));
        Vector3 bottomRight = mainCamera.WorldToViewportPoint(position + rotation * new Vector3(-barWidth/2 + actualWidth, 0, 0));
        Vector3 topLeft = mainCamera.WorldToViewportPoint(position + rotation * new Vector3(-barWidth/2, barHeight, 0));
        Vector3 topRight = mainCamera.WorldToViewportPoint(position + rotation * new Vector3(-barWidth/2 + actualWidth, barHeight, 0));
        
        GL.Begin(GL.QUADS);
        GL.Vertex3(bottomLeft.x, bottomLeft.y, 0);
        GL.Vertex3(bottomRight.x, bottomRight.y, 0);
        GL.Vertex3(topRight.x, topRight.y, 0);
        GL.Vertex3(topLeft.x, topLeft.y, 0);
        GL.End();
        
        GL.PopMatrix();
    }
} 