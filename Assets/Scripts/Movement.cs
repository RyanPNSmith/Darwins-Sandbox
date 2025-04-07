using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
//using NN;
//using System;
// using MathNet.Numerics;
// using MathNet.Numerics.LinearAlgebra;

public class Movement : MonoBehaviour
{
    public CharacterController controller;
    private bool hasController = false;
    private Vector3 playerVelocity;
    private float gravityValue = -9.81f;
    public float speed = 10.0F;
    public float rotateSpeed = 10.0F;
    public float FB = 0;
    public float LR = 0;

    private ObjectTracker objectTracker;
    private Creature creature;

    void Awake()
    {
        objectTracker = FindObjectOfType<ObjectTracker>();
        creature = GetComponent<Creature>();
        
        // Try to get the CharacterController component
        controller = GetComponent<CharacterController>();
        
        // Check if we found it, if not, add one
        if (controller == null)
        {
            Debug.LogWarning("No CharacterController found on " + gameObject.name + ", adding one automatically.");
            controller = gameObject.AddComponent<CharacterController>();
            // Set some reasonable default values
            controller.height = 2.0f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0, 1.0f, 0);
        }
    }

    public void Move(float FB, float LR)
    {
        // Ensure we have a controller before trying to use it
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
            if (controller == null)
            {
                Debug.LogError("No CharacterController available on " + gameObject.name);
                return;
            }
        }
        
        //clamp the values of LR and FB
        LR = Mathf.Clamp(LR, -1, 1);
        FB = Mathf.Clamp(FB, 0.3f, 1); // Changed from 0 to 0.3f as minimum to ensure movement

        //move the agent
        if (!creature.isDead)
        {
            // Rotate around y - axis
            transform.Rotate(0, LR * rotateSpeed, 0);

            // Move forward / backward
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            controller.SimpleMove(forward * speed * FB);
        }

        //Checks to see if the agent is grounded, if it is, don't apply gravity
        if (controller.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }
        else
        {
            // Gravity
            playerVelocity.y += gravityValue * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);
        }
    }
}
