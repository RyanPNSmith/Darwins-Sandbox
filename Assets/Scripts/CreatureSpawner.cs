using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureSpawner : MonoBehaviour
{
    public GameObject agentPrefab;
    private GameObject[] agentList;
    public int floorScale = 1;
    public int initialPopulation = 5;
    public int spawnRadius = 20;

    void Start()
    {
        // Spawn initial population
        for (int i = 0; i < initialPopulation; i++)
        {
            SpawnCreature();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        agentList = GameObject.FindGameObjectsWithTag("Agent");

        // if there are no agents in the scene, spawn one at a random location. 
        // This is to ensure that there is always at least one agent in the scene.
        if (agentList.Length < 1)
        {
            SpawnCreature();
        } 
    }

    void SpawnCreature()
    {
        // Create a random position within the spawn radius
        float randomAngle = Random.Range(0f, 360f);
        float randomDistance = Random.Range(0f, spawnRadius);
        
        // Convert polar coordinates to Cartesian
        float x = Mathf.Cos(randomAngle * Mathf.Deg2Rad) * randomDistance * floorScale;
        float z = Mathf.Sin(randomAngle * Mathf.Deg2Rad) * randomDistance * floorScale;
        
        // Instantiate the wolf slightly above the ground to ensure it doesn't fall through
        GameObject newCreature = Instantiate(agentPrefab, new Vector3(x, 1.5f, z), Quaternion.identity);
        
        // Make sure it has all required components
        if (!newCreature.GetComponent<CharacterController>())
        {
            CharacterController controller = newCreature.AddComponent<CharacterController>();
            controller.height = 2.0f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0, 1.0f, 0);
        }
        
        Debug.Log("Spawned new creature at position: " + newCreature.transform.position);
    }
}
