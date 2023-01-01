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

        int lastPlayerInput = 0;

        private Vector3 lookAheadOffset;
        public Vector2 lookAheadDistance;
        public float lookAheadSmoothTime = 0.2f; // Time to reach look ahead target

        // Reference variables for use with Unity's SmoothDamp function
        private Vector3 velocity;
        private Vector3 lookAheadVelocity;

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

            // Calculate target position
            Vector3 target = playerTransform.position + offset + lookAheadOffset;
            target.z = -10;

            // Move towards target position
            transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
        }
    }
}
