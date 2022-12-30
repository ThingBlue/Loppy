using Loppy;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy
{
    public class CameraController : MonoBehaviour
    {
        public Transform playerTransform;
        public Vector3 offset = new(0, 2); // Camera position offset from player

        public float smoothTime = 0.2f; // Time to reach overall position target

        // Look ahead variables
        public bool lookAheadEnabled = false;

        private Vector3 lookAheadOffset;
        public float lookAheadDistance = 4f;
        public float lookAheadSmoothTime = 0.2f; // Time to reach look ahead target

        // Reference variables for use with Unity's SmoothDamp function
        private Vector3 velocity;
        private Vector3 lookAheadVelocity;

        private void LateUpdate() // Late update to prevent stuttering
        {
            #region Look ahead

            // Calculate look ahead offset
            Vector2 input = Vector2.zero;
            if (InputManager.instance.getKey("left")) { input.x -= 1; }
            if (InputManager.instance.getKey("right")) { input.x += 1; }
            if (InputManager.instance.getKey("up")) { input.y += 1; }
            if (InputManager.instance.getKey("down")) { input.y -= 1; }

            /*
            // Only look up and down if no horizontal input
            if (input.x != 0) input.y = 0;
            input.x = 0;
            */

            // Smoothly move lookAheadOffset towards lookAheadPos in time of lookAheadSmoothTime
            Vector3 lookAheadPos = input * lookAheadDistance;
            lookAheadOffset = Vector3.SmoothDamp(lookAheadOffset, lookAheadPos, ref lookAheadVelocity, lookAheadSmoothTime);

            if (lookAheadEnabled == false) lookAheadOffset = Vector3.zero;

            #endregion

            // Calculate target position
            Vector3 target = playerTransform.position + offset + lookAheadOffset;
            target.z = -10;

            // Move towards target position
            transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
        }
    }
}
