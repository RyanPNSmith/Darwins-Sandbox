using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    public float spawnRate = 1;
    public int floorScale = 1;
    public GameObject myPrefab;
    public float timeElapsed = 0;
    public int spawnRadius = 100;
    public int initialFoodCount = 50;

    void Start()
    {
        // Make sure we have a prefab
        if (myPrefab == null)
        {
            Debug.LogError("Food prefab is missing! Please assign a prefab to the FoodSpawner.");
            return;
        }
        
        // Spawn food at random locations at the start of the game
        for (int i = 0; i < initialFoodCount; i++)
        {
            SpawnFood();
        }
    }

    // FixedUpdate is called once per physics frame
    void FixedUpdate()
    {
        //spawn food every second with timeElapsed
        timeElapsed += Time.deltaTime;
        if (timeElapsed >= spawnRate)
        {
            timeElapsed = timeElapsed % spawnRate;
            SpawnFood();
        }
    }

    void SpawnFood()
    {
        if (myPrefab == null) return;
        
        // Generate random position
        float randomAngle = Random.Range(0f, 360f);
        float randomDistance = Random.Range(0f, spawnRadius);
        
        // Convert polar coordinates to Cartesian
        float x = Mathf.Cos(randomAngle * Mathf.Deg2Rad) * randomDistance * floorScale;
        float z = Mathf.Sin(randomAngle * Mathf.Deg2Rad) * randomDistance * floorScale;
        
        // Instantiate food
        GameObject food = Instantiate(myPrefab, new Vector3(x, 0.5f, z), Quaternion.identity);
        
        // Set the tag (since it's what our creatures are looking for)
        food.tag = "Food";
        
        // Make sure it has a collider
        if (!food.GetComponent<Collider>())
        {
            SphereCollider collider = food.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
        }
        
        // Make sure the collider is a trigger
        Collider[] colliders = food.GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.isTrigger = true;
        }
    }
}
