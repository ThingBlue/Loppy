using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy
{
    [CreateAssetMenu]
    public class PlayerPhysicsStats : ScriptableObject
    {
        [Header("LAYERS")]
        [Tooltip("Set this to the layer your player is on")]
        public LayerMask playerLayer;

        [Tooltip("Set this to the layer climbable walls are on")]
        public LayerMask climbableLayer;

        [Header("MOVEMENT")]
        [Tooltip("The top horizontal movement speed")]
        public float maxRunSpeed = 10;

        [Tooltip("The player's capacity to gain horizontal speed")]
        public float acceleration = 100;

        [Tooltip("The pace at which the player comes to a stop")]
        public float groundDeceleration = 50;

        [Tooltip("Deceleration in air only after stopping input mid-air")]
        public float airDeceleration = 30;

        [Tooltip("A constant downward force applied while grounded. Helps on slopes"), Range(0f, -10f)]
        public float groundingForce = -3.0f;

        [Header("JUMP")]
        [Tooltip("The immediate velocity applied when jumping")]
        public float jumpStrength = 25;

        [Tooltip("The maximum vertical movement speed")]
        public float maxFallSpeed = 40;

        [Tooltip("The player's capacity to gain fall speed. a.k.a. In Air Gravity")]
        public float fallAcceleration = 100;

        [Tooltip("The gravity multiplier added when jump is released early")]
        public float jumpEndEarlyGravityModifier = 3;

        [Tooltip("The amount of time before coyote jump becomes unusable. Coyote jump allows jump to execute even after leaving a ledge")]
        public float coyoteTime = 0.1f;

        [Tooltip("The amount of time we buffer a jump. This allows jump input before actually hitting the ground")]
        public float jumpBufferTime = 0.1f;

        [Header("WALLS")]
        [Tooltip("How fast you climb walls.")]
        public float wallClimbSpeed = 5;

        [Tooltip("The player's capacity to gain wall sliding speed. 0 = stick to wall")]
        public float wallFallAcceleration = 8;

        [Tooltip("Clamps the maximum fall speed")]
        public float maxWallFallSpeed = 15;

        [Tooltip("Fast fall speed on wall")]
        public float fastWallFallSpeed = 20;

        [Tooltip("The immediate velocity horizontal velocity applied when wall jumping")]
        public Vector2 wallJumpStrength = new(10, 25);

        [Tooltip("The amount of time before full horizontal movement is returned after a wall jump")]
        public float wallJumpInputLossTime = 0.2f;

        [Tooltip("The amount of time where you can still wall jump after pressing to leave a wall")]
        public float wallJumpCoyoteTime = 0.1f;

        [Header("LEDGES")]
        [Tooltip("The rate at which we slow our velocity to grab a ledge. Too low, we slide off. Too high, we won't match our GrabPoint")]
        public float ledgeGrabDeceleration = 4f;

        [Tooltip("Relative point from the player's position where the ledge corner will be when hanging")]
        public Vector2 ledgeGrabPoint = new(0.4f, 0.6f);

        [Tooltip("Relative point from the ledge corner where the new player position will be after climbing up (Tip: have Y be slightly > 0)")]
        public Vector2 standUpOffset = new(0.2f, 0.02f);

        [Tooltip("The raycast distance for ledge detection"), Min(0.05f)]
        public float ledgeRaycastDistance = 1f;

        [Tooltip("How long movement will be locked out. Animation clip length")]
        public float ledgeClimbDuration = 0.5f;

        [Header("DASH")]
        [Tooltip("The velocity of the dash")]
        public float dashVelocity = 50;

        [Tooltip("How many seconds the dash will last")]
        public float dashTime = 0.2f;

        [Tooltip("How many seconds needs to pass between dashes")]
        public float dashCooldownTime = 0.2f;

        [Tooltip("The horizontal speed retained when dash has completed")]
        public float dashEndHorizontalMultiplier = 0.25f;

        [Tooltip("The amount of time before coyote dash becomes unusable")]
        public float dashCoyoteTime = 0.1f;

        [Tooltip("The amount of time a dash is buffered")]
        public float dashBufferTime = 0.1f;

        [Header("GLIDE")]
        [Tooltip("Maximum fall speed during glide")]
        public float glideFallSpeed = 4;

        [Tooltip("Gliding gravity")]
        public float glideFallAcceleration = 20;

        [Header("COLLISION")]
        [Tooltip("The raycast distance for collision detection"), Range(0f, 1.0f)]
        public float raycastDistance = 0.05f;

        [Tooltip("Maximum angle of walkable ground"), Range(0f, 1.0f)]
        public float maxWalkAngle = 30;

        [Tooltip("Maximum angle of climbable wall"), Range(0f, 1.0f)]
        public float maxClimbAngle = 30;

        [Header("EXTERNAL")]
        [Tooltip("The rate at which external velocity decays. Should be close to Fall Acceleration")]
        public int externalVelocityDecay = 100; // This may become deprecated in a future version

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (playerLayer.value <= 1) Debug.LogWarning("Please assign a Player Layer that matches the one given to your Player", this);
            if (climbableLayer.value <= 1) Debug.LogWarning("Please assign a Climbable Layer that matches your Climbable colliders", this);
        }
#endif
    }
}
