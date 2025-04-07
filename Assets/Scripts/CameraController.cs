using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // The point to orbit around
    public float distance = 10.0f; // Initial distance from target
    public float minDistance = 2.0f; // Minimum zoom distance
    public float maxDistance = 50.0f; // Increased maximum zoom distance

    [Header("Orbit Settings")]
    public float xSpeed = 60.0f; // Slower horizontal rotation speed
    public float ySpeed = 60.0f; // Slower vertical rotation speed
    public float yMinLimit = -20f; // Minimum vertical angle
    public float yMaxLimit = 80f; // Maximum vertical angle

    private float x = 0.0f; // Current x rotation
    private float y = 0.0f; // Current y rotation

    void Start()
    {
        // Initialize rotation angles
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        // If no target is set, try to find the ground
        if (target == null)
        {
            GameObject ground = GameObject.FindGameObjectWithTag("Ground");
            if (ground != null)
            {
                target = ground.transform;
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Get mouse input for rotation
        if (Input.GetMouseButton(0)) // Changed to left mouse button
        {
            x += Input.GetAxis("Mouse X") * xSpeed * distance * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

            // Clamp vertical rotation
            y = ClampAngle(y, yMinLimit, yMaxLimit);
        }

        // Get scroll wheel input for zooming
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance = Mathf.Clamp(distance - scroll * 5, minDistance, maxDistance);

        // Calculate rotation
        Quaternion rotation = Quaternion.Euler(y, x, 0);

        // Calculate position
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
        Vector3 position = rotation * negDistance + target.position;

        // Apply rotation and position
        transform.rotation = rotation;
        transform.position = position;
    }

    // Helper function to clamp angles
    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
} 