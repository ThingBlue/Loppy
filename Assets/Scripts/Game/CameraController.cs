using Loppy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy
{
    public class CameraController : MonoBehaviour
    {
        public Transform playerTransform;
        public PlayerController playerController;

        public Vector3 offset = new(0, 2); // Camera position offset from player
        public float smoothTime = 0.2f; // Time to reach overall position target

        // Look ahead variables
        public bool xLookAheadEnabled = true;
        public bool yLookAheadEnabled = true;

        private Vector3 lookAheadOffset;
        public Vector2 lookAheadDistance;
        public float lookAheadSmoothTime = 0.2f; // Time to reach look ahead target

        // Vertical pan variables
        private Vector3 verticalPanOffset;
        public float verticalPanDistance = 4;
        public float verticalPanSmoothTime = 0.2f; // Time to reach vertical pan target

        // Reference variables for use with Unity's SmoothDamp function
        private Vector3 velocity;
        private Vector3 lookAheadVelocity;
        private Vector3 verticalPanVelocity;

        private void LateUpdate() // Late update to prevent stuttering
        {
            #region Look ahead

            if (playerController != null)
            {
                // Calculate look ahead offset
                Vector3 lookAheadPosition = new();

                // x look ahead
                lookAheadPosition.x = playerController.facingDirection * lookAheadDistance.x;
                // y look ahead, only when player is falling
                lookAheadPosition.y = !playerController.onGround && playerController.velocity.y < 0 ? -lookAheadDistance.y : 0;

                lookAheadOffset = Vector3.SmoothDamp(lookAheadOffset, lookAheadPosition, ref lookAheadVelocity, lookAheadSmoothTime);
            }

            // Disable look ahead if enabled flag is set to false
            if (!xLookAheadEnabled) lookAheadOffset.x = 0;
            if (!yLookAheadEnabled) lookAheadOffset.y = 0;

            #endregion

            #region Vertical pan

            // We want to pan the camera up or down to allow the player to see more of the level
            // Only do this if the player holds up or down while standing still
            if (playerController.onGround && !InputManager.instance.getKey("left") && !InputManager.instance.getKey("right"))
            {
                Vector3 verticalPanPosition = new();
                if (InputManager.instance.getKey("up")) verticalPanPosition.y += verticalPanDistance;
                if (InputManager.instance.getKey("down")) verticalPanPosition.y -= verticalPanDistance;
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
