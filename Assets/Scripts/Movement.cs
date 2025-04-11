using UnityEngine;

public class Movement : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private float gravityValue = -9.81f;
    public float speed = 5.0f;
    public float rotateSpeed = 300.0f;  // Extremely high rotation speed
    public float FB = 0;
    public float LR = 0;
    
    // Debug variables
    public bool showDebug = true;

    private Creature creature;
    private Transform wolfTransform;
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    void Awake()
    {
        creature = GetComponent<Creature>();
        wolfTransform = transform;
        lastPosition = wolfTransform.position;
        lastRotation = wolfTransform.rotation;
        
        // Get or add CharacterController
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 2.0f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0, 1.0f, 0);
        }
    }

    public void Move(float targetFB, float targetLR)
    {
        if (controller == null || creature.isDead) return;
        
        // Debug input values
        if (showDebug && Time.frameCount % 60 == 0)
        {
            // Debug.Log($"Move inputs: FB={targetFB}, LR={targetLR}");
        }
        
        // ROTATION - Apply rotation with balanced left/right turning
        if (Mathf.Abs(targetLR) > 0.05f)  // Lower threshold to catch small turning values
        {
            // Scale up the rotation amount significantly
            float rotationAmount = targetLR * rotateSpeed;
            
            // Direct rotation using Quaternion
            wolfTransform.Rotate(0, rotationAmount * Time.deltaTime, 0, Space.World);
            
            // Log significant rotation
            if (showDebug && Mathf.Abs(rotationAmount) > 50f)
            {
                Debug.Log($"Wolf ROTATING by {rotationAmount} degrees/sec, direction={targetLR}");
            }
        }

        // MOVEMENT - Apply forward movement in the direction the wolf is facing
        if (targetFB > 0.1f)  // Only move if FB is significant
        {
            // Calculate movement direction (use forward direction directly)
            Vector3 moveDirection = wolfTransform.forward;
            
            // Apply movement using the controller
            controller.SimpleMove(moveDirection * speed * targetFB);
            
            // Visualize movement with debug rays
            if (showDebug && Time.frameCount % 30 == 0)
            {
                Debug.DrawRay(wolfTransform.position, moveDirection * 3, Color.green, 0.5f);
            }
        }

        // Handle gravity
        if (!controller.isGrounded)
        {
            playerVelocity.y += gravityValue * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);
        }
        else
        {
            playerVelocity.y = 0;
        }
        
        // Check if movement occurred
        if (Time.frameCount % 120 == 0)
        {
            Vector3 positionDelta = wolfTransform.position - lastPosition;
            Quaternion rotationDelta = Quaternion.Inverse(lastRotation) * wolfTransform.rotation;
            float moveDist = positionDelta.magnitude;
            float rotateAngle = Quaternion.Angle(Quaternion.identity, rotationDelta);
            
            if (showDebug)
            {
                Debug.Log($"Movement: {moveDist:F2} units, Rotation: {rotateAngle:F1} degrees in 2 seconds");
            }
            
            lastPosition = wolfTransform.position;
            lastRotation = wolfTransform.rotation;
        }
    }
}
