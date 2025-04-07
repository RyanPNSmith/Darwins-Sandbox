using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject foodPrefab;  // The food/sheep prefab to spawn
    public Transform platform;     // Reference to the platform
    public int maxFood = 20;       // Maximum number of food items at once
    public float spawnRate = 2f;   // Time between spawn attempts
    
    [Header("Size Detection")]
    public bool autoDetectPlatformSize = true;  // Auto-detect or use manual size
    public Vector2 platformSize = new Vector2(10, 10);  // Manual size (if not auto-detecting)
    
    // Private variables
    private float spawnTimer = 0f;
    private List<GameObject> spawnedFood = new List<GameObject>();
    private Renderer platformRenderer;
    private Collider platformCollider;
    
    void Start()
    {
        // Validate food prefab
        if (foodPrefab == null)
        {
            Debug.LogError("Food prefab not assigned to FoodSpawner!");
            enabled = false;  // Disable script
            return;
        }
        
        // Find platform if not assigned
        if (platform == null)
        {
            Debug.LogWarning("Platform not assigned to FoodSpawner, trying to find one in scene...");
            platform = GameObject.FindGameObjectWithTag("Ground")?.transform;
            
            if (platform == null)
            {
                Debug.LogError("Could not find a platform! Please assign one in the inspector.");
                enabled = false;  // Disable script
                return;
            }
        }
        
        // Get platform components for size detection
        platformRenderer = platform.GetComponent<Renderer>();
        platformCollider = platform.GetComponent<Collider>();
        
        if (autoDetectPlatformSize)
        {
            DetectPlatformSize();
        }
        
        // Start with some initial food
        for (int i = 0; i < maxFood / 2; i++)
        {
            SpawnFood();
        }
    }
    
    void Update()
    {
        // Clean up destroyed food from list
        spawnedFood.RemoveAll(item => item == null);
        
        // Spawn food if under maximum
        if (spawnedFood.Count < maxFood)
        {
            spawnTimer += Time.deltaTime;
            
            if (spawnTimer >= spawnRate)
            {
                spawnTimer = 0f;
                SpawnFood();
            }
        }
    }
    
    void DetectPlatformSize()
    {
        if (platformCollider != null)
        {
            // Use collider bounds
            Bounds bounds = platformCollider.bounds;
            platformSize.x = bounds.size.x * 0.9f;  // Use 90% of platform size to keep food away from edges
            platformSize.y = bounds.size.z * 0.9f;  // Y in our 2D representation is Z in 3D world
            
            Debug.Log($"Platform size detected from collider: {platformSize.x} x {platformSize.y}");
        }
        else if (platformRenderer != null)
        {
            // Use renderer bounds
            Bounds bounds = platformRenderer.bounds;
            platformSize.x = bounds.size.x * 0.9f;
            platformSize.y = bounds.size.z * 0.9f;
            
            Debug.Log($"Platform size detected from renderer: {platformSize.x} x {platformSize.y}");
        }
        else
        {
            Debug.LogWarning("Could not detect platform size - no renderer or collider found. Using default size.");
        }
    }
    
    void SpawnFood()
    {
        // Calculate random position within platform bounds
        float halfWidth = platformSize.x * 0.5f;
        float halfHeight = platformSize.y * 0.5f;
        
        Vector3 platformCenter = platform.position;
        
        // Generate random position
        float xPos = Random.Range(platformCenter.x - halfWidth, platformCenter.x + halfWidth);
        float zPos = Random.Range(platformCenter.z - halfHeight, platformCenter.z + halfHeight);
        
        // Add a small vertical offset to avoid spawning inside platform
        Vector3 spawnPos = new Vector3(xPos, platformCenter.y + 0.5f, zPos);
        
        // Check if position is valid (not too close to other food)
        if (IsClearPosition(spawnPos, 2.0f))
        {
            // Spawn the food
            GameObject food = Instantiate(foodPrefab, spawnPos, Quaternion.identity);
            food.tag = "Food";  // Ensure it has the food tag
            spawnedFood.Add(food);
        }
    }
    
    bool IsClearPosition(Vector3 position, float minDistance)
    {
        // Check if the position is far enough from other food
        foreach (GameObject food in spawnedFood)
        {
            if (food != null && Vector3.Distance(food.transform.position, position) < minDistance)
            {
                return false;
            }
        }
        return true;
    }
    
    // Utility method to spawn food at a specific position (can be called from other scripts)
    public GameObject SpawnFoodAt(Vector3 position)
    {
        if (spawnedFood.Count < maxFood)
        {
            GameObject food = Instantiate(foodPrefab, position, Quaternion.identity);
            food.tag = "Food";
            spawnedFood.Add(food);
            return food;
        }
        return null;
    }
    
    // Visualize the spawn area in the editor
    void OnDrawGizmos()
    {
        if (platform != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.2f);  // Semi-transparent green
            
            Vector3 center = platform.position;
            Vector3 size = new Vector3(platformSize.x, 0.1f, platformSize.y);
            
            Gizmos.DrawCube(center, size);
        }
    }
}
