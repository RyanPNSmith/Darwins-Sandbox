using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Creature : MonoBehaviour
{
    // State machine enum
    public enum WolfState
    {
        Hunting,
        Wandering,
        Mating,
        Resting
    }
    
    // Current state and state machine properties
    public WolfState currentState = WolfState.Wandering;
    private float stateTimer = 0f;
    
    // Mating properties
    public float loveLevel = 0f;
    public float maxLoveLevel = 100f;
    public float loveIncreaseRate = 0.2f;
    public float loveThreshold = 75f;
    private bool isLookingForMate = false;
    private GameObject currentMate = null;
    private float matingDistance = 3f;
    private float matingTime = 5f;
    private float matingTimer = 0f;
    
    // Core attributes
    public GameObject agentPrefab;
    public bool isUser = false;
    public float size = 1.0f;
    
    // Vision attributes
    public float viewDistance = 20f;
    public float viewAngle = 120f;
    
    // Energy attributes
    public float hunger = 50f; // Current hunger level (0 = starving, 100 = full)
    public float maxHunger = 100f; // Maximum hunger level
    public float hungerGained = 30f; // How much hunger is reduced when eating
    public float hungerIncreaseRate = 0.5f; // How quickly hunger increases over time
    public float hungerThreshold = 40f; // Below this hunger level, wolf prioritizes hunting
    
    // Reproduction attributes
    public float reproductionHunger = 0;
    public float reproductionHungerThreshold = 60f;
    public float reproductionHungerGained = 5f;
    public int numberOfChildren = 1;
    
    // Movement control
    public float FB = 0.3f; // Forward/Backward
    public float LR = 0;    // Left/Right
    
    // Hunting behavior
    public float huntingSpeedMultiplier = 1.5f;
    public float idleMovementChance = 0.02f;
    public float wanderStrength = 0.3f;
    
    // Neural network & mutation
    public float mutationAmount = 0.8f;
    public float mutationChance = 0.2f; 
    public bool mutateMutations = true;
    private bool isMutated = false;
    public NN nn;
    
    // Internal state tracking
    private Movement movement;
    private float[] sensorInputs;
    private float elapsed = 0f;
    public float lifeSpan = 0f;
    public bool isDead = false;
    
    // Prey and mate tracking
    private GameObject targetPrey = null;
    private float preyTrackedTimer = 0f;
    private float preyMemoryDuration = 3f; // How long wolf remembers prey after losing sight
    private List<GameObject> detectedPreyList = new List<GameObject>();
    private List<GameObject> detectedMatesList = new List<GameObject>();
    
    // Start is called before the first frame update
    void Awake()
    {
        // Get or add required components
        nn = GetComponent<NN>();
        if (nn == null)
        {
            nn = gameObject.AddComponent<NN>();
            Debug.LogWarning("NN component missing on " + gameObject.name + ". Adding it automatically.");
        }
        
        movement = GetComponent<Movement>();
        if (movement == null)
        {
            movement = gameObject.AddComponent<Movement>();
            Debug.LogWarning("Movement component missing on " + gameObject.name + ". Adding it automatically.");
        }
        
        // Make sure we have a collider for food detection
        if (GetComponent<Collider>() == null)
        {
            CapsuleCollider collider = gameObject.AddComponent<CapsuleCollider>();
            collider.height = 2.0f;
            collider.radius = 0.5f;
            collider.center = new Vector3(0, 1.0f, 0);
            collider.isTrigger = true;
            Debug.LogWarning("Collider missing on " + gameObject.name + ". Adding CapsuleCollider automatically.");
        }
        
        // Add UI component for status bars if missing
        if (GetComponent<CreatureUICanvas>() == null)
        {
            gameObject.AddComponent<CreatureUICanvas>();
        }
        
        // Check if we need to set agentPrefab
        if (agentPrefab == null)
        {
            // Try to find a wolf prefab in the project
            agentPrefab = Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(g => g.name.Contains("Wolf") && g.GetComponent<Creature>() != null);
            
            if (agentPrefab == null)
            {
                // If still null, use ourselves as a template
                agentPrefab = gameObject;
                Debug.LogWarning("Agent prefab missing on " + gameObject.name + ". Using self as template.");
            }
        }
        
        // Set up arrays and name
        sensorInputs = new float[6]; // 5 direction sensors + 1 hunger level sensor
        this.name = "Wolf";
        
        // Set the correct tag
        if (gameObject.tag == "Untagged")
        {
            gameObject.tag = "Agent";
            Debug.LogWarning("Tag missing on " + gameObject.name + ". Setting tag to 'Agent'.");
        }
        
        // Initialize state with random values to create variation
        hunger = Random.Range(40f, maxHunger);
        loveLevel = Random.Range(0f, maxLoveLevel / 2);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(!isMutated)
        {
            MutateCreature();
            this.transform.localScale = new Vector3(size, size, size);
            isMutated = true;
        }

        ManageEnergy();
        UpdateStateAndBehavior();
        movement.Move(FB, LR);
    }

    void UpdateStateAndBehavior()
    {
        // Update prey and mate tracking
        DetectPrey();
        DetectMates();
        
        // Visualize view cone based on current state
        DrawViewCone();
        
        // Calculate inputs for neural network (5 distance sensors + hunger level)
        UpdateSensorInputs();
        
        // Get outputs from neural network
        float[] outputsFromNN = nn.Brain(sensorInputs);
        
        // Default behavior from neural network
        FB = outputsFromNN[0];
        LR = outputsFromNN[1];
        
        // Update state machine
        UpdateStateMachine();
        
        // Apply behavior based on current state
        switch (currentState)
        {
            case WolfState.Hunting:
                if (targetPrey != null)
                {
                    ApplyHuntingBehavior();
                }
                else
                {
                    ApplyWanderingBehavior();
                }
                break;
                
            case WolfState.Wandering:
                ApplyWanderingBehavior();
                break;
                
            case WolfState.Mating:
                if (currentMate != null)
                {
                    ApplyMatingBehavior();
                }
                else
                {
                    currentState = WolfState.Wandering;
                }
                break;
                
            case WolfState.Resting:
                // When resting, move very little
                FB = 0.1f;
                LR = Random.Range(-0.1f, 0.1f);
                break;
        }
        
        // User control overrides everything
        if (isUser)
        {
            FB = Input.GetAxis("Vertical");
            LR = Input.GetAxis("Horizontal") / 5f;
        }
    }
    
    void UpdateStateMachine()
    {
        // Increment love level over time
        loveLevel += loveIncreaseRate * Time.deltaTime;
        loveLevel = Mathf.Min(loveLevel, maxLoveLevel);
        
        // Update state timer
        stateTimer += Time.deltaTime;
        
        // Define fullness threshold - when wolf is considered "full"
        float fullnessThreshold = maxHunger * 0.2f;  // 20% of max hunger (80% full)
        
        // State transitions
        switch(currentState)
        {
            case WolfState.Wandering:
                // If hungry and not full, switch to hunting (only if prey is detected)
                if (hunger > hungerThreshold && hunger > fullnessThreshold && targetPrey != null)
                {
                    currentState = WolfState.Hunting;
                    stateTimer = 0f;
                }
                // If ready to mate and a mate is found, switch to mating
                else if (loveLevel > loveThreshold && currentMate != null)
                {
                    currentState = WolfState.Mating;
                    stateTimer = 0f;
                    isLookingForMate = true;
                }
                break;
                
            case WolfState.Hunting:
                // If full or no prey, go back to wandering
                if (targetPrey == null || hunger <= fullnessThreshold)
                {
                    currentState = WolfState.Wandering;
                    stateTimer = 0f;
                }
                // If love is very high, prioritize mating over eating (unless really hungry)
                else if (loveLevel > loveThreshold * 1.2f && hunger < hungerThreshold * 1.3f && currentMate != null)
                {
                    currentState = WolfState.Mating;
                    stateTimer = 0f;
                }
                break;
                
            case WolfState.Mating:
                if (currentMate == null)
                {
                    // If mate is lost, go back to wandering
                    currentState = WolfState.Wandering;
                    stateTimer = 0f;
                    isLookingForMate = false;
                }
                else if (matingTimer >= matingTime)
                {
                    // After mating, rest for a while
                    currentState = WolfState.Resting;
                    stateTimer = 0f;
                    isLookingForMate = false;
                    loveLevel = 0f; // Reset love after mating
                }
                break;
                
            case WolfState.Resting:
                // After resting for a while, go back to wandering
                if (stateTimer > 3f)
                {
                    currentState = WolfState.Wandering;
                    stateTimer = 0f;
                }
                break;
        }
        
        // Emergency transition - if very hungry, always prioritize hunting over other states
        if (hunger > hungerThreshold * 1.5f && targetPrey != null && currentState != WolfState.Hunting)
        {
            currentState = WolfState.Hunting;
            stateTimer = 0f;
        }
    }
    
    // Add tag check helper method at the top of the class
    private bool IsPrey(GameObject obj)
    {
        if (obj == null) return false;
        
        // Check if it has the Food tag
        try {
            return obj.CompareTag("Food");
        }
        catch {
            // In case of exceptions, handle gracefully
            return false;
        }
    }
    
    // Add method to check if another creature is a potential mate
    private bool IsPotentialMate(GameObject obj)
    {
        if (obj == null || obj == this.gameObject) return false;
        
        try {
            // Check if it's another agent
            if (obj.CompareTag("Agent"))
            {
                Creature otherCreature = obj.GetComponent<Creature>();
                // Check if other creature is also looking for a mate
                if (otherCreature != null && otherCreature.loveLevel > otherCreature.loveThreshold)
                {
                    return true;
                }
            }
            return false;
        }
        catch {
            return false;
        }
    }
    
    void DetectPrey()
    {
        // Clear previous detections but keep tracked prey in memory for a while
        detectedPreyList.Clear();
        
        // If we have a target but lost sight, count down memory timer
        if (targetPrey != null)
        {
            preyTrackedTimer -= Time.deltaTime;
            if (preyTrackedTimer <= 0f || targetPrey == null)
            {
                targetPrey = null;
            }
        }
        
        // Detect prey in view cone
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, viewDistance);
        foreach (var hitCollider in hitColliders)
        {
            // Use helper method to check if it's prey
            if (IsPrey(hitCollider.gameObject))
            {
                // Check if prey is within view angle
                Vector3 directionToPrey = hitCollider.transform.position - transform.position;
                float angleToTarget = Vector3.Angle(transform.forward, directionToPrey);
                
                if (angleToTarget <= viewAngle / 2)
                {
                    // Check line of sight
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position + Vector3.up * 0.1f, directionToPrey.normalized, out hit, viewDistance))
                    {
                        if (IsPrey(hit.collider.gameObject))
                        {
                            // Valid prey in sight
                            detectedPreyList.Add(hitCollider.gameObject);
                            Debug.DrawLine(transform.position + Vector3.up * 0.1f, hit.point, Color.green);
                        }
                    }
                }
            }
        }
        
        // Set new target prey if we don't already have one
        if (detectedPreyList.Count > 0 && targetPrey == null)
        {
            // Find closest prey
            float closestDistance = float.MaxValue;
            GameObject closestPrey = null;
            
            foreach (var prey in detectedPreyList)
            {
                float distance = Vector3.Distance(transform.position, prey.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPrey = prey;
                }
            }
            
            targetPrey = closestPrey;
            preyTrackedTimer = preyMemoryDuration;
        }
        else if (detectedPreyList.Count > 0 && targetPrey != null)
        {
            // We have prey in sight, reset memory timer
            preyTrackedTimer = preyMemoryDuration;
            
            // Check if our target is still in the detected list
            bool targetStillVisible = false;
            foreach (var prey in detectedPreyList)
            {
                if (prey == targetPrey)
                {
                    targetStillVisible = true;
                    break;
                }
            }
            
            // If not, choose the closest visible prey
            if (!targetStillVisible)
            {
                float closestDistance = float.MaxValue;
                GameObject closestPrey = null;
                
                foreach (var prey in detectedPreyList)
                {
                    float distance = Vector3.Distance(transform.position, prey.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPrey = prey;
                    }
                }
                
                targetPrey = closestPrey;
            }
        }
    }
    
    void DetectMates()
    {
        // Only look for mates if ready
        if (loveLevel < loveThreshold)
        {
            currentMate = null;
            isLookingForMate = false;
            return;
        }
        
        // Clear previous mate detections
        detectedMatesList.Clear();
        
        // Detect potential mates in view cone
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, viewDistance);
        foreach (var hitCollider in hitColliders)
        {
            if (IsPotentialMate(hitCollider.gameObject))
            {
                // Check if within view angle
                Vector3 directionToMate = hitCollider.transform.position - transform.position;
                float angleToMate = Vector3.Angle(transform.forward, directionToMate);
                
                if (angleToMate <= viewAngle / 2)
                {
                    // Check line of sight
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position + Vector3.up * 0.1f, directionToMate.normalized, out hit, viewDistance))
                    {
                        if (hit.collider.gameObject == hitCollider.gameObject)
                        {
                            // Valid mate in sight
                            detectedMatesList.Add(hitCollider.gameObject);
                            Debug.DrawLine(transform.position + Vector3.up * 0.1f, hit.point, Color.magenta);
                        }
                    }
                }
            }
        }
        
        // Find closest mate
        if (detectedMatesList.Count > 0)
        {
            float closestDistance = float.MaxValue;
            GameObject closestMate = null;
            
            foreach (var mate in detectedMatesList)
            {
                float distance = Vector3.Distance(transform.position, mate.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestMate = mate;
                }
            }
            
            currentMate = closestMate;
            isLookingForMate = true;
        }
        else if (isLookingForMate && detectedMatesList.Count == 0)
        {
            // Lost sight of potential mates
            if (currentMate != null)
            {
                // Check if current mate is still valid and in range
                if (currentMate != null && Vector3.Distance(transform.position, currentMate.transform.position) > viewDistance)
                {
                    currentMate = null;
                }
            }
        }
    }
    
    void DrawViewCone()
    {
        float halfAngle = viewAngle / 2;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-halfAngle, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(halfAngle, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * transform.forward;
        Vector3 rightRayDirection = rightRayRotation * transform.forward;
        
        // Color based on state
        Color coneColor = Color.yellow;
        switch (currentState)
        {
            case WolfState.Hunting: coneColor = Color.red; break;
            case WolfState.Mating: coneColor = Color.magenta; break;
            case WolfState.Resting: coneColor = Color.blue; break;
        }
        
        Debug.DrawRay(transform.position + Vector3.up * 0.1f, leftRayDirection * viewDistance, coneColor);
        Debug.DrawRay(transform.position + Vector3.up * 0.1f, transform.forward * viewDistance, coneColor);
        Debug.DrawRay(transform.position + Vector3.up * 0.1f, rightRayDirection * viewDistance, coneColor);
    }
    
    void UpdateSensorInputs()
    {
        // Use 5 raycasts for environmental sensing
        int numRaycasts = 5;
        float angleBetweenRaycasts = 30;
        
        for (int i = 0; i < numRaycasts; i++)
        {
            float angle = ((2 * i + 1 - numRaycasts) * angleBetweenRaycasts / 2);
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 rayDirection = rotation * transform.forward;
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            
            // Default to max distance
            sensorInputs[i] = 1.0f;
            
            RaycastHit hit;
            if (Physics.Raycast(rayStart, rayDirection, out hit, viewDistance))
            {
                // Normalize distance value between 0-1
                sensorInputs[i] = hit.distance / viewDistance;
                
                // Use color to indicate what was detected
                Color rayColor = Color.white;
                
                if (IsPrey(hit.collider.gameObject))
                {
                    rayColor = Color.green; // Prey
                }
                else if (IsPotentialMate(hit.collider.gameObject))
                {
                    rayColor = Color.magenta; // Potential mate
                }
                else
                {
                    rayColor = Color.red; // Obstacle
                }
                
                Debug.DrawRay(rayStart, rayDirection * hit.distance, rayColor);
            }
            else
            {
                // Nothing detected
                Debug.DrawRay(rayStart, rayDirection * viewDistance, Color.blue);
            }
        }
        
        // Add normalized hunger level as the last input
        sensorInputs[5] = hunger / maxHunger;
    }

    void ApplyHuntingBehavior()
    {
        if (targetPrey != null)
        {
            // Calculate direction to prey
            Vector3 directionToPrey = targetPrey.transform.position - transform.position;
            
            // Calculate angle to prey
            float angleToTarget = Vector3.SignedAngle(transform.forward, directionToPrey, Vector3.up);
            
            // Draw a line to current target
            Debug.DrawLine(transform.position, targetPrey.transform.position, Color.red);
            
            // Convert angle to steering input - note that we DO NOT flip the sign anymore
            // This assumes Movement.cs uses positive LR to turn right and negative to turn left
            LR = Mathf.Clamp(angleToTarget / 30f, -1f, 1f);
            
            // Increase speed when hunting - IMPORTANT: Change sign to positive for forward movement
            FB = Mathf.Clamp01(1.0f - Mathf.Abs(angleToTarget) / 180f) * huntingSpeedMultiplier;
            
            // Minimum forward speed
            FB = Mathf.Max(FB, 0.3f);
        }
    }
    
    void ApplyMatingBehavior()
    {
        if (currentMate != null)
        {
            // Calculate direction to mate
            Vector3 directionToMate = currentMate.transform.position - transform.position;
            
            // Calculate angle to mate
            float angleToMate = Vector3.SignedAngle(transform.forward, directionToMate, Vector3.up);
            
            // Distance to mate
            float distanceToMate = directionToMate.magnitude;
            
            // Draw a line to current mate
            Debug.DrawLine(transform.position, currentMate.transform.position, Color.magenta);
            
            // If close enough to mate, start the mating process
            if (distanceToMate < matingDistance)
            {
                // When close to mate, slow down
                FB = 0.1f;
                LR = 0f;
                
                // Increment mating timer when close
                matingTimer += Time.deltaTime;
                
                // Check if mating is complete
                if (matingTimer >= matingTime)
                {
                    // Attempt to reproduce with the mate
                    AttemptReproduction(currentMate);
                    
                    // Reset mating timer and love level
                    matingTimer = 0f;
                    loveLevel = 0f;
                    
                    // Update mate's state too
                    Creature mateCreature = currentMate.GetComponent<Creature>();
                    if (mateCreature != null)
                    {
                        mateCreature.loveLevel = 0f;
                        mateCreature.currentState = WolfState.Resting;
                    }
                    
                    // Change to resting state
                    currentState = WolfState.Resting;
                    currentMate = null;
                }
            }
            else
            {
                // Move toward mate
                LR = Mathf.Clamp(angleToMate / 30f, -1f, 1f);
                
                // Speed proportional to distance - slow down when getting closer
                FB = Mathf.Clamp01(distanceToMate / 10f);
                FB = Mathf.Max(FB, 0.3f);
            }
        }
    }
    
    void ApplyWanderingBehavior()
    {
        // Add random movement when wandering to simulate searching
        if (Random.value < idleMovementChance)
        {
            LR = Random.Range(-wanderStrength, wanderStrength);
        }
        
        // Keep moving forward at a moderate pace
        FB = Mathf.Clamp(FB, 0.2f, 0.8f);
    }

    // Called when the wolf collides with prey
    void OnTriggerEnter(Collider col)
    {
        // Use the helper method
        if (IsPrey(col.gameObject))
        {
            // Eat the prey
            hunger -= hungerGained;
            hunger = Mathf.Max(hunger, 0f);
            
            // Gain reproduction hunger
            reproductionHunger -= reproductionHungerGained;
            
            // Remove the eaten prey
            Destroy(col.gameObject);
            
            // Clear target
            if (targetPrey == col.gameObject)
            {
                targetPrey = null;
            }
        }
    }

    public void ManageEnergy()
    {
        // Only manage energy if not dead
        if (isDead) return;

        // Decrease energy over time
        hunger -= hungerIncreaseRate * Time.deltaTime;
        
        // Clamp energy between 0 and maxHunger
        hunger = Mathf.Clamp(hunger, 0f, maxHunger);
        
        // Die if energy reaches zero
        if (hunger <= 0f)
        {
            isDead = true;
            // Stop movement
            FB = 0f;
            LR = 0f;
            // Disable components
            if (movement != null) movement.enabled = false;
            if (GetComponent<Collider>() != null) GetComponent<Collider>().enabled = false;
            // Change color to indicate death
            if (GetComponent<Renderer>() != null)
            {
                GetComponent<Renderer>().material.color = Color.gray;
            }
        }
        
        // Update reproduction hunger
        reproductionHunger += reproductionHungerGained * Time.deltaTime;
        reproductionHunger = Mathf.Clamp(reproductionHunger, 0f, maxHunger);
    }

    private void MutateCreature()
    {
        if(mutateMutations)
        {
            mutationAmount += Random.Range(-1.0f, 1.0f)/100;
            mutationChance += Random.Range(-1.0f, 1.0f)/100;
        }

        // Ensure mutation parameters stay positive
        mutationAmount = Mathf.Max(mutationAmount, 0);
        mutationChance = Mathf.Max(mutationChance, 0);

        nn.MutateNetwork(mutationAmount, mutationChance);
    }

    // New method for reproduction that requires two wolves
    private void AttemptReproduction(GameObject mate)
    {
        // Check if both wolves have enough reproduction hunger
        Creature mateCreature = mate.GetComponent<Creature>();
        if (mateCreature == null) return;
        
        if (reproductionHunger >= reproductionHungerThreshold && 
            mateCreature.reproductionHunger >= mateCreature.reproductionHungerThreshold)
        {
            // Check if we have a prefab
            if (agentPrefab == null)
            {
                Debug.LogError("Agent prefab is missing! Cannot reproduce.");
                return;
            }
            
            // Create offspring between the two parents
            for (int i = 0; i < numberOfChildren; i++)
            {
                // Create child at a position between the parents with slight offset
                Vector3 midPoint = (transform.position + mate.transform.position) / 2f;
                GameObject child = Instantiate(agentPrefab, new Vector3(
                    midPoint.x + Random.Range(-3, 4), 
                    0.75f, 
                    midPoint.z + Random.Range(-3, 4)), 
                    Quaternion.identity);
                
                // Mix neural networks from both parents with mutations
                Creature childCreature = child.GetComponent<Creature>();
                if (childCreature != null && childCreature.nn != null)
                {
                    // 50% chance to inherit from each parent for each layer
                    for (int layerIdx = 0; layerIdx < nn.layers.Length; layerIdx++)
                    {
                        if (Random.value < 0.5f)
                        {
                            // Inherit from first parent (this wolf)
                            childCreature.nn.layers[layerIdx] = nn.layers[layerIdx];
                        }
                        else
                        {
                            // Inherit from second parent (mate)
                            childCreature.nn.layers[layerIdx] = mateCreature.nn.layers[layerIdx];
                        }
                    }
                    
                    // Apply mutations to child's neural network
                    childCreature.MutateCreature();
                }
            }
            
            // Reset reproduction hunger for both parents
            reproductionHunger = 0;
            mateCreature.reproductionHunger = 0;
            
            // Slightly reduce hunger from both parents due to reproduction effort
            hunger -= 10f;
            mateCreature.hunger -= 10f;
        }
    }
}
