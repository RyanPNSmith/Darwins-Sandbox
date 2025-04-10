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
    public float loveLevel = 0f;         // Start at 0 love
    public float maxLoveLevel = 100f;
    public float loveIncreaseRate = 0.2f;
    public float loveThreshold = 100f;   // Changed from 75f to 100f - require full love
    private GameObject currentMate = null;
    private float matingDistance = 3f;
    private float matingTime = 5f;
    private float matingTimer = 0f;
    
    // Core attributes
    public GameObject agentPrefab;
    public bool isUser = false;
    
    // Vision attributes
    public float viewDistance = 20f;
    public float viewAngle = 360f;  // Changed to full 360 degrees for AOE
    
    // Energy attributes
    public float hunger = 100f;         // Start at 100 hunger (full)
    public float maxHunger = 100f;
    public float hungerGained = 25f;    // Amount hunger increases when eating
    public float hungerDecreaseRate = 5f;    // Increased to 5f to make hunger decrease faster
    
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
    
    // Neural network
    public NN nn;
    
    // Internal state tracking
    private Movement movement;
    private float[] sensorInputs;
    public float lifeSpan = 0f;
    public bool isDead = false;
    
    // Prey and mate tracking
    private GameObject targetPrey = null;
    private float preyTrackedTimer = 0f;
    private float preyMemoryDuration = 3f; // How long wolf remembers prey after losing sight
    private List<GameObject> detectedPreyList = new List<GameObject>();
    private List<GameObject> detectedMatesList = new List<GameObject>();

    // Add debug visualization toggle
    public bool showRaycasts = false;
    public Color rayColor = Color.yellow;
    public bool showAOESensors = true;  // New toggle for AOE visualization
    public bool showDebugLogs = false;  // New toggle to control debug logging
    
    // Add AOE sensor settings
    public int numSensors = 6;  // Number of directions to sense (evenly spaced in a circle)
    public float[] sensorDistances;  // Will store detected distances
    
    // Make sure we always use a consistent size for inputs
    private readonly int TOTAL_INPUTS = 9;  // 6 sensors + hunger + prey direction + prey distance

    // Add prey detection input for neural network
    private float preyDirectionInput = 0f;
    private float preyDistanceInput = 1f;

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
        
        // Set debug logging on NN component to match our setting
        if (nn != null)
        {
            nn.showDebugLogs = showDebugLogs;
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
        sensorDistances = new float[numSensors];
        // Always initialize with TOTAL_INPUTS size
        sensorInputs = new float[TOTAL_INPUTS]; 
        this.name = "Wolf";
        
        // Set the correct tag
        if (gameObject.tag == "Untagged")
        {
            gameObject.tag = "Agent";
            Debug.LogWarning("Tag missing on " + gameObject.name + ". Setting tag to 'Agent'.");
        }
        
        // Initialize state with fixed values for consistent behavior
        hunger = maxHunger * 0.7f; // Start at 70% hunger
        loveLevel = Random.Range(0f, maxLoveLevel / 2);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        ManageEnergy();
        UpdateStateAndBehavior();
        movement.Move(FB, LR);
    }

    void UpdateStateAndBehavior()
    {
        // Update prey and mate tracking
        DetectPrey();
        DetectMates();
        
        // Calculate inputs for neural network (5 distance sensors + hunger level)
        UpdateSensorInputs();
        
        // Get outputs from neural network - these will be our base movement values
        float[] outputsFromNN = nn.Brain(sensorInputs);
        
        // Use neural network outputs directly without clamping
        FB = outputsFromNN[0];
        LR = outputsFromNN[1];
        
        // Apply wandering behavior which may override the neural network
        if (Random.value < 0.3f)  // 30% chance to override NN with wandering
        {
            ApplyWanderingBehavior();
        }
        
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
                break;
                
            case WolfState.Wandering:
                // Already applied wandering behavior
                break;
                
            case WolfState.Mating:
                if (currentMate != null)
                {
                    ApplyMatingBehavior();
                }
                break;
                
            case WolfState.Resting:
                // Resting - minimal forward movement with occasional turning
                FB = 0.2f;
                if (Random.value < 0.05f)
                {
                    LR = Random.Range(-1.0f, 1.0f);
                }
                break;
        }
        
        // User control overrides everything
        if (isUser)
        {
            FB = Input.GetAxis("Vertical");
            LR = Input.GetAxis("Horizontal");
        }
        
        // Ensure extreme values for turning
        if (Mathf.Abs(LR) < 0.3f && Random.value < 0.1f)
        {
            // Force more extreme turning if the current value is too weak
            LR = Mathf.Sign(LR) * Random.Range(0.5f, 1.0f);
        }
        
        // Debug log movements
        if (Time.frameCount % 60 == 0)
        {
            // Debug.Log($"Wolf state: {currentState}, Movement: FB={FB}, LR={LR}");
        }
    }
    
    void UpdateStateMachine()
    {
        // Increment love level over time
        loveLevel += loveIncreaseRate * Time.deltaTime;
        loveLevel = Mathf.Min(loveLevel, maxLoveLevel);
        
        // Update state timer
        stateTimer += Time.deltaTime;
        
        // Define hunger threshold - when wolf is considered "hungry"
        float hungerThreshold = maxHunger * 0.5f;  // 50% of max hunger
        
        // State transitions
        switch(currentState)
        {
            case WolfState.Wandering:
                // PRIORITY: If hungry (below 50%) and prey is detected, switch to hunting
                if (hunger < hungerThreshold && targetPrey != null)
                {
                    currentState = WolfState.Hunting;
                    stateTimer = 0f;
                }
                // If ready to mate (FULL love) and a mate is found AND not hungry, switch to mating
                else if (loveLevel >= maxLoveLevel && currentMate != null && hunger >= hungerThreshold)
                {
                    currentState = WolfState.Mating;
                    stateTimer = 0f;
                }
                break;
                
            case WolfState.Hunting:
                // If not hungry or no prey, go back to wandering
                if (targetPrey == null || hunger >= hungerThreshold * 1.2f) // Give some buffer before leaving hunting
                {
                    currentState = WolfState.Wandering;
                    stateTimer = 0f;
                }
                // Only consider mating if we're not hungry
                else if (loveLevel >= maxLoveLevel && currentMate != null && hunger >= hungerThreshold)
                {
                    currentState = WolfState.Mating;
                    stateTimer = 0f;
                }
                break;
                
            case WolfState.Mating:
                // If mate is lost, go back to wandering
                if (currentMate == null)
                {
                    currentState = WolfState.Wandering;
                    stateTimer = 0f;
                }
                // If we get hungry during mating, prioritize hunting
                else if (hunger < hungerThreshold && targetPrey != null)
                {
                    currentState = WolfState.Hunting;
                    stateTimer = 0f;
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
        if (hunger < hungerThreshold * 0.6f && targetPrey != null && currentState != WolfState.Hunting)
        {
            currentState = WolfState.Hunting;
            stateTimer = 0f;
        }
    }
    
    // Add tag check helper method at the top of the class
    private bool IsPrey(GameObject obj)
    {
        if (obj == null) 
        {
            return false;
        }
        
        // Check for the Food tag
        try {
            bool isFood = obj.CompareTag("Food");
            
            // Log food detection more frequently when hungry
            if (isFood && hunger < maxHunger * 0.5f && Random.value < 0.1f && showDebugLogs)
            {
                // Debug.Log($"FOOD DETECTED: {obj.name} is food! Current hunger: {hunger:F1}");
            }
            
            return isFood;
        }
        catch (System.Exception e) {
            // In case of exceptions, handle gracefully
            if (showDebugLogs) {
                // Debug.LogError("Error checking prey tag on " + obj.name + ": " + e.Message);
            }
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
                // Check if other creature has full love level
                if (otherCreature != null && !otherCreature.isDead && otherCreature.loveLevel >= otherCreature.maxLoveLevel)
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
    
    void UpdateSensorInputs()
    {
        // Always create a fixed-size array matching TOTAL_INPUTS (9)
        sensorInputs = new float[TOTAL_INPUTS];
        
        // Initialize all inputs to default values (no objects detected)
        for (int i = 0; i < TOTAL_INPUTS; i++)
        {
            sensorInputs[i] = 1.0f;  // Default to max distance/no detection
        }
        
        // Initialize sensor distances to max view distance
        for (int i = 0; i < numSensors; i++)
        {
            sensorDistances[i] = viewDistance;
        }

        // Reset prey detection inputs
        preyDirectionInput = 0f;
        preyDistanceInput = 1f;

        // Detect objects in all directions using OverlapSphere
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, viewDistance);
        
        foreach (var hitCollider in hitColliders)
        {
            // Skip self
            if (hitCollider.gameObject == gameObject) continue;
            
            // Get direction and distance to the detected object
            Vector3 directionToObject = hitCollider.transform.position - transform.position;
            float distanceToObject = directionToObject.magnitude;
            
            // Check if this is prey
            bool isPrey = IsPrey(hitCollider.gameObject);
            
            // If it's prey, update prey direction input for neural network
            if (isPrey && (targetPrey == hitCollider.gameObject || targetPrey == null))
            {
                // Calculate angle for prey direction
                float preyAngleFromForward = Vector3.SignedAngle(transform.forward, directionToObject, Vector3.up);
                
                // Normalize to -1 to 1 range for neural network
                preyDirectionInput = Mathf.Clamp(preyAngleFromForward / 90f, -1f, 1f);
                
                // Normalize distance to 0-1 range (0 = far, 1 = close)
                preyDistanceInput = 1f - Mathf.Clamp01(distanceToObject / viewDistance);
                
                // Visualize prey detection
                if (showAOESensors)
                {
                    Debug.DrawLine(
                        transform.position + Vector3.up * 0.1f, 
                        hitCollider.transform.position, 
                        Color.red, 
                        0.1f
                    );
                    
                    // Debug log prey detection - only if debug logging enabled
                    if (Time.frameCount % 60 == 0 && showDebugLogs)
                    {
                        // Debug.Log($"Prey detected: Angle={preyAngleFromForward:F1}, Direction={preyDirectionInput:F2}, Distance={preyDistanceInput:F2}");
                    }
                }
            }
            
            // Determine which sensor this object belongs to based on angle
            float sensorAngleFromForward = Vector3.SignedAngle(transform.forward, directionToObject, Vector3.up);
            // Convert to 0-360 range
            if (sensorAngleFromForward < 0) sensorAngleFromForward += 360f;
            
            // Calculate which sensor this belongs to
            int sensorIndex = Mathf.FloorToInt(sensorAngleFromForward / (360f / numSensors));
            sensorIndex = Mathf.Clamp(sensorIndex, 0, numSensors - 1);
            
            // Update sensor distance if this object is closer than previously detected
            if (distanceToObject < sensorDistances[sensorIndex])
            {
                sensorDistances[sensorIndex] = distanceToObject;
                
                // Draw debug visualization if enabled
                if (showAOESensors)
                {
                    Debug.DrawLine(
                        transform.position + Vector3.up * 0.1f, 
                        hitCollider.transform.position, 
                        GetSensorColor(sensorIndex), 
                        0.1f
                    );
                }
            }
        }
        
        // Convert sensor distances to neural network inputs (normalized 0-1)
        // Only copy as many sensors as we have, and only up to 6 (leaving room for hunger and prey data)
        int sensorsToCopy = Mathf.Min(numSensors, 6);
        for (int i = 0; i < sensorsToCopy; i++)
        {
            // Normalize distance (0 = object is touching, 1 = no object detected within range)
            sensorInputs[i] = sensorDistances[i] / viewDistance;
        }
        
        // Add hunger level as input at index 6
        sensorInputs[6] = 1.0f - (hunger / maxHunger);
        
        // Add prey direction and distance as additional inputs at indices 7 and 8
        sensorInputs[7] = preyDirectionInput; // Direction to turn to reach prey (-1 to 1)
        sensorInputs[8] = preyDistanceInput;  // How close prey is (0 to 1)
        
        // Draw the sensor ranges if visualization is enabled
        if (showAOESensors)
        {
            DrawAOESensors();
        }
    }
    
    // Visualize the AOE sensors
    private void DrawAOESensors()
    {
        float angleStep = 360f / numSensors;
        
        for (int i = 0; i < numSensors; i++)
        {
            float angle = i * angleStep;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 direction = rotation * Vector3.forward;
            
            // Draw a ray for each sensor direction
            Debug.DrawRay(
                transform.position + Vector3.up * 0.1f, 
                direction * sensorDistances[i], 
                GetSensorColor(i), 
                0.1f
            );
        }
    }
    
    // Get a unique color for each sensor direction
    private Color GetSensorColor(int sensorIndex)
    {
        switch (sensorIndex % 6)
        {
            case 0: return Color.red;
            case 1: return Color.green;
            case 2: return Color.blue;
            case 3: return Color.yellow;
            case 4: return Color.cyan;
            case 5: return Color.magenta;
            default: return Color.white;
        }
    }

    void ApplyHuntingBehavior()
    {
        if (targetPrey != null)
        {
            // Calculate direction to prey
            Vector3 directionToPrey = targetPrey.transform.position - transform.position;
            float distanceToPrey = directionToPrey.magnitude;
            float angleToTarget = Vector3.SignedAngle(transform.forward, directionToPrey, Vector3.up);
            
            // When very close to prey, boost forward movement to increase collision chance
            if (distanceToPrey < 3.0f)
            {
                // Override neural network briefly to ensure collision
                FB = Mathf.Max(FB, 0.9f);
                
                // Ensure accurate turning toward prey when close
                LR = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);
                
                if (showDebugLogs) {
                    // Debug.Log($"CLOSE TO PREY: Boosting speed to ensure collision! Distance={distanceToPrey:F2}");
                }
            }
            
            // Debug hunting information - only if debug logging enabled
            if (Time.frameCount % 60 == 0 && showDebugLogs)
            {
                // Debug.Log($"Hunting: Distance={distanceToPrey:F2}, Angle={angleToTarget:F1}, FB={FB:F2}, LR={LR:F2}");
            }
            
            // Visualize hunting path
            if (showAOESensors)
            {
                // Show line to target
                Debug.DrawLine(transform.position, targetPrey.transform.position, Color.red, 0.1f);
                
                // Show predicted movement direction
                Debug.DrawRay(transform.position, -transform.forward * 3f, Color.blue, 0.1f);
            }
        }
    }
    
    void ApplyMatingBehavior()
    {
        if (currentMate != null)
        {
            // Calculate direction to mate
            Vector3 directionToMate = currentMate.transform.position - transform.position;
            float angleToMate = Vector3.SignedAngle(transform.forward, directionToMate, Vector3.up);
            float distanceToMate = directionToMate.magnitude;
            
            // Use proper turning to face mate
            LR = Mathf.Clamp(angleToMate / 45f, -1.0f, 1.0f);
            
            // Move forward at moderate speed when not close enough
            if (distanceToMate > matingDistance + 0.5f)
            {
                FB = 0.7f;
            }
            else if (distanceToMate <= matingDistance)
            {
                // When close enough, slow down to stay in position
                FB = 0.1f;
                // Fine-tune turning to face each other
                LR = Mathf.Clamp(angleToMate / 30f, -0.5f, 0.5f);
                
                // Increment mating timer when close
                matingTimer += Time.deltaTime;
                
                // Check if mating is complete
                if (matingTimer >= matingTime)
                {
                    // Attempt to reproduce with the mate
                    AttemptReproduction(currentMate);
                    
                    // Reset mating timer
                    matingTimer = 0f;
                    currentMate = null;
                }
            }
        }
    }
    
    void ApplyWanderingBehavior()
    {
        // FORCE EXTREME TURNING - 15% chance every frame to make a significant turn
        if (Random.value < 0.15f)
        {
            // Force a very strong turn in a random direction
            LR = Random.value < 0.5f ? -1.0f : 1.0f;  // Full left or right
            
            // Log forced turns
            // Debug.Log($"Wolf FORCED TURN: LR = {LR}");
        }
        
        // Always ensure forward movement
        FB = Mathf.Max(FB, 0.5f);
    }

    // Called when the wolf collides with prey - improve collision detection
    void OnTriggerEnter(Collider col)
    {
        if (showDebugLogs) {
            // Debug.Log($"COLLISION: Wolf {gameObject.name} collided with: {col.gameObject.name}");
        }
        
        // Use the helper method
        if (IsPrey(col.gameObject))
        {
            if (showDebugLogs) {
                // Debug.Log($"EATING: Wolf detected prey: {col.gameObject.name}");
            }
            
            // Try to get the Prey component
            Prey prey = col.gameObject.GetComponent<Prey>();
            if (prey != null)
            {
                if (showDebugLogs) {
                    // Debug.Log($"EATING: Found Prey component on {col.gameObject.name}");
                }
                // Let the prey handle being eaten
                prey.GetEaten(this);
            }
            else
            {
                if (showDebugLogs) {
                    // Debug.Log($"EATING: No Prey component found on {col.gameObject.name}, using fallback eating behavior");
                }
                // Fallback for prey without the Prey component
                // Eat the prey - INCREASES hunger now
                hunger += hungerGained;
                hunger = Mathf.Min(hunger, maxHunger);
                
                // Gain reproduction hunger
                reproductionHunger -= reproductionHungerGained;
                
                // Remove the eaten prey
                Destroy(col.gameObject);
            }
            
            // Clear target
            if (targetPrey == col.gameObject)
            {
                targetPrey = null;
            }
        }
    }

    // Add OnTriggerStay to handle continuous collision
    void OnTriggerStay(Collider col)
    {
        // If we're very hungry, check for food even in continuous contact
        if (hunger < maxHunger * 0.3f && IsPrey(col.gameObject))
        {
            if (showDebugLogs) {
                // Debug.Log($"CONTINUOUS EATING: Wolf {gameObject.name} eating {col.gameObject.name} during continuous contact");
            }
            
            // Try to get the Prey component
            Prey prey = col.gameObject.GetComponent<Prey>();
            if (prey != null)
            {
                prey.GetEaten(this);
            }
            else
            {
                // Fallback eating behavior
                hunger += hungerGained * 0.5f * Time.deltaTime; // Slower eating in continuous contact
                hunger = Mathf.Min(hunger, maxHunger);
                
                // Only destroy if we've eaten enough
                if (Random.value < 0.01f)
                {
                    Destroy(col.gameObject);
                    if (showDebugLogs) {
                        // Debug.Log("FOOD CONSUMED in continuous contact");
                    }
                }
            }
        }
    }

    public void ManageEnergy()
    {
        // Only manage hunger if not dead
        if (isDead) return;

        // Decrease hunger over time - MUST use Time.deltaTime to be frame-rate independent
        hunger -= hungerDecreaseRate * Time.deltaTime;
        
        // Debug log to verify hunger is decreasing - only if debug logging enabled
        if (hunger % 10 < 0.1f && showDebugLogs)  // Log approximately every 10 units
        {
            // Debug.Log(gameObject.name + " - Hunger: " + hunger);
        }
        
        // Clamp hunger between 0 and maxHunger
        hunger = Mathf.Clamp(hunger, 0f, maxHunger);
        
        // Die if hunger reaches zero
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
            
            if (showDebugLogs) {
                // Debug.Log(gameObject.name + " has died from hunger!");
            }
            
            // Destroy the GameObject after a delay
            StartCoroutine(DestroyAfterDelay(1.4f));
        }
        
        // Update reproduction hunger
        reproductionHunger += reproductionHungerGained * Time.deltaTime;
        reproductionHunger = Mathf.Clamp(reproductionHunger, 0f, maxHunger);
    }

    // New coroutine to destroy the GameObject after a delay
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    private void MutateCreature()
    {
        nn.MutateNetwork(0.8f, 0.2f);
    }

    // New method for reproduction that requires two wolves
    private void AttemptReproduction(GameObject mate)
    {
        // Check if both wolves have maximum love
        Creature mateCreature = mate.GetComponent<Creature>();
        if (mateCreature == null) return;
        
        // Only check if both have full love (100)
        if (loveLevel >= maxLoveLevel && mateCreature.loveLevel >= maxLoveLevel)
        {
            // Check if we have a prefab
            if (agentPrefab == null)
            {
                // Debug.LogError("Agent prefab is missing! Cannot reproduce.");
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
                
                // Debug.Log("Wolf reproduction successful! New wolf spawned.");
                
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
            
            // Reset love level for both parents
            loveLevel = 0;
            mateCreature.loveLevel = 0;
            
            // Slightly reduce hunger from both parents due to reproduction effort
            hunger = Mathf.Max(hunger - 10f, 0);
            mateCreature.hunger = Mathf.Max(mateCreature.hunger - 10f, 0);
            
            // Update state for both parents
            currentState = WolfState.Resting;
            mateCreature.currentState = WolfState.Resting;
        }
    }

    void InitializeNeuralNetwork()
    {
        // Create neural network with 6 inputs (5 distance sensors + hunger level)
        // and 2 outputs (forward/backward and left/right)
        nn = new NN();
        nn.networkShape = new int[] {6, 32, 2};  // 6 inputs instead of 5
        nn.Awake();  // Initialize the network
        
        // Get the weights from the neural network
        float[] weights = nn.weights;
        
        // Modify weights to bias towards hunting when hunger is high
        // The hunger input is the last input (index 5)
        // We'll make it strongly influence the forward/backward output (index 0)
        // when hunger is high
        
        // Bias the forward/backward output to move forward when hungry
        // This is done by making the weight from hunger to forward/backward positive
        int hungerToForwardWeightIndex = 5 * 32; // 5 is hunger input index, 32 is number of neurons in first layer
        weights[hungerToForwardWeightIndex] = 2.0f; // Strong positive weight
        
        // Set the modified weights back to the neural network
        nn.weights = weights;
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
        
        // Detect prey in AOE
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, viewDistance);
        foreach (var hitCollider in hitColliders)
        {
            // Use helper method to check if it's prey
            if (IsPrey(hitCollider.gameObject))
            {
                // Check line of sight
                Vector3 directionToPrey = hitCollider.transform.position - transform.position;
                
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up * 0.1f, directionToPrey.normalized, out hit, viewDistance))
                {
                    if (IsPrey(hit.collider.gameObject))
                    {
                        // Valid prey in sight
                        detectedPreyList.Add(hitCollider.gameObject);
                        
                        if (showAOESensors)
                        {
                            // Visualize detected prey
                            Debug.DrawLine(transform.position + Vector3.up * 0.1f, hitCollider.transform.position, Color.red, 0.1f);
                        }
                    }
                }
            }
        }
        
        // Update target prey if we have valid detections
        if (detectedPreyList.Count > 0)
        {
            // Find closest prey
            GameObject closestPrey = null;
            float closestDistance = float.MaxValue;
            
            foreach (GameObject prey in detectedPreyList)
            {
                float distance = Vector3.Distance(transform.position, prey.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPrey = prey;
                }
            }
            
            if (closestPrey != null)
            {
                targetPrey = closestPrey;
                preyTrackedTimer = preyMemoryDuration;
                
                if (Time.frameCount % 60 == 0)
                {
                    // Debug.Log($"Tracking prey: {targetPrey.name} at distance {closestDistance:F2}");
                }
            }
        }
    }
    
    void DetectMates()
    {
        // Only look for mates if ready
        if (loveLevel < maxLoveLevel || isDead)
        {
            currentMate = null;
            return;
        }
        
        // Clear previous mate detections
        detectedMatesList.Clear();
        
        // Log that we're looking for mates
        if (Time.frameCount % 60 == 0)
        {
            // Debug.Log($"Wolf is looking for mates. Love: {loveLevel:F1}/{maxLoveLevel:F1}");
        }
        
        // Detect potential mates in AOE
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, viewDistance);
        foreach (var hitCollider in hitColliders)
        {
            if (IsPotentialMate(hitCollider.gameObject))
            {
                // Check line of sight
                Vector3 directionToMate = hitCollider.transform.position - transform.position;
                
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up * 0.1f, directionToMate.normalized, out hit, viewDistance))
                {
                    if (hit.collider.gameObject == hitCollider.gameObject)
                    {
                        // Valid mate in sight
                        detectedMatesList.Add(hitCollider.gameObject);
                        
                        if (showAOESensors)
                        {
                            // Visualize detected mate
                            Debug.DrawLine(transform.position + Vector3.up * 0.1f, hitCollider.transform.position, Color.green, 0.1f);
                        }
                        
                        // Debug.Log($"Found potential mate: {hitCollider.gameObject.name}");
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
            // Debug.Log($"Selected mate: {currentMate.name} at distance {closestDistance:F2}");
        }
    }
}

