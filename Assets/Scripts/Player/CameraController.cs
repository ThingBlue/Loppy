using Loppy.GameCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy.Player
{
    public class CameraController : MonoBehaviour
    {
        #region Variables

        [Header("CAMERA FOLLOW")]
        public Transform playerTransform;
        public PlayerController playerController;
        public Vector3 offset = new(0, 2); // Camera position offset from player
        public float smoothTime = 0.2f; // Time to reach overall position target

        [Header("LOOK AHEAD")]
        public bool xLookAheadEnabled = true;
        public bool yLookAheadEnabled = true;
        public Vector2 lookAheadDistance;
        public float lookAheadSmoothTime = 0.2f; // Time to reach look ahead target
        private Vector3 lookAheadOffset;

        [Header("VERTICAL PAN")]
        public float verticalPanDistance = 8;
        public float verticalPanSmoothTime = 0.2f; // Time to reach vertical pan target
        public float verticalPanKeyHoldTime = 0.5f; // Time that up or down key must be held to initiate vertical pan
        private Vector3 verticalPanOffset;
        private int currentVerticalPanDirection = 0;
        private float verticalPanKeyTimer = 0;

        // Reference variables for use with Unity's SmoothDamp function
        private Vector3 velocity;
        private Vector3 lookAheadVelocity;
        private Vector3 verticalPanVelocity;

        #endregion

        private void FixedUpdate()
        {
            // Increment vertical pan key timer
            verticalPanKeyTimer += Time.fixedDeltaTime;

            // Check for conditions to reset vertical pan key timer
            // Check if player is not idle
            if (!playerController.onGround || InputManager.instance.getKey("left") || InputManager.instance.getKey("right"))
            {
                verticalPanKeyTimer = 0;
                currentVerticalPanDirection = 0;
            }
            // Check if no key held
            if (!InputManager.instance.getKey("up") && !InputManager.instance.getKey("down"))
            {
                verticalPanKeyTimer = 0;
                currentVerticalPanDirection = 0;
            }
            // Check for up key down
            if (InputManager.instance.getKey("up") && currentVerticalPanDirection != 1)
            {
                verticalPanKeyTimer = 0;
                currentVerticalPanDirection = 1;
            }
            // Check if down key down
            if (InputManager.instance.getKey("down") && currentVerticalPanDirection != -1)
            {
                verticalPanKeyTimer = 0;
                currentVerticalPanDirection = -1;
            }
        }

        private void LateUpdate() // Late update to prevent stuttering
        {

            #region Look ahead

            if (playerController != null)
            {
                // Calculate look ahead offset
                Vector3 lookAheadPosition = new();

                // x look ahead
                lookAheadPosition.x = playerController.faceDirection * lookAheadDistance.x;
                // y look ahead, only when player is falling
                lookAheadPosition.y = !playerController.onGround && playerController.velocity.y < 0 ? -lookAheadDistance.y : 0;

                lookAheadOffset = Vector3.SmoothDamp(lookAheadOffset, lookAheadPosition, ref lookAheadVelocity, lookAheadSmoothTime);
            }

            // Disable look ahead if enabled flag is set to false
            if (!xLookAheadEnabled) lookAheadOffset.x = 0;
            if (!yLookAheadEnabled) lookAheadOffset.y = 0;

            #endregion

            #region Vertical pan

            // Pan the camera up or down to allow the player to see more of the level
            if (verticalPanKeyTimer > verticalPanKeyHoldTime)
            {
                Vector3 verticalPanPosition = new();
                verticalPanPosition.y = currentVerticalPanDirection * verticalPanDistance;
                verticalPanOffset = Vector3.SmoothDamp(verticalPanOffset, verticalPanPosition, ref verticalPanVelocity, verticalPanSmoothTime);
            }
            // Smoothly reset verticalPanOffset
            else
            {
                verticalPanOffset = Vector3.SmoothDamp(verticalPanOffset, Vector3.zero, ref verticalPanVelocity, verticalPanSmoothTime);
            }

            #endregion

            // Calculate target position
            Vector3 target = playerTransform.position + offset + lookAheadOffset + verticalPanOffset;
            target.z = -10;

            // Move towards target position
            transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
        }
    }
}
