using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loppy
{
    [CreateAssetMenu]
    public class PlayerPhysicsData : ScriptableObject
    {
        [Header("LAYERS")]
        [Tooltip("Layer containing player")]
        public LayerMask playerLayer;

        [Tooltip("Layer containing enemies")]
        public LayerMask enemyLayer;

        [Tooltip("Layer containing terrain")]
        public LayerMask terrainLayer;

        [Header("MOVEMENT")]
        [Tooltip("Maximum horizontal velocity")]
        public float maxRunSpeed = 10;

        [Tooltip("Rate of horizontal velocity gain")]
        public float acceleration = 100;

        [Tooltip("Rate of horizontal velocity loss while on ground")]
        public float groundDeceleration = 200;

        [Tooltip("Rate of horizontal velocity loss while airborne")]
        public float airDeceleration = 100;

        [Tooltip("A constant downward velocity applied while on ground")]
        public float groundingForce = -2;

        [Header("JUMP")]
        [Tooltip("Vertical velocity applied instantly upon jumping")]
        public float jumpStrength = 18;

        [Tooltip("Maximum downwards vertical velocity")]
        public float maxFallSpeed = 40;

        [Tooltip("Rate of downwards vertical velocity gain from gravity")]
        public float fallAcceleration = 50;

        [Tooltip("Downwards velocity applied upon ending a jump early")]
        public float jumpEndEarlyGravityModifier = 5;

        [Tooltip("Amount of time that a jump is buffered")]
        public float jumpBufferTime = 0.1f;

        [Tooltip("Amount of time where jump is still usable after leaving ground")]
        public float coyoteTime = 0.1f;

        [Header("WALLS")]
        [Tooltip("Velocity of upwards movement on walls")]
        public float wallClimbSpeed = 8;

        [Tooltip("Rate of downwards velocity gain when on walls")]
        public float wallFallAcceleration = 16;

        [Tooltip("Maximum downwards velocity when on walls")]
        public float maxWallFallSpeed = 6;

        [Tooltip("Fast fall speed on walls")]
        public float fastWallFallSpeed = 12;

        [Tooltip("Velocity applied instantly when wall jumping")]
        public Vector2 wallJumpStrength = new(7, 20);

        [Tooltip("Amount of time before full horizontal movement is returned after a wall jump")]
        public float wallJumpControlLossTime = 0.15f;

        [Tooltip("Amount of time where wall jump is still usable after leaving a wall")]
        public float wallJumpCoyoteTime = 0.1f;

        [Header("LEDGES")]
        [Tooltip("Rate of velocity loss when grabbing a ledge")]
        public float ledgeGrabDeceleration = 8;

        [Tooltip("Relative point from the player's position where the ledge corner will be when hanging")]
        public Vector2 ledgeGrabPoint = new(0.4f, 1f);

        [Tooltip("Relative point from the ledge corner where the new player position will be after climbing up")]
        public Vector2 standUpOffset = new(0.4f, 0f);

        [Tooltip("Raycast distance for ledge detection"), Min(0.05f)]
        public float ledgeRaycastDistance = 2;

        [Tooltip("Ledge climb animation time")]
        public float ledgeClimbDuration = 0.2f;

        [Header("DASH")]
        [Tooltip("Horizontal velocity applied instantly upon dashing")]
        public float dashVelocity = 25;

        [Tooltip("Duration of dash")]
        public float dashTime = 0.2f;

        [Tooltip("Amount of time that must pass between dashes")]
        public float dashCooldownTime = 0.2f;

        [Tooltip("Percentage of horizontal velocity retained when dash has completed")]
        public float dashEndHorizontalMultiplier = 0.25f;

        [Tooltip("Amount of time a dash is buffered")]
        public float dashBufferTime = 0.1f;

        [Tooltip("Amount of time where dash is still usable after leaving ground or wall")]
        public float dashCoyoteTime = 0.1f;

        [Tooltip("Velocity applied instantly upon wall jumping")]
        public Vector2 dashJumpStrength = new(30, 18);

        [Tooltip("Amount of time before full horizontal movement is returned after a dash jump")]
        public float dashJumpControlLossTime = 0.3f;

        [Header("GLIDE")]
        [Tooltip("Maximum fall speed during glide")]
        public float glideFallSpeed = 4;

        [Tooltip("Rate of downwards velocity gain during glide")]
        public float glideFallAcceleration = 20;

        [Header("GRAPPLE")]
        [Tooltip("Magnitude of velocity applied when grappling")]
        public float grappleVelocity = 10;

        [Tooltip("Amount of time a grapple freeze lasts")]
        public float grappleFreezeTime = 4;

        [Tooltip("Amount of time a grapple is buffered")]
        public float grappleBufferTime = 0.1f;

        [Tooltip("Rate of velocity loss during grapple freeze")]
        public float grappleFreezeDeceleration = 100;

        [Tooltip("Factor of timescale reduction per frame")]
        public float timeScaleLerpFactor = 0.5f;

        [Tooltip("Amount of time between each time scale lerp")]
        public float timeScaleLerpTime = (1f / 60f);

        [Tooltip("Amount of time before full horizontal movement is returned after a grapple")]
        public float grappleControlLossTime = 0.2f;

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
    }
}
