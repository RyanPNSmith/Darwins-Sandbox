using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prey : MonoBehaviour
{
    [Header("Prey Settings")]
    public float hungerValue = 25f;  // How much hunger this prey satisfies when eaten
    
    void Start()
    {
        // Ensure this object has the Food tag
        gameObject.tag = "Food";
        
        // Make sure it has a collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // Add a sphere collider if none exists
            col = gameObject.AddComponent<SphereCollider>();
            ((SphereCollider)col).radius = 0.5f;
            Debug.Log("Added sphere collider to prey: " + gameObject.name);
        }
        
        // Make sure the collider is a trigger
        col.isTrigger = true;
        
        // Log that this prey is ready
        Debug.Log("Prey initialized: " + gameObject.name + " (Tag: " + gameObject.tag + ", Has Trigger: " + col.isTrigger + ")");
    }
    
    // This method will be called when the wolf eats the prey
    public void GetEaten(Creature predator)
    {
        if (predator != null)
        {
            // Increase the predator's hunger
            predator.hunger += hungerValue;
            predator.hunger = Mathf.Min(predator.hunger, predator.maxHunger);
            
            // Adjust reproduction hunger
            predator.reproductionHunger -= predator.reproductionHungerGained;
            
            // Log that this prey was eaten
            Debug.Log("Prey eaten by: " + predator.gameObject.name);
        }
        
        // Destroy this prey
        Destroy(gameObject);
    }
} 